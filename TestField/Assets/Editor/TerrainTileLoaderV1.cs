// Terrain Tile Loading Tool
// Ian Lowery, Luke Natili, 8/14/2026

using UnityEngine;
using UnityEditor;
using System.IO;

public class TerrainTileLoader : EditorWindow  //This lets the tool draw a new window in the unity editor
{
    private string folderPath = "Assets/";          
    private string fileNamePattern = "tile_{x}_{y}.raw";    // {x} and {y} get replaced with grid indices
    private int gridWidth = 1;              // number of tiles along X (columns)
    private int gridHeight = 1;             // number of tiles along Z (rows)
    private float tileWorldSize = 1000;     // 1km per tile, in meters
    private float terrainMaxHeight = 100;   // global max height (meters) all tiles were normalized against this same value
    private float terrainYOffset = 0f;      // shared Y position for every tile
    private int heightmapResolution = 33;   // must be 2^n + 1 (e.g. 33, 65, 129, 257 513, 1025, 2049, 4097)
    private bool sixteenBitBigEndian = false;   // Unity raw export is little-endian by default on most platforms

    // If your tile_{x}_{y} filenames use image-style row ordering (y=0 = top/north row, y increases downward),
    // enable this so tile placement and neighbor stitching are flipped to match Unity's bottom-left world origin.
    private bool yIndexIsTopLeftOrigin = true;

    // Identifies which grid this load belongs to, used to name the root object and its Generated subfolder,
    // so loading multiple separate grids (e.g. grid 1, grid 2...) doesn't overwrite or mix up assets.
    private int gridNumber = 1;


    [MenuItem("Tools/Terrain/Load Heightmap Tiles")]    //Registers the script in the toolbar
    public static void ShowWindow()
    {
        GetWindow<TerrainTileLoader>("Terrain Tile Loader");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Heightmap Grid Settings", EditorStyles.boldLabel);
        folderPath = EditorGUILayout.TextField("Folder (project-relative)", folderPath);
        fileNamePattern = EditorGUILayout.TextField("Filename Pattern", fileNamePattern);
        gridWidth = EditorGUILayout.IntField("Grid Width (tiles, X)", gridWidth);
        gridHeight = EditorGUILayout.IntField("Grid Height (tiles, Z)", gridHeight);
        tileWorldSize = EditorGUILayout.FloatField("Tile Size (meters)", tileWorldSize);
        terrainMaxHeight = EditorGUILayout.FloatField("Max Terrain Height", terrainMaxHeight);
        terrainYOffset = EditorGUILayout.FloatField("Terrain Y Offset (shared)", terrainYOffset);
        heightmapResolution = EditorGUILayout.IntField("Heightmap Resolution", heightmapResolution);
        sixteenBitBigEndian = EditorGUILayout.Toggle("Big-Endian Raw", sixteenBitBigEndian);
        yIndexIsTopLeftOrigin = EditorGUILayout.Toggle("Filenames Use Top-Left Origin (y=0 = north)", yIndexIsTopLeftOrigin);

        EditorGUILayout.Space();
        gridNumber = EditorGUILayout.IntField("Terrain Grid Number", gridNumber);

        EditorGUILayout.Space();
        if (GUILayout.Button("Load All Tiles Into Scene"))
        {
            LoadAllTiles();
        }
    }

    void LoadAllTiles()
    {
        Terrain[,] terrains = new Terrain[gridWidth, gridHeight];
        GameObject root = new GameObject($"Terrain_Grid_{gridNumber}");
        string generatedFolder = $"Terrain/Heightmaps/Generated/Grid_{gridNumber}";

        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                string fileName = fileNamePattern.Replace("{x}", x.ToString()).Replace("{y}", y.ToString());
                string fullPath = Path.Combine(Application.dataPath, "..", folderPath, fileName);

                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"Missing heightmap tile: {fullPath}");
                    continue;  
                }

                float[,] heights = ReadRawHeightmap(fullPath, heightmapResolution, sixteenBitBigEndian);
                if (heights == null) continue;

                // WHERE THE MAGIC HAPPENS

                TerrainData terrainData = new TerrainData();    //Creates unity terrain object for the tile
                terrainData.heightmapResolution = heightmapResolution;
                terrainData.size = new Vector3(tileWorldSize, terrainMaxHeight, tileWorldSize);
                terrainData.SetHeights(0, 0, heights);      //Write heightmap data to unity terrain tile

                // Save the TerrainData asset into a subfolder specific to this grid number
                string assetPath = $"Assets/{generatedFolder}/TerrainData_{x}_{y}.asset";
                Directory.CreateDirectory(Path.Combine(Application.dataPath, generatedFolder));
                AssetDatabase.CreateAsset(terrainData, assetPath);

                GameObject terrainGO = Terrain.CreateTerrainGameObject(terrainData);
                terrainGO.name = $"Terrain_{x}_{y}";
                terrainGO.transform.parent = root.transform;

                // If filenames follow image convention (y=0 at top/north), flip so y=0 lands at the
                // north edge of the world grid and the highest y lands at z=0 (Unity's bottom-left origin).
                int zRow = yIndexIsTopLeftOrigin ? (gridHeight - 1 - y) : y;
                terrainGO.transform.position = new Vector3(x * tileWorldSize, terrainYOffset, zRow * tileWorldSize);

                terrains[x, y] = terrainGO.GetComponent<Terrain>();
            }
        }

        StitchNeighbors(terrains);

        foreach (var t in terrains)
        {
            if (t == null) continue;
            t.terrainData.SyncHeightmap();
            t.Flush();        
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Terrain grid {gridNumber} loaded and stitched successfully ({gridWidth}x{gridHeight} tiles, saved to Assets/{generatedFolder}).");
    }

    float[,] ReadRawHeightmap(string path, int resolution, bool bigEndian)
    {
        byte[] bytes = File.ReadAllBytes(path);
        long expected = (long)resolution * resolution * 2; 

        if (bytes.Length != expected)
        {
            Debug.LogError($"{Path.GetFileName(path)}: unexpected file size. " +
                    $"Got {bytes.Length} bytes, expected {expected} for a {resolution}x{resolution} 16-bit raw. " +
                    "Check heightmapResolution or the file itself.");
            return null;
        }

        float[,] heights = new float[resolution, resolution];
        int idx = 0;

        for (int row = 0; row < resolution; row++)
        {
            for (int col = 0; col < resolution; col++)
            {
                ushort value = bigEndian
                    ? (ushort)((bytes[idx] << 8) | bytes[idx + 1])
                    : (ushort)((bytes[idx + 1] << 8) | bytes[idx]);

                idx += 2;
                heights[row, col] = value / 65535f; // normalize to 0-1
            }
        }

        return heights;
    }


    // Wires up SetNeighbors so tile edges blend seamlessly
    void StitchNeighbors(Terrain[,] terrains)
    {
        int width = terrains.GetLength(0);
        int height = terrains.GetLength(1);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Terrain current = terrains[x, y];
                if (current == null) continue;

                Terrain left  = (x > 0)         ? terrains[x - 1, y] : null;
                Terrain right = (x < width - 1) ? terrains[x + 1, y] : null;

                // With a top-left filename origin, decreasing y moves north (+Z) and increasing y moves south (-Z) —
                // the opposite of a bottom-left origin, so which neighbor is "top" (north) vs "bottom" (south) flips.
                Terrain north, south;
                if (yIndexIsTopLeftOrigin)
                {
                    north = (y > 0)          ? terrains[x, y - 1] : null;
                    south = (y < height - 1) ? terrains[x, y + 1] : null;
                }
                else
                {
                    north = (y < height - 1) ? terrains[x, y + 1] : null;
                    south = (y > 0)          ? terrains[x, y - 1] : null;
                }

                current.SetNeighbors(left, north, right, south);
            }
        }
    }
}
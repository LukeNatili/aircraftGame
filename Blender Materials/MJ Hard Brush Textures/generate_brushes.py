import bpy
import os

folder = r"//brush_textures"

# Base brush to duplicate (Paint Hard)
base = bpy.data.brushes.get("Paint Hard")
for file in os.listdir(bpy.path.abspath(folder)):
    if not file.lower().endswith(".png"):
        continue

    tex_path = os.path.join(folder, file)   

    new_brush = base.copy()
    new_brush.name = os.path.splitext(file)[0]
    new_brush.icon_filepath = tex_path

    img = bpy.data.images.load(tex_path)

    if new_brush.texture is None:
        new_brush.texture = bpy.data.textures.new(new_brush.name + "_tex", type='IMAGE')

    new_brush.texture.image = img
    new_brush.texture_slot.map_mode = 'VIEW_PLANE'
    new_brush.use_fake_user = True

print("Brush generation complete")

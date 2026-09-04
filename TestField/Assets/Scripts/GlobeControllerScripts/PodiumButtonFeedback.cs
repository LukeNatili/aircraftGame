using UnityEngine;
using UnityEngine.InputSystem;

public class PodiumButtonFeedback : MonoBehaviour
{
    public enum ArrowButton { Up, Down, Left, Right, Center }

    [System.Serializable]
    public class ButtonConfig
    {
        public ArrowButton Button;
        public Transform ButtonTransform;
        public Renderer ButtonRenderer;

        [HideInInspector] public InputAction Action;
        [HideInInspector] public Vector3 IdlePosition;
        [HideInInspector] public Vector3 PressedPosition;
        [HideInInspector] public MaterialPropertyBlock mpb;
    }

    public ButtonConfig[] Buttons;
    public float PressDepth = 0.01f;
    public float PressSpeed = 15f;
    //public Color IdleColor = Color.black;
    //public Color LitColor = Color.cyan;
    public string PressedProperty = "_Pressed";
    public Vector3 PressAxis = new Vector3(0, 0, -1);

    private PlayerInputActions PlayerInput;

    void Awake()
    {
        PlayerInput = new PlayerInputActions();

        foreach (var b in Buttons)
        {
            b.Action = GetAction(b.Button);
            b.IdlePosition = b.ButtonTransform.localPosition;
            b.PressedPosition = b.IdlePosition + PressAxis * PressDepth;
            b.mpb = new MaterialPropertyBlock();
        }
    }

    InputAction GetAction(ArrowButton type)
    {
        switch (type)
        {
            case ArrowButton.Up: return PlayerInput.Globe.RotateUp;
            case ArrowButton.Down: return PlayerInput.Globe.RotateDown;
            case ArrowButton.Left: return PlayerInput.Globe.RotateLeft;
            case ArrowButton.Right: return PlayerInput.Globe.RotateRight;
            //case ArrowButton.Center: return PlayerInput.Globe.Select;
            default: return null;
        }
    }

    void OnEnable()
    {
        PlayerInput.Globe.Enable();
    }

    void OnDisable()
    {
        PlayerInput.Globe.Disable();
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var b in Buttons)
        {
            bool isPressed = b.Action.IsPressed();

            Vector3 targetPos = isPressed ? b.PressedPosition : b.IdlePosition;
            b.ButtonTransform.localPosition = Vector3.Lerp(b.ButtonTransform.localPosition, targetPos, Time.deltaTime * PressSpeed);

            //Color targetColor = isPressed ? LitColor : IdleColor;
            b.ButtonRenderer.GetPropertyBlock(b.mpb);
            b.mpb.SetFloat(PressedProperty, isPressed ? 1f : 0f);
            b.ButtonRenderer.SetPropertyBlock(b.mpb);
        }
    }

    void FixedUpdate()
    {

    }
}

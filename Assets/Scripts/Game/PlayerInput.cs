using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Deliverable3
{
    public static class PlayerInput
    {
// Funciona tant amb l'Input Manager antic com amb l'Input System nou.
#if ENABLE_LEGACY_INPUT_MANAGER
        public static float MoveX { get { return Axis(KeyCode.A, KeyCode.LeftArrow, KeyCode.D, KeyCode.RightArrow); } }
        public static float MoveZ { get { return Axis(KeyCode.S, KeyCode.DownArrow, KeyCode.W, KeyCode.UpArrow); } }
        public static bool Jump { get { return Input.GetKey(KeyCode.Space); } }
        public static bool Crouch { get { return Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl); } }
        public static bool Fire { get { return Input.GetMouseButton(0); } }
        public static Vector2 MousePosition { get { return Input.mousePosition; } }

        static float Axis(KeyCode negative, KeyCode negative2, KeyCode positive, KeyCode positive2)
        {
            float value = 0f;
            if (Input.GetKey(negative) || Input.GetKey(negative2)) value -= 1f;
            if (Input.GetKey(positive) || Input.GetKey(positive2)) value += 1f;
            return value;
        }
#elif ENABLE_INPUT_SYSTEM
        public static float MoveX { get { return Axis(Key.A, Key.LeftArrow, Key.D, Key.RightArrow); } }
        public static float MoveZ { get { return Axis(Key.S, Key.DownArrow, Key.W, Key.UpArrow); } }
        public static bool Jump { get { return Held(Key.Space); } }
        public static bool Crouch { get { return Held(Key.C) || Held(Key.LeftCtrl); } }
        public static bool Fire { get { return Mouse.current != null && Mouse.current.leftButton.isPressed; } }
        public static Vector2 MousePosition { get { return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero; } }

        static bool Held(Key key) { return Keyboard.current != null && Keyboard.current[key].isPressed; }

        static float Axis(Key negative, Key negative2, Key positive, Key positive2)
        {
            float value = 0f;
            if (Held(negative) || Held(negative2)) value -= 1f;
            if (Held(positive) || Held(positive2)) value += 1f;
            return value;
        }
#else
        public static float MoveX { get { return 0f; } }
        public static float MoveZ { get { return 0f; } }
        public static bool Jump { get { return false; } }
        public static bool Crouch { get { return false; } }
        public static bool Fire { get { return false; } }
        public static Vector2 MousePosition { get { return Vector2.zero; } }
#endif
    }
}

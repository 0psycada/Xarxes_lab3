using System;

namespace Deliverable3
{
    [Serializable]
    public struct InputDTO
    {
        public const byte Jump = 1;
        public const byte Crouch = 2;
        public const byte Fire = 4;

        public uint sequence;
        public float moveX;
        public float moveZ;
        public float aimYaw;
        // Botons com a estat (premut o no), no com a esdeveniment: si es perd un paquet, el següent porta el mateix.
        public byte buttons;

        public bool Has(byte button) { return (buttons & button) != 0; }
    }
}

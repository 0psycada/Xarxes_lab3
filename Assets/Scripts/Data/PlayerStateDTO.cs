using System;
using UnityEngine;

namespace Deliverable3
{
    [Serializable]
    public struct PlayerStateDTO
    {
        public const byte Crouching = 1;
        public const byte Alive = 2;

        public string name;
        public Vector3 position;
        public float yaw;
        public byte flags;
        public byte health;
        public byte score;
        public byte shots;

        public bool Has(byte flag) { return (flags & flag) != 0; }
    }
}

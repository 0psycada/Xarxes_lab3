using System;
using System.Collections.Generic;

namespace Deliverable3
{
    [Serializable]
    public struct StateDTO
    {
        public uint tick;
        public GamePhase phase;
        // El guanyador i el marcador van a cada snapshot: si se'n perd un, el següent ho arregla.
        public string winner;
        public float restartIn;
        public List<PlayerStateDTO> players;
    }
}

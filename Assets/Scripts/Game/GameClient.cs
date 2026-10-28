using System;
using System.Net;
using UnityEngine;

namespace Deliverable3
{
    public class GameClient : MonoBehaviour
    {
        public const float InputRate = 30f;
        const float JoinRetrySeconds = 0.5f;

        public InputDTO input;

        public string PlayerName { get; private set; }
        public bool HasState { get; private set; }
        public StateDTO State { get; private set; }

        readonly UdpChannel m_channel = new UdpChannel();
        EndPoint m_server;
        uint m_sequence;
        float m_sendTimer;
        float m_joinTimer;

        public bool Begin(string serverIp, int port, string playerName)
        {
            IPAddress address;
            if (!IPAddress.TryParse(serverIp, out address)) return false;
            m_server = new IPEndPoint(address, port);
            PlayerName = playerName;
            return m_channel.Open(0);
        }

        void OnDestroy() { m_channel.Close(); }

        void Update()
        {
            if (m_server == null) return;

            UdpChannel.Packet packet;
            while (m_channel.TryReceive(out packet)) Handle(packet);

            if (!HasState)
            {
                m_joinTimer -= Time.deltaTime;
                if (m_joinTimer <= 0f)
                {
                    m_joinTimer = JoinRetrySeconds;
                    // Repetim el GameJoin fins que arriba el primer estat, que fa de confirmació.
                    m_channel.Send(Messages.GameJoin(PlayerName), m_server);
                }
                return;
            }

            m_sendTimer += Time.deltaTime;
            if (m_sendTimer >= 1f / InputRate)
            {
                m_sendTimer = 0f;
                input.sequence = ++m_sequence;
                m_channel.Send(Messages.Input(input), m_server);
            }
        }

        void Handle(UdpChannel.Packet packet)
        {
            try
            {
                if (Messages.Type(packet.data) != MsgType.State) return;
                StateDTO state = Serializer.DeserializeStateDTO(Messages.Body(packet.data));
                // Estat endarrerit o repetit: el descartem.
                if (HasState && state.tick <= State.tick) return;
                State = state;
                HasState = true;
            }
            catch (Exception e)
            {
                Debug.Log("[GAME CLIENT] Dropped a bad packet: " + e.Message);
            }
        }
    }
}

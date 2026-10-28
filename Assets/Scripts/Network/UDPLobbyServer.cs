using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Deliverable3
{
    public class UDPLobbyServer : LobbyServer
    {
        public float timeoutSeconds = 5f;

        const int MaxPacketSize = 64 * 1024;

        Socket m_socket;
        Thread m_thread;
        volatile bool m_running;

        public override bool IsRunning { get { return m_running; } }

        public override bool StartServer()
        {
            try
            {
                m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                m_socket.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            catch (SocketException e)
            {
                Log("[SERVER] Could not start: " + e.SocketErrorCode);
                try { m_socket.Close(); } catch { }
                m_socket = null;
                return false;
            }

            m_running = true;
            Log("[SERVER] UDP room " + serverName + " listening on " + port);
            m_thread = new Thread(ReceiveLoop) { IsBackground = true };
            m_thread.Start();
            return true;
        }

        public override void StopServer()
        {
            if (!m_running) return;
            m_running = false;
            try { m_socket.Close(); } catch { }
            m_socket = null;
            if (m_thread != null) m_thread.Join(500);
            ClearPlayers();
            Log("[SERVER] Stopped");
        }

        void OnDestroy() { StopServer(); }

        void ReceiveLoop()
        {
            byte[] buffer = new byte[MaxPacketSize];
            while (m_running)
            {
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                int received;
                try { received = m_socket.ReceiveFrom(buffer, ref from); }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.ConnectionReset) continue;
                    return;
                }
                catch (ObjectDisposedException) { return; }
                catch (NullReferenceException) { return; }

                byte[] payload = new byte[received];
                Array.Copy(buffer, payload, received);
                m_inbox.Enqueue(new NetEvent { from = from, data = payload });
            }
        }

        protected override void Update()
        {
            base.Update();

            float now = Time.realtimeSinceStartup;
            foreach (Player p in Players)
                if (now - p.lastSeen > timeoutSeconds) { RemovePlayer(p, "timed out"); break; }
        }

        protected override void Send(object connection, byte[] message)
        {
            Socket s = m_socket;
            if (s == null) return;
            try { s.SendTo(message, (EndPoint)connection); }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }

        protected override void Drop(object connection) { }
    }
}

using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Deliverable3
{
    public class UdpChannel
    {
        public struct Packet { public EndPoint from; public byte[] data; }

        const int MaxPacketSize = 64 * 1024;

        readonly ConcurrentQueue<Packet> m_inbox = new ConcurrentQueue<Packet>();
        Socket m_socket;
        Thread m_thread;
        volatile bool m_running;

        public bool Open(int port)
        {
            try
            {
                m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                m_socket.Bind(new IPEndPoint(IPAddress.Any, port));
            }
            catch (SocketException)
            {
                Close();
                return false;
            }

            m_running = true;
            m_thread = new Thread(ReceiveLoop) { IsBackground = true };
            m_thread.Start();
            return true;
        }

        public void Close()
        {
            m_running = false;
            Socket s = m_socket; m_socket = null;
            if (s != null) try { s.Close(); } catch { }
            if (m_thread != null) m_thread.Join(500);
            m_thread = null;
        }

        public void Send(byte[] message, EndPoint to)
        {
            Socket s = m_socket;
            if (s == null) return;
            try { s.SendTo(message, to); }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }

        public bool TryReceive(out Packet packet) { return m_inbox.TryDequeue(out packet); }

        void ReceiveLoop()
        {
            byte[] buffer = new byte[MaxPacketSize];
            while (m_running)
            {
                Socket s = m_socket;
                if (s == null) return;
                EndPoint from = new IPEndPoint(IPAddress.Any, 0);
                int received;
                try { received = s.ReceiveFrom(buffer, ref from); }
                catch (SocketException e)
                {
                    // A Windows, un SendTo anterior ha arribat a un port tancat. No és aquest Receive: seguim.
                    if (e.SocketErrorCode == SocketError.ConnectionReset) continue;
                    return;
                }
                catch (ObjectDisposedException) { return; }

                // El fil de xarxa només posa el paquet a la cua: es processa a Update(), al fil principal.
                byte[] data = new byte[received];
                Array.Copy(buffer, data, received);
                m_inbox.Enqueue(new Packet { from = from, data = data });
            }
        }
    }
}

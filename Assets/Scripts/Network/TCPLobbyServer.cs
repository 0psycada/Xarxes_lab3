using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Deliverable3
{
    public class TCPLobbyServer : LobbyServer
    {
        const int MaxPacketSize = 64 * 1024;

        Socket m_listener;
        readonly List<Socket> m_sockets = new List<Socket>();
        readonly List<Thread> m_threads = new List<Thread>();
        volatile bool m_running;

        public override bool IsRunning { get { return m_running; } }

        public override bool StartServer()
        {
            try
            {
                m_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_listener.Bind(new IPEndPoint(IPAddress.Any, port));
                m_listener.Listen(10);
            }
            catch (SocketException e)
            {
                Log("[SERVER] Could not start: " + e.SocketErrorCode);
                CloseSocket(m_listener); m_listener = null;
                return false;
            }

            m_running = true;
            Log("[SERVER] TCP room " + serverName + " listening on " + port);
            StartThread(AcceptLoop);
            return true;
        }

        public override void StopServer()
        {
            if (!m_running) return;
            m_running = false;
            CloseSocket(m_listener); m_listener = null;

            Socket[] sockets;
            lock (m_sockets) { sockets = m_sockets.ToArray(); m_sockets.Clear(); }
            foreach (Socket s in sockets) CloseSocket(s);

            Thread[] threads;
            lock (m_threads) { threads = m_threads.ToArray(); m_threads.Clear(); }
            foreach (Thread t in threads) t.Join(500);

            ClearPlayers();
            Log("[SERVER] Stopped");
        }

        void OnDestroy() { StopServer(); }

        void AcceptLoop()
        {
            while (m_running)
            {
                Socket client;
                try { client = m_listener.Accept(); }
                catch (SocketException) { break; }
                catch (ObjectDisposedException) { break; }

                lock (m_sockets) m_sockets.Add(client);
                Socket captured = client;
                StartThread(delegate { ClientLoop(captured); });
            }
        }

        void ClientLoop(Socket socket)
        {
            byte[] header = new byte[4];
            while (m_running)
            {
                if (!ReadExactly(socket, header, 4)) break;
                int size = BitConverter.ToInt32(header, 0);
                if (size <= 0 || size > MaxPacketSize) break;

                byte[] payload = new byte[size];
                if (!ReadExactly(socket, payload, size)) break;

                m_inbox.Enqueue(new NetEvent { from = socket, data = payload });
            }

            lock (m_sockets) m_sockets.Remove(socket);
            CloseSocket(socket);
            m_inbox.Enqueue(new NetEvent { from = socket, data = null });
        }

        bool ReadExactly(Socket socket, byte[] buffer, int count)
        {
            int total = 0;
            while (total < count)
            {
                int read;
                try { read = socket.Receive(buffer, total, count - total, SocketFlags.None); }
                catch (SocketException) { return false; }
                catch (ObjectDisposedException) { return false; }
                if (read == 0) return false;
                total += read;
            }
            return true;
        }

        protected override void Send(object connection, byte[] payload)
        {
            Socket socket = (Socket)connection;
            byte[] framed = new byte[4 + payload.Length];
            BitConverter.GetBytes(payload.Length).CopyTo(framed, 0);
            payload.CopyTo(framed, 4);

            try { socket.Send(framed); }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
        }

        protected override void Drop(object connection)
        {
            try { ((Socket)connection).Shutdown(SocketShutdown.Send); } catch { }
        }

        protected override string Describe(object connection)
        {
            try { return ((Socket)connection).RemoteEndPoint.ToString(); } catch { return "?"; }
        }

        void StartThread(ThreadStart work)
        {
            Thread t = new Thread(work) { IsBackground = true };
            lock (m_threads) m_threads.Add(t);
            t.Start();
        }

        static void CloseSocket(Socket s)
        {
            if (s == null) return;
            try { s.Shutdown(SocketShutdown.Both); } catch { }
            try { s.Close(); } catch { }
        }
    }
}

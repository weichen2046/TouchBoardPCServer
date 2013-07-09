using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace TouchPadPCServer
{
    public class MonitorServer
    {
        private bool isStarted = false;

        /// <summary>
        /// 开启监听服务器
        /// </summary>
        public void Start()
        {
            if (!isStarted)
            {
                lock (this)
                {
                    if (!isStarted)
                    {
                        new Thread(ListenThreadProc).Start();
                        isStarted = true;
                    }
                }
            }
        }

        /// <summary>
        /// 停止监听服务器
        /// </summary>
        public void Stop()
        {
            if (isStarted)
            {
                lock (this)
                {
                    if (isStarted)
                    {
                        isStarted = false;
                    }

                    // a dummy socket connect
                    Socket dummy = new Socket(AddressFamily.InterNetwork,
                        SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint localEndPoint = new IPEndPoint(
                        IPAddress.Parse(Properties.Settings.Default.LocalIP),
                        Properties.Settings.Default.LocalPort);

                    System.Diagnostics.Debug.WriteLine(localEndPoint.ToString());
                    try
                    {
                        dummy.Connect(localEndPoint);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.Message);
                    }
                    finally
                    {
                        dummy.Close();
                    }
                }
                // TODO maybe at this time, I dont need to stop the simulate engine
                // because if we don't need to listen, we still can use simulate
                // engine with transmit socket previous accept
                SimulateEngine.Instance.Stop();
            }
        }

        private void ListenThreadProc(object threadContext)
        {
            System.Diagnostics.Debug.WriteLine("Listen thread is running.");

            IPEndPoint localEndPoint = new IPEndPoint(
                IPAddress.Parse(Properties.Settings.Default.LocalIP),
                Properties.Settings.Default.LocalPort);
            System.Diagnostics.Debug.WriteLine("Listen end point: " + localEndPoint.ToString());

            Socket listener = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(20);

                while (true)
                {
                    Socket client = listener.Accept();
                    System.Diagnostics.Debug.WriteLine("Accept a client socket.");

                    bool exit = false;
                    lock (this)
                    {
                        exit = !isStarted;
                    }
                    if (exit)
                    {
                        client.Close();
                        break;
                    }

                    // start a new thead to handle accepted client socket
                    new Thread(HandleClientThreadProc).Start(client);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            System.Diagnostics.Debug.WriteLine("Listen thread is exiting.");
        }

        private void HandleClientThreadProc(object threadContext)
        {
            Socket client = threadContext as Socket;
            if (client == null)
            {
                System.Diagnostics.Debug.WriteLine("In HandleClientThreadProc, client is null, just return.");
                return;
            }

            int timeoutflag = 0;
            Timer timer = null;
            byte[] revDatas = new byte[1024];
            ManualResetEvent threadExitEvent = new ManualResetEvent(false);

            // 1 read datas from client
            IAsyncResult res = client.BeginReceive(revDatas, 0, 1024, SocketFlags.None,
                new AsyncCallback(state => {
                    System.Diagnostics.Debug.WriteLine("HandleClientThreadProc(...) ok, async receive callback going.");
                    if (Interlocked.CompareExchange(ref timeoutflag, 1, 0) != 0)
                    {
                        // the flag was set elsewhere, so return immediately.
                        return;
                    }
                    System.Diagnostics.Debug.WriteLine("HandleClientThreadProc(...) ok, going to read client data.");
                    // we set the flag to 1, indicating it was completed.
                    if (timer != null)
                    {
                        // stop the timer from firing.
                        timer.Dispose();
                    }
                    // process the read.
                    int revLen = client.EndReceive(state);
                    string revStr = Encoding.UTF8.GetString(revDatas, 0, revLen);
                    System.Diagnostics.Debug.WriteLine("Datas from client: " + revStr);
                    if (revStr.Equals(CLIENT_DETECT_SERVER_TAG))
                    {
                        // 2 if data from client indicate that client is detecting server,
                        // then send server tag to client
                        byte[] datas = Encoding.UTF8.GetBytes(SERVER_TAG);
                        long dataLen = datas.LongLength;
                        if (BitConverter.IsLittleEndian)
                        {
                            dataLen = IPAddress.HostToNetworkOrder(dataLen);
                        }
                        byte[] lenBytes = BitConverter.GetBytes(dataLen);
                        client.Send(lenBytes);
                        client.Send(datas);
                        client.Close();
                    }
                    else if (revStr.Equals(TIME_TUNNEL_TAG))
                    {
                        // new a thread to hold this client socket to communicate with phone side
                        SimulateEngine.Instance.Start(client);
                    }
                    threadExitEvent.Set();
                }), null);

            if (!res.IsCompleted)
            {
                timer = new Timer(state =>
                    {
                        System.Diagnostics.Debug.WriteLine("HandleClientThreadProc(...) ok, timeout callback going.");
                        if (Interlocked.CompareExchange(ref timeoutflag, 1, 0) != 0)
                        {
                            // the flag was set elsewhere, so return immediately.
                            return;
                        }
                        System.Diagnostics.Debug.WriteLine("HandleClientThreadProc(...) ok, timeout.");
                        // we set the flag to 2, indicating a timeout was hit.
                        timer.Dispose();
                        client.Close();
                        threadExitEvent.Set();
                    }, null, 2000, Timeout.Infinite);
            }

            System.Diagnostics.Debug.WriteLine("HandleClientThreadProc(...) wait thread exit.");
            threadExitEvent.WaitOne();
            threadExitEvent.Dispose();
            System.Diagnostics.Debug.WriteLine("HandleClientThreadProc(...) exit.");
        }

        private const int DEFAULT_PORT = 8123;
        private const string SERVER_TAG = "Yes, I am server.";
        private const string CLIENT_DETECT_SERVER_TAG = "Are you a server.";
        private const string TIME_TUNNEL_TAG = "Time Tunnel.";
    }
}

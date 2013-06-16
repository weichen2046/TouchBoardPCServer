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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            System.Diagnostics.Debug.WriteLine("Listen thread is exiting.");
        }

        private const int DEFAULT_PORT = 8123;
        private const string SERVER_TAG = "Yes, I am server.";
    }
}

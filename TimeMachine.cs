using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace TouchPadPCServer
{
    enum TransmitControlTag
    {
        Data = 1,
        Quit = 0xFFFF, // 0xFFFFFFFE
        Unknown = -1
    }

    public class DataArrivedEventArgs : EventArgs
    {
        private byte[] data;
        public DataArrivedEventArgs(byte[] data)
        {
            this.data = data;
        }

        public byte[] Data
        {
            get { return data; }
        }
    }

    public delegate void DataArrivedEventHandler(object sender, DataArrivedEventArgs args);
    public delegate void QuitEventHanlder(object sender, EventArgs args);

    public class TimeMachine
    {
        /// <summary>
        /// 退出事件
        /// </summary>
        public event DataArrivedEventHandler DataArrivedEvent;
        public event QuitEventHanlder QuitEvent;

        public TimeMachine(Socket socket)
        {
            this.client = socket;
        }

        public void Start()
        {
            if (workingThread == null)
            {
                workingThread = new Thread(WorkingThreadProc);
                workingThread.Name = "TimeMachineThread";
                workingThread.Start(this);
            }
        }

        public void Stop()
        {
            Log.d(LOG_TAG, 
                string.Format("Stop called, and working thread is {0} null.",
                ((workingThread != null)?"not":string.Empty)));
            if (workingThread != null)
            {
                System.Diagnostics.Debug.WriteLine("TimeMachine workingThread is not null.");
                client.Close();
                client = null;
                if (workingThread.IsAlive)
                {
                    Log.d(LOG_TAG, "working thread is alive, going to abort it.");
                    workingThread.Abort();
                    workingThread = null;
                }
            }
        }

        public void SendResponseOK()
        {
            if (client != null)
            {
                byte[] datas = Encoding.UTF8.GetBytes(SERVER_RESP_OK);
                try
                {
                    client.Send(datas);
                }
                catch (Exception ex)
                {
                    Log.d(LOG_TAG, "exception when send OK response from time machine to phone side.");
                    Log.d(LOG_TAG, string.Format("exception message: {0}", ex.Message));
                }
            }
        }

        /// <summary>
        /// 从Socket中读取一个字符串
        /// </summary>
        /// <returns></returns>
        private string ReadString()
        {
            byte[] received = ReadData();

            if (received != null)
                return Encoding.UTF8.GetString(received);

            return null;
        }

        private byte[] ReadData()
        {
            if (client == null)
            {
                throw new Exception("Client socket can not be null.");
            }

            byte[] received = null;
            // 1 read data length buffer
            byte[] dataLenBuf = new byte[MESSAGE_LEN_BUFFER];
            long dataLen = 0;

            try
            {
                int readLen = client.Receive(dataLenBuf, 0, MESSAGE_LEN_BUFFER, SocketFlags.None);
                if (readLen != MESSAGE_LEN_BUFFER)
                {
                    Log.d(LOG_TAG, 
                        string.Format("received {0} bytes data, but we need {1} bytes for one data received from timemachine.",
                        readLen, MESSAGE_LEN_BUFFER));
                    return null;
                }
                // 2 get data length
                dataLen = BitConverter.ToInt64(dataLenBuf, 0);
                if (BitConverter.IsLittleEndian)
                {
                    dataLen = IPAddress.NetworkToHostOrder(dataLen);
                }

                received = GetSpecificLenData(dataLen);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            // for debug, print received data
            if (received != null)
            {
                StringBuilder sb = new StringBuilder(1024);
                for (int i = 0; i < received.Length; i++)
                {
                    sb.AppendFormat("{0:X2} ", received[i]);
                }
                Log.d(LOG_TAG, string.Format("received {1} bytes: {0}", sb.ToString(), received.Length));
            }

            return received;
        }

        private byte[] GetSpecificLenData(long totalLen)
        {
            Log.d(LOG_TAG, string.Format("going to receive {0} bytes data.", totalLen));
            List<byte> bytesRet = new List<byte>();
            byte[] datas = new byte[4096];

            int totalReaded = 0;
            int readOnce = 0;

            try
            {
                while (totalReaded < totalLen)
                {
                    int goingToRead = (int)((totalLen - (long)totalReaded > 4096) ? 4096 : (totalLen - (long)totalReaded));
                    readOnce = client.Receive(datas, 0, goingToRead, SocketFlags.None);
                    if (readOnce > 0)
                    {
                        totalReaded += readOnce;
                        for (int i = 0; i < readOnce; i++)
                        {
                            bytesRet.Add(datas[i]);
                        }
                    }
                }

                return bytesRet.ToArray();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return null;
        }

        private TransmitControlTag GetTransmitControlTag()
        {
            // read TRANSMIT_CONTROL_TAG_LEN bytes
            byte[] tagBytes = GetSpecificLenData(TRANSMIT_CONTROL_TAG_LEN);
            Log.dIf((tagBytes == null), LOG_TAG, "can not receive transmit control tag, tagBytes = null.");
            Log.dIf((tagBytes != null), LOG_TAG, string.Format("received tarnsmit control tag bytes length = {0}", tagBytes.Length));
            if (tagBytes != null && tagBytes.Length == TRANSMIT_CONTROL_TAG_LEN)
            {
                // TODO if TRANSMIT_CONTROL_TAG_LEN not 4 byte, there will be a bug
                int tagInt = BitConverter.ToInt32(tagBytes, 0);
                if (BitConverter.IsLittleEndian)
                {
                    tagInt = IPAddress.NetworkToHostOrder(tagInt);
                }
                Log.d(LOG_TAG, string.Format("received transmit control tag int value = {0}", tagInt));
                // TODO if tagInt can not map to TransmitControlTag enum
                // then need to do something here
                return (TransmitControlTag)tagInt;
            }

            return TransmitControlTag.Unknown;
        }

        private void OnQuit(EventArgs args)
        {
            if (QuitEvent != null)
                QuitEvent(this, args);
        }

        private void OnDataArrived(DataArrivedEventArgs args)
        {
            if (DataArrivedEvent != null)
                DataArrivedEvent(this, args);
        }

        private void WorkingThreadProc(object threadContext)
        {
            try
            {
                Log.d(LOG_TAG, "working thread running...");
                SendResponseOK();
                bool exitAll = false;
                while (true)
                {
                    // read transmit control tag
                    switch (GetTransmitControlTag())
                    {
                        case TransmitControlTag.Data:
                            // read client data
                            byte[] received = ReadData();
                            // fire data arrived event
                            OnDataArrived(new DataArrivedEventArgs(received));
                            break;
                        case TransmitControlTag.Quit:
                            // fire quit event
                            OnQuit(new EventArgs());
                            exitAll = true;
                            break;
                        default:
                            Log.d(LOG_TAG, "received unknown transmit control tag.");
                            break;
                    }
                    if (exitAll)
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            Log.d(LOG_TAG, "working thread exit.");
        }

        

        private Socket client;
        private Thread workingThread = null;
        private const string SERVER_RESP_OK = "OK";
        /// <summary>
        /// 存放接收数据长度缓存的字节数，这里使用8个字节
        /// </summary>
        private const int MESSAGE_LEN_BUFFER = 8;
        private const int TRANSMIT_CONTROL_TAG_LEN = 4;

        private const string LOG_TAG = "TimeMachine";
    }
}

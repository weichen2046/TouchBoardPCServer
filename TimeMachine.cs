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
            if (workingThread != null)
            {
                System.Diagnostics.Debug.WriteLine("Going to stop server.");
                client.Close();
                client = null;
                workingThread.Abort();
                workingThread = null;
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
                    System.Diagnostics.Debug.WriteLine("Exception when send OK response from server.");
                    System.Diagnostics.Debug.WriteLine("Exception: " + ex.Message);
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
                    System.Diagnostics.Debug.WriteLine("Received length byte buffer is " + readLen.ToString());
                    return null;
                }
                // 2 get data length
                dataLen = BitConverter.ToInt64(dataLenBuf, 0);
                if (BitConverter.IsLittleEndian)
                {
                    dataLen = IPAddress.NetworkToHostOrder(dataLen);
                }
                System.Diagnostics.Debug.WriteLine("Going to receive data length is "
                    + dataLen.ToString());
                received = GetSpecificLenData(dataLen);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }

            return received;
        }

        private byte[] GetSpecificLenData(long totalLen)
        {
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
            if (tagBytes != null && tagBytes.Length == TRANSMIT_CONTROL_TAG_LEN)
            {
                // TODO if TRANSMIT_CONTROL_TAG_LEN not 4 byte, there will be a bug
                int tagInt = BitConverter.ToInt32(tagBytes, 0);
                if (BitConverter.IsLittleEndian)
                {
                    tagInt = IPAddress.NetworkToHostOrder(tagInt);
                }
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
                System.Diagnostics.Debug.WriteLine("Time machine working thread running...");
                SendResponseOK();
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
                            break;
                        default:
                            System.Diagnostics.Debug.WriteLine("TimeMachine received unknown transmit control tag.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            System.Diagnostics.Debug.WriteLine("Time machine working thread exit...");
        }

        private Socket client;
        private Thread workingThread = null;
        private const string SERVER_RESP_OK = "OK";
        /// <summary>
        /// 存放接收数据长度缓存的字节数，这里使用8个字节
        /// </summary>
        private const int MESSAGE_LEN_BUFFER = 8;
        private const int TRANSMIT_CONTROL_TAG_LEN = 4;
    }
}

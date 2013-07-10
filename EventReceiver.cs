using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;

namespace TouchPadPCServer
{
    /// <summary>
    /// 从TimeMachine中收到的数据包中检测到的事件标记
    /// </summary>
    enum EventTag
    {
        MOTION = 1,
        CLICK = 4,
        Unknown = -1
    }

    enum EventType
    {
        MOVE = 1,
        CLICK = 2,
        DOUBLE_CLICK = 3,
        Unknown = -1
    }

    public delegate void ClickEventHandler(object sender, EventArgs args);

    public class EventReceiver
    {
        public EventReceiver(TimeMachine tunnel)
        {
            mTimeMachine = tunnel;
            mTimeMachine.DataArrivedEvent += new DataArrivedEventHandler(mTimeMachine_DataArrivedEvent);
            mTimeMachine.QuitEvent += new QuitEventHanlder(mTimeMachine_QuitEvent);
        }

        public event ClickEventHandler ClickEvent;
        public event QuitEventHanlder QuitEvent;

        public void Start()
        {
            System.Diagnostics.Debug.WriteLine("EventReceiver.Start(...) called.");
            if (mTimeMachine != null)
            {
                Interlocked.Exchange(ref mStopWork, 0);

                mDataBuffer = new Queue<DataArrivedEventArgs>(DATA_BUFFER_LEN);

                mSign = new AutoResetEvent(false);
                mWorkingThread = new Thread(WorkingThreadProc);
                mWorkingThread.Start();

                mTimeMachine.Start();
            }
        }

        public void Stop()
        {
            System.Diagnostics.Debug.WriteLine("EventReceiver.Stop() called.");
            if (mTimeMachine != null)
            {
                mTimeMachine.Stop();
                mTimeMachine = null;
                mDataBuffer = null;

                Interlocked.Exchange(ref mStopWork, 1);
                mSign.Set();
            }
        }

        private void mTimeMachine_DataArrivedEvent(object sender, DataArrivedEventArgs args)
        {
            // This callback running in TimeMachine working thread
            System.Diagnostics.Debug.WriteLine("New data from TimeMachine arrived.");
            lock (mLockObjForQueue)
            {
                mDataBuffer.Enqueue(args);
            }
            mSign.Set();
        }

        private void mTimeMachine_QuitEvent(object sender, EventArgs args)
        {
            System.Diagnostics.Debug.WriteLine("TimeMachine quit event arrived.");
            Interlocked.Exchange(ref mStopWork, 1);
            mSign.Set();

            mTimeMachine = null;
        }

        private void WorkingThreadProc()
        {
            System.Diagnostics.Debug.WriteLine("EventReceiver working thread running...");
            while (true)
            {
                mSign.WaitOne();
                long exit = Interlocked.Read(ref mStopWork);
                if (exit == 1)
                {
                    mSign.Dispose();
                    break;
                }

                while (true)
                {
                    DataArrivedEventArgs data = null;
                    lock (mLockObjForQueue)
                    {
                        if (mDataBuffer.Count != 0)
                            data = mDataBuffer.Dequeue();
                    }
                    if (data != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Process data in buffer.");
                        // consume data in buffer
                        ParseData(data);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            // fire quit event there
            OnQuit(new EventArgs());
            System.Diagnostics.Debug.WriteLine("EventReceiver working thread exit.");
        }

        private void ParseData(DataArrivedEventArgs data)
        {
            if (data == null)
                throw new ArgumentNullException("data", "Parameter data can not be null.");

            // read event tag
            switch (GetEventTag(data.Data))
            {
                case EventTag.CLICK:
                    System.Diagnostics.Debug.WriteLine("Click event received from timemachine.");
                    OnClick(new EventArgs());
                    break;
                case EventTag.MOTION:
                    System.Diagnostics.Debug.WriteLine("Motion event received from timemachine.");
                    break;
                default:
                    System.Diagnostics.Debug.WriteLine("Unknown event received from timemachine.");
                    break;
            }
        }

        private EventTag GetEventTag(byte[] data)
        {
            if (data == null || data.Length < INT_SIZE)
                throw new ArgumentNullException("data", "Parameter data can not be null.");

            int tagInt = BitConverter.ToInt32(data, 0);
            if (BitConverter.IsLittleEndian)
                tagInt = IPAddress.NetworkToHostOrder(tagInt);
            try
            {
                return (EventTag)Enum.ToObject(typeof(EventTag), tagInt);
            }
            catch (Exception ex)
            {
                // do nothing
                System.Diagnostics.Debug.WriteLine(
                    string.Format("Can't change {0} to enum EventTag."), tagInt);
            }

            return EventTag.Unknown;
        }

        private void OnClick(EventArgs args)
        {
            if (ClickEvent != null)
                ClickEvent(this, args);
        }

        private void OnQuit(EventArgs args)
        {
            if (QuitEvent != null)
                QuitEvent(this, args);
        }

        private TimeMachine mTimeMachine = null;

        private const int DATA_BUFFER_LEN = 1024;
        private Queue<DataArrivedEventArgs> mDataBuffer;
        private object mLockObjForQueue = new object();

        private Thread mWorkingThread;
        private AutoResetEvent mSign;
        /// <summary>
        /// 是否退出工作线程，0不退出，1退出。
        /// </summary>
        private long mStopWork;

        private const int INT_SIZE = 4;
    }
}

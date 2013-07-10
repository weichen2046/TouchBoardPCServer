using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TouchPadPCServer
{
    public class EventReceiver
    {
        public EventReceiver(TimeMachine tunnel)
        {
            mTimeMachine = tunnel;
            mTimeMachine.DataArrivedEvent += new DataArrivedEventHandler(mTimeMachine_DataArrivedEvent);
            mTimeMachine.QuitEvent += new QuitEventHanlder(mTimeMachine_QuitEvent);
        }

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
                    }
                    else
                    {
                        break;
                    }
                }
            }
            System.Diagnostics.Debug.WriteLine("EventReceiver working thread exit.");
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
    }
}

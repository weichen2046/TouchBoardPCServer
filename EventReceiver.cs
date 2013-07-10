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

                mDataBuffer = new object[DATA_BUFFER_LEN];

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

            long avai = Interlocked.Read(ref mAvai);
            mDataBuffer[avai] = args;
            long allocateNextAvai = (avai + 1) % DATA_BUFFER_LEN;
            Interlocked.CompareExchange(ref allocateNextAvai, avai, mData);

            if (allocateNextAvai == avai) // not allocate buffer to store arrived data
            {
                System.Diagnostics.Debug.WriteLine("This data will lose.");
            }
            else
            {
                Interlocked.Exchange(ref mAvai, allocateNextAvai);
                mSign.Set();
            }
            System.Diagnostics.Debug.WriteLine("Next store data index: " + allocateNextAvai.ToString());
        }

        private void mTimeMachine_QuitEvent(object sender, EventArgs args)
        {
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
                    System.Diagnostics.Debug.WriteLine("Going to stop EventReceiver working thread.");
                    mSign.Dispose();
                    break;
                }

                while (true)
                {
                    System.Diagnostics.Debug.WriteLine("Still have data in buffer not processed.");
                    // consume data in buffer
                    long data = Interlocked.Read(ref mData);
                    System.Diagnostics.Debug.WriteLineIf((data == mPreProcessedIndex),
                        "No new data need to process, break while.");
                    if (data == mPreProcessedIndex)
                        break;

                    // process data
                    System.Diagnostics.Debug.WriteLine("Processed data at " 
                        + (mPreProcessedIndex+1).ToString());
                    mPreProcessedIndex = data;

                    long nextData = (mPreProcessedIndex + 1) % DATA_BUFFER_LEN;
                    Interlocked.CompareExchange(ref nextData, mPreProcessedIndex, mAvai);
                    if (nextData == mPreProcessedIndex)
                    {
                        System.Diagnostics.Debug.WriteLine("No new data need process.");
                        break;
                    }
                    else
                    {
                        Interlocked.Exchange(ref mData, nextData);
                    }
                    System.Diagnostics.Debug.WriteLine("Next process data index: " + nextData);
                }
            }
            System.Diagnostics.Debug.WriteLine("EventReceiver working thread exit.");
        }

        private TimeMachine mTimeMachine = null;

        private object[] mDataBuffer;
        private const int DATA_BUFFER_LEN = 1024;

        private long mPreProcessedIndex = -1;
        private long mData = 0;
        /// <summary>
        /// 前一次分配到的缓存下标
        /// </summary>
        private long mAvai = 0;

        private Thread mWorkingThread;
        private AutoResetEvent mSign;
        /// <summary>
        /// 是否退出工作线程，0不退出，1退出。
        /// </summary>
        private long mStopWork;
    }
}

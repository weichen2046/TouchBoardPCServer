using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TouchPadPCServer
{
    public class MotionEventTracker
    {
        public MotionEventTracker()
        {
            mEvents = new Queue<MotionEvent>();
        }

        private Queue<MotionEvent> mEvents;
        public Queue<MotionEvent> Events
        {
            get { return mEvents; }
        }

        private int mMaxPointerCount;
        public int MaxPointerCount
        {
            get { return mMaxPointerCount; }
        }

        public void Add(MotionEvent evt)
        {
            Log.d(LOG_TAG,
                string.Format("Add(...) method called, mMaxPointerCount = {0}, evt.PointerCount = {1}",
                mMaxPointerCount, evt.PointerCount));
            // update max pointer count
            if (mMaxPointerCount < evt.PointerCount)
                mMaxPointerCount = evt.PointerCount;

            mEvents.Enqueue(evt);
        }

        public void Reset()
        {
            mMaxPointerCount = 0;
            mEvents.Clear();
        }

        private const string LOG_TAG = "MotionEventTracker";
    }
}

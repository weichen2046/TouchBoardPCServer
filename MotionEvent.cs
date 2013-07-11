using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace TouchPadPCServer
{
    public enum MotionEventAction
    {
        ACTION_DOWN = 0,
        ACTION_UP = 1,
        ACTION_MOVE = 2,
        ACTION_CANCEL = 3,
        ACTION_OUTSIDE = 4,
        //ACTION_POINTER_1_DOWN = 5,
        ACTION_POINTER_DOWN = 5,
        //ACTION_POINTER_1_UP   = 6,
        ACTION_POINTER_UP = 6,
        ACTION_HOVER_MOVE = 7,
        //ACTION_POINTER_ID_SHIFT = 8,
        ACTION_POINTER_INDEX_MASK = 8,
        ACTION_HOVER_ENTER = 9,
        ACTION_HOVER_EXIT = 10,
        ACTION_MASK = 255,
        ACTION_POINTER_2_DOWN = 261,
        ACTION_POINTER_2_UP = 262,
        ACTION_POINTER_3_DOWN = 517,
        ACTION_POINTER_3_UP = 518,
        ACTION_POINTER_ID_MASK = 65280,
        UNKNOWN = -1,
    }

    public class MotionEvent
    {
        private MotionEventAction action;
        public MotionEventAction Action
        {
            get { return action; }
            private set { action = value; }
        }

        public int PointerCount
        {
            get { return mPointerCount; }
        }

        public int TotalBytes
        {
            get { return mParsedBytes - mInitialIndex; }
        }

        public static MotionEvent CreateFrom(byte[] data, int index)
        {
            MotionEvent instant = new MotionEvent();
            instant.InstantialCreateFrom(data, index);
            System.Diagnostics.Debug.WriteLine(instant.ToString());
            return instant;
        }

        private void InstantialCreateFrom(byte[] data, int index)
        {
            if (data == null)
                throw new ArgumentNullException("data", "参数data不能为null。");

            // reset initial index
            mInitialIndex = index;
            // reset parsed bytes count
            mParsedBytes = mInitialIndex;
            // reset max pointer
            mPointerCount = 0;
            // reset data
            mData = data;
            if (mData == null)
                throw new ArgumentNullException("data", "Parameter data can not be null.");

            // read action, 4 bytes
            this.action = GetMotionAction();
            // read pointer count, 4 bytes
            this.mPointerCount = GetPointerCount();
        }

        private MotionEventAction GetMotionAction()
        {
            int actionInt = GetInt();
            System.Diagnostics.Debug.WriteLine("Parse MotionEvent action, int value = " + actionInt);
            try
            {
                return (MotionEventAction)Enum.ToObject(typeof(MotionEventAction), actionInt);
            }
            catch (Exception ex)
            {
                // do nothing
                System.Diagnostics.Debug.WriteLine(
                    string.Format("Can't change {0} to enum MotionEventAction."), actionInt);
            }

            return MotionEventAction.UNKNOWN;
        }

        private int GetPointerCount()
        {
            return GetInt();
        }

        private int GetInt()
        {
            if (mData.Length - mParsedBytes < CommonConstans.INT_SIZE)
                throw new Exception("mData have not enough bytes to get a Int32.");

            int ret = BitConverter.ToInt32(mData, mParsedBytes);
            if (BitConverter.IsLittleEndian)
                ret = IPAddress.NetworkToHostOrder(ret);

            mParsedBytes += CommonConstans.INT_SIZE;

            return ret;
        }

        public override string ToString()
        {
            return string.Format("MotionEvent: action={0}({3}), pointer count={1}, all bytes count={2}",
                action, mPointerCount, mParsedBytes - mInitialIndex, (int)action);
        }

        private int mInitialIndex;
        private byte[] mData;
        private int mParsedBytes;
        private int mPointerCount;
    }
}

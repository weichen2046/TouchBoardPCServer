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

    public enum ToolType
    {
        TOOL_TYPE_UNKNOWN = 0,
        TOOL_TYPE_FINGER = 1,
        TOOL_TYPE_STYLUS = 2,
        TOOL_TYPE_MOUSE = 3,
        TOOL_TYPE_ERASER = 4
    }

    public class MotionEvent
    {
        private MotionEventAction action;
        public MotionEventAction Action
        {
            get { return action; }
            private set { action = value; }
        }

        private int mPointerCount;
        public int PointerCount
        {
            get { return mPointerCount; }
        }

        private MotionEventAction mActionMasked;
        public MotionEventAction ActionMasked
        {
            get { return mActionMasked; }
        }

        private int mActionIndex;
        public int ActionIndex
        {
            get { return mActionIndex; }
        }

        private Dictionary<int, PointerProperties> mPointers;
        public Dictionary<int, PointerProperties> Pointers
        {
            get { return mPointers; }
        }

        private int mHistoricalSize;
        public int HistoricalSize
        {
            get { return mHistoricalSize; }
        }

        private Dictionary<int, List<PointerCoordsClass>> mHistoricalPointerCoords;
        public Dictionary<int, List<PointerCoordsClass>> HistoricalPointerCoords
        {
            get { return mHistoricalPointerCoords; }
        }

        private Dictionary<int, PointerCoordsClass> mPointerCoords;
        public Dictionary<int, PointerCoordsClass> PointerCoords
        {
            get { return mPointerCoords; }
        }

        public int TotalBytes
        {
            get { return mParsedBytes - mInitialIndex; }
        }

        public static MotionEvent CreateFrom(byte[] data, int index)
        {
            MotionEvent instant = new MotionEvent();
            instant.InstantialCreateFrom(data, index);
            //System.Diagnostics.Debug.WriteLine(instant.ToString());
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
            // read masked action, 4 bytes
            this.mActionMasked = GetActionMasked();
            // read action index, 4 bytes
            this.mActionIndex = GetActionIndex();
            // read history size, 4 bytes
            this.mHistoricalSize = GetHistoricalSize();
            // read pinters data
            this.mPointers = new Dictionary<int, PointerProperties>();
            this.mPointerCoords = new Dictionary<int, PointerCoordsClass>();
            this.mHistoricalPointerCoords = new Dictionary<int, List<PointerCoordsClass>>();
            for (int i = 0; i < mPointerCount; i++)
            {
                PointerProperties pp = new PointerProperties();
                // read pointer id, 4 bytes
                pp.ID = ReadPointerId();

                PointerCoordsClass pc = new PointerCoordsClass();
                // read pointer X, 4 bytes
                pc.X = ReadX();
                // read pointer Y, 4 bytes
                pc.Y = ReadY();
                this.mPointerCoords.Add(i, pc);

                List<PointerCoordsClass> listPc = new List<PointerCoordsClass>();
                for (int j = 0; j < mHistoricalSize; j++)
                {
                    pc = new PointerCoordsClass();
                    // read pointer X, 4 bytes
                    pc.X = ReadX();
                    // read pointer Y, 4 bytes
                    pc.Y = ReadY();
                    listPc.Add(pc);
                }
                this.mHistoricalPointerCoords.Add(i, listPc);

                this.mPointers.Add(i, pp);
            }

            // because first 4 bytes in parameter data is consumed in TimeMachine as TransmitControlTag
            //Log.d(LOG_TAG, string.Format("left {0} bytes unparsed when create MotionEvent instance from bytes.",
            //    (data.Length - 4) - (mParsedBytes - mInitialIndex)));
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

        private MotionEventAction GetActionMasked()
        {
            int actionInt = GetInt();
            System.Diagnostics.Debug.WriteLine("Parse MotionEvent masked action, int value = " + actionInt);
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

        private int GetActionIndex()
        {
            return GetInt();
        }

        private int GetHistoricalSize()
        {
            return GetInt();
        }

        private int ReadPointerId()
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

        private float ReadX()
        {
            return ReadFloat();
        }

        private float ReadY()
        {
            return ReadFloat();
        }

        private float ReadFloat()
        {
            if (mData.Length - mParsedBytes < CommonConstans.FLOAT_SIZE)
                throw new Exception("mData have not enough bytes to get a Single.");

            int ret = BitConverter.ToInt32(mData, mParsedBytes);
            if (BitConverter.IsLittleEndian)
                ret = IPAddress.NetworkToHostOrder(ret);

            mParsedBytes += CommonConstans.FLOAT_SIZE;

            byte[] floatBytes = BitConverter.GetBytes(ret);
            StringBuilder sb = new StringBuilder(1024);
            for (int i = 0; i < floatBytes.Length; i++)
            {
                sb.AppendFormat("{0:X2} ", floatBytes[i]);
            }
            //Log.d(LOG_TAG, string.Format("float bytes: {0}", sb.ToString()));

            return BitConverter.ToSingle(floatBytes, 0); ;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("***************** MotionEvent begin *******************");
            sb.AppendFormat("all bytes count: {0}, parsed bytes count: {1}\n",
                mData.Length, mParsedBytes - mInitialIndex);
            sb.AppendFormat("action: {0}({1}), action masked: {2}({3})\n",
                action, (int)action, mActionMasked, (int)mActionMasked);
            sb.AppendFormat("pointer count: {0}\n", mPointerCount);
            sb.AppendFormat("action index: {0}\n", mActionIndex);
            sb.AppendFormat("historical count: {0}\n", mHistoricalSize);
            for(int i=0; i<mPointerCount; i++)
            {
                sb.AppendFormat("pointer index: {0}\n", i);
                sb.AppendFormat("\t{0}\n", mPointers[i].ToString());
                sb.AppendFormat("\t{0}\n", mPointerCoords[i].ToString());
                for (int j = 0; j <mHistoricalSize; j++)
                {
                    sb.AppendFormat("\thistory: {0} {1}\n", j, mHistoricalPointerCoords[i][j].ToString());
                }

            }
            sb.Append("********************* MotionEvent end **********************");

            return sb.ToString();
        }

        public class PointerProperties
        {
            private int mId;
            public int ID
            {
                get { return mId; }
                set { mId = value; }
            }

            private ToolType mToolType;
            public ToolType ToolType
            {
                get { return mToolType; }
            }

            public static bool operator==(PointerProperties left, PointerProperties right)
            {
                if (left == null && right == null)
                    return true;

                if (left != null && right != null)
                {
                    return left.mId == right.mId && left.mToolType == right.mToolType;
                }

                return false;
            }

            public static bool operator !=(PointerProperties left, PointerProperties right)
            {
                return !(left == right);
            }

            public override bool Equals(object obj)
            {
                if (obj == null || !(obj is PointerProperties))
                    return false;
                return this == (obj as PointerProperties);
            }

            public override int GetHashCode()
            {
                return mId | (((int)mToolType)<<8);
            }

            public override string ToString()
            {
                return string.Format("PointerProperties[ID: {0} ToolType: {1}]",
                    mId, mToolType);
            }
        }

        public class PointerCoordsClass
        {
            private float mX;
            public float X
            {
                get { return mX; }
                set { mX = value; }
            }

            private float mY;
            public float Y
            {
                get { return mY; }
                set { mY = value; }
            }

            private float mPressure;
            public float Pressure
            {
                get { return mPressure; }
            }

            private float mSize;
            public float Size
            {
                get { return mSize; }
            }

            public override string ToString()
            {
                return string.Format("PointerCoords[X: {0} Y: {1} Pressure: {2} Size: {3}]",
                    mX, mY, mPressure, mSize);
            }
        }

        private int mInitialIndex;
        private byte[] mData;
        private int mParsedBytes;
        private const string LOG_TAG = "MotionEvent";
        /// <summary>
        /// An invalid pointer id.
        /// This value (-1) can be used as a placeholder to indicate that a pointer id
        /// has not been assigned or is not available.  It cannot appear as
        /// a pointer id inside a <see cref="MotionEvent"/>.
        /// </summary>
        private const int INVALID_POINTER_ID = -1;
    }
}

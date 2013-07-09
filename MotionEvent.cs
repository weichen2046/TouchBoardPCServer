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
        
        ACTION_POINTER_DOWN = 5,
        ACTION_POINTER_1_DOWN = 5,
        ACTION_POINTER_1_UP = 6,
        ACTION_POINTER_2_DOWN = 261,
        ACTION_POINTER_2_UP = 262,
    }

    public class MotionEvent
    {
        private MotionEventAction action;
        public MotionEventAction Action
        {
            get { return action; }
            private set { action = value; }
        }

        private int pointerCount;
        public int PointerCount
        {
            get { return pointerCount; }
            private set { pointerCount = value; }
        }

        public static MotionEvent CreateFrom(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("data", "参数data不能为null。");
            bool isLittleEndian = BitConverter.IsLittleEndian;
            int temp = 0;
            int consumed = 0;
            MotionEvent instant = new MotionEvent();
            // read action, 4 bytes
            temp = BitConverter.ToInt32(data, consumed);
            if (isLittleEndian)
                temp = IPAddress.HostToNetworkOrder(temp);
            instant.action = (MotionEventAction)temp;
            consumed += 4;

            // read pointer count, 4 bytes
            temp = BitConverter.ToInt32(data, consumed);
            if (isLittleEndian)
                temp = IPAddress.HostToNetworkOrder(temp);
            instant.pointerCount = temp;
            consumed += 4;
            return instant;
        }
    }
}

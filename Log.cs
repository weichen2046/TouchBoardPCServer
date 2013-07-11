using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TouchPadPCServer
{
    public class Log
    {
        public static void d(string tag, string msg)
        {
            System.Diagnostics.Debug.WriteLine(
                string.Format("{0} : {1}", tag, msg));
        }

        public static void dIf(Boolean condition, string tag, string msg)
        {
            if (condition)
                d(tag, msg);
        }
    }
}

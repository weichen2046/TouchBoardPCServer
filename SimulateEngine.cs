using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace TouchPadPCServer
{
    public class SimulateEngine
    {
        #region Sigleton implementation
        SimulateEngine()
        {
        }

        public static SimulateEngine Instance
        {
            get { return SigletonHolder.instance; }
        }

        class SigletonHolder
        {
            static SigletonHolder() { }
            internal static readonly SimulateEngine instance = new SimulateEngine();
        }
        #endregion

        public void Start(Socket tunnel)
        {
            System.Diagnostics.Debug.WriteLine("SimulateEngine.Start(...) called.");
            if (!started)
            {
                started = true;

                mReceiver = new EventReceiver(new TimeMachine(tunnel));

                // set listeners to mReceiver

                mReceiver.Start();
            }
        }

        public void Stop()
        {
            System.Diagnostics.Debug.WriteLine("SimulateEngine.Stop() called.");
            if (started)
            {
                started = false;
                // TODO other operations
                mReceiver.Stop();
            }
        }

        private bool started = false;
        private EventReceiver mReceiver;
    }
}

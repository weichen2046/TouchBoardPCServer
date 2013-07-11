﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using WindowsInput;

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

                sim = new InputSimulator();

                mReceiver = new EventReceiver(new TimeMachine(tunnel));

                // set listeners to mReceiver
                RegisterEvent();
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

        private void RegisterEvent()
        {
            if (mReceiver == null)
                throw new Exception("mReceiver can't be null.");

            mReceiver.ClickEvent += new ClickEventHandler(mReceiver_ClickEvent);
            //mReceiver.DoubleClickEvent += new DoubleClickHandler(mReceiver_DoubleClickEvent);
            mReceiver.RightClickEvent += new RightClickHandler(mReceiver_RightClickEvent);
            mReceiver.QuitEvent += new QuitEventHanlder(mReceiver_QuitEvent);
        }

        private void mReceiver_ClickEvent(object sender, EventArgs args)
        {
            sim.Mouse.LeftButtonClick();
        }

        //private void mReceiver_DoubleClickEvent(object sender, EventArgs args)
        //{
        //}

        private void mReceiver_RightClickEvent(object sender, EventArgs args)
        {
            sim.Mouse.RightButtonClick();
        }

        private void mReceiver_QuitEvent(object sender, EventArgs args)
        {
            started = false;
        }

        private bool started = false;
        private EventReceiver mReceiver;
        private InputSimulator sim;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

/************************
 * Regression test for bug Bug 749068:WatsonCrash: coreclr.dll!Thread::DoAppropriateWaitWorker -- APPLICATION_HANG_BlockedOn_EventHandle c0000194
 *
 * Should be run with complus_GCStress=3
 *
 * During GC, no IO completion threads are created. So if there was no IO completion thread to begin with,
 * there will be no threads monitoring the event which signals to schedule the corresponding callback,
 * this blocks whatever code, which is waiting for the callback to finish or unregister it, indefinitely
 *
 ************************/
namespace Prog
{
    class Callback
    {
        ManualResetEvent sessionNotification;
        RegisteredWaitHandle sessionRegisteredWait;

        public Callback()
        {
            this.sessionRegisteredWait = null;
            this.sessionNotification = null;
        }

        public void ServiceCallbackOnPositionAvailable(Object state, bool timedOut)
        {

            if (this.sessionRegisteredWait == null)
            {
                this.sessionNotification.Reset();
                this.sessionRegisteredWait.Unregister(null);

                this.sessionRegisteredWait =
                        ThreadPool.RegisterWaitForSingleObject(this.sessionNotification,
                                                                ServiceCallbackOnPositionAvailable,
                                                                this,   /* object state */
                                                                -1,     /* INFINITE */
                                                                true    /* ExecuteOnlyOnce */);
            }

            Console.WriteLine("callback running");
        }

        public void call()
        {
            if (this.sessionNotification != null)
                this.sessionNotification.Set();
        }

        public void register()
        {
            this.sessionNotification = new ManualResetEvent(false);

            this.sessionRegisteredWait = ThreadPool.RegisterWaitForSingleObject(
                                                            this.sessionNotification,
                                                            ServiceCallbackOnPositionAvailable,
                                                            this,   /* object state */
                                                            -1,     /* INFINITE */
                                                            true    /* ExecuteOnlyOnce */);
        }

        public void unregister()
        {
            ManualResetEvent callbackThreadComplete = new ManualResetEvent(false);
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            if (this.sessionRegisteredWait != null)
            {
                if (this.sessionRegisteredWait.Unregister(callbackThreadComplete))
                {
                    Console.WriteLine("waiting on successful unregister");
                    callbackThreadComplete.WaitOne();
                }
            }
            this.sessionRegisteredWait = null;

            long elapsed = sw.ElapsedMilliseconds;
            Console.WriteLine("Elapsed: {0} millisec", elapsed);
        }
    }

    class Program
    {
        static int Main(string[] args)
        {
            Callback obj = new Callback();

            Console.WriteLine("start");
            obj.register();

            obj.call();
            obj.unregister();

            Console.WriteLine("end");
            return 100;
        }
    }
}

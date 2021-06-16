// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using MonoDelta;

namespace HelloWorld
{
    internal class Program
    {
	class State {
	    public readonly ManualResetEventSlim mreIn;
	    public readonly ManualResetEventSlim mreOut;
	    public readonly ManualResetEventSlim mreBusy;
	    public string res;
	    private volatile bool busyChanged;

	    public State() {
		mreIn = new ManualResetEventSlim ();
		mreOut = new ManualResetEventSlim ();
		mreBusy = new ManualResetEventSlim ();
		res = "";
		busyChanged = false;
	    }


	    public bool BusyChanged {get => busyChanged ; set { busyChanged = value; mreBusy.Set ();} }

	    public void WaitForBusy () {
		mreBusy.Wait ();
		mreBusy.Reset ();
	    }

	    public string ConsumerStep () {
		mreIn.Set ();
		mreOut.Wait ();
		mreOut.Reset ();
		return res;
	    }

	    public void ProducerStep (Func<string> step) {
		mreIn.Wait ();
		mreIn.Reset ();
		res = step ();
		mreOut.Set ();
	    }
	}

        private static int Main(string[] args)
        {
            bool isMono = typeof(object).Assembly.GetType("Mono.RuntimeStructs") != null;
            Console.WriteLine($"Hello World {(isMono ? "from Mono!" : "from CoreCLR!")}");
            Console.WriteLine(typeof(object).Assembly.FullName);
            Console.WriteLine(System.Reflection.Assembly.GetEntryAssembly ());
            Console.WriteLine(System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);

	    Assembly assm = typeof (TestClass).Assembly;
	    var replacer = DeltaHelper.Make ();

	    var st = new State ();
	    var t = new Thread (MutatorThread);
	    t.Start (st);
	    var t2 = new Thread (BusyThread) { IsBackground = true };
	    t2.Start (st);

	    string res = st.ConsumerStep ();
	    if (res != "OLD STRING")
		return 1;

	    replacer.Update (assm);

	    res = st.ConsumerStep ();
	    if (res != "NEW STRING")
		return 2;

	    st.WaitForBusy ();
	    Console.WriteLine ("BusyChanged: {0}", st.BusyChanged);

	    return 0;
	}

	private static void MutatorThread (object o)
	{
	    var st = (State)o;
	    static string Step () => TestClass.TargetMethod ();
	    st.ProducerStep (Step);
	    st.ProducerStep (Step);
        }

	// This method is not affected by the update, but it calls the target
	// method which is.  Still we expect to see "BusyThread" and its
	// callees show up in the trace output when it safepoints during an
	// update.
        private static void BusyThread (object o)
        {
	    State st = (State)o;
	    string prev = TestClass.TargetMethod ();
            while (true) {
		Thread.Sleep (0);
		for (int i = 0; i < 5000; ++i) {
		    if (i % 1000 == 0) {
			string cur = TestClass.TargetMethod ();
			if (cur != prev) {
			    st.BusyChanged = true;
			    prev = cur;
			}
		    }
		}
	    }
	}

    }
}


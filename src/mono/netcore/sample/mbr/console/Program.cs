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
	    public string res;
	    public State() {
		mreIn = new ManualResetEventSlim ();
		mreOut = new ManualResetEventSlim ();
		res = "";
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

	    string res = st.ConsumerStep ();
	    if (res != "OLD STRING")
		return 1;

	    replacer.Update (assm);

	    res = st.ConsumerStep ();
	    if (res != "NEW STRING")
		return 2;

	    return 0;
	}

	private static void MutatorThread (object o)
	{
	    var st = (State)o;
	    static string Step () => TestClass.TargetMethod ();
	    st.ProducerStep (Step);
	    st.ProducerStep (Step);
	}

    }
}


using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;

class C
{
	const int StepSize = 5;
	const int Iterations = 8;

	public static void Main ()
	{
		AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(HandleException);
		var args  = new object[] {C.Iterations * C.StepSize};
		typeof (C).GetMethod ("InvokeChain", BindingFlags.NonPublic | BindingFlags.Instance).Invoke (new C (), args);
	}

	public static void HandleException (object sender, UnhandledExceptionEventArgs e)
	{
		var ex = e.ExceptionObject as Exception;

		int iterations = 0;
		while (ex != null) {
			string fullTrace = ex.StackTrace;

			string[] frames = fullTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
			// Console.WriteLine("Split into {0} lines", frames.Length);

			int count = 0;
			for (int i=0; i < frames.Length; i++)
			{
				var words = frames [i].Split((char []) null, StringSplitOptions.RemoveEmptyEntries);
				if (words.Length > 1 && words [0] == "at" && words [1].StartsWith("C.InvokeChain"))
					count++;
				// Console.WriteLine("Line: {0} {1}", frames [i], words.Length);
			}

			if (count != 0)
			{
				if (count > 1 && count != C.StepSize)
				{
					Console.WriteLine("Fail step size");
					Environment.Exit(1);
				}
				if (count == C.StepSize)
					iterations += 1;
			}

			ex = ex.InnerException;
		}

		if (iterations != C.Iterations) {
			Console.WriteLine ("{0} iterations", iterations);
			Environment.Exit (1);
		}

		// Prevents the runtime from printing the exception
		Environment.Exit (0);
	}


	[MethodImpl(MethodImplOptions.NoInlining)]
	private void InvokeChain (int depth)
	{
		if (depth == 0) {
			Base ();
		} else if (depth % C.StepSize == 0) {
			//Console.WriteLine ("InvokeChain {0} indirect", depth);
			typeof (C).GetMethod ("InvokeChain", BindingFlags.NonPublic | BindingFlags.Instance).Invoke (this, new object[] {depth - 1});
		} else {
			//Console.WriteLine ("InvokeChain {0} direct", depth);
			InvokeChain (depth - 1);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void Base ()
	{
		throw new NotImplementedException ();
	}
}

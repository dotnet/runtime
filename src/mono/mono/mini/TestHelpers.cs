using System;
using System.Threading;
using System.Reflection;

namespace MonoTests.Helpers {

	public static class FinalizerHelpers {
		private static IntPtr aptr;

		private static unsafe void NoPinActionHelper (int depth, Action act)
		{
			// Avoid tail calls
			int* values = stackalloc int [20];
			aptr = new IntPtr (values);

			if (depth <= 0)
				act ();
			else
				NoPinActionHelper (depth - 1, act);
		}

		public static void PerformNoPinAction (Action act)
		{
			Thread thr = new Thread (() => NoPinActionHelper (1024, act));
			thr.Start ();
			thr.Join ();
		}
	}

	public static class OOMHelpers {
		public static void RunTest (string test)
		{
			Assembly asm = Assembly.Load (test);

			try {
				// Support both (void) and (string[]) signatures
				if (asm.EntryPoint.GetParameters ().Length == 1)
					asm.EntryPoint.Invoke (null, new string[] { null });
				else
					asm.EntryPoint.Invoke (null, null);
			} catch (TargetInvocationException e) {
				if (e.InnerException is OutOfMemoryException)
					Console.WriteLine ("Catched oom");
				else
					throw;
			}
		}
	}
}


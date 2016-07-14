using System;
using System.Threading;

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
}


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

			if (depth <= 0) {
				//
				// When the action is called, this new thread might have not allocated
				// anything yet in the nursery. This means that the address of the first
				// object that would be allocated would be at the start of the tlab and
				// implicitly the end of the previous tlab (address which can be in use
				// when allocating on another thread, at checking if an object fits in
				// this other tlab). We allocate a new dummy object to avoid this type
				// of false pinning for most common cases.
				//
				new object ();
				act ();
			} else {
				NoPinActionHelper (depth - 1, act);
			}
		}

		public static void PerformNoPinAction (Action act)
		{
			Thread thr = new Thread (() => NoPinActionHelper (128, act));
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


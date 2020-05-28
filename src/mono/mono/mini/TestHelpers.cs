using System;
using System.Threading;
using System.Reflection;

namespace MonoTests.Helpers {

	// False pinning cases are still possible. For example the thread can die
	// and its stack reused by another thread. It also seems that a thread that
	// does a GC can keep on the stack references to objects it encountered
	// during the collection which are never released afterwards. This would
	// be more likely to happen with the interpreter which reuses more stack.
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


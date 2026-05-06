
using System;
using System.Runtime.InteropServices;
using System.Threading;

class Driver
{
	[DllImport ("libc")]
	extern static void pthread_exit (IntPtr value);

	[DllImport ("kernel32")]
	extern static void ExitThread (IntPtr value);

	static Thread GetThread1 ()
	{
		return new Thread (() => {
			/* Exit bypassing completely the runtime */
			if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
			    pthread_exit (IntPtr.Zero);
			else
			    ExitThread (IntPtr.Zero);
		});
	}

	static Thread GetThread2 ()
	{
		return new Thread (() => {
			/* Exit without returning from the ThreadStart delegate */
			Thread.CurrentThread.Abort ();
		});
	}

	static Thread GetThread3 ()
	{
		return new Thread (() => {
			/* Exit by returning from the ThreadStart delegate */
			return;
		});
	}

	static Thread[] CreateThreads ()
	{
		return new Thread [] {
			GetThread1 (),
			GetThread2 (),
			GetThread3 (),
		};
	}

	public static void Main ()
	{
		Thread[] threads;

		{
			threads = CreateThreads ();

			for (int i = 0; i < threads.Length; ++i)
				threads [i].Start ();

			for (int i = 0; i < threads.Length; ++i)
				threads [i].Join ();
		}

		{
			threads = CreateThreads ();

			for (int i = 0; i < threads.Length; ++i)
				threads [i].Start ();
		}
	}
}
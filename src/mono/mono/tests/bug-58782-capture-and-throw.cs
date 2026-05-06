using System;
using System.Runtime.InteropServices;

class Driver
{
	[DllImport ("libtest")]
	static extern void mono_test_native_to_managed_exception_rethrow (Action action);

	[DllImport ("libc")]
	static extern void _exit (int exitCode);

	static int Main (string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (sender, exception_args) =>
		{
			CustomException exc = exception_args.ExceptionObject as CustomException;
			if (exc == null) {
				Console.WriteLine ($"FAILED - Unknown exception: {exception_args.ExceptionObject}");
				_exit (1);
			}

			Console.WriteLine (exc.StackTrace);
			if (string.IsNullOrEmpty (exc.StackTrace)) {
				Console.WriteLine ("FAILED - StackTrace is null for unhandled exception.");
				_exit (2);
			} else {
				Console.WriteLine ("SUCCESS - StackTrace is not null for unhandled exception.");
				_exit (0);
			}
		};

		mono_test_native_to_managed_exception_rethrow (CaptureAndThrow);
		Console.WriteLine ("Should have exited in the UnhandledException event handler.");
		return 2;
	}

	static void CaptureAndThrow ()
	{
		try {
			Throw ();
		} catch (Exception e) {
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture (e).Throw ();
		}
	}

	static void Throw ()
	{
		throw new CustomException ("C");
	}

	class CustomException : Exception
	{
		public CustomException(string s) : base(s) {}
	}
}
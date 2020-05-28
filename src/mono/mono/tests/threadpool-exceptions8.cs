using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

public class Tests {

	public static int Main (string[] args) {
		runner ();
		return 63; // should not be reached
	}

	public static void runner () {
		// need to run the test in a domain so that we can deal with unhandled exceptions
		var ad = AppDomain.CreateDomain ("Inner Domain");
		var helperType = typeof(TaskAwaiterOnCompletedHelper);
		var helper = (TaskAwaiterOnCompletedHelper)ad.CreateInstanceAndUnwrap (helperType.Assembly.ToString(), helperType.FullName);
		var holder = new ResultHolder ();
		helper.TheTest (holder);
		// HACK: If everything went well, a thread is running in the other domain and is blocked in OnUnhandled
		// waiting for AllDone().  Don't send it.  Instead just exit without waiting.  If we send AllDone, the
		// process will terminate with a 255.  Don't try to unload the domain either, since the other thread
		// will never finish.
		// 
		//helper.AllDone();
		//AppDomain.Unload (ad);
		Environment.Exit (holder.Result);
	}

	public class ResultHolder : MarshalByRefObject  {
		public ResultHolder () { }

		public int Result { get; set; }
	}

	public class TaskAwaiterOnCompletedHelper : MarshalByRefObject {
			
		public class SpecialExn : Exception {
			public SpecialExn () : base () {}
		}
			
		public void TheTest (ResultHolder holder)
		{
			this.holder = holder;
			holder.Result = TheRealTest ();
		}

		ResultHolder holder;
		
		public int TheRealTest ()
		{
			// Regression test for https://github.com/mono/mono/issues/19166
			//
			// Check that if in a call to
			// t.GetAwaiter().OnCompleted(cb) the callback cb
			// throws, that the exception's stack trace includes
			// the method that threw and not just the task
			// machinery's frames.

			// Calling "WhenCompleted" will throw "SpecialExn"
			//
			// If "OnUhandled" is installed as an unhandled exception handler, it will
			//  capture the stack trace of the SpecialExn and allow WaitForExn() to finish waiting.
			// The stack trace is expected to include ThrowerMethodInfo

			var helper = this;
			var d = new UnhandledExceptionEventHandler (helper.OnUnhandled);
			AppDomain.CurrentDomain.UnhandledException += d;

			// this is TaskToApm.Begin (..., callback) where the callback is helper.WhenCompleted
			Task.Delay (100).GetAwaiter().OnCompleted (helper.WhenCompleted);

			var wasSet = helper.WaitForExn (10000); // wait upto 10 seconds for the task to throw

			AppDomain.CurrentDomain.UnhandledException -= d;

			if (!wasSet) {
				Console.WriteLine ("event not set, no exception thrown?");
				return 1;
			}

			return 0;

		}

		private ManualResetEventSlim coord;
		private ManualResetEventSlim coord2;

		private StackFrame[] frames;
			
		public TaskAwaiterOnCompletedHelper ()
		{
			coord = new ManualResetEventSlim ();
			coord2 = new ManualResetEventSlim ();
		}

		public MethodBase ThrowerMethodInfo => typeof(TaskAwaiterOnCompletedHelper).GetMethod (nameof (WhenCompletedThrower));

		[MethodImpl (MethodImplOptions.NoInlining)]
		public void WhenCompleted ()
		{
			WhenCompletedThrower ();
		}

		[MethodImpl (MethodImplOptions.NoInlining)]
		public void WhenCompletedThrower ()
		{
			throw new SpecialExn ();
		}

		public void OnUnhandled (object sender, UnhandledExceptionEventArgs args)
		{
			if (args.ExceptionObject is SpecialExn exn) {
				try {
					var trace = new StackTrace (exn);
					frames = trace.GetFrames ();
					if (frames == null) {
						holder.Result = 2;
						return;
					}
					Console.WriteLine ("got {0} frames ", frames.Length);
					bool found = false;
					foreach (var frame in frames) {
						if (frame.GetMethod ().Equals (ThrowerMethodInfo)) {
							found = true;
							break;
						}
					}
					if (!found) {
						Console.WriteLine ("expected to see {0} in stack trace, but it wasn't there", ThrowerMethodInfo.ToString());
						holder.Result = 3;
						return;
					}
				} finally {
					coord.Set ();
				
					coord2.Wait ();
				}
			}
		}

		public StackFrame[] CapturedStackTraceFrames => frames;


		public bool WaitForExn (int timeoutMilliseconds)
		{
			return coord.Wait (timeoutMilliseconds);
		}

		public void AllDone ()
		{
			coord2.Set ();
		}
	}
}

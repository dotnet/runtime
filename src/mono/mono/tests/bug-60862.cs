/* https://bugzilla.xamarin.com/show_bug.cgi?id=60862 */
using System;
using System.Threading;

namespace StackOverflowTest
{
	class Program
	{
		static bool fault = false;
		static Exception ex = null;

		public static int Main(string[] args)
		{
			Thread t = new Thread (Run);
			t.Start ();
			t.Join ();
			if (fault) {
				if (ex == null) {
					Console.WriteLine ("fault occured, but no exception object available");
					return 1;
				} else {
					bool is_stackoverlfow = ex is StackOverflowException;
					Console.WriteLine ("fault occured: ex = " + is_stackoverlfow);
					return is_stackoverlfow ? 0 : 3;
				}
			}
			Console.WriteLine("no fault");
			return 2;
		}

	  static void Run()
	  {
		  try {
			  Execute ();
		  } catch(Exception e) {
			  ex = e;
			  fault = true;
		  }
	  }

	  static void Execute ()
	  {
		  WaitOne ();
	  }

	  static bool WaitOne (bool killProcessOnInterrupt = false, bool throwOnInterrupt = false)
	  {
		  try {
			  return WaitOne();
		  } catch(ThreadInterruptedException e) { }
		  return false;
	  }
  }
}

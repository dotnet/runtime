using System;
using System.Threading;

public class mytest {
	public static int Main(String [] args) {
		int                   rValue = 100;
		RegisteredWaitHandle  rwh    = null;
		
		Console.WriteLine("Test AutoResetEvent for expected NullRef Exceptions");
		Console.WriteLine( );

		
		try {
			rwh.Equals(new ManualResetEvent(true));
			rValue = 4;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (rwh.Equals(new ManualResetEvent()))");
		}

		try {
			rwh.GetHashCode();
			rValue = 5;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (rwh.GetHasCode())");
		}

		// try {
		// 	rwh.GetLifetimeService();
		// 	rValue = 6;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (rwh.GetLifetimeService())");
		// }		

		try {
			rwh.GetType();
			rValue = 7;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (rwh.GetType())");
		}

		// try {
		// 	rwh.InitializeLifetimeService();
		// 	rValue = 8;
		// }
		// catch (NullReferenceException) {
		// 	Console.WriteLine("Caught NullReferenceException   (rwh.InitializeLifeTimeService())");
		// }

		try {
			rwh.ToString();
			rValue = 11;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (rwh.ToString())");
		}

		try {
			rwh.Unregister(new AutoResetEvent(true));
			rValue = 12;
		}
		catch (NullReferenceException) {
			Console.WriteLine("Caught NullReferenceException   (rwh.Unregister())");
		}
		

		Console.WriteLine("Return Code == {0}",rValue);
		return rValue;
	}
}

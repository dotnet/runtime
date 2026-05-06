using System;

public class FinallyTest {
	public static void MyHandler(object sender,
				     UnhandledExceptionEventArgs args) {

		Console.WriteLine("UnhandledExceptionEventHandler called");
	}

	public static void Main() {
		Console.WriteLine("Top level block");
		
		AppDomain domain = AppDomain.CurrentDomain;
		domain.UnhandledException +=
			new UnhandledExceptionEventHandler(MyHandler);
		
		try {
			Console.WriteLine("First try block");
			try {
				Console.WriteLine("Second try block");
				throw new Exception();
			} finally {
				Console.WriteLine("Second finally block");
			}
		} finally {
			Console.WriteLine("First finally block");
		}

		Console.WriteLine("Back to top level block");
	}
}


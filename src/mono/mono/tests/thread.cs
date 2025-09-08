
using System;
using System.Threading;

public class Test {
	private void Thread_func() {
		Console.WriteLine("In a thread!");
	}
	
	public static int Main () {
		Console.WriteLine ("Hello, World!");
		Test test = new Test();
		Thread thr=new Thread(new ThreadStart(test.Thread_func));
		thr.Start();
		Console.WriteLine("In the main line!");
		thr.Join ();
		return 0;
	}
}


using System;
using System.Threading;

public class MultiThreadExceptionTest {
	public static void MyThreadStart() {
		try {
			Console.WriteLine("{0} started", 
							  Thread.CurrentThread.Name);
			throw new Exception();
		} catch (Exception) {
		}
	}
	
	public static void Main() {
		Thread t1 = new Thread(new ThreadStart
			(MultiThreadExceptionTest.MyThreadStart));
		t1.Name = "Thread 1";
		
		Thread t2 = new Thread(new ThreadStart
			(MultiThreadExceptionTest.MyThreadStart));
		t2.Name = "Thread 2";
		
		t1.Start();
		t2.Start();
	}
}


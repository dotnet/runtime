//Test is checking the ReserveSlot function
//   If someone screws up the function we will end up
//   setting values in the wrong slots and the totals will be wrong

using System;
using System.IO;
using System.Threading;
using Xunit;

// Test Description:
// Just basic heavy reading and writing from ThreadStatic members in normal threads and threadpools threads as well.
// Ported from .NET Framework test: BaseServices\Regression\V1\Threads\ThreadStatic\threadstatic1.cs

public class Sensor 
{
	[ThreadStatic]
	static int A = 1;
	[ThreadStatic]
	static int B = 2;
	[ThreadStatic]
	static int C = 3;
	[ThreadStatic]
	static int D = 4;
	[ThreadStatic]
	static DateTime T = DateTime.Now;
	[ThreadStatic]
	static String S = "John Stockton";

	static volatile int AA = -1;
    static volatile int BB = -2;
    static volatile int CC = -3;
    static volatile int DD = -4;
    static DateTime TT = DateTime.Now;
    static String SS = "Karl Malone";
    static volatile int Result = 100;
	
	[ThreadStatic]
	static int AAA = 5;
	[ThreadStatic]
	static int BBB = 6;
	[ThreadStatic]
	static int CCC = 7;
	[ThreadStatic]
	static int DDD = 8;
	[ThreadStatic]
	static DateTime TTT = DateTime.Now;
	[ThreadStatic]
	static String SSS = "Olden Polynice";

	[Fact]
	public static int TestEntryPoint()
	{
		Console.WriteLine("Hello NBA Fans!!");
		Console.WriteLine("ThreadStatic test 2: Various reading and writing of Threadstatic variables.");
		Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}", A, B, C, D, T, S);
		Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}", AA, BB, CC, DD, TT, SS);

		ValueMess1();

		StartThread();
		if (Result != 100)
		{
			Console.WriteLine("Test Failed.");
		}
		else
		{
			Console.WriteLine("Test Succeeded.");
		}

		return Result;
	}
	
	public static void StartThread()
	{
		Thread		SimulationThread;
		Thread		SimulationThread2;
		Thread		SimulationThread3;
		Thread		SimulationThread4;
		Thread		SimulationThread5;
		Thread		SimulationThread6;

		ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadFunc2), null);
		ValueMess2();

		ThreadPool.QueueUserWorkItem(new WaitCallback(ThreadFunc2), null);

		ValueMess2();
  		SimulationThread = new Thread(new ThreadStart(ThreadFunc));
    		SimulationThread.Start();

		ValueMess2();
  		SimulationThread2 = new Thread(new ThreadStart(ThreadFunc));
    		SimulationThread2.Start();

		ValueMess2();
  		SimulationThread3 = new Thread(new ThreadStart(ThreadFunc));
    		SimulationThread3.Start();

		ValueMess2();
  		SimulationThread4 = new Thread(new ThreadStart(ThreadFunc));
    		SimulationThread4.Start();

		ValueMess2();
  		SimulationThread5 = new Thread(new ThreadStart(ThreadFunc));
    		SimulationThread5.Start();

		ValueMess2();
  		SimulationThread6 = new Thread(new ThreadStart(ThreadFunc));
    		SimulationThread6.Start();
		Thread.Sleep(500);
		SimulationThread6.Join();

		ValueMess1();
		Console.WriteLine("Main A: {0}, {1}, {2}, {3}, {4}, {5}", A, B, C, D, T, S);
		Console.WriteLine("Main AA: {0}, {1}, {2}, {3}, {4}, {5}", AA, BB, CC, DD, TT, SS);
		Console.WriteLine("Main AAA: {0}, {1}, {2}, {3}, {4}, {5}", AAA, BBB, CCC, DDD, TTT, SSS);
		int Y = Thread.CurrentThread.GetHashCode();
		int X = (A ^ Y) | (B ^ Y) | (C ^ Y) |(D ^ Y)| (AAA ^ Y) | (BBB ^ Y) | (CCC ^ Y) |(DDD ^ Y);
		Console.WriteLine("X: {0}, {1}", X, S);
		
		if (X != 0)
		{
			Console.WriteLine("Something went wrong in thread: {0}", S);
			Result = 700 + Y;
		}
	}

	public static void ThreadFunc() 
	{
		Console.WriteLine("ThreadStarted.. {0}", Thread.CurrentThread.GetHashCode().ToString());
		Console.WriteLine("A: {0}, {1}, {2}, {3}, {4}, {5}", A, B, C, D, T, S);
		Console.WriteLine("AA: {0}, {1}, {2}, {3}, {4}, {5}", AA, BB, CC, DD, TT, SS);
		Console.WriteLine("AAA: {0}, {1}, {2}, {3}, {4}, {5}", AAA, BBB, CCC, DDD, TTT, SSS);

		ValueMess1();

		Console.WriteLine("A: {0}, {1}, {2}, {3}, {4}, {5}", A, B, C, D, T, S);
		Console.WriteLine("AA: {0}, {1}, {2}, {3}, {4}, {5}", AA, BB, CC, DD, TT, SS);
		Console.WriteLine("AAA: {0}, {1}, {2}, {3}, {4}, {5}", AAA, BBB, CCC, DDD, TTT, SSS);
	
		int Y = Thread.CurrentThread.GetHashCode();
		int X = (A ^ Y) | (B ^ Y) | (C ^ Y) |(D ^ Y)| (AAA ^ Y) | (BBB ^ Y) | (CCC ^ Y) |(DDD ^ Y);
		Console.WriteLine("X: {0}, {1}", X, S);
		
		if (X != 0)
		{
			Console.WriteLine("Something went wrong in thread: {0}", S);
			Result = 700 + Y;
		}
	}

	public static void ThreadFunc2(Object O) 
	{
		Console.WriteLine("Threadpool Started.. {0}", Thread.CurrentThread.GetHashCode().ToString());
		Console.WriteLine("TP A: {0}, {1}, {2}, {3}, {4}, {5}", A, B, C, D, T, S);
		Console.WriteLine("TP AA: {0}, {1}, {2}, {3}, {4}, {5}", AA, BB, CC, DD, TT, SS);
		Console.WriteLine("TP AAA: {0}, {1}, {2}, {3}, {4}, {5}", AAA, BBB, CCC, DDD, TTT, SSS);

		ValueMess1();

		Console.WriteLine("TP A: {0}, {1}, {2}, {3}, {4}, {5}", A, B, C, D, T, S);
		Console.WriteLine("TP AA: {0}, {1}, {2}, {3}, {4}, {5}", AA, BB, CC, DD, TT, SS);
		Console.WriteLine("TP AAA: {0}, {1}, {2}, {3}, {4}, {5}", AAA, BBB, CCC, DDD, TTT, SSS);

		int Y = Thread.CurrentThread.GetHashCode();
		int X = (A ^ Y) | (B ^ Y) | (C ^ Y) |(D ^ Y)| (AAA ^ Y) | (BBB ^ Y) | (CCC ^ Y) |(DDD ^ Y);
		Console.WriteLine("X: {0}, {1}", X, S);
		
		if (X != 0)
		{
			Console.WriteLine("Something went wrong in thread: {0}", S);
			Result = 700 + Y;
		}
	}

	public static void ValueMess1()
	{
		A = Thread.CurrentThread.GetHashCode();
		B = Thread.CurrentThread.GetHashCode();
		C = Thread.CurrentThread.GetHashCode();
		D = Thread.CurrentThread.GetHashCode();
		T = DateTime.Now;
		S = Thread.CurrentThread.GetHashCode().ToString();

		AA = Thread.CurrentThread.GetHashCode();
		BB = Thread.CurrentThread.GetHashCode();
		CC = Thread.CurrentThread.GetHashCode();
		DD = Thread.CurrentThread.GetHashCode();
		TT = DateTime.Now;
		SS = Thread.CurrentThread.GetHashCode().ToString();

		AAA = Thread.CurrentThread.GetHashCode();
		BBB = Thread.CurrentThread.GetHashCode();
		CCC = Thread.CurrentThread.GetHashCode();
		DDD = Thread.CurrentThread.GetHashCode();
		TTT = DateTime.Now;
		SSS = Thread.CurrentThread.GetHashCode().ToString();
	}

	public static void ValueMess2()
	{
		A = -Thread.CurrentThread.GetHashCode();
		B = -Thread.CurrentThread.GetHashCode();
		C = -Thread.CurrentThread.GetHashCode();
		D = -Thread.CurrentThread.GetHashCode();
		T = DateTime.Now;
		S = Thread.CurrentThread.GetHashCode().ToString();

		AA = -Thread.CurrentThread.GetHashCode();
		BB = -Thread.CurrentThread.GetHashCode();
		CC = -Thread.CurrentThread.GetHashCode();
		DD = -Thread.CurrentThread.GetHashCode();
		TT = DateTime.Now;
		SS = Thread.CurrentThread.GetHashCode().ToString();

		AAA = -Thread.CurrentThread.GetHashCode();
		BBB = -Thread.CurrentThread.GetHashCode();
		CCC = -Thread.CurrentThread.GetHashCode();
		DDD = -Thread.CurrentThread.GetHashCode();
		TTT = DateTime.Now;
		SSS = Thread.CurrentThread.GetHashCode().ToString();
	}
}

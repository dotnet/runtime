using System;
using System.Threading;

interface IGen<T>
{
	void Target(object p);
	T Dummy(T t);
}

class GenInt : IGen<int>
{
	public int Dummy(int t) { return t; }

	public void Target(object p)
	{			
		if (Test.Xcounter>=Test.nThreads)
		{
			ManualResetEvent evt = (ManualResetEvent) p;	
			evt.Set();
		}
		else
		{
			Interlocked.Increment(ref Test.Xcounter);	
		}
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent evt = new ManualResetEvent(false);		
		
		IGen<int> obj = new GenInt();

		TimerCallback tcb = new TimerCallback(obj.Target);
		Timer timer = new Timer(tcb,evt,Test.delay,Test.period);
	
		evt.WaitOne();
		timer.Dispose();
		Test.Eval(Test.Xcounter>=Test.nThreads);
		Test.Xcounter = 0;
	}
}

class GenDouble : IGen<double>
{
	public double Dummy(double t) { return t; }

	public void Target(object p)
	{			
		if (Test.Xcounter>=Test.nThreads)
		{
			ManualResetEvent evt = (ManualResetEvent) p;	
			evt.Set();
		}
		else
		{
			Interlocked.Increment(ref Test.Xcounter);	
		}
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent evt = new ManualResetEvent(false);		
		
		IGen<double> obj = new GenDouble();

		TimerCallback tcb = new TimerCallback(obj.Target);
		Timer timer = new Timer(tcb,evt,Test.delay,Test.period);
	
		evt.WaitOne();
		timer.Dispose();
		Test.Eval(Test.Xcounter>=Test.nThreads);
		Test.Xcounter = 0;
	}
}

class GenString : IGen<string>
{
	public string Dummy(string t) { return t; }

	public void Target(object p)
	{			
		if (Test.Xcounter>=Test.nThreads)
		{
			ManualResetEvent evt = (ManualResetEvent) p;	
			evt.Set();
		}
		else
		{
			Interlocked.Increment(ref Test.Xcounter);	
		}
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent evt = new ManualResetEvent(false);		
		
		IGen<string> obj = new GenString();

		TimerCallback tcb = new TimerCallback(obj.Target);
		Timer timer = new Timer(tcb,evt,Test.delay,Test.period);
	
		evt.WaitOne();
		timer.Dispose();
		Test.Eval(Test.Xcounter>=Test.nThreads);
		Test.Xcounter = 0;
	}
}

class GenObject : IGen<object>
{
	public object Dummy(object t) { return t; }

	public void Target(object p)
	{			
		if (Test.Xcounter>=Test.nThreads)
		{
			ManualResetEvent evt = (ManualResetEvent) p;	
			evt.Set();
		}
		else
		{
			Interlocked.Increment(ref Test.Xcounter);	
		}
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent evt = new ManualResetEvent(false);		
		
		IGen<object> obj = new GenObject();

		TimerCallback tcb = new TimerCallback(obj.Target);
		Timer timer = new Timer(tcb,evt,Test.delay,Test.period);
	
		evt.WaitOne();
		timer.Dispose();
		Test.Eval(Test.Xcounter>=Test.nThreads);
		Test.Xcounter = 0;
	}
}

class GenGuid : IGen<Guid>
{
	public Guid Dummy(Guid t) { return t; }

	public void Target(object p)
	{			
		if (Test.Xcounter>=Test.nThreads)
		{
			ManualResetEvent evt = (ManualResetEvent) p;	
			evt.Set();
		}
		else
		{
			Interlocked.Increment(ref Test.Xcounter);	
		}
	}
	
	public static void ThreadPoolTest()
	{
		ManualResetEvent evt = new ManualResetEvent(false);		
		
		IGen<Guid> obj = new GenGuid();

		TimerCallback tcb = new TimerCallback(obj.Target);
		Timer timer = new Timer(tcb,evt,Test.delay,Test.period);
	
		evt.WaitOne();
		timer.Dispose();
		Test.Eval(Test.Xcounter>=Test.nThreads);
		Test.Xcounter = 0;
	}
}

public class Test
{
	public static int delay = 0;
	public static int period = 2;
	public static int nThreads = 5;
	public static int counter = 0;
	public static int Xcounter = 0;
	public static bool result = true;
	public static void Eval(bool exp)
	{
		counter++;
		if (!exp)
		{
			result = exp;
			Console.WriteLine("Test Failed at location: " + counter);
		}
	
	}
	
	public static int Main()
	{
	
		GenInt.ThreadPoolTest();
		GenDouble.ThreadPoolTest();
		GenString.ThreadPoolTest();
		GenObject.ThreadPoolTest(); 
		GenGuid.ThreadPoolTest(); 	
		
		if (result)
		{
			Console.WriteLine("Test Passed");
			return 100;
		}
		else
		{
			Console.WriteLine("Test Failed");
			return 1;
		}
	}
}		



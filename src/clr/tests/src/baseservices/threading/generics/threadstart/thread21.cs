using System;
using System.Threading;


interface IGen<T>
{
	void Target<U>();
	T Dummy(T t);
}

class Gen<T> : IGen<T>
{
	public T Dummy(T t) {return t;}

	public virtual void Target<U>()
	{		
		//dummy line to avoid warnings
		Test.Eval(typeof(U)!=null);
		Interlocked.Increment(ref Test.Xcounter);
	}
	public static void ThreadPoolTest<U>()
	{
		Thread[] threads = new Thread[Test.nThreads];
		IGen<T> obj = new Gen<T>();

		for (int i = 0; i < Test.nThreads; i++)
		{	
			threads[i]  = new Thread(new ThreadStart(obj.Target<U>));
			threads[i].Start();
		}

		for (int i = 0; i < Test.nThreads; i++)
		{	
			threads[i].Join();
		}
		
		Test.Eval(Test.Xcounter==Test.nThreads);
		Test.Xcounter = 0;
	}
}

public class Test
{
	public static int nThreads =50;
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
		Gen<int>.ThreadPoolTest<object>();
		Gen<double>.ThreadPoolTest<string>();
		Gen<string>.ThreadPoolTest<Guid>();
		Gen<object>.ThreadPoolTest<int>(); 
		Gen<Guid>.ThreadPoolTest<double>(); 

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



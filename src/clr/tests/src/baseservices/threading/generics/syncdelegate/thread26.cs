using System;
using System.Threading;

struct Gen<T> 
{
	public static void Target()
	{			
		Interlocked.Increment(ref Test.Xcounter);
	}
	public static void DelegateTest()
	{
		ThreadStart d = new ThreadStart(Gen<T>.Target);
		
		
		d();
		Test.Eval(Test.Xcounter==1);
		Test.Xcounter = 0;
	}
}

public class Test
{
	public static int nThreads = 50;
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
		Gen<int>.DelegateTest();
		Gen<double>.DelegateTest();
		Gen<string>.DelegateTest();
		Gen<object>.DelegateTest(); 
		Gen<Guid>.DelegateTest(); 

		Gen<int[]>.DelegateTest(); 
		Gen<double[,]>.DelegateTest();
		Gen<string[][][]>.DelegateTest(); 
		Gen<object[,,,]>.DelegateTest();
		Gen<Guid[][,,,][]>.DelegateTest();

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



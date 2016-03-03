using System;
using System.Threading;


public struct ValX1<T> {}
public class RefX1<T> {}
struct Gen<T> 
{
	public static void TryEnterTest()
	{
// #pragma warning disable 219
// 		Gen<T> inst = new Gen<T>();
// #pragma warning restore
		Type monitor = typeof(Gen<T>);
		TestHelper myHelper = new TestHelper(Test.nThreads);
		// MonitorDelegateTS[] consumer = new MonitorDelegateTS[Test.nThreads];
		// for(int i=0;i<consumer.Length;i++){
		// 	consumer[i] = new MonitorDelegateTS(myHelper.ConsumerTryEnter);
		// 	consumer[i].BeginInvoke(monitor,100,null,null);
		// }

		for (int i = 0; i < Test.nThreads; i++)
		{
			ThreadPool.QueueUserWorkItem(state =>
			{
				myHelper.ConsumerTryEnter(monitor, 100);
			});
		}

		for(int i=0;i<6;i++){
			if(myHelper.m_Event.WaitOne(10000))//,true))
				break;
			if(myHelper.Error == true)
				break;
		}
		Test.Eval(!myHelper.Error);
	}	
}

public class Test
{
	public static int nThreads = 25;
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
		Gen<int>.TryEnterTest();	
		/*Gen<double>.TryEnterTest();
		Gen<string>.TryEnterTest();
		Gen<object>.TryEnterTest();
		Gen<Guid>.TryEnterTest();

		Gen<int[]>.TryEnterTest();
		Gen<double[,]>.TryEnterTest();
		Gen<string[][][]>.TryEnterTest();
		Gen<object[,,,]>.TryEnterTest();
		Gen<Guid[][,,,][]>.TryEnterTest();

		Gen<RefX1<int>[]>.TryEnterTest();
		Gen<RefX1<double>[,]>.TryEnterTest();
		Gen<RefX1<string>[][][]>.TryEnterTest();
		Gen<RefX1<object>[,,,]>.TryEnterTest();
		Gen<RefX1<Guid>[][,,,][]>.TryEnterTest();

		Gen<ValX1<int>[]>.TryEnterTest();
		Gen<ValX1<double>[,]>.TryEnterTest();
		Gen<ValX1<string>[][][]>.TryEnterTest();
		Gen<ValX1<object>[,,,]>.TryEnterTest();
		Gen<ValX1<Guid>[][,,,][]>.TryEnterTest();*/

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



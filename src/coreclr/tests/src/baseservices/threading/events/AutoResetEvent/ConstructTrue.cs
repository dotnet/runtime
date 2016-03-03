using System;
using System.Threading;
using System.Diagnostics;

public class ARETestClass
{

	public static int Main()
	{
		ARETestClass testAutoReset = new ARETestClass();

		int ret = testAutoReset.Run();
		Console.WriteLine(ret == 100 ? "Test Passed":"Test Failed");
		return ret;
	}

	public int Run()
	{
		AutoResetEvent are = new AutoResetEvent(true);
		Stopwatch sw = new Stopwatch();		
		
		if(!are.WaitOne(0))//,false))
{
			Console.WriteLine("Signalled event should always return true on call to !are.WaitOne(0,false).");
			return -3;
		}
		
		sw.Start();
		bool ret = are.WaitOne(1000);//,false);
		sw.Stop();
		//We should never get signaled
		if(ret)
		{
			Console.WriteLine("AutoResetEvent should never be signalled.");
			return -1;
		}

		if(sw.ElapsedMilliseconds < 900)
		{
			Console.WriteLine("It should take at least 900 milliseconds to call bool ret = are.WaitOne(1000,false);.");
			Console.WriteLine("sw.ElapsedMilliseconds = " + sw.ElapsedMilliseconds);			
			return -2;
		}
		return 100;
		
	}
}
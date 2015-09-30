using System;
using System.Threading;

public class Test{

	public static int Main()
	{
		int rValue = 0;		
		int foo = 1;
		
		try{
			Monitor.Exit(foo);
			rValue = -1;
		}
		catch(SynchronizationLockException)
		{
			rValue = 100;
		}
		Console.WriteLine(100 == rValue ? "Test Passed":"Test Failed");
		return rValue;
	}
}
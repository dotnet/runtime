

using System;

public class GenException<T> : Exception {}

public class Gen 
{
	public static void ExceptionTest<Ex,T>(Ex e)  where Ex : Exception where T : Exception
	{
		try
		{
			throw e;
		}
		catch(T)
		{
				Console.WriteLine("Caught Wrong Exception");
				Test.Eval(false);			
		}
		catch(Ex E)
		{
			Test.Eval(Object.ReferenceEquals(e,E));
		
		}
	}
}

public class Test
{
	public static int counter = 0;
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
		Gen.ExceptionTest<Exception,InvalidOperationException>(new Exception());
		Gen.ExceptionTest<Exception,GenException<int>>(new Exception());
		Gen.ExceptionTest<Exception,GenException<string>>(new Exception());
		Gen.ExceptionTest<GenException<int>,GenException<string>>(new GenException<int>());
		Gen.ExceptionTest<GenException<string>,GenException<int>>(new GenException<string>());
		Gen.ExceptionTest<GenException<object>,GenException<string>>(new GenException<object>());
		
		if (result)
		{
			Console.WriteLine("Test Passed");
			return 100;
		}
		else
		{
			Console.WriteLine("Test Failed");
			return 0;
		}
	}
		
}


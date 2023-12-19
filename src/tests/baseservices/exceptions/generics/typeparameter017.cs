// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// <Area> Generics - Expressions - specific catch clauses </Area>
// <Title> 
// catch type parameters bound by Exception or a subclass of it in the form catch(T)
// </Title>
// <RelatedBugs> VSW48886 </RelatedBugs>  

//<Expects Status=skip>  </Expects>
//<Expects Status=success></Expects>

// <Code> 

using System;
using Xunit;

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
				Test_typeparameter017.Eval(false);			
		}
		catch(Ex E)
		{
			Test_typeparameter017.Eval(Object.ReferenceEquals(e,E));
		
		}
	}
}

public class Test_typeparameter017
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
	
	[Fact]
	public static int TestEntryPoint()
	{
		Gen.ExceptionTest<Exception,InvalidOperationException>(new Exception());
		Gen.ExceptionTest<Exception,GenException<int>>(new Exception());
		Gen.ExceptionTest<Exception,GenException<string>>(new Exception());
		Gen.ExceptionTest<GenException<int>,GenException<string>>(new GenException<int>());
		Gen.ExceptionTest<GenException<string>,GenException<int>>(new GenException<string>());
		Gen.ExceptionTest<GenException<object>,GenException<string>>(new GenException<object>());
		Gen.ExceptionTest<GenException<string>,GenException<object>>(new GenException<string>());
		
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

// </Code>

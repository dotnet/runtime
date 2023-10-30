// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// <Area> Generics - Expressions - specific catch clauses </Area>
// <Title> 
// catch type parameters bound by Exception or a subclass of it in the form catch(T)
// </Title>
// <RelatedBugs> </RelatedBugs>  

//<Expects Status=success></Expects>

// <Code> 

using System;
using Xunit;

public class GenException<T> : Exception {}

public class GenExceptionSub<T> : GenException<T> {}

public struct Gen
{
	public void ExceptionTest<Ex,T>(Ex e) where Ex : GenException<T>
	{
		try
		{
			throw e;
		}
		catch(Ex E)
		{
			Test_typeparameter016.Eval(Object.ReferenceEquals(e,E));
		}
		catch
		{
			Console.WriteLine("Caught Wrong Exception");
			Test_typeparameter016.Eval(false);
		}
	}
}

public class Test_typeparameter016
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
		new Gen().ExceptionTest<GenException<int>,int>(new GenExceptionSub<int>());
		new Gen().ExceptionTest<GenException<string>,string>(new GenExceptionSub<string>());
		new Gen().ExceptionTest<GenException<Guid>,Guid>(new GenExceptionSub<Guid>());
		
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// <Area> Generics - Expressions - specific catch clauses </Area>
// <Title> 
// catch type parameters bound by Exception or a subclass of it in the form catch(T)
// </Title>
// <RelatedBugs> </RelatedBugs>  

//<Expects Status=success></Expects>

// <Code> 

using System;

public class GenException<T> : Exception {}

public class GenExceptionSub<T> : GenException<T> {}

public class Gen
{
	public static void ExceptionTest<Ex,T>(Ex e) where Ex : GenException<T>
	{
		try
		{
			throw e;
		}
		catch(Ex E)
		{
			Test.Eval(Object.ReferenceEquals(e,E));
		}
		catch
		{
			Console.WriteLine("Caught Wrong Exception");
			Test.Eval(false);
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
		Gen.ExceptionTest<GenException<int>,int>(new GenExceptionSub<int>());
		Gen.ExceptionTest<GenException<string>,string>(new GenExceptionSub<string>());
		Gen.ExceptionTest<GenException<Guid>,Guid>(new GenExceptionSub<Guid>());
		
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

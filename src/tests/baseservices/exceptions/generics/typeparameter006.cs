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

public struct ValX0 {}
public struct ValY0 {}
public struct ValX1<T> {}
public struct ValY1<T> {}
public struct ValX2<T,U> {}
public struct ValY2<T,U>{}
public struct ValX3<T,U,V>{}
public struct ValY3<T,U,V>{}
public class RefX0 {}
public class RefY0 {}
public class RefX1<T> {}
public class RefY1<T> {}
public class RefX2<T,U> {}
public class RefY2<T,U>{}
public class RefX3<T,U,V>{}
public class RefY3<T,U,V>{}


public class GenException<T> : Exception {}

public struct Gen 
{
	public static void ExceptionTest<Ex>(Ex e) where Ex : Exception
	{
		try
		{
			throw e;
		}
		catch(Ex E)
		{
			Test_typeparameter006.Eval(Object.ReferenceEquals(e,E));
		}
		catch
		{
			Console.WriteLine("Caught Wrong Exception");
			Test_typeparameter006.Eval(false);
		}
	}
}

public class Test_typeparameter006
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
		Gen.ExceptionTest<Exception>(new Exception()); 
		Gen.ExceptionTest<Exception>(new InvalidOperationException());
		Gen.ExceptionTest<Exception>(new GenException<int>());
		Gen.ExceptionTest<Exception>(new GenException<string>());
		Gen.ExceptionTest<Exception>(new GenException<Guid>());
		Gen.ExceptionTest<Exception>(new GenException<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>());
		
		Gen.ExceptionTest<InvalidOperationException>(new InvalidOperationException());

		Gen.ExceptionTest<GenException<int>>(new GenException<int>());
		Gen.ExceptionTest<GenException<string>>(new GenException<string>());
		Gen.ExceptionTest<GenException<Guid>>(new GenException<Guid>());
		Gen.ExceptionTest<GenException<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>>(new GenException<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>());
		
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

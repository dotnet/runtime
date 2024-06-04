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

public class Gen<Ex> where Ex : Exception 
{
	public static void ExceptionTest(Ex e)
	{
		try
		{
			throw e;
		}
		catch(Ex E)
		{
			Test_typeparameter001.Eval(Object.ReferenceEquals(e,E));
		}
		catch
		{
			Console.WriteLine("Caught Wrong Exception");
			Test_typeparameter001.Eval(false);
		}
	}
}

public class Test_typeparameter001
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
		Gen<Exception>.ExceptionTest(new Exception()); 
		Gen<Exception>.ExceptionTest(new InvalidOperationException());
		Gen<Exception>.ExceptionTest(new GenException<int>());
		Gen<Exception>.ExceptionTest(new GenException<string>());
		Gen<Exception>.ExceptionTest(new GenException<Guid>());
		Gen<Exception>.ExceptionTest(new GenException<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>());
		
		Gen<InvalidOperationException>.ExceptionTest(new InvalidOperationException());

		Gen<GenException<int>>.ExceptionTest(new GenException<int>());
		Gen<GenException<string>>.ExceptionTest(new GenException<string>());
		Gen<GenException<Guid>>.ExceptionTest(new GenException<Guid>());
		Gen<GenException<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>>.ExceptionTest(new GenException<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>());
		
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

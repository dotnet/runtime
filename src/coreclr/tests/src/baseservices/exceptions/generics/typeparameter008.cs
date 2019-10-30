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
	public void ExceptionTest<Ex>(Ex e) where Ex : Exception
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
		new Gen().ExceptionTest<Exception>(new Exception()); 
		new Gen().ExceptionTest<Exception>(new InvalidOperationException());
		new Gen().ExceptionTest<Exception>(new GenException<int>());
		new Gen().ExceptionTest<Exception>(new GenException<string>());
		new Gen().ExceptionTest<Exception>(new GenException<Guid>());
		new Gen().ExceptionTest<Exception>(new GenException<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>());
		
		new Gen().ExceptionTest<InvalidOperationException>(new InvalidOperationException());

		new Gen().ExceptionTest<GenException<int>>(new GenException<int>());
		new Gen().ExceptionTest<GenException<string>>(new GenException<string>());
		new Gen().ExceptionTest<GenException<Guid>>(new GenException<Guid>());
		new Gen().ExceptionTest<GenException<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>>(new GenException<ValX3<ValX1<int[][,,,]>,ValX2<object[,,,][][],Guid[][][]>,ValX3<double[,,,,,,,,,,],Guid[][][][,,,,][,,,,][][][],string[][][][][][][][][][][]>>>());
		
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

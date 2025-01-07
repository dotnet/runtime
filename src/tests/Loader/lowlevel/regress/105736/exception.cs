// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public struct GS1<T>
{
	public T t;
	public GS1(T t) 
	{ 
		this.t = t;
	}
}

public abstract class Base
{
	public abstract T vMeth1<T>(T t) ;
	public abstract T vMeth2<T>(out T t);
}

public class Sub : Base
{	
	public override  T vMeth1<T>(T t) 
	{ 	
		return t; 
	}

	public override T vMeth2<T>(out T t) 
	{ 
		t = default(T);
		return t; 
	}
}

public class Test_exception
{
	[Fact]
	public static int TestEntryPoint()
	{
		try
		{
			GS1<string> TestValue = new GS1<string>("string");
			Sub obj = new Sub();

			obj.vMeth1<GS1<string>>(TestValue);
			obj.vMeth2<GS1<string>>(out TestValue);
			System.Console.WriteLine(TestValue.t);

			// no exceptions caught
			Console.WriteLine("PASS");
			return 100;
		}
		catch (System.NotSupportedException ex)
		{
			Console.WriteLine("{0} \n Caught unexpected System.NotSupportedException exception.", ex);
			Console.WriteLine("FAIL");
			return 101;
		}
		catch (System.Exception ex)
		{
			Console.WriteLine("{0} \n Caught unexpected exception.", ex);
			Console.WriteLine("FAIL");
			return 101;
		}
	}
}

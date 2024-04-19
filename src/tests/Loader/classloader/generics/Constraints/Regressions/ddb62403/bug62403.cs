// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;


public class TestClass
{
    public static void N<U,V>() where U : V { }

    public static void M<U,V>() where U : V
    {
        N<U,U>();
    }

    [Fact]
    public static int TestEntryPoint()
    {
	try {
		M<object,object>();
		Console.WriteLine("PASS");
		return 100;
	} catch (Exception e)
	{
		Console.WriteLine("CATCH UNEXPECTED EXCEPTION: " + e.ToString());
		Console.WriteLine("FAIL");
		return 99;
	}
    }
}

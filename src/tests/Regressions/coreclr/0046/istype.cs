// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

public struct MyStruct {}

public class IsType
{
	[Fact]
	public static int TestEntryPoint()
	{
		MyStruct? m = default(MyStruct);
		object o = m;

		if (null == m)
		{
			Console.WriteLine("FAIL: o is null");
			return 0;
		}

		if (o is MyStruct)
		{
			Console.WriteLine("PASS");
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL: o is not MyStruct");
			return 0;
		}
	}
}

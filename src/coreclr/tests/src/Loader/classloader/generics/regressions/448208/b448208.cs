// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// this test is regression test for VSW 448208
// A program AVed if there was a readonly static in a generic type


using System;

public class GenType1<T>
{
	static readonly int s_i = 0;

	public static bool foo()
	{
		return s_i == 0;
	}
	
}

public class Test
{
	public static int Main()
	{
		GenType1<int>.foo();
		Console.WriteLine("PASS");
		return 100;
	}
}

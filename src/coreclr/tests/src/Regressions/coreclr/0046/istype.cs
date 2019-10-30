// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public struct MyStruct {}

public class IsType
{
	public static int Main()
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

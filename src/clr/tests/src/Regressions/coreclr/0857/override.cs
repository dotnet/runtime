// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class Test
{
	public static int Main()
	{
		Test t = new Test();

		if (t.ToString().Equals("Hi"))
		{
			return 100;
		}
		

		return 0;
	}

	public override string ToString()
	{
		return "Hi";
	}
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

public class Hello
{
	public static int Main()
	{
		Console.WriteLine("Make sure to set the following env vars:");
		Console.WriteLine("  COMPlus_LogEnable=1");
		Console.WriteLine("  COMPlus_LogToConsole=1");
		Console.WriteLine("This test ensures that logging is working");
		return 100;
	}
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;

public class Hello
{
	public static int Main()
	{
		Console.WriteLine("Make sure to set the following env vars:");
		Console.WriteLine("  DOTNET_LogEnable=1");
		Console.WriteLine("  DOTNET_LogToConsole=1");
		Console.WriteLine("This test ensures that logging is working");
		return 100;
	}
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;
using TestLibrary;

public class Nullable
{

	public static bool BoxUnboxToNQ(object o)
	{
		return ((int)(ValueType)o == (int)55);
	}

	public static bool Run()
	{
		int? i = 55;
		
		return BoxUnboxToNQ(i);
	}

 [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
	[Fact]
	public static int TestEntryPoint()
	{
		if (Run())
		{
			Console.WriteLine("PASS");
			return 100;
		}
		else
		{
			Console.WriteLine("FAIL");
			return 0;
		}
	}
}

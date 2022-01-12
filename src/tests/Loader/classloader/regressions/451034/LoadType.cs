// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this is regression test for VSW 451034
// ngening the assembly and running it resulted in AV

using System;

public class Test_LoadType
{
	public static int Main()
	{
		S s = CReloc5<char>.s;
			
		Console.WriteLine("PASS");
		return 100;
	
	}
}

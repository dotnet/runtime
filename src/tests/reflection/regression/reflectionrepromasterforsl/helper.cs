// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;

public class Helper
{
    [SecurityCritical]
    public Helper(string s)
    {
        Console.WriteLine("FAIL: Public Helper..ctor(string) is called!");
    }

    [SecurityCritical]
    internal Helper()
    {
        Console.WriteLine("FAIL: Internal Helper..ctor() is called!");
    }

    [SecurityCritical]
    public static void PublicSecurityCriticalMethod()
    {
        Console.WriteLine("FAIL: Helper.PublicSecurityCriticalMethod is called!");
    }

	[SecurityCritical]
	internal static void InternalSecurityCriticalMethod()
	{
		Console.WriteLine("FAIL: Helper.InternalSecurityCriticalMethod is called!");
	}
}
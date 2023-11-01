// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class test
{
	private object _state = null;
	private static test _obj = null;

	[Fact]
	public static int TestEntryPoint()
	{
        int ret = 0;
		try
		{
            try
            {
                Object text = _obj._state;
            }
            catch (NullReferenceException)
            {
                ret = 100;
            }
        }
		catch (Exception ex)
		{
			Console.WriteLine("Invalid read = {0} (should be NullRef)",ex.GetType());
            ret = 10;
        }
        Console.WriteLine(100 == ret ? "Test Passed" : "Test Failed");
        return ret;
    }
}


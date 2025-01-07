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
        Object temp = new Object();
		try
		{
            try
            {
                _obj._state = temp;
            }
            catch (NullReferenceException)
            {
                ret = 100;
            }
        }
		catch (Exception ex)
		{
			Console.WriteLine("Invalid write (assigning an object) = {0} (should be NullRef)",ex.GetType());
            ret = 10;
        }
        Console.WriteLine(100 == ret ? "Test Passed" : "Test Failed");
        return ret;
    }
}

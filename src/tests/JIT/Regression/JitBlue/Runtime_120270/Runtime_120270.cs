// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Xunit;

public class Runtime_120270
{
    [Fact]
    public static int TestEntryPoint()
    {
        int i = 0;
        bool failed = false;
        try
        {
            while (i < 100_000)
            {
                i++;
                var values = new[] { DayOfWeek.Saturday }.Cast<int>();
                foreach (var value in values)
                {
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(i);
            Console.WriteLine(ex);
            failed = true;
        }

        return failed ? -1 : 100;
    }
}


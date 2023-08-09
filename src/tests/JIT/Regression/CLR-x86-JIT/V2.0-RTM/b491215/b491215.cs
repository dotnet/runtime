// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public class Test
{
    internal static void IsType<T>(object o, bool expectedValue)
    {
        bool isType = o is T;
        Console.WriteLine("{0} is {1} (expected {2}): {3}", o.GetType(), typeof(T), expectedValue, isType);
        if (expectedValue != isType)
            throw new Exception("Casting failed");
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Object o = null;

        try
        {
            o = new ArgumentException();
            IsType<Exception>(o, true);
            IsType<IEnumerable>(o, false);
            IsType<IEnumerable<int>>(o, false);

            o = new Dictionary<string, bool>();
            IsType<Exception>(o, false);
            IsType<IEnumerable>(o, true);
            IsType<IEnumerable<KeyValuePair<string, bool>>>(o, true);

            o = new List<int>();
            IsType<Exception>(o, false);
            IsType<IEnumerable>(o, true);
            IsType<IEnumerable<int>>(o, true);

            Console.WriteLine("Test SUCCESS");
            return 100;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine("Test FAILED");
            return 101;
        }
    }
}

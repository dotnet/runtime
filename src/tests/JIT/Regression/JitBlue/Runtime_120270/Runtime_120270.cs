// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

class Base{}
class Derived : Base {}

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

    [Fact]
    public static int TestEntryPoint2()
    {
        bool failed = false;
        try
        {
            Foo<Base>([new Derived()]);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            failed = true;
        }
        return failed ? -1 : 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Foo<T>(object[] x)
    {
        int i = 0;
        while (i < 100_000)
        {
            i++;
            var values = x.Cast<T>();
            foreach (var value in values)
            {
            }
        }
    }

    [Fact]
    public static int TestEntryPoint3()
    {
        for (int i = 0; i < 100_000; i++)
        {
            Derived[] d = [new Derived()];
            IEnumerable<Base> e = d;
            IEnumerator<Base> en = e.GetEnumerator();
            string s = en.GetType().ToString();

            if (i == 0)
            {
                Console.WriteLine($"Enumerator type: {s}");
            }

            if (!s.Equals("System.SZGenericArrayEnumerator`1[Base]"))
            {
                Console.WriteLine($"Enumerator type changed at {i} to {s}");
                return -1;
            }
        }
        return 100;
    }
}


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class Foo
{

    [Fact]
    public static int TestEntryPoint()
    {

        const int a = 0x7fffffff; // highest positive int
        const int b = -a - 1; // lowest negative int
        int intMin = b;

        const long d = 0x7fffffffffffffff; // highest positive long
        const long e = -d - 1; // lowest negative long
        long longMin = e;

        long r;
        try
        {
            r = intMin / -1;
            Console.WriteLine("ok");
        }
        catch (Exception f)
        {
            Console.WriteLine(f);
        }
        try
        {
            r = intMin % -1;
            Console.WriteLine("ok");
        }
        catch (Exception f)
        {
            Console.WriteLine(f);
        }
        try
        {
            r = longMin / -1;
            Console.WriteLine("ok");
        }
        catch (Exception f)
        {
            Console.WriteLine(f);
        }
        try
        {
            r = longMin % -1;
            Console.WriteLine("ok");
        }
        catch (Exception f)
        {
            Console.WriteLine(f);
        }

        return 100;

    }

}

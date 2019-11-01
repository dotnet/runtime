// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal class Sample4
{
    private static int s_s = 1;

    [System.Runtime.CompilerServices.MethodImplAttribute(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static void func(object o1, object o2)
    {
        o1.GetType();
        o2.GetType();
        o1.GetType();
        if (s_s == 1)
        {
            o2.GetType();
        }
    }

    private static int Main(string[] args)
    {
        try
        {
            func(new Object(), new Object());
            Console.WriteLine("Passed");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 101;
        }
    }
}

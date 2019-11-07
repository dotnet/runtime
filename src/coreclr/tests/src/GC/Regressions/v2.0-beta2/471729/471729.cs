// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal static class Repro
{
    private struct S
    {
        // disabling unused variable warning
#pragma warning disable 0414
        public bool b;
#pragma warning restore 0414
        public string s;

        public S(bool b, string s)
        {
            this.b = b;
            this.s = s;
        }
    }

    private static S ReturnsS()
    {
        S s = new S(true, "S");
        Console.WriteLine(s.s);
        return s;
    }

    private static void Test(bool throwException)
    {
        if (throwException)
        {
            throw new ArgumentException(ReturnsS().s, new Exception(ReturnsS().s));
        }
        else
        {
            Console.WriteLine("No throw");
        }
    }

    private static int Main()
    {
        try
        {
            Test(true);
            Test(false);
        }
        catch (ArgumentException)
        {
            Console.WriteLine("Test passed");
            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        Console.WriteLine("Test failed");
        return 1;
    }
}

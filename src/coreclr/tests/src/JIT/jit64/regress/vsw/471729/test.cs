// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal static class Repro
{
    private struct S
    {
        public bool b;
        public string s;
    }

    private static S ReturnsS()
    {
        S s = new S();
        s.b = true;
        s.s = "S";
        Console.WriteLine(s.s);
        return s;
    }

    private static void Test(bool f)
    {
        if (f)
        {
            throw new Exception(ReturnsS().s, new Exception(ReturnsS().s));
        }
        else
        {
            Console.WriteLine("blah");
        }
    }

    private static int Main()
    {
        int rc = 1;
        try
        {
            Test(true);
            Test(false);
        }
        catch
        {
            rc = 100;
        }

        return rc;
    }
}

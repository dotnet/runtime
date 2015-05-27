// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal static class Exploit
{
    private static int Main()
    {
        string s = "my string";
        IntPtr i = Helper.RetypeObject<IntPtr, string>(s);
        Console.WriteLine(i);
        if (i != IntPtr.Zero)
        {
            return 101;
        }
        else
        {
            return 100;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class foo
{
    private static object s_o = typeof(string);
    private static int Main()
    {
        bool f = typeof(string) == s_o as Type;
        Console.WriteLine(f);
        if (f) return 100; else return 101;
    }
}

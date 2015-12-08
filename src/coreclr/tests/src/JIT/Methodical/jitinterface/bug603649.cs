// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

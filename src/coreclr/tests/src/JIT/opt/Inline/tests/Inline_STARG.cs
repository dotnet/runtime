// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

internal class MainApp
{
    public static void Foo_NoInline(string s)
    {
        Console.WriteLine(s);
        s = "New string";
        Console.WriteLine(s);
    }

    public static int Main()
    {
        try
        {
            string orig = "Original string";
            Console.WriteLine(orig);
            Foo_NoInline(orig);

            return 100;
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return 666;
        }
    }
}



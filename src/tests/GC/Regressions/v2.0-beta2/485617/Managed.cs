// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Reflection;

public class Managed
{
    public static int Foo(String s)
    {
        int count = GC.CollectionCount(GC.MaxGeneration);
        Console.WriteLine(count);
        return count;
    }
}


// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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


// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;

internal class test
{
    public static int Main(String[] args)
    {
        Type t = typeof(int);
        Type t2 = typeof(long);

        RuntimeTypeHandle th = t.TypeHandle;
        RuntimeTypeHandle th2 = t2.TypeHandle;

        Console.WriteLine(th.Equals(th2));
        Console.WriteLine(th.Equals(th));

        RuntimeTypeHandle[] arr = new RuntimeTypeHandle[2];
        arr[0] = t.TypeHandle;
        arr[1] = t2.TypeHandle;

        if (arr[0].Equals(arr[1]))
        {
            Console.WriteLine("ERR");
            return 0;
        }
        return 100;
    }
}


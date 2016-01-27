// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;

public class GenType1<T>
{
    private static readonly int s_i = 0;

    public static bool foo()
    {
        return s_i == 0;
    }
}

public class cs1
{
#pragma warning disable 0414
    internal static int s_i = 0;
#pragma warning restore 0414

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Main(String[] args)
    {
        try
        {
            GenType1<int>.foo();
            Console.WriteLine("Test SUCCESS");
            return 100;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine("Test FAILURE");
            return 101;
        }
    }
}

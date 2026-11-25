// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

public static class Program
{
    public static int Main(string[] args)
    {
        Console.WriteLine($"RuntimeFeature.IsDynamicCodeSupported: {RuntimeFeature.IsDynamicCodeSupported}");
        Console.WriteLine($"RuntimeFeature.IsDynamicCodeCompiled: {RuntimeFeature.IsDynamicCodeCompiled}");

        return RuntimeFeature.IsDynamicCodeSupported ? 42 : 1;
    }
}

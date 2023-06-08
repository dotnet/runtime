// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

public static class Program
{
    [DllImport("__Internal")]
    public static extern void mono_ios_set_summary (string value);

    public static async Task<int> Main(string[] args)
    {
        mono_ios_set_summary($"Starting functional test");
        Console.WriteLine("Done!");
        await Task.Delay(5000);

        var runtimeFeatureType = typeof(object).Assembly.GetType("System.Runtime.CompilerServices.RuntimeFeature");
        var isDynamicCodeSupportedGetter = runtimeFeatureType?.GetMethod("get_IsDynamicCodeSupported");

        // get_IsDynamicCodeSupported should have been trimmed-out (substituted by false)
        return isDynamicCodeSupportedGetter is null ? 42 : -1;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Xunit;

public class Runtime_100437
{
    [Fact]
    public static int TestCollectibleEmptyArrayNotInFrozenHeap()
    {
        string assemblyPath = typeof(Runtime_100437).Assembly.Location

        // Skip this test for single file
        if (string.IsNullOrEmpty(assemblyPath))
            return;

        WeakReference[] wrs = new WeakReference[10];
        for (int i = 0; i < wrs.Length; i++)
        {
            var alc = new MyAssemblyLoadContext();
            var a = alc.LoadFromAssemblyPath(assemblyPath);
            wrs[i] = (WeakReference)a.GetType("Runtime_100437").GetMethod("Work").Invoke(null, null);
            GC.Collect();
        }

        int result = 0;
        foreach (var wr in wrs)
        {
            // This is testing that the empty array from Work(), if collected, should not have been allocated on the Frozen heap
            // otherwise it will result in a random crash.
            result += wr.Target?.ToString()?.GetHashCode() ?? 0;
        }
        return (result & 0) + 100;
    }

    public static WeakReference Work()
    {
        return new WeakReference(Array.Empty<Runtime_100437>());
    }

    private class MyAssemblyLoadContext : AssemblyLoadContext
    {
        public MyAssemblyLoadContext()
            : base(isCollectible: true)
        {
        }
    }
}

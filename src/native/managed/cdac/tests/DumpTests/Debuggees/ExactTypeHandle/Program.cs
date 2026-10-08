// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime;

/// <summary>
/// Debuggee for cDAC dump tests — exercises the round-trip path through
/// DacDbiImpl.GetExactTypeHandle. Allocates objects of various shapes
/// (primitive arrays, multi-dim arrays, generic instantiations, plain
/// classes, etc.), keeps them alive through esoteric GC handles, then
/// crashes.
/// </summary>
internal static class Program
{
    public const string TestStringValue = "cDAC-ExactTypeHandle-test-string";

    private static void Main()
    {
        // Heap-rooted instances of types whose round-trip we want to cover.
        // - object[], string[]      : SzArray over reference element type
        // - int[]                   : SzArray over primitive element type
        // - int[,]                  : multi-dimensional array
        // - Dictionary<string,int>  : generic class instantiation
        // - plain object            : System.Object directly
        // - string                  : System.String directly
        // - PlainClass              : non-generic user-defined class
        object[] objArr = new object[] { "a", 1, new object() };
        string[] strArr = new[] { TestStringValue };
        int[] intArr = new int[8];
        int[,] intArr2D = new int[2, 3];
        var dict = new Dictionary<string, int> { ["k"] = 1 };
        object plainObj = new object();
        string str = TestStringValue;
        PlainClass plainClass = new PlainClass { Value = 42 };

        // Esoteric GC handles — enumerable from the dump via IGC.GetHandles().
        GCHandle pinned = GCHandle.Alloc(intArr, GCHandleType.Pinned);
        GCHandle strong = GCHandle.Alloc(objArr, GCHandleType.Normal);
        GCHandle weakLong = GCHandle.Alloc(strArr, GCHandleType.WeakTrackResurrection);
        GCHandle strongStr = GCHandle.Alloc(str, GCHandleType.Normal);
        GCHandle strongDict = GCHandle.Alloc(dict, GCHandleType.Normal);
        GCHandle strongMd = GCHandle.Alloc(intArr2D, GCHandleType.Normal);
        GCHandle strongClass = GCHandle.Alloc(plainClass, GCHandleType.Normal);
        DependentHandle dep = new(plainObj, dict);

        GC.KeepAlive(objArr);
        GC.KeepAlive(strArr);
        GC.KeepAlive(intArr);
        GC.KeepAlive(intArr2D);
        GC.KeepAlive(dict);
        GC.KeepAlive(plainObj);
        GC.KeepAlive(str);
        GC.KeepAlive(plainClass);
        GC.KeepAlive(pinned);
        GC.KeepAlive(strong);
        GC.KeepAlive(weakLong);
        GC.KeepAlive(strongStr);
        GC.KeepAlive(strongDict);
        GC.KeepAlive(strongMd);
        GC.KeepAlive(strongClass);
        GC.KeepAlive(dep);

        Environment.FailFast("cDAC dump test: ExactTypeHandle debuggee intentional crash");
    }

    internal class PlainClass
    {
        public int Value;
    }
}

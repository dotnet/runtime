// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public enum Enum1 : int { A }
public enum Enum2 : uint { A }

class TypeTestFolding
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void SideEffect() { }

    //static bool True0() => typeof(delegate*<int, double>) == typeof(delegate* unmanaged<float, void*, void>);
    //static bool True1()
    //{
    //    var t0 = typeof(delegate*<int, double>);
    //    SideEffect();
    //    var t1 = typeof(delegate* unmanaged<float, void*, void>);
    //    return t0 == t1;
    //}

    static bool True2() => typeof(TypeTestFolding) == typeof(TypeTestFolding);
    static bool True3()
    {
        var t0 = typeof(TypeTestFolding);
        SideEffect();
        var t1 = typeof(TypeTestFolding);
        return t0 == t1;
    }

    static bool True4() => typeof(ValueTuple<TypeTestFolding>) == typeof(ValueTuple<TypeTestFolding>);
    static bool True5()
    {
        var t0 = typeof(ValueTuple<TypeTestFolding>);
        SideEffect();
        var t1 = typeof(ValueTuple<TypeTestFolding>);
        return t0 == t1;
    }

    //static bool True6() => typeof(delegate*<int>) == typeof(nint);
    //static bool True7()
    //{
    //    var t0 = typeof(delegate*<int>);
    //    SideEffect();
    //    var t1 = typeof(nint);
    //    return t0 == t1;
    //}

    static bool False0() => typeof(List<object>) == typeof(List<string>);
    static bool False1()
    {
        var t0 = typeof(List<object>);
        SideEffect();
        var t1 = typeof(List<string>);
        return t0 == t1;
    }

    static bool False2() => typeof(int) == typeof(Enum1);
    static bool False3()
    {
        var t0 = typeof(int);
        SideEffect();
        var t1 = typeof(Enum1);
        return t0 == t1;
    }

    static bool False4() => typeof(Enum1) == typeof(Enum2);
    static bool False5()
    {
        var t0 = typeof(Enum1);
        SideEffect();
        var t1 = typeof(Enum2);
        return t0 == t1;
    }

    static bool False6() => typeof(int?) == typeof(uint?);
    static bool False7()
    {
        var t0 = typeof(int?);
        SideEffect();
        var t1 = typeof(uint?);
        return t0 == t1;
    }

    static bool False8() => typeof(int?) == typeof(Enum1?);
    static bool False9()
    {
        var t0 = typeof(int?);
        SideEffect();
        var t1 = typeof(Enum1?);
        return t0 == t1;
    }

    static bool False10() => typeof(ValueTuple<TypeTestFolding>) == typeof(ValueTuple<string>);
    static bool False11()
    {
        var t0 = typeof(ValueTuple<TypeTestFolding>);
        SideEffect();
        var t1 = typeof(ValueTuple<string>);
        return t0 == t1;
    }

    //static bool False12() => typeof(delegate*<int>[]) == typeof(delegate*<float>[]);
    //static bool False13()
    //{
    //    var t0 = typeof(delegate*<int>[]);
    //    SideEffect();
    //    var t1 = typeof(delegate*<float>[]);
    //    return t0 == t1;
    //}

    static bool False14() => typeof(int[]) == typeof(uint[]);
    static bool False15()
    {
        var t0 = typeof(int[]);
        SideEffect();
        var t1 = typeof(uint[]);
        return t0 == t1;
    }

    //static bool False16() => typeof(delegate*<int>) == typeof(IntPtr);
    //static bool False17()
    //{
    //    var t0 = typeof(delegate*<int>);
    //    SideEffect();
    //    var t1 = typeof(UIntPtr);
    //    return t0 == t1;
    //}

    unsafe static int Main()
    {
        delegate*<bool>[] trueFuncs = new delegate*<bool>[] { &True2, &True3, &True4, &True5 };
        delegate*<bool>[] falseFuncs = new delegate*<bool>[] { &False0, &False1, &False2, &False3, &False4, &False5,
                                                               &False6, &False7, &False8, &False9, &False10, &False11,
                                                               &False14, &False15 };

        int result = 100;
        int trueCount = 0;
        int falseCount = 0;

        foreach (var tf in trueFuncs)
        {
            if (!tf())
            {
                Console.WriteLine($"True{trueCount} failed");
                result++;
            }
            trueCount++;
        }

        foreach (var ff in falseFuncs)
        {
            if (ff())
            {
                Console.WriteLine($"False{falseCount} failed");
                result++;
            }
            falseCount++;
        }

        Console.WriteLine($"Ran {trueCount + falseCount} tests; result {result}");
        return result;
    }
}




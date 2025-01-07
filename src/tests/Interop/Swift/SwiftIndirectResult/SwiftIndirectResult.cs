// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Swift;
using Xunit;

public unsafe class SwiftIndirectResultTests
{
    private struct NonFrozenStruct
    {
        public int A;
        public int B;
        public int C;
    }

    private const string SwiftLib = "libSwiftIndirectResult.dylib";

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s19SwiftIndirectResult21ReturnNonFrozenStruct1a1b1cAA0efG0Vs5Int32V_A2ItF")]
    public static extern void ReturnNonFrozenStruct(SwiftIndirectResult result, int a, int b, int c);

    [UnmanagedCallConv(CallConvs = new Type[] { typeof(CallConvSwift) })]
    [DllImport(SwiftLib, EntryPoint = "$s19SwiftIndirectResult26SumReturnedNonFrozenStruct1fs5Int32VAA0fgH0VyXE_tF")]
    public static extern int SumReturnedNonFrozenStruct(delegate* unmanaged[Swift]<SwiftIndirectResult, SwiftSelf, void> func, void* funcContext);

    [Fact]
    public static void TestReturnNonFrozenStruct()
    {
        // In normal circumstances this instance would have unknown/dynamically determined size.
        NonFrozenStruct instance;
        ReturnNonFrozenStruct(new SwiftIndirectResult(&instance), 10, 20, 30);
        Assert.Equal(10, instance.A);
        Assert.Equal(20, instance.B);
        Assert.Equal(30, instance.C);
    }

    [UnmanagedCallersOnly(CallConvs = new Type[] { typeof(CallConvSwift) })]
    private static void ReversePInvokeReturnNonFrozenStruct(SwiftIndirectResult result, SwiftSelf self)
    {
        // In normal circumstances this would require using dynamically sized memcpy and members to create the struct.
        *(NonFrozenStruct*)result.Value = new NonFrozenStruct { A = 10, B = 20, C = 30 };
    }

    [Fact]
    public static void TestSumReturnedNonFrozenStruct()
    {
        int result = SumReturnedNonFrozenStruct(&ReversePInvokeReturnNonFrozenStruct, null);
        Assert.Equal(60, result);
    }
}

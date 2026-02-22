// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

public unsafe static class VectorcallPInvokes
{
    private const string NativeLib = "VectorcallNative";

    // Basic integer test
    [UnmanagedCallConv(CallConvs = [typeof(CallConvVectorcall)])]
    [DllImport(NativeLib, EntryPoint = "Double_Vectorcall")]
    public static extern int Double_Vectorcall(int a, int* b);

    // Float arguments (should be in XMM registers)
    [UnmanagedCallConv(CallConvs = [typeof(CallConvVectorcall)])]
    [DllImport(NativeLib, EntryPoint = "AddFloats_Vectorcall")]
    public static extern float AddFloats_Vectorcall(float a, float b);

    // Double arguments (should be in XMM registers)
    [UnmanagedCallConv(CallConvs = [typeof(CallConvVectorcall)])]
    [DllImport(NativeLib, EntryPoint = "AddDoubles_Vectorcall")]
    public static extern double AddDoubles_Vectorcall(double a, double b);

    // Mixed int/float (separate register banks on vectorcall)
    [UnmanagedCallConv(CallConvs = [typeof(CallConvVectorcall)])]
    [DllImport(NativeLib, EntryPoint = "MixedIntFloat_Vectorcall")]
    public static extern double MixedIntFloat_Vectorcall(int a, float b, int c, double d);

    // Six floats - exercises XMM0-XMM5
    [UnmanagedCallConv(CallConvs = [typeof(CallConvVectorcall)])]
    [DllImport(NativeLib, EntryPoint = "SixFloats_Vectorcall")]
    public static extern float SixFloats_Vectorcall(float a, float b, float c, float d, float e, float f);

    // Six doubles - exercises XMM0-XMM5
    [UnmanagedCallConv(CallConvs = [typeof(CallConvVectorcall)])]
    [DllImport(NativeLib, EntryPoint = "SixDoubles_Vectorcall")]
    public static extern double SixDoubles_Vectorcall(double a, double b, double c, double d, double e, double f);

    // Return float in XMM0
    [UnmanagedCallConv(CallConvs = [typeof(CallConvVectorcall)])]
    [DllImport(NativeLib, EntryPoint = "ReturnFloat_Vectorcall")]
    public static extern float ReturnFloat_Vectorcall(int value);

    // Return double in XMM0
    [UnmanagedCallConv(CallConvs = [typeof(CallConvVectorcall)])]
    [DllImport(NativeLib, EntryPoint = "ReturnDouble_Vectorcall")]
    public static extern double ReturnDouble_Vectorcall(int value);

    // Vector128 P/Invokes - These declarations are syntactically valid but cannot actually
    // be used at runtime because Vector128<T> is a generic type and generic types cannot
    // be marshaled through P/Invoke. Use function pointers (delegate* unmanaged[Vectorcall])
    // with a non-generic struct wrapper instead. See VectorcallVector128Test.cs for examples.
    [UnmanagedCallConv(CallConvs = [typeof(CallConvVectorcall)])]
    [DllImport(NativeLib, EntryPoint = "AddVector128_Vectorcall")]
    public static extern Vector128<float> AddVector128_Vectorcall(Vector128<float> a, Vector128<float> b);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvVectorcall)])]
    [DllImport(NativeLib, EntryPoint = "MixedIntVector128_Vectorcall")]
    public static extern Vector128<float> MixedIntVector128_Vectorcall(int scalar, Vector128<float> vec);

    [UnmanagedCallConv(CallConvs = [typeof(CallConvVectorcall)])]
    [DllImport(NativeLib, EntryPoint = "SixVector128s_Vectorcall")]
    public static extern Vector128<float> SixVector128s_Vectorcall(
        Vector128<float> a, Vector128<float> b, Vector128<float> c,
        Vector128<float> d, Vector128<float> e, Vector128<float> f);

    // Callback test - native calls managed with vectorcall
    [UnmanagedCallConv(CallConvs = [typeof(CallConvVectorcall)])]
    [DllImport(NativeLib, EntryPoint = "TestCallback_Vectorcall")]
    public static extern double TestCallback_Vectorcall(delegate* unmanaged[Vectorcall]<int, float, int, double, double> callback);
}

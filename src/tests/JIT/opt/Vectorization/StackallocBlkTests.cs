// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;


public unsafe class StackallocTests
{
    public static int Main()
    {
        int numberOftests = 0;
        foreach (var method in typeof(StackallocTests)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(t => t.Name.StartsWith("Test")))
        {
            // Invoke the test and make sure both return value and out
            // parameters are empty guids
            var args = new object[1];
            args[0] = Guid.NewGuid();
            var value = (Guid)method.Invoke(null, args);
            
            if ((Guid)args[0] != Guid.Empty || value != Guid.Empty)
                throw new InvalidOperationException();
            numberOftests++;
        }
        return numberOftests + 70;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void PoisonStack()
    {
        var b = stackalloc byte[20000];
        Unsafe.InitBlockUnaligned(b, 0xFF, 20000);
        Consume(b);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void EnsureZeroed(byte* ptr, int size)
    {
        for (int i = 0; i < size; i++)
        {
            if (ptr[i] != 0)
                throw new InvalidOperationException();
        }
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(byte* ptr) {} // to avoid dead-code elimination

    [MethodImpl(MethodImplOptions.NoInlining)]
    static T ToVar<T>(T o) => o; // convert a constant to a variable

    // Tests: Constant-sized

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test0(out Guid g)
    {
        const int size = 0;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test1(out Guid g)
    {
        const int size = 1;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test8(out Guid g)
    {
        const int size = 8;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test16(out Guid g)
    {
        const int size = 16;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test32(out Guid g)
    {
        const int size = 32;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test40(out Guid g)
    {
        const int size = 40;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test64(out Guid g)
    {
        const int size = 64;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test100(out Guid g)
    {
        const int size = 100;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test128(out Guid g)
    {
        const int size = 128;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test200(out Guid g)
    {
        const int size = 200;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test256(out Guid g)
    {
        const int size = 256;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test512(out Guid g)
    {
        const int size = 512;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test4096(out Guid g)
    {
        const int size = 4096;
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test20000(out Guid g)
    {
        const int size = 20000; // larger than a typical page (but still constant)
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    // Variable-sized tests

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test0_var(out Guid g)
    {
        int size = ToVar(0);
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test1_var(out Guid g)
    {
        int size = ToVar(1);
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test8_var(out Guid g)
    {
        int size = ToVar(8);
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test16_var(out Guid g)
    {
        int size = ToVar(16);
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test32_var(out Guid g)
    {
        int size = ToVar(32);
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test40_var(out Guid g)
    {
        int size = ToVar(40);
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test64_var(out Guid g)
    {
        int size = ToVar(64);
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test100_var(out Guid g)
    {
        int size = ToVar(100);
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test128_var(out Guid g)
    {
        int size = ToVar(128);
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test200_var(out Guid g)
    {
        int size = ToVar(200);
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test256_var(out Guid g)
    {
        int size = ToVar(256);
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test512_var(out Guid g)
    {
        int size = ToVar(512);
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Guid Test20000_var(out Guid g)
    {
        int size = ToVar(20000); 
        PoisonStack();
        byte* p = stackalloc byte[size];
        EnsureZeroed(p, size);
        g = default;
        return default;
    }

    // A couple of SkipLocalsInit, just to make sure there are no asserts or unexpected garbage in Guids

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    public static Guid Test32_SkipLocalsInit(out Guid g)
    {
        const int size = 32;
        PoisonStack();
        byte* p = stackalloc byte[size];
        Consume(p);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    public static Guid Test256_SkipLocalsInit(out Guid g)
    {
        const int size = 256;
        PoisonStack();
        byte* p = stackalloc byte[size];
        Consume(p);
        g = default;
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    [SkipLocalsInit]
    public static Guid Test20000_SkipLocalsInit(out Guid g)
    {
        const int size = 20000;
        PoisonStack();
        byte* p = stackalloc byte[size];
        Consume(p);
        g = default;
        return default;
    }
}

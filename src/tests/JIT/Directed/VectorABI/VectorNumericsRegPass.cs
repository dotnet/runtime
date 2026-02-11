// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// Test that System.Numerics.Vector<T> types are passed in registers on Unix AMD64
// for the active hardware-dependent vector size (for example, 16 bytes with SSE or
// 32 bytes on AVX-capable machines) according to the System V ABI.

public static class VectorNumericsRegPass
{
    private const int PASS = 100;
    private const int FAIL = 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<float> AddVectors(Vector<float> a, Vector<float> b)
    {
        return a + b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<int> MultiplyVectors(Vector<int> a, Vector<int> b)
    {
        return a * b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<double> SubtractVectors(Vector<double> a, Vector<double> b)
    {
        return a - b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static float SumVector(Vector<float> v)
    {
        float sum = 0;
        for (int i = 0; i < Vector<float>.Count; i++)
        {
            sum += v[i];
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector<float> PassMultipleVectors(Vector<float> a, Vector<float> b, Vector<float> c, int scalar)
    {
        return (a + b) * c + new Vector<float>(scalar);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine($"Vector<float>.Count = {Vector<float>.Count}");
        Console.WriteLine($"Vector<float> size = {Vector<float>.Count * sizeof(float)} bytes");
        Console.WriteLine($"Vector<int>.Count = {Vector<int>.Count}");
        Console.WriteLine($"Vector<double>.Count = {Vector<double>.Count}");

        // Test with float vectors
        var vf1 = new Vector<float>(1.0f);
        var vf2 = new Vector<float>(2.0f);
        var vfResult = AddVectors(vf1, vf2);
        
        for (int i = 0; i < Vector<float>.Count; i++)
        {
            if (Math.Abs(vfResult[i] - 3.0f) > 0.001f)
            {
                Console.WriteLine($"FAIL: Float vector addition failed at index {i}: {vfResult[i]} != 3.0");
                return FAIL;
            }
        }

        // Test with int vectors
        var vi1 = new Vector<int>(5);
        var vi2 = new Vector<int>(3);
        var viResult = MultiplyVectors(vi1, vi2);
        
        for (int i = 0; i < Vector<int>.Count; i++)
        {
            if (viResult[i] != 15)
            {
                Console.WriteLine($"FAIL: Int vector multiplication failed at index {i}: {viResult[i]} != 15");
                return FAIL;
            }
        }

        // Test with double vectors
        var vd1 = new Vector<double>(10.0);
        var vd2 = new Vector<double>(3.0);
        var vdResult = SubtractVectors(vd1, vd2);
        
        for (int i = 0; i < Vector<double>.Count; i++)
        {
            if (Math.Abs(vdResult[i] - 7.0) > 0.001)
            {
                Console.WriteLine($"FAIL: Double vector subtraction failed at index {i}: {vdResult[i]} != 7.0");
                return FAIL;
            }
        }

        // Test sum operation
        var vfSum = new Vector<float>(4.0f);
        float sum = SumVector(vfSum);
        float expectedSum = 4.0f * Vector<float>.Count;
        if (Math.Abs(sum - expectedSum) > 0.001f)
        {
            Console.WriteLine($"FAIL: Sum operation failed: {sum} != {expectedSum}");
            return FAIL;
        }

        // Test multiple vector parameters with scalar mixing
        var va = new Vector<float>(1.0f);
        var vb = new Vector<float>(2.0f);
        var vc = new Vector<float>(3.0f);
        var vmResult = PassMultipleVectors(va, vb, vc, 5);
        
        // (1 + 2) * 3 + 5 = 14
        for (int i = 0; i < Vector<float>.Count; i++)
        {
            if (Math.Abs(vmResult[i] - 14.0f) > 0.001f)
            {
                Console.WriteLine($"FAIL: Multiple parameter test failed at index {i}: {vmResult[i]} != 14.0");
                return FAIL;
            }
        }

        Console.WriteLine("PASS: All tests succeeded!");
        return PASS;
    }
}

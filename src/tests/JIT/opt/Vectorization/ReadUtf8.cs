// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Xunit;

public class ReadUtf8
{
    [Fact]
    public static int TestEntryPoint()
    {
        // Warm up for PGO
        for (int i=0; i<200; i++)
        {
            Test_empty();
            Test_hello();
            Test_CJK();
            Test_SIMD();
            Thread.Sleep(10);
        }
        return 100;
    }

    static void Test_empty()
    {
        byte[] bytes = new byte[100];
        int bytesWritten = 0;

        Span<byte> span = bytes.AsSpan(0, 6);
        AssertIsTrue(TryGetBytes_5(span, out bytesWritten));
        AssertEquals(0, bytesWritten);
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched

        // Reset data
        bytesWritten = 0;
        bytes.AsSpan().Clear();

        span = bytes.AsSpan(0, 0);
        AssertIsTrue(TryGetBytes_5(span, out bytesWritten));
        AssertEquals(0, bytesWritten);
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TryGetBytes_5(Span<byte> buffer, out int bytesWritten) => 
            Encoding.UTF8.TryGetBytes("", buffer, out bytesWritten);
    }

    static void Test_hello()
    {
        byte[] bytes = new byte[100];
        int bytesWritten = 0;

        Span<byte> span = bytes.AsSpan(0, 6);
        AssertIsTrue(TryGetBytes_5(span, out bytesWritten));
        AssertEquals(5, bytesWritten);
        AssertIsTrue(span.SequenceEqual("hello\0"u8));
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched

        // Reset data
        bytesWritten = 0;
        bytes.AsSpan().Clear();

        span = bytes.AsSpan(0, 5);
        AssertIsTrue(TryGetBytes_5(span, out bytesWritten));
        AssertEquals(5, bytesWritten);
        AssertIsTrue(span.SequenceEqual("hello"u8));
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched

        // Reset data
        bytesWritten = 0;
        bytes.AsSpan().Clear();

        span = bytes.AsSpan(0, 1);
        AssertIsTrue(!TryGetBytes_5(span, out bytesWritten));
        AssertEquals(0, bytesWritten);

        // Reset data
        bytesWritten = 0;
        bytes.AsSpan().Clear();

        span = bytes.AsSpan(0, 0);
        AssertIsTrue(!TryGetBytes_5(span, out bytesWritten));
        AssertEquals(0, bytesWritten);
        AssertIsTrue(span.SequenceEqual(""u8));
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TryGetBytes_5(Span<byte> buffer, out int bytesWritten) => 
            Encoding.UTF8.TryGetBytes("hello", buffer, out bytesWritten);
    }

    static void Test_CJK()
    {
        byte[] bytes = new byte[100];
        int bytesWritten = 0;

        Span<byte> span = bytes.AsSpan(0, 3);
        AssertIsTrue(TryGetBytes_5(span, out bytesWritten));
        AssertEquals(3, bytesWritten);
        AssertIsTrue(span.SequenceEqual(new byte[] { 0xE9, 0x89, 0x84 }));
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TryGetBytes_5(Span<byte> buffer, out int bytesWritten) => 
            Encoding.UTF8.TryGetBytes("\u9244", buffer, out bytesWritten);
    }

    static void Test_SIMD()
    {
        byte[] bytes = new byte[1024];
        int bytesWritten = 0;

        Span<byte> span = bytes.AsSpan(0, 15);
        AssertIsTrue(TryGetBytes_15(span, out bytesWritten));
        AssertEquals(15, bytesWritten);
        AssertIsTrue(span.SequenceEqual("000011112222333"u8));
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched

        // Reset data
        bytesWritten = 0;
        bytes.AsSpan().Clear();

        span = bytes.AsSpan(0, 16);
        AssertIsTrue(TryGetBytes_16(span, out bytesWritten));
        AssertEquals(16, bytesWritten);
        AssertIsTrue(span.SequenceEqual("0000111122223333"u8));
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched

        // Reset data
        bytesWritten = 0;
        bytes.AsSpan().Clear();

        span = bytes.AsSpan(0, 31);
        AssertIsTrue(TryGetBytes_31(span, out bytesWritten));
        AssertEquals(31, bytesWritten);
        AssertIsTrue(span.SequenceEqual("0000111122223333000011112222333"u8));
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched

        // Reset data
        bytesWritten = 0;
        bytes.AsSpan().Clear();

        span = bytes.AsSpan(0, 32);
        AssertIsTrue(TryGetBytes_32(span, out bytesWritten));
        AssertEquals(32, bytesWritten);
        AssertIsTrue(span.SequenceEqual("00001111222233330000111122223333"u8));
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched

        // Reset data
        bytesWritten = 0;
        bytes.AsSpan().Clear();

        span = bytes.AsSpan(0, 64);
        AssertIsTrue(TryGetBytes_64(span, out bytesWritten));
        AssertEquals(64, bytesWritten);
        AssertIsTrue(span.SequenceEqual("0000111122223333000011112222333300001111222233330000111122223333"u8));
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched

        // Reset data
        bytesWritten = 0;
        bytes.AsSpan().Clear();

        span = bytes.AsSpan(0, 128);
        AssertIsTrue(TryGetBytes_128(span, out bytesWritten));
        AssertEquals(128, bytesWritten);
        AssertIsTrue(span.SequenceEqual("00001111222233330000111122223333000011112222333300001111222233330000111122223333000011112222333300001111222233330000111122223333"u8));
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched

        // Reset data
        bytesWritten = 0;
        bytes.AsSpan().Clear();

        span = bytes.AsSpan(0, 31);
        AssertIsTrue(TryGetBytes_31(span, out bytesWritten));
        AssertEquals(31, bytesWritten);
        AssertIsTrue(span.SequenceEqual("0000111122223333000011112222333"u8));
        IsEmpty(bytes.AsSpan(span.Length)); // the rest is untouched


        span = bytes.AsSpan();
        AssertIsTrue(!TryGetBytes_16(span.Slice(0, 15), out bytesWritten));
        AssertEquals(0, bytesWritten);
        AssertIsTrue(!TryGetBytes_31(span.Slice(0, 30), out bytesWritten));
        AssertEquals(0, bytesWritten);
        AssertIsTrue(!TryGetBytes_32(span.Slice(0, 31), out bytesWritten));
        AssertEquals(0, bytesWritten);
        AssertIsTrue(!TryGetBytes_64(span.Slice(0, 63), out bytesWritten));
        AssertEquals(0, bytesWritten);
        AssertIsTrue(!TryGetBytes_128(span.Slice(0, 127), out bytesWritten));
        AssertEquals(0, bytesWritten);


        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TryGetBytes_15(Span<byte> buffer, out int bytesWritten) => 
            Encoding.UTF8.TryGetBytes("000011112222333", buffer, out bytesWritten);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TryGetBytes_16(Span<byte> buffer, out int bytesWritten) => 
            Encoding.UTF8.TryGetBytes("0000111122223333", buffer, out bytesWritten);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TryGetBytes_31(Span<byte> buffer, out int bytesWritten) => 
            Encoding.UTF8.TryGetBytes("0000111122223333000011112222333", buffer, out bytesWritten);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TryGetBytes_32(Span<byte> buffer, out int bytesWritten) => 
            Encoding.UTF8.TryGetBytes("00001111222233330000111122223333", buffer, out bytesWritten);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TryGetBytes_64(Span<byte> buffer, out int bytesWritten) => 
            Encoding.UTF8.TryGetBytes("0000111122223333000011112222333300001111222233330000111122223333", buffer, out bytesWritten);

        [MethodImpl(MethodImplOptions.NoInlining)]
        static bool TryGetBytes_128(Span<byte> buffer, out int bytesWritten) => 
            Encoding.UTF8.TryGetBytes("00001111222233330000111122223333000011112222333300001111222233330000111122223333000011112222333300001111222233330000111122223333", buffer, out bytesWritten);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void AssertIsTrue(bool value)
    {
        if (!value)
            throw new Exception();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void AssertEquals(int actual, int expected)
    {
        if (expected != actual)
            throw new Exception($"{actual} != {expected}");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void IsEmpty(Span<byte> span)
    {
        foreach (byte item in span)
        {
            if (item != 0)
                throw new Exception($"{item} != 0");
        }
    }
}

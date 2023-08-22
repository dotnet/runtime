// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;
using System.Threading;
using Xunit;

public class ReadUtf8
{
    [Fact]
    public static void TestEntryPoint()
    {
        // Warm up for PGO
        for (int i=0; i<200; i++)
        {
            Test_empty();
            Test_hello();
            Test_CJK();
            Test_SIMD();
            Test_1();
            Test_2();
            Test_3();
            Test_4();
            Test_5();
            Thread.Sleep(10);
        }
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

    // ReadUtf8 is used inside Utf8.TryWrite + interpolation syntax:

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test_1()
    {
        var buffer = new byte[1024];
        ValidateResult("", Utf8.TryWrite(buffer, $"", out var written1), buffer, written1);
        ValidateResult("1", Utf8.TryWrite(buffer, $"1", out var written2), buffer, written2);
        ValidateResult("12", Utf8.TryWrite(buffer, $"12", out var written3), buffer, written3);
        ValidateResult("123", Utf8.TryWrite(buffer, $"123", out var written4), buffer, written4);
        ValidateResult("1234", Utf8.TryWrite(buffer, $"1234", out var written5), buffer, written5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test_2()
    {
        var buffer = new byte[1024];
        ValidateResult("12345", Utf8.TryWrite(buffer, $"12345", out var written1), buffer, written1);
        ValidateResult("123456", Utf8.TryWrite(buffer, $"123456", out var written2), buffer, written2);
        ValidateResult("1234567", Utf8.TryWrite(buffer, $"1234567", out var written3), buffer, written3);
        ValidateResult("12345678", Utf8.TryWrite(buffer, $"12345678", out var written4), buffer, written4);
        ValidateResult("123456789", Utf8.TryWrite(buffer, $"123456789", out var written5), buffer, written5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test_3()
    {
        var buffer = new byte[1024];
        ValidateResult("123456789A", Utf8.TryWrite(buffer, $"123456789A", out var written1), buffer, written1);
        ValidateResult("123456789AB", Utf8.TryWrite(buffer, $"123456789AB", out var written2), buffer, written2);
        ValidateResult("123456789ABC", Utf8.TryWrite(buffer, $"123456789ABC", out var written3), buffer, written3);
        ValidateResult("123456789ABCD", Utf8.TryWrite(buffer, $"123456789ABCD", out var written4), buffer, written4);
        ValidateResult("123456789ABCDE", Utf8.TryWrite(buffer, $"123456789ABCDE", out var written5), buffer, written5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test_4()
    {
        var buffer = new byte[1024];
        ValidateResult("123456789ABCDEF", Utf8.TryWrite(buffer, $"123456789ABCDEF", out var written1), buffer, written1);
        ValidateResult("123456789ABCDEF\u0419", Utf8.TryWrite(buffer, $"123456789ABCDEF\u0419", out var written2), buffer, written2);
        ValidateResult("123456789ABCDEF\u0419\u044C", Utf8.TryWrite(buffer, $"123456789ABCDEF\u0419\u044C", out var written3), buffer, written3);
        ValidateResult("123456789ABCDEF\u0419\u044Cf", Utf8.TryWrite(buffer, $"123456789ABCDEF\u0419\u044Cf", out var written4), buffer, written4);
        ValidateResult("123456789ABCDEF\u0419\u044Cf.", Utf8.TryWrite(buffer, $"123456789ABCDEF\u0419\u044Cf.", out var written5), buffer, written5);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test_5()
    {
        var buffer = new byte[1024];
        ValidateResult("\uD800b", Utf8.TryWrite(buffer, $"\uD800b", out var written1), buffer, written1);
        ValidateResult("1\uD800b", Utf8.TryWrite(buffer, $"1\uD800b", out var written2), buffer, written2);
        ValidateResult("11\uD800b", Utf8.TryWrite(buffer, $"11\uD800b", out var written3), buffer, written3);
        ValidateResult("\uD800b\uD800b", Utf8.TryWrite(buffer, $"\uD800b\uD800b", out var written4), buffer, written4);
        ValidateResult("\uD800b435345435", Utf8.TryWrite(buffer, $"\uD800b435345435", out var written5), buffer, written5);
        ValidateResult("342532523\uD800b\uD800b35235", Utf8.TryWrite(buffer, $"342532523\uD800b\uD800b35235", out var written6), buffer, written6);
        ValidateResult("efewfwfwfwfwefwe\uD800bfewfw\uD800bwfwefew\uD800b", Utf8.TryWrite(buffer, $"efewfwfwfwfwefwe\uD800bfewfw\uD800bwfwefew\uD800b", out var written7), buffer, written7);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void ValidateResult(string str, bool actualResult, byte[] actualData, int actualBytesWritten)
    {
        byte[] expectedData = new byte[actualData.Length];
        bool expectedResult = Utf8.TryWrite(expectedData, $"{str}", out int expectedBytesWritten);
        if (expectedResult != actualResult)
        {
            throw new Exception($"Unexpected return value: {actualResult}");
        }

        if (actualBytesWritten != expectedBytesWritten)
        {
            throw new Exception($"bytesWritten value: {actualBytesWritten} != {expectedBytesWritten}");
        }

        if (expectedResult && !actualData.AsSpan(0, actualBytesWritten).SequenceEqual(
                expectedData.AsSpan(0, expectedBytesWritten)))
        {
            throw new Exception("actualData != expectedData");
        }
    }
}

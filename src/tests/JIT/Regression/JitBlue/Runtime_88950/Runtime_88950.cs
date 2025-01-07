// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_88950
{
    internal readonly struct Example
    {
        public readonly object? Value;
        public readonly ulong Inner;

        struct ExampleInner
        {
            public int Offset;
            public int Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Example(object? value, int offset, int length)
        {
            var inner = new ExampleInner
            {
                Offset = offset,
                Length = length
            };

            Value = value;
            Inner = Unsafe.As<ExampleInner, ulong>(ref inner);
        }
        public int Offset
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
            get
            {
                var inner = Inner;
                return Unsafe.As<ulong, ExampleInner>(ref inner).Offset;
            }
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.NoInlining)]
            get
            {
                ulong inner = Inner;
                return Unsafe.As<ulong, ExampleInner>(ref inner).Length;
            }
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        var repro = new Example(new object(), 1234, 5678);
        if (repro.Offset != 1234)
        {
            Console.WriteLine($"Offset: {repro.Offset}");
            return 1;
        }
        if (repro.Length != 5678)
        {
            Console.WriteLine($"Length: {repro.Length}");
            return 2;
        }
        return 100;
    }
}

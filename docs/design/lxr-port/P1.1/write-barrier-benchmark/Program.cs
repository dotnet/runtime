// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Running;

namespace P11.WriteBarrierBenchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        if (args is ["--direct"])
        {
            var benchmark = new WriteBarrierBenchmark();
            benchmark.Setup();
            object fieldValue = benchmark.FieldStores();
            object arrayValue = benchmark.ArrayStores();
            object copyValue = benchmark.ReferenceArrayCopy();
            int clearLength = benchmark.ReferenceArrayClear();
            object fillValue = benchmark.ReferenceSpanFill();
            object structValue = benchmark.MixedStructCopy();
            object veryLargeStructValue = benchmark.VeryLargeMixedStructCopy();
            int veryLargeClearCount = benchmark.VeryLargeMixedStructClear();
            Console.WriteLine(
                ReferenceEquals(fieldValue, arrayValue) &&
                ReferenceEquals(arrayValue, copyValue) &&
                ReferenceEquals(copyValue, fillValue) &&
                ReferenceEquals(fillValue, structValue) &&
                ReferenceEquals(structValue, veryLargeStructValue) &&
                (clearLength == 16) &&
                (veryLargeClearCount == 4096)
                    ? "PASS"
                    : "FAIL");
            return;
        }

        BenchmarkSwitcher.FromAssembly(typeof(WriteBarrierBenchmark).Assembly).Run(args);
    }
}

[MinColumn]
[MaxColumn]
[Q1Column]
[Q3Column]
public class WriteBarrierBenchmark
{
    private readonly Fields _fields = new();
    private readonly object[] _array = new object[16];
    private readonly object[] _copySource = new object[16];
    private readonly object[] _copyDestination = new object[16];
    private readonly MixedHolder _mixedDestination = new();
    private readonly VeryLargeMixedHolder _veryLargeMixedDestination = new();
    private object _value = null!;
    private MixedValue _mixedSource;
    private VeryLargeMixedValue _veryLargeMixedSource;

    [GlobalSetup]
    public void Setup()
    {
        _value = new object();
        Array.Fill(_copySource, _value);
        Array.Fill(_copyDestination, _value);
        RefChunk chunk = new()
        {
            F0 = _value,
            S0 = 1,
            F1 = _value,
            S1 = 2,
            F2 = _value,
            S2 = 3,
            F3 = _value,
            S3 = 4,
        };
        _mixedSource = new MixedValue
        {
            C00 = chunk,
            C01 = chunk,
            C02 = chunk,
            C03 = chunk,
            C04 = chunk,
            C05 = chunk,
            C06 = chunk,
            C07 = chunk,
            C08 = chunk,
            C09 = chunk,
            C10 = chunk,
            C11 = chunk,
            C12 = chunk,
            C13 = chunk,
            C14 = chunk,
            C15 = chunk,
        };

        VeryLargeMixedElement veryLargeElement = new()
        {
            First = _value,
            Scalar = 42,
            Second = _value,
        };
        for (int index = 0; index < 2048; index++)
        {
            _veryLargeMixedSource[index] = veryLargeElement;
        }
    }

    [Benchmark(OperationsPerInvoke = 16)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public object FieldStores()
    {
        _fields.F00 = _value;
        _fields.F01 = _value;
        _fields.F02 = _value;
        _fields.F03 = _value;
        _fields.F04 = _value;
        _fields.F05 = _value;
        _fields.F06 = _value;
        _fields.F07 = _value;
        _fields.F08 = _value;
        _fields.F09 = _value;
        _fields.F10 = _value;
        _fields.F11 = _value;
        _fields.F12 = _value;
        _fields.F13 = _value;
        _fields.F14 = _value;
        _fields.F15 = _value;

        return _fields.F15;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public object ArrayStores()
    {
        _array[0] = _value;
        _array[1] = _value;
        _array[2] = _value;
        _array[3] = _value;
        _array[4] = _value;
        _array[5] = _value;
        _array[6] = _value;
        _array[7] = _value;
        _array[8] = _value;
        _array[9] = _value;
        _array[10] = _value;
        _array[11] = _value;
        _array[12] = _value;
        _array[13] = _value;
        _array[14] = _value;
        _array[15] = _value;

        return _array[15];
    }

    [Benchmark(OperationsPerInvoke = 16)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public object ReferenceArrayCopy()
    {
        Array.Copy(_copySource, _copyDestination, _copySource.Length);
        return _copyDestination[^1];
    }

    [Benchmark(OperationsPerInvoke = 16)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int ReferenceArrayClear()
    {
        Array.Clear(_copyDestination);
        return _copyDestination.Length;
    }

    [Benchmark(OperationsPerInvoke = 16)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public object ReferenceSpanFill()
    {
        _copyDestination.AsSpan().Fill(_value);
        return _copyDestination[^1];
    }

    [Benchmark(OperationsPerInvoke = 64)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public object MixedStructCopy()
    {
        _mixedDestination.Value = _mixedSource;
        return _mixedDestination.Value.C15.F3!;
    }

    [Benchmark(OperationsPerInvoke = 4096)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public object VeryLargeMixedStructCopy()
    {
        _veryLargeMixedDestination.Value = _veryLargeMixedSource;
        return _veryLargeMixedDestination.Value[2047].Second!;
    }

    [Benchmark(OperationsPerInvoke = 4096)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public int VeryLargeMixedStructClear()
    {
        _veryLargeMixedDestination.Value = default;
        return (_veryLargeMixedDestination.Value[0].First is null) &&
            (_veryLargeMixedDestination.Value[2047].Second is null)
                ? 4096
                : 0;
    }

    private sealed class Fields
    {
        public object F00 = null!;
        public object F01 = null!;
        public object F02 = null!;
        public object F03 = null!;
        public object F04 = null!;
        public object F05 = null!;
        public object F06 = null!;
        public object F07 = null!;
        public object F08 = null!;
        public object F09 = null!;
        public object F10 = null!;
        public object F11 = null!;
        public object F12 = null!;
        public object F13 = null!;
        public object F14 = null!;
        public object F15 = null!;
    }

    private sealed class MixedHolder
    {
        public MixedValue Value;
    }

    private sealed class VeryLargeMixedHolder
    {
        public VeryLargeMixedValue Value;
    }

    private struct MixedValue
    {
        public RefChunk C00;
        public RefChunk C01;
        public RefChunk C02;
        public RefChunk C03;
        public RefChunk C04;
        public RefChunk C05;
        public RefChunk C06;
        public RefChunk C07;
        public RefChunk C08;
        public RefChunk C09;
        public RefChunk C10;
        public RefChunk C11;
        public RefChunk C12;
        public RefChunk C13;
        public RefChunk C14;
        public RefChunk C15;
    }

    private struct RefChunk
    {
        public object? F0;
        public nuint S0;
        public object? F1;
        public nuint S1;
        public object? F2;
        public nuint S2;
        public object? F3;
        public nuint S3;
    }

    private struct VeryLargeMixedElement
    {
        public object? First;
        public nuint Scalar;
        public object? Second;
    }

    [InlineArray(2048)]
    private struct VeryLargeMixedValue
    {
        private VeryLargeMixedElement _element0;
    }
}

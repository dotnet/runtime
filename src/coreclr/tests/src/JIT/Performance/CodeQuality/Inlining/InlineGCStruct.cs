// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public class InlineGCStruct
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 2500000;
#endif

[MethodImpl(MethodImplOptions.NoInlining)]
public static int FastFunctionNotCallingStringFormat(int param)
{
    if (param < 0) {
        throw new Exception(String.Format("We do not like the value {0:N0}.", param));
    }

    if (param == int.MaxValue) {
        throw new Exception(String.Format("{0:N0} is maxed out.", param));
    }

    if (param > int.MaxValue / 2) {
        throw new Exception(String.Format("We do not like the value {0:N0} either.", param));
    }

    return param * 2;
}

[MethodImpl(MethodImplOptions.NoInlining)]
public static int FastFunctionNotHavingStringFormat(int param)
{
    if (param < 0) {
        throw new ArgumentOutOfRangeException("param", "We do not like this value.");
    }

    if (param == int.MaxValue) {
        throw new ArgumentOutOfRangeException("param", "Maxed out.");
    }

    if (param > int.MaxValue / 2) {
        throw new ArgumentOutOfRangeException("param", "We do not like this value either.");
    }

    return param * 2;
}

[Benchmark]
public static bool WithFormat()
{
    int result = 0;

    foreach (var iteration in Benchmark.Iterations) {
        using (iteration.StartMeasurement()) {
            for (int i = 0; i < Iterations; i++) {
                result |= FastFunctionNotCallingStringFormat(11);
            }
        }
    }

    return (result == 22);
}

[Benchmark]
public static bool WithoutFormat()
{
    int result = 0;

    foreach (var iteration in Benchmark.Iterations) {
        using (iteration.StartMeasurement()) {
            for (int i = 0; i < Iterations; i++) {
                result |= FastFunctionNotHavingStringFormat(11);
            }
        }
    }

    return (result == 22);
}

public static bool WithoutFormatBase()
{
    int result = 0;

    for (int i = 0; i < Iterations; i++) {
        result |= FastFunctionNotHavingStringFormat(11);
    }

    return (result == 22);
}

public static bool WithFormatBase()
{
    int result = 0;

    for (int i = 0; i < Iterations; i++) {
        result |= FastFunctionNotCallingStringFormat(11);
    }

    return (result == 22);
}

public static int Main()
{
    bool withFormat = WithFormatBase();
    bool withoutFormat = WithoutFormatBase();

    return (withFormat && withoutFormat ? 100 : -1);
}

}


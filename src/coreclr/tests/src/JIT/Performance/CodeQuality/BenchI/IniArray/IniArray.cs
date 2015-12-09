// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using Microsoft.Xunit.Performance;
using System;
using System.Runtime.CompilerServices;
using Xunit;

[assembly: OptimizeForBenchmarks]
[assembly: MeasureInstructionsRetired]

public static class IniArray
{

#if DEBUG
    public const int Iterations = 1;
#else
    public const int Iterations = 10000000;
#endif

    const int Allotted = 16;
    static volatile object VolatileObject;

    static void Escape(object obj) {
        VolatileObject = obj;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Bench() {
        char[] workarea = new char[Allotted];
        for (int i = 0; i < Iterations; i++) {
            for (int j = 0; j < Allotted; j++) {
                workarea[j] = ' ';
            }
        }
        Escape(workarea);
        return true;
    }

    [Benchmark]
    public static void Test() {
        foreach (var iteration in Benchmark.Iterations) {
            using (iteration.StartMeasurement()) {
                Bench();
            }
        }
    }

    static bool TestBase() {
        bool result = Bench();
        return result;
    }
    
    public static int Main() {
        bool result = TestBase();
        return (result ? 100 : -1);
    }
}

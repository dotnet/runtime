// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

using var baseline = File.OpenRead("baseline.object");
using var compare = File.OpenRead("compare.object");

Console.WriteLine($"Baseline size: {baseline.Length}");
Console.WriteLine($"Compare size: {compare.Length}");

if (baseline.Length != compare.Length)
    throw new Exception("Different sizes");

long length = baseline.Length;
for (int i = 0; i < length; i++)
{
    if (baseline.ReadByte() != compare.ReadByte())
        throw new Exception($"Different at byte {i}");
}

// We're not interested in running this, we just want some junk to compile
if (Environment.GetEnvironmentVariable("Never") == "Ever")
{
#if !INCREMENTAL_COMPILATION_EXPERIMENT
    Delegates.Run();
    Devirtualization.Run();
    Generics.Run();
    Interfaces.Run();
#endif
#if INCREMENTAL_COMPILATION_EXPERIMENT
    Console.WriteLine(IncrementalFixture.GetValue(1));
#endif
}

return 100;

#if INCREMENTAL_COMPILATION_EXPERIMENT
static class IncrementalFixture
{
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    public static int GetValue(int value) => value + 0x61234567;
}
#endif

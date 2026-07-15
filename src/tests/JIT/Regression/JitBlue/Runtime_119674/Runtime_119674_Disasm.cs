// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_119674_Disasm
{
    private static int s_value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int Consume(int* value)
    {
        GC.Collect(0, GCCollectionMode.Forced, blocking: true);
        return *value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int ReadPrimitiveStatic()
    {
        // X64-NOT: bword ptr
        // ARM64-NOT: str {{x[0-9]+}}, [{{(fp|sp)}}
        fixed (int* value = &s_value)
        {
            return Consume(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe int ReadRva()
    {
        // X64-NOT: bword ptr
        // ARM64-NOT: str {{x[0-9]+}}, [{{(fp|sp)}}
        ReadOnlySpan<int> values = [1, 2, 3, 4];
        fixed (int* value = values)
        {
            return Consume(value);
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        s_value = 42;

        Assert.Equal(42, ReadPrimitiveStatic());
        Assert.Equal(1, ReadRva());
    }
}

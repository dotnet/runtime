// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_133502;

public unsafe class Runtime_133502
{
    [Fact]
    public static void TestEntryPoint()
    {
        InlineArray value = default;
        value.Values[0] = 3.14159;
        InlineArray result = Echo(value);

        Assert.Equal(value.Values[0], result.Values[0]);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static InlineArray Echo(InlineArray value) => value;

    private struct InlineArray
    {
        public fixed double Values[1];
    }
}

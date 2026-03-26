// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

public class Runtime_125615
{
    [Fact]
    public static void TestEntryPoint()
    {
        long result = MutateImplicitByRef(default).GetAwaiter().GetResult();
        Assert.Equal(1, result);
    }

    private static async Task<long> MutateImplicitByRef(ImpByRef value)
    {
        await Task.Yield();
        value.A = 1;
        await Task.Yield();
        return value.A;
    }

    private struct ImpByRef
    {
        public long A, B, C, D, E;
    }
}

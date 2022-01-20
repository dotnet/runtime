// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class ShaTests
{
    [ConditionalTheory(nameof(RunTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    public void TestSha256MessageSchedule2(long a0, long a1, long b0, long b1, long expected0, long expected1)
    {
        Vector128<byte> a = Vector128.Create(a0, a1).AsByte();
        Vector128<byte> b = Vector128.Create(b0, b1).AsByte();

        Vector128<byte> result = Sha.Sha256MessageSchedule2(a, b);
        Vector128<long> longResult = result.AsInt64();

        Assert.Equal(expected0, longResult[0]);
        Assert.Equal(expected1, longResult[1]);
    }
}

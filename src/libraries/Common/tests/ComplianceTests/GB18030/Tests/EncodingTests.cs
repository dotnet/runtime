// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace GB18030.Tests;

public class EncodingTests
{
    [Theory]
    [MemberData(nameof(TestHelper.EncodedMemberData), MemberType = typeof(TestHelper))]
    public void Roundtrips(byte[] testData)
    {
        Assert.True(testData.AsSpan().SequenceEqual(
            TestHelper.GB18030Encoding.GetBytes(TestHelper.GB18030Encoding.GetString(testData))));
    }
}

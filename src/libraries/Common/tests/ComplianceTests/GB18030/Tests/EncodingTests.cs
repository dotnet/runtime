using System;
using Xunit;

namespace GB18030.Tests;

public class EncodingTests
{
    [Theory]
    [MemberData(nameof(TestHelper.EncodedTestData), MemberType = typeof(TestHelper))]
    public void Roundtrips(byte[] testData)
    {
        Assert.True(testData.AsSpan().SequenceEqual(
            TestHelper.GB18030Encoding.GetBytes(TestHelper.GB18030Encoding.GetString(testData))));
    }
}

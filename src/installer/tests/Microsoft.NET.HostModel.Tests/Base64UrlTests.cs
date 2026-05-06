using System;
using Xunit;
using Microsoft.NET.HostModel;
using System.Collections.Generic;
using System.Security.Cryptography;

public class Base64UrlTests
{
    public static IEnumerable<object[]> GetTestCases()
    {
        yield return new object[] { Array.Empty<byte>() };
        yield return new object[] { new byte[] { 1 } };
        yield return new object[] { new byte[] { 255, 254, 253, 252 } };
        yield return new object[] { new byte[] { 0, 0, 0, 0 } };
        yield return new object[] { new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 } };
        yield return new object[] { Guid.NewGuid().ToByteArray() };
        yield return new object[] { new byte[256] };
        SHA256 sha256 = SHA256.Create();
        yield return new object[] { sha256.ComputeHash(new byte[1024]) };
        yield return new object[] { sha256.ComputeHash(new byte[2048]) };
    }

    [Theory]
    [MemberData(nameof(GetTestCases))]
    [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Base64Url is not in .NET Framework BCL.")]
    public void CustomBase64UrlEncoderMatchesBCL(byte[] data)
    {
        var expected = System.Buffers.Text.Base64Url.EncodeToString(data);
        var actual = Microsoft.NET.HostModel.Base64Url.EncodeToString(data);

        Assert.Equal(expected, actual);
    }
}

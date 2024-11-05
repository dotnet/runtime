// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.ProtectedDataTests;

public class SpanProtectedDataTests : ProtectedDataTests
{
    protected override byte[] Protect(byte[] plain, byte[]? entropy, DataProtectionScope scope)
    {
        ReadOnlySpan<byte> inputSpan = plain;
        ReadOnlySpan<byte> entropySpan = entropy;
        Span<byte> destination = stackalloc byte[400];
        int written = ProtectedData.Protect(inputSpan, scope, destination, entropySpan);
        return destination.Slice(0, written).ToArray();
    }

    protected override byte[] Unprotect(byte[] encrypted, byte[]? entropy, DataProtectionScope scope)
    {
        ReadOnlySpan<byte> inputSpan = encrypted;
        ReadOnlySpan<byte> entropySpan = entropy;
        Span<byte> destination = stackalloc byte[encrypted.Length];
        int written = ProtectedData.Unprotect(inputSpan, scope, destination, entropySpan);
        return destination.Slice(0, written).ToArray();
    }

    [Fact]
    public void ZeroBufferThrows()
    {
        Assert.Throws<ArgumentException>(() =>
            ProtectedData.Protect([1, 2, 3], DataProtectionScope.CurrentUser, Span<byte>.Empty));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void NearCorrectSizeBufferTests(int delta, bool success)
    {
        if (success)
        {
            Execute(out int original, out int resized);
            Assert.Equal(original, resized);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => Execute(out _, out _));
        }

        return;

        void Execute(out int firstSize, out int secondSize)
        {
            Span<byte> buffer = stackalloc byte[400];
            firstSize = ProtectedData.Protect([1, 2, 3], DataProtectionScope.CurrentUser, buffer);
            secondSize = ProtectedData.Protect([1, 2, 3],
                DataProtectionScope.CurrentUser,
                buffer.Slice(0, firstSize + delta));
        }
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    public void NearCorrectSizeBufferUnprotect(int delta, bool success)
    {
        if (success)
        {
            Execute(out int original, out int resized);
            Assert.Equal(original, resized);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => Execute(out _, out _));
        }

        return;

        void Execute(out int firstSize, out int secondSize)
        {
            Span<byte> buffer = stackalloc byte[400];
            byte[] protectedData = ProtectedData.Protect([1, 2, 3], DataProtectionScope.CurrentUser);
            firstSize = ProtectedData.Unprotect(protectedData, DataProtectionScope.CurrentUser, buffer);
            secondSize = ProtectedData.Unprotect(protectedData,
                DataProtectionScope.CurrentUser,
                buffer.Slice(0, firstSize + delta));
        }
    }
}

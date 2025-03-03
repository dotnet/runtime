// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.ProtectedDataTests
{
    public class TrySpanProtectedDataTests : ProtectedDataTests
    {
        private const int DefaultProtectedBufferSize = 400;

        protected override byte[] Protect(byte[] plain, byte[]? entropy, DataProtectionScope scope)
        {
            ReadOnlySpan<byte> inputSpan = plain;
            ReadOnlySpan<byte> entropySpan = entropy;
            Span<byte> destination = stackalloc byte[DefaultProtectedBufferSize];
            Assert.True(ProtectedData.TryProtect(inputSpan, scope, destination, out int written, entropySpan));
            return destination.Slice(0, written).ToArray();
        }

        protected override byte[] Unprotect(byte[] encrypted, byte[]? entropy, DataProtectionScope scope)
        {
            ReadOnlySpan<byte> inputSpan = encrypted;
            ReadOnlySpan<byte> entropySpan = entropy;
            Span<byte> destination = stackalloc byte[encrypted.Length];
            Assert.True(ProtectedData.TryUnprotect(inputSpan, scope, destination, out int written, entropySpan));
            return destination.Slice(0, written).ToArray();
        }

        [Fact]
        public void ZeroBufferReturnsFalse()
        {
            Assert.False(ProtectedData.TryProtect([1, 2, 3], DataProtectionScope.CurrentUser, Span<byte>.Empty, out _));
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        public void NearCorrectSizeBufferProtect(int delta, bool success)
        {
            byte[]? buffer = new byte[DefaultProtectedBufferSize];

            int originalSize;
            Assert.True(ProtectedData.TryProtect([1, 2, 3],
                DataProtectionScope.CurrentUser,
                buffer,
                out originalSize));

            int resized;
            Assert.Equal(success, ProtectedData.TryProtect([1, 2, 3],
                DataProtectionScope.CurrentUser,
                buffer.AsSpan(0, originalSize + delta),
                out resized));
            if (success)
            {
                Assert.Equal(originalSize, resized);
            }
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        public void NearCorrectSizeBufferUnprotect(int delta, bool success)
        {
            byte[] buffer = new byte[DefaultProtectedBufferSize];
            byte[] protectedData = ProtectedData.Protect([1, 2, 3], DataProtectionScope.CurrentUser);
            int original = ProtectedData.Unprotect(protectedData, DataProtectionScope.CurrentUser, buffer.AsSpan());
            int secondSize;
            Assert.Equal(success, ProtectedData.TryUnprotect(protectedData, DataProtectionScope.CurrentUser, buffer.AsSpan(0, original + delta), out secondSize));
            if (success)
            {
                Assert.Equal(original, secondSize);
            }
        }
    }
}

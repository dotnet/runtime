// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.ProtectedDataTests
{
    public class SpanProtectedDataTests : ProtectedDataTests
    {
        private const int DefaultProtectedBufferSize = 400;

        protected override byte[] Protect(byte[] plain, byte[]? entropy, DataProtectionScope scope)
        {
            ReadOnlySpan<byte> inputSpan = plain;
            ReadOnlySpan<byte> entropySpan = entropy;
            Span<byte> destination = stackalloc byte[DefaultProtectedBufferSize];
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
            AssertExtensions.Throws<ArgumentException>(
                "destination",
                () => ProtectedData.Protect([1, 2, 3], DataProtectionScope.CurrentUser, Span<byte>.Empty));
        }

        [Theory]
        [InlineData(-1, false)]
        [InlineData(0, true)]
        [InlineData(1, true)]
        public void NearCorrectSizeBufferTests(int delta, bool success)
        {
            byte[] buffer = new byte[DefaultProtectedBufferSize];
            int original = ProtectedData.Protect([1, 2, 3], DataProtectionScope.CurrentUser, buffer.AsSpan());

            if (success)
            {
                Assert.Equal(original, ProtectedData.Protect(
                    [1, 2, 3],
                    DataProtectionScope.CurrentUser,
                    buffer.AsSpan(0, original + delta)));
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => ProtectedData.Protect([1, 2, 3], DataProtectionScope.CurrentUser, buffer.AsSpan(0, original + delta)));
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
            if (success)
            {
                Assert.Equal(original, ProtectedData.Unprotect(
                    protectedData,
                    DataProtectionScope.CurrentUser,
                    buffer.AsSpan(0, original + delta)));
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>(
                    "destination",
                    () => ProtectedData.Unprotect(protectedData, DataProtectionScope.CurrentUser, buffer.AsSpan(0, original + delta)));
            }
        }
    }
}

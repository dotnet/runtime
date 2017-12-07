// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Numerics.Tests
{
    public partial class ExtractBytesMembersTests
    {
        [Theory]
        [MemberData(nameof(FromIntTests_MemberData))]
        public void TryWriteBytes_FromIntTests(int i, bool isUnsigned, bool isBigEndian, byte[] expectedBytes) =>
            ValidateGetByteCountAndTryWriteBytes(new BigInteger(i), isUnsigned, isBigEndian, expectedBytes);

        [Theory]
        [MemberData(nameof(FromLongTests_MemberData))]
        public void TryWriteBytes_FromLongTests(long l, bool isUnsigned, bool isBigEndian, byte[] expectedBytes) =>
            ValidateGetByteCountAndTryWriteBytes(new BigInteger(l), isUnsigned, isBigEndian, expectedBytes);

        [Theory]
        [MemberData(nameof(FromStringTests_MemberData))]
        public void TryWriteBytes_FromStringTests(string str, bool isUnsigned, bool isBigEndian, byte[] expectedBytes) =>
            ValidateGetByteCountAndTryWriteBytes(BigInteger.Parse(str), isUnsigned, isBigEndian, expectedBytes);

        private void ValidateGetByteCountAndTryWriteBytes(BigInteger bi, bool isUnsigned, bool isBigEndian, byte[] expectedBytes)
        {
            if (bi.Sign < 0 && isUnsigned)
            {
                Assert.Throws<OverflowException>(() => bi.GetByteCount(isUnsigned));
                Assert.Throws<OverflowException>(() => bi.TryWriteBytes(Span<byte>.Empty, out _, isUnsigned, isBigEndian));
                return;
            }

            byte[] bytes = bi.ToByteArray(isUnsigned, isBigEndian);
            Assert.Equal(expectedBytes, bytes);

            int count = bi.GetByteCount(isUnsigned);
            Assert.Equal(expectedBytes.Length, count);

            Validate(new byte[expectedBytes.Length]); // make sure it works with a span just long enough
            Validate(new byte[expectedBytes.Length + 1]); // make sure it also works with a longer span

            void Validate(Span<byte> destination)
            {
                // Fails if the span is too small
                int bytesWritten;
                Assert.False(bi.TryWriteBytes(destination.Slice(0, expectedBytes.Length - 1), out bytesWritten, isUnsigned, isBigEndian));
                Assert.Equal(0, bytesWritten);

                // Succeeds if the span is sufficiently large
                Assert.True(bi.TryWriteBytes(destination, out bytesWritten, isUnsigned, isBigEndian));
                Assert.Equal(expectedBytes.Length, bytesWritten);
                Assert.Equal<byte>(expectedBytes, destination.Slice(0, bytesWritten).ToArray());
            }
        }
    }
}

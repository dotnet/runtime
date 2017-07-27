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
        public void TryWriteBytes_FromIntTests(int i, byte[] expectedBytes) =>
            ValidateGetByteCountAndTryWriteBytes(new BigInteger(i), expectedBytes);

        [Theory]
        [MemberData(nameof(FromLongTests_MemberData))]
        public void TryWriteBytes_FromLongTests(long l, byte[] expectedBytes) =>
            ValidateGetByteCountAndTryWriteBytes(new BigInteger(l), expectedBytes);

        [Theory]
        public void TryWriteBytes_FromStringTests(string str, byte[] expectedBigEndianBytes)
        {
            byte[] expectedBytes = (byte[])expectedBigEndianBytes.Clone();
            Array.Reverse(expectedBytes);
            ValidateGetByteCountAndTryWriteBytes(BigInteger.Parse(str), expectedBytes);
        }

        private void ValidateGetByteCountAndTryWriteBytes(BigInteger bi, byte[] expectedBytes)
        {
            byte[] bytes = bi.ToByteArray();
            Assert.Equal(expectedBytes, bytes);

            int count = bi.GetByteCount();
            Assert.Equal(expectedBytes.Length, count);

            int bytesWritten;
            for (int i = 0; i < 2; i++)
            {
                Span<byte> destination = i == 0 ?
                    new byte[expectedBytes.Length] : // make sure it works with a span just long enough
                    new byte[expectedBytes.Length + 1]; // make sure it also works with a longer span

                // Fails if the span is too small
                Assert.False(bi.TryWriteBytes(destination.Slice(0, expectedBytes.Length - 1), out bytesWritten));
                Assert.Equal(0, bytesWritten);

                // Succeeds if the span is sufficiently large
                Assert.True(bi.TryWriteBytes(destination, out bytesWritten));
                Assert.Equal(expectedBytes.Length, bytesWritten);
                Assert.Equal<byte>(expectedBytes, destination.Slice(0, bytesWritten).ToArray());
            }
        }
    }
}

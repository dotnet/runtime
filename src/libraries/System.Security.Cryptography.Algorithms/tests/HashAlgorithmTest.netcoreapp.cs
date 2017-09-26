// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public abstract partial class HashAlgorithmTest
    {
        partial void Verify_Span(byte[] input, string output)
        {
            byte[] expected = ByteUtils.HexToByteArray(output);
            byte[] actual;
            int bytesWritten;

            using (HashAlgorithm hash = Create())
            {
                // Too small
                actual = new byte[expected.Length - 1];
                Assert.False(hash.TryComputeHash(input, actual, out bytesWritten));
                Assert.Equal(0, bytesWritten);

                // Just right
                actual = new byte[expected.Length];
                Assert.True(hash.TryComputeHash(input, actual, out bytesWritten));
                Assert.Equal(expected.Length, bytesWritten);
                Assert.Equal(expected, actual);

                // Bigger than needed
                actual = new byte[expected.Length + 1];
                actual[actual.Length - 1] = 42;
                Assert.True(hash.TryComputeHash(input, actual, out bytesWritten));
                Assert.Equal(expected.Length, bytesWritten);
                Assert.Equal(expected, actual.AsSpan().Slice(0, expected.Length).ToArray());
                Assert.Equal(42, actual[actual.Length - 1]);
            }
        }

        [Fact]
        public void ComputeHash_TryComputeHash_HashSetExplicitlyByBoth()
        {
            using (HashAlgorithm hash = Create())
            {
                byte[] input = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();

                byte[] computeHashResult = hash.ComputeHash(input);
                Assert.NotNull(computeHashResult);
                Assert.NotNull(hash.Hash);
                Assert.NotSame(computeHashResult, hash.Hash);
                Assert.Equal(computeHashResult, hash.Hash);

                Assert.True(hash.TryComputeHash(input, computeHashResult, out int bytesWritten));
                Assert.Equal(computeHashResult.Length, bytesWritten);
                Assert.Null(hash.Hash);
            }
        }

        [Fact]
        public void Dispose_TryComputeHash_ThrowsException()
        {
            HashAlgorithm hash = Create();
            hash.Dispose();
            Assert.Throws<ObjectDisposedException>(() => hash.ComputeHash(new byte[1]));
            Assert.Throws<ObjectDisposedException>(() => hash.TryComputeHash(new byte[1], new byte[1], out int bytesWritten));
        }
    }
}

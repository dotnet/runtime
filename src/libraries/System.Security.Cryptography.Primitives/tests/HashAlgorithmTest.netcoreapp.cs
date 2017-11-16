// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Security.Cryptography.Hashing.Tests
{
    public partial class HashAlgorithmTest
    {
        [Fact]
        public void SpanMethodsUsed_NotOverridden_ArrayMethodsInvoked()
        {
            byte[] input = Enumerable.Range(0, 1024).Select(i => (byte)i).ToArray();
            byte[] output;
            int bytesWritten;

            var testAlgorithm = new SummingTestHashAlgorithm();

            output = new byte[sizeof(long) - 1];
            Assert.False(testAlgorithm.TryComputeHash(input, output, out bytesWritten));
            Assert.Equal(0, bytesWritten);
            Assert.Equal<byte>(new byte[sizeof(long) - 1], output);

            output = new byte[sizeof(long)];
            Assert.True(testAlgorithm.TryComputeHash(input, output, out bytesWritten));
            Assert.Equal(sizeof(long), bytesWritten);
            Assert.Equal(input.Sum(b => (long)b), BitConverter.ToInt64(output, 0));
        }

        private sealed class SummingTestHashAlgorithm : HashAlgorithm
        {
            private long _sum;

            public SummingTestHashAlgorithm() => HashSizeValue = sizeof(long)*8;

            public override void Initialize() => _sum = 0;

            protected override void HashCore(byte[] array, int ibStart, int cbSize)
            {
                for (int i = ibStart; i < ibStart + cbSize; i++) _sum += array[i];
            }

            protected override byte[] HashFinal() => BitConverter.GetBytes(_sum);
            
            // Do not override HashCore(ReadOnlySpan) and TryHashFinal.  Consuming
            // test verifies that calling the base implementations invokes the array
            // implementations by verifying the right value is produced.
        }
    }
}

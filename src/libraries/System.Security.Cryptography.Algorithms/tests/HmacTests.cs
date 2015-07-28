// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Test.Cryptography;
using Xunit;

namespace System.Security.Cryptography.Hashing.Algorithms.Tests
{
    public abstract class HmacTests
    {
        // RFC2202 defines the test vectors for HMACMD5 and HMACSHA1
        // RFC4231 defines the test vectors for HMACSHA{224,256,384,512}
        // They share the same datasets for cases 1-5, but cases 6 and 7 differ.
        private readonly byte[][] _testKeys;
        private readonly byte[][] _testData;

        protected HmacTests(byte[][] testKeys, byte[][] testData)
        {
            _testKeys = testKeys;
            _testData = testData;
        }

        protected abstract HMAC Create();

        protected void VerifyHmac(
            int testCaseId,
            string digest,
            int truncateSize = -1)
        {
            byte[] digestBytes = ByteUtils.HexToByteArray(digest);
            byte[] computedDigest;

            using (HMAC hmac = Create())
            {
                Assert.True(hmac.HashSize > 0);

                byte[] key = (byte[])_testKeys[testCaseId].Clone();
                hmac.Key = key;

                // make sure the getter returns different objects each time
                Assert.NotSame(key, hmac.Key); 
                Assert.NotSame(hmac.Key, hmac.Key);

                // make sure the setter didn't cache the exact object we passed in
                key[0] = (byte)(key[0] + 1); 
                Assert.NotEqual<byte>(key, hmac.Key);

                computedDigest = hmac.ComputeHash(_testData[testCaseId]);
            }

            if (truncateSize != -1)
            {
                byte[] tmp = new byte[truncateSize];
                Array.Copy(computedDigest, 0, tmp, 0, truncateSize);
                computedDigest = tmp;
            }

            Assert.Equal(digestBytes, computedDigest);
        }
    }
}

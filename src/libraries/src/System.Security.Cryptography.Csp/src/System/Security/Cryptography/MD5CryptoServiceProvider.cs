// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;

namespace System.Security.Cryptography
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class MD5CryptoServiceProvider : MD5
    {
        private const int HashSizeBits = 128;
        private readonly IncrementalHash _incrementalHash;

        public MD5CryptoServiceProvider()
        {
            _incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
        }

        public override void Initialize()
        {
            // Nothing to do here. We expect HashAlgorithm to invoke HashFinal() and Initialize() as a pair. This reflects the 
            // reality that our native crypto providers (e.g. CNG) expose hash finalization and object reinitialization as an atomic operation.
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _incrementalHash.AppendData(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            return _incrementalHash.GetHashAndReset();
        }

        // The Hash property is not overridden since the correct value exists on base.
        public override int HashSize => HashSizeBits;

        protected sealed override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _incrementalHash.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}

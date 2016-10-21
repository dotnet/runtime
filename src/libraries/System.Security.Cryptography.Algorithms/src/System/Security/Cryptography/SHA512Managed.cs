// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;
using System.ComponentModel;

namespace System.Security.Cryptography
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    // SHA512Managed has a copy of the same implementation as SHA512
    public sealed class SHA512Managed : SHA512
    {
        private readonly HashProvider _hashProvider;

        public SHA512Managed()
        {
            _hashProvider = HashProviderDispenser.CreateHashProvider(HashAlgorithmNames.SHA512);
        }

        public sealed override int HashSize
        {
            get
            {
                return _hashProvider.HashSizeInBytes * 8;
            }
        }

        protected sealed override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _hashProvider.AppendHashData(array, ibStart, cbSize);
        }

        protected sealed override byte[] HashFinal()
        {
            return _hashProvider.FinalizeHashAndReset();
        }

        public sealed override void Initialize()
        {
            // Nothing to do here. We expect HashAlgorithm to invoke HashFinal() and Initialize() as a pair. This reflects the 
            // reality that our native crypto providers (e.g. CNG) expose hash finalization and object reinitialization as an atomic operation.
            return;
        }

        protected sealed override void Dispose(bool disposing)
        {
            _hashProvider.Dispose(disposing);
            base.Dispose(disposing);
        }
    }
}

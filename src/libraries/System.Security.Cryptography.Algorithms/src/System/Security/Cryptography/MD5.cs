// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    //
    // If you change anything in this class, you must make the same change in the other *Provider classes. This is a pain but given that the
    // preexisting contract from the desktop locks all of these into deriving directly from the abstract HashAlgorithm class, 
    // it can't be helped.
    //

    public abstract class MD5 : HashAlgorithm
    {
        protected MD5() { }

        public static new MD5 Create() => new Implementation();

        public static new MD5 Create(string algName) => (MD5)CryptoConfig.CreateFromName(algName);

        private sealed class Implementation : MD5
        {
            private readonly HashProvider _hashProvider;

            public Implementation()
            {
                _hashProvider = HashProviderDispenser.CreateHashProvider(HashAlgorithmNames.MD5);
                HashSizeValue = _hashProvider.HashSizeInBytes * 8;
            }

            protected sealed override void HashCore(byte[] array, int ibStart, int cbSize) =>
                _hashProvider.AppendHashData(array, ibStart, cbSize);

            protected sealed override void HashCore(ReadOnlySpan<byte> source) =>
                _hashProvider.AppendHashData(source);

            protected sealed override byte[] HashFinal() =>
                _hashProvider.FinalizeHashAndReset();

            protected sealed override bool TryHashFinal(Span<byte> destination, out int bytesWritten) =>
                _hashProvider.TryFinalizeHashAndReset(destination, out bytesWritten);

            public sealed override void Initialize()
            {
                // Nothing to do here. We expect HashAlgorithm to invoke HashFinal() and Initialize() as a pair. This reflects the 
                // reality that our native crypto providers (e.g. CNG) expose hash finalization and object reinitialization as an atomic operation.
            }

            protected sealed override void Dispose(bool disposing)
            {
                _hashProvider.Dispose(disposing);
                base.Dispose(disposing);
            }
        }
    }
}

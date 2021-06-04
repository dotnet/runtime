// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using System.ComponentModel;

namespace System.Security.Cryptography
{
    [Obsolete(Obsoletions.DerivedCryptographicTypesMessage, DiagnosticId = Obsoletions.DerivedCryptographicTypesDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    // SHA1Managed has a copy of the same implementation as SHA1
    public sealed class SHA1Managed : SHA1
    {
        private readonly HashProvider _hashProvider;

        public SHA1Managed()
        {
            _hashProvider = HashProviderDispenser.CreateHashProvider(HashAlgorithmNames.SHA1);
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

        public sealed override void Initialize() => _hashProvider.Reset();

        protected sealed override void Dispose(bool disposing)
        {
            _hashProvider.Dispose(disposing);
            base.Dispose(disposing);
        }
    }
}

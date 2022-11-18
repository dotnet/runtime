// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    [Obsolete(Obsoletions.DerivedCryptographicTypesMessage, DiagnosticId = Obsoletions.DerivedCryptographicTypesDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class MD5CryptoServiceProvider : MD5
    {
        private readonly IncrementalHash _incrementalHash;
        private bool _running;

        public MD5CryptoServiceProvider()
        {
            _incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.MD5);
            HashSizeValue = HashSizeInBits;
        }

        public override void Initialize()
        {
            if (_running)
            {
                Span<byte> destination = stackalloc byte[HashSizeInBytes];

                if (!_incrementalHash.TryGetHashAndReset(destination, out _))
                {
                    Debug.Fail("Reset expected a properly sized buffer.");
                    throw new CryptographicException();
                }

                _running = false;
            }
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _running = true;
            _incrementalHash.AppendData(array, ibStart, cbSize);
        }

        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            _running = true;
            _incrementalHash.AppendData(source);
        }

        protected override byte[] HashFinal()
        {
            _running = false;
            return _incrementalHash.GetHashAndReset();
        }

        protected override bool TryHashFinal(Span<byte> destination, out int bytesWritten)
        {
            _running = false;
            return _incrementalHash.TryGetHashAndReset(destination, out bytesWritten);
        }

        // The Hash and HashSize properties are not overridden since the correct values are returned from base.

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _incrementalHash.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}

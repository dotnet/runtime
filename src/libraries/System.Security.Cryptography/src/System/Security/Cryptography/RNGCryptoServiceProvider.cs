// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Security.Cryptography
{
    [Obsolete(Obsoletions.RNGCryptoServiceProviderMessage, DiagnosticId = Obsoletions.RNGCryptoServiceProviderDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class RNGCryptoServiceProvider : RandomNumberGenerator
    {
        private readonly RandomNumberGenerator _impl;

        public RNGCryptoServiceProvider() : this((CspParameters?)null) { }
        public RNGCryptoServiceProvider(string str) : this((CspParameters?)null) { }
        public RNGCryptoServiceProvider(byte[] rgb) : this((CspParameters?)null) { }

        public RNGCryptoServiceProvider(CspParameters? cspParams)
        {
            if (cspParams != null)
                throw new PlatformNotSupportedException();

            // This class wraps RandomNumberGenerator.Create() from Algorithms assembly
            _impl = Create();
        }

        public override void GetBytes(byte[] data) => _impl.GetBytes(data);
        public override void GetBytes(byte[] data, int offset, int count) => _impl.GetBytes(data, offset, count);
        public override void GetBytes(Span<byte> data) => _impl.GetBytes(data);
        public override void GetNonZeroBytes(byte[] data) => _impl.GetNonZeroBytes(data);
        public override void GetNonZeroBytes(Span<byte> data) => _impl.GetNonZeroBytes(data);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _impl.Dispose();
                base.Dispose(disposing);
            }
        }
    }
}

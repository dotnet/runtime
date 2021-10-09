// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography.Pal
{
    internal sealed partial class AppleCertificatePal : ICertificatePal
    {
        public DSA? GetDSAPrivateKey()
        {
            if (_identityHandle == null)
                return null;

            throw new PlatformNotSupportedException();
        }

        public ICertificatePal CopyWithPrivateKey(DSA privateKey)
        {
            throw new PlatformNotSupportedException();
        }

        public ICertificatePal CopyWithPrivateKey(ECDsa privateKey)
        {
            return ImportPkcs12(new UnixPkcs12Reader.CertAndKey { Cert = this, Key = privateKey });
        }

        public ICertificatePal CopyWithPrivateKey(ECDiffieHellman privateKey)
        {
            return ImportPkcs12(new UnixPkcs12Reader.CertAndKey { Cert = this, Key = privateKey });
        }

        public ICertificatePal CopyWithPrivateKey(RSA privateKey)
        {
            return ImportPkcs12(new UnixPkcs12Reader.CertAndKey { Cert = this, Key = privateKey });
        }
    }
}

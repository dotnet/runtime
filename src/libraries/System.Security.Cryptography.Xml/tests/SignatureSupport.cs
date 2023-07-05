// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class SignatureSupport
    {
        private static int _supportsRsaSha1Signatures = 0;

        public static bool SupportsRsaSha1Signatures
        {
            get
            {
                if (_supportsRsaSha1Signatures == 0)
                {
                    bool supported;
                    try
                    {
                        using var rsa = RSA.Create();
                        rsa.SignData(Array.Empty<byte>(), HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1);
                        supported = true;
                    }
                    catch (CryptographicException)
                    {
                        supported = false;
                    }

                    _supportsRsaSha1Signatures = supported ? 1 : -1;
                }
                return _supportsRsaSha1Signatures == 1;
            }
        }
    }
}

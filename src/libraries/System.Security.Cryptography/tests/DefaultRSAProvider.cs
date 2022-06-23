// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Tests;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class DefaultRSAProvider : IRSAProvider
    {
        private bool? _supports384PrivateKey;
        private bool? _supportsSha1Signatures;

        public RSA Create() => RSA.Create();

        public RSA Create(int keySize)
        {
#if NETCOREAPP
            return RSA.Create(keySize);
#else
            RSA rsa = Create();

            rsa.KeySize = keySize;
            return rsa;
#endif
        }

        public bool Supports384PrivateKey
        {
            get
            {
                if (!_supports384PrivateKey.HasValue)
                {
                    // For Windows 7 (Microsoft Windows 6.1) and Windows 8 (Microsoft Windows 6.2) this is false for RSACng.
                    _supports384PrivateKey = !RuntimeInformation.OSDescription.Contains("Windows 6.1") &&
                        !RuntimeInformation.OSDescription.Contains("Windows 6.2");
                }

                return _supports384PrivateKey.Value;
            }
        }

        public bool SupportsSha1Signatures => _supportsSha1Signatures ??= SignatureSupport.CanProduceSha1Signature(Create());

        public bool SupportsLargeExponent => true;

        public bool SupportsSha2Oaep { get; } = true;

        public bool SupportsPss { get; } = true;
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new DefaultRSAProvider();
    }
}

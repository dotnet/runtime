// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class DefaultRSAProvider : IRSAProvider
    {
        private bool? _supports384PrivateKey;

        public RSA Create()
        {
            return RSA.Create();
        }

        public RSA Create(int keySize)
        {
            RSA rsa = Create();
            rsa.KeySize = keySize;
            return rsa;
        }

        public bool Supports384PrivateKey
        {
            get
            {
                if (!_supports384PrivateKey.HasValue)
                {
                    bool hasSupport = true;

                    // For Windows 7 (Microsoft Windows 6.1) this is false for RSACng.
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        hasSupport = !RuntimeInformation.OSDescription.Contains("Windows 6.1");
                    }

                    _supports384PrivateKey = hasSupport;
                }

                return _supports384PrivateKey.Value;
            }
        }

        public bool SupportsSha2Oaep
        {
            // Currently only RSACng does, which is the default provider on Windows.
            get { return RuntimeInformation.IsOSPlatform(OSPlatform.Windows); }
        }
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new DefaultRSAProvider();
    }
}

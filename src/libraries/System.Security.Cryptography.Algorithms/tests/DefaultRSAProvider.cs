// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class DefaultRSAProvider : IRSAProvider
    {
        private bool? _supports384PrivateKey;

        public RSA Create() => RSA.Create();

        public RSA Create(int keySize)
        {
#if netcoreapp
            return RSA.Create(keySize);
#else
            RSA rsa = Create();

            if (PlatformDetection.IsFullFramework && rsa is RSACryptoServiceProvider)
            {
                rsa.Dispose();
                return new RSACryptoServiceProvider(keySize);
            }
            
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

        public bool SupportsSha2Oaep
        {
            // Currently only RSACng does, which is the default provider on Windows.
            get { return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !(Create() is RSACryptoServiceProvider); }
        }

        public bool SupportsDecryptingIntoExactSpaceRequired => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new DefaultRSAProvider();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.Rsa.Tests
{
    public class RSACngProvider : IRSAProvider
    {
        private bool? _supports384PrivateKey;

        public RSA Create()
        {
            return new RSACng();
        }

        public RSA Create(int keySize)
        {
            return new RSACng(keySize);
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
            get { return true; }
        }
    }

    public partial class RSAFactory
    {
        private static readonly IRSAProvider s_provider = new RSACngProvider();
    }
}

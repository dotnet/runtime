// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Rsa.Tests
{
    public abstract class RSAProvider
    {
        private bool? _supports16384;

        public abstract RSA Create();
        public abstract RSA Create(int keySize);
        public abstract bool Supports384PrivateKey { get; }
        public abstract bool SupportsLargeExponent { get; }
        public abstract bool SupportsSha2Oaep { get; }
        public abstract bool SupportsPss { get; }
        public abstract bool SupportsSha1Signatures { get; }
        public abstract bool SupportsMd5Signatures { get; }
        public abstract bool SupportsSha3 { get; }

        public bool NoSupportsSha3 => !SupportsSha3;

        public bool Supports16384 => _supports16384 ??= TestRsa16384();

        public RSA Create(RSAParameters rsaParameters)
        {
            RSA rsa = Create();
            rsa.ImportParameters(rsaParameters);
            return rsa;
        }

        private bool TestRsa16384()
        {
            if (PlatformDetection.IsAndroid)
            {
                // We cannot detect this on Android at the moment. Even attempting to generate or import a 16K RSA key
                // may leave the error queue in the incorrect state. See https://github.com/google/conscrypt/issues/1507
                return false;
            }

            try
            {
                using (RSA rsa = Create())
                {
                    rsa.ImportParameters(TestData.RSA16384Params);
                }

                return true;
            }
            catch (Exception e) when (e is CryptographicException or PlatformNotSupportedException)
            {
                // The key is too big for this platform or the platform is not supported.
                return false;
            }
        }
    }
}

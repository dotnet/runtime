// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public class ECDsaOpenSslProvider : ECDsaProvider
    {
        public static readonly ECDsaOpenSslProvider Instance = new ECDsaOpenSslProvider();

        private ECDsaOpenSslProvider() { }

        public override ECDsa Create()
        {
            return new ECDsaOpenSsl();
        }

        public override ECDsa Create(int keySize)
        {
            return new ECDsaOpenSsl(keySize);
        }

        public override ECDsa Create(ECCurve curve)
        {
            return new ECDsaOpenSsl(curve);
        }

        public override bool IsCurveValid(Oid oid)
        {
            if (!string.IsNullOrEmpty(oid.Value))
            {
                // Value is passed before FriendlyName
                return IsValueOrFriendlyNameValid(oid.Value);
            }
            return IsValueOrFriendlyNameValid(oid.FriendlyName);
        }

        private static bool IsValueOrFriendlyNameValid(string friendlyNameOrValue)
        {
            if (string.IsNullOrEmpty(friendlyNameOrValue))
            {
                return false;
            }

            IntPtr key = Interop.Crypto.EcKeyCreateByOid(friendlyNameOrValue);
            if (key != IntPtr.Zero)
            {
                Interop.Crypto.EcKeyDestroy(key);
                return true;
            }
            return false;
        }

        public override bool ExplicitCurvesSupported => PlatformDetection.IsNotSymCryptOpenSsl;

    }
}

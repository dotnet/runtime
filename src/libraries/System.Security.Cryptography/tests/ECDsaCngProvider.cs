// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public class ECDsaCngProvider : ECDsaProvider
    {
        public static readonly ECDsaCngProvider Instance = new ECDsaCngProvider();

        private ECDsaCngProvider() { }

        public override ECDsa Create()
        {
            return new ECDsaCng();
        }

        public override ECDsa Create(int keySize)
        {
            return new ECDsaCng(keySize);
        }

        public override ECDsa Create(ECCurve curve)
        {
            return new ECDsaCng(curve);
        }

        public override bool IsCurveValid(Oid oid)
        {
            // Friendly name required for windows
            return NativeOidFriendlyNameExists(oid.FriendlyName);
        }

        public override bool ExplicitCurvesSupported
        {
            get
            {
                return PlatformDetection.WindowsVersion >= 10;
            }
        }

        private static bool NativeOidFriendlyNameExists(string oidFriendlyName)
        {
            if (string.IsNullOrEmpty(oidFriendlyName))
                return false;

            try
            {
                // By specifying OidGroup.PublicKeyAlgorithm, no caches are used
                // Note: this throws when there is no oid value, even when friendly name is valid
                // so it cannot be used for curves with no oid value such as curve25519
                return !string.IsNullOrEmpty(Oid.FromFriendlyName(oidFriendlyName, OidGroup.PublicKeyAlgorithm).FriendlyName);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

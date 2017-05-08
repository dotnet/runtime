// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public class ECDsaProvider : IECDsaProvider
    {
        public ECDsa Create()
        {
            return new ECDsaCng();
        }

        public ECDsa Create(int keySize)
        {
            return new ECDsaCng(keySize);
        }

#if netcoreapp
        public ECDsa Create(ECCurve curve)
        {
            return new ECDsaCng(curve);
        }
#endif

        public bool IsCurveValid(Oid oid)
        {
            // Friendly name required for windows
            return NativeOidFriendlyNameExists(oid.FriendlyName);
        }

        public bool ExplicitCurvesSupported
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

    public partial class ECDsaFactory
    {
        private static readonly IECDsaProvider s_provider = new ECDsaProvider();
    }
}

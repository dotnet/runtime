// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public class ECDiffieHellmanProvider : IECDiffieHellmanProvider
    {
        public ECDiffieHellman Create()
        {
            return new ECDiffieHellmanCng();
        }

        public ECDiffieHellman Create(int keySize)
        {
            return new ECDiffieHellmanCng(keySize);
        }

#if NETCOREAPP
        public ECDiffieHellman Create(ECCurve curve)
        {
            return new ECDiffieHellmanCng(curve);
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

        public bool CanDeriveNewPublicKey => true;

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

    public partial class ECDiffieHellmanFactory
    {
        private static readonly IECDiffieHellmanProvider s_provider = new ECDiffieHellmanProvider();
    }
}

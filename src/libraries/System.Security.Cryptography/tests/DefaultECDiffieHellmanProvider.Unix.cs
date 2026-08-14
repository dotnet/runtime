// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class DefaultECDiffieHellmanProvider : ECDiffieHellmanProvider
    {
        public override bool IsCurveValid(Oid oid)
        {
            if (PlatformDetection.IsApplePlatform)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(oid.Value))
            {
                // Value is passed before FriendlyName
                return IsValueOrFriendlyNameValid(oid.Value);
            }
            return IsValueOrFriendlyNameValid(oid.FriendlyName);
        }

        public override bool ExplicitCurvesSupported
        {
            get
            {
                if (PlatformDetection.IsApplePlatform || PlatformDetection.IsSymCryptOpenSsl)
                {
                    return false;
                }

                return true;
            }
        }

        public override bool CanDeriveNewPublicKey => !PlatformDetection.IsiOS && !PlatformDetection.IstvOS && !PlatformDetection.IsMacCatalyst;
        public override bool SupportsRawDerivation => true;
        public override bool SupportsSha3 => PlatformDetection.SupportsSha3;

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
    }
}

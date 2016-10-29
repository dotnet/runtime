// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.EcDsa.Tests
{
    public class ECDsaProvider : IECDsaProvider
    {
        public ECDsa Create()
        {
            return ECDsa.Create();
        }

        public ECDsa Create(int keySize)
        {
            ECDsa ec = Create();
            ec.KeySize = keySize;
            return ec;
        }

        public ECDsa Create(ECCurve curve)
        {
            return ECDsa.Create(curve);
        }

        public bool IsCurveValid(Oid oid)
        {
            if (PlatformDetection.IsWindows)
            {
                // Friendly name required for windows
                return NativeOidFriendlyNameExists(oid.FriendlyName);
            }
            if (!string.IsNullOrEmpty(oid.Value))
            {
                // Value is passed before FriendlyName
                return IsValueOrFriendlyNameValid(oid.Value);
            }
            return IsValueOrFriendlyNameValid(oid.FriendlyName);
        }

        public bool ExplicitCurvesSupported
        {
            get
            {
                if (PlatformDetection.IsWindows)
                {
                    return PlatformDetection.WindowsVersion >= 10;
                }
                return true;
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

    public partial class ECDsaFactory
    {
        private static readonly IECDsaProvider s_provider = new ECDsaProvider();
    }

}

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyCreateByOid")]
        internal static extern System.IntPtr EcKeyCreateByOid(string oid);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcKeyDestroy")]
        internal static extern void EcKeyDestroy(System.IntPtr r);
    }
}

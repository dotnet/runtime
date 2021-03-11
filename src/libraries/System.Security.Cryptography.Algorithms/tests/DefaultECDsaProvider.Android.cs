// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.EcDsa.Tests
{
    public partial class ECDsaProvider : IECDsaProvider
    {
        public bool IsCurveValid(Oid oid)
        {
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
                if (PlatformDetection.IsOSXLike)
                {
                    return false;
                }

                return true;
            }
        }

        private static bool IsValueOrFriendlyNameValid(string friendlyNameOrValue)
        {
            if (string.IsNullOrEmpty(friendlyNameOrValue))
            {
                return false;
            }

            IntPtr key = Interop.AndroidCrypto.EcKeyCreateByOid(friendlyNameOrValue);
            if (key != IntPtr.Zero)
            {
                Interop.AndroidCrypto.EcKeyDestroy(key);
                return true;
            }
            return false;
        }
    }
}

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_EcKeyCreateByOid")]
        internal static extern System.IntPtr EcKeyCreateByOid(string oid);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_EcKeyDestroy")]
        internal static extern void EcKeyDestroy(System.IntPtr r);
    }
}

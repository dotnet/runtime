// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography.Tests;

namespace System.Security.Cryptography.EcDsa.Tests
{
    public partial class DefaultECDsaProvider : ECDsaProvider
    {
        private static readonly Lazy<bool> s_explicitCurvesSupported = new(ProbeExplicitCurveImport);

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

        public override bool ExplicitCurvesSupported => !PlatformDetection.IsApplePlatform && OpenSslExplicitCurvesSupported;

        internal static bool OpenSslExplicitCurvesSupported => !PlatformDetection.IsSymCryptOpenSsl || s_explicitCurvesSupported.Value;

        // Azure Linux can have SymCrypt installed without OpenSSL selecting it for EC.
        // SymCrypt rejects explicit curves, so probe support by importing an explicit P-256 key.
        private static bool ProbeExplicitCurveImport()
        {
            using (ECDsa key = ECDsa.Create())
            {
                try
                {
                    key.ImportParameters(EccTestData.GetNistP256ExplicitTestData());
                }
                catch (CryptographicException)
                {
                    return false;
                }
                catch (PlatformNotSupportedException)
                {
                    return false;
                }
            }

            return true;
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

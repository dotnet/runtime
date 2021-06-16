// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanProvider : IECDiffieHellmanProvider
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

        public bool ExplicitCurvesSupported => true;

        public bool CanDeriveNewPublicKey => false;

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

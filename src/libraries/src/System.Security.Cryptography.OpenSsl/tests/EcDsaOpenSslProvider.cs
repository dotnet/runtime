// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public class ECDsaProvider : IECDsaProvider
    {
        public ECDsa Create()
        {
            return new ECDsaOpenSsl();
        }

        public ECDsa Create(int keySize)
        {
            return new ECDsaOpenSsl(keySize);
        }

        public ECDsa Create(ECCurve curve)
        {
            return new ECDsaOpenSsl(curve);
        }

        public bool IsCurveValid(Oid oid)
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

        public bool ExplicitCurvesSupported
        {
            get
            {
                return true;
            }
        }
    }

    public partial class ECDsaFactory
    {
        private static readonly IECDsaProvider s_provider = new ECDsaProvider();
    }
}

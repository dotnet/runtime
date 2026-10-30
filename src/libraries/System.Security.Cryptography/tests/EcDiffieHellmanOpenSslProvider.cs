// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public sealed class ECDiffieHellmanOpenSslProvider : ECDiffieHellmanProvider
    {
        public static readonly ECDiffieHellmanOpenSslProvider Instance = new ECDiffieHellmanOpenSslProvider();

        private ECDiffieHellmanOpenSslProvider() { }

        public override ECDiffieHellman Create()
        {
            return new ECDiffieHellmanOpenSsl();
        }

        public override ECDiffieHellman Create(int keySize)
        {
            return new ECDiffieHellmanOpenSsl(keySize);
        }

        public override ECDiffieHellman Create(ECCurve curve)
        {
            return new ECDiffieHellmanOpenSsl(curve);
        }

        public override bool IsCurveValid(Oid oid) => EcDsa.Tests.ECDsaOpenSslProvider.Instance.IsCurveValid(oid);

        public override bool ExplicitCurvesSupported => EcDsa.Tests.ECDsaOpenSslProvider.Instance.ExplicitCurvesSupported;

        public override bool CanDeriveNewPublicKey => true;
        public override bool SupportsRawDerivation => true;
        public override bool SupportsSha3 => PlatformDetection.SupportsSha3;
    }
}

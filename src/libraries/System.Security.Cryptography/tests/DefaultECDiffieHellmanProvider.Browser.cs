// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class DefaultECDiffieHellmanProvider : ECDiffieHellmanProvider
    {
        public override bool IsCurveValid(Oid oid) => false;
        public override bool ExplicitCurvesSupported => false;
        public override bool CanDeriveNewPublicKey => false;
        public override bool SupportsRawDerivation => false;
        public override bool SupportsSha3 => false;
    }
}

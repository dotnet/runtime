// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.EcDiffieHellman.Tests
{
    public partial class ECDiffieHellmanProvider : IECDiffieHellmanProvider
    {
        public bool IsCurveValid(Oid oid) => false;
        public bool ExplicitCurvesSupported => false;
        public bool CanDeriveNewPublicKey => false;
        public bool SupportsRawDerivation => false;
        public bool SupportsSha3 =>  false;
    }
}

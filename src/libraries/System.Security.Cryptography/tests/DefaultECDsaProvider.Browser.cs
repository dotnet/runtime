// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public partial class DefaultECDsaProvider : ECDsaProvider
    {
        public override bool IsCurveValid(Oid oid) => false;
        public override bool ExplicitCurvesSupported => false;
    }
}

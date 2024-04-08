// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Security.Cryptography.EcDsa.Tests
{
    public partial class ECDsaProvider : IECDsaProvider
    {
        public bool IsCurveValid(Oid oid) => false;
        public bool ExplicitCurvesSupported => false;
        private static bool IsValueOrFriendlyNameValid(string friendlyNameOrValue) => false;
    }
}

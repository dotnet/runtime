// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal sealed class HpkeImplementation : Hpke
    {
        private HpkeImplementation(HpkeSuite suite) : base(suite)
        {
        }

        internal static bool IsSupportedImpl(HpkeSuite suite) =>
            suite.KemMetadata.IsSupported &&
            suite.KdfMetadata.IsSupported &&
            suite.AeadMetadata.IsSupported;
    }
}

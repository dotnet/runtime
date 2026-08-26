// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography.EcDsa.Tests
{
    public abstract class ECDsaProvider
    {
        public abstract ECDsa Create();
        public abstract ECDsa Create(int keySize);
        public abstract ECDsa Create(ECCurve curve);
        public abstract bool IsCurveValid(Oid oid);
        public abstract bool ExplicitCurvesSupported { get; }

        // In OSSL 3+ we use EVP_PKEY APIs instead of EC_KEY APIs so import and export of explicit curves also fails for SymCrypt.
        public static bool ExplicitCurvesSupportFailOnUseOnly => PlatformDetection.IsSymCryptOpenSsl && SafeEvpPKeyHandle.OpenSslVersion < 0x3_00_00_00_0;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public interface IECDsaProvider
    {
        ECDsa Create();
        ECDsa Create(int keySize);
#if NET
        ECDsa Create(ECCurve curve);
#endif
        bool IsCurveValid(Oid oid);
        bool ExplicitCurvesSupported { get; }

        // In OSSL 3+ we use EVP_PKEY APIs instead of EC_KEY APIs so import and export of explicit curves also fails for SymCrypt.
        bool ExplicitCurvesSupportFailOnUseOnly => PlatformDetection.IsSymCryptOpenSsl && SafeEvpPKeyHandle.OpenSslVersion < 0x3_00_00_00_0;
    }

    public static partial class ECDsaFactory
    {
        public static ECDsa Create()
        {
            return s_provider.Create();
        }

        public static ECDsa Create(int keySize)
        {
            return s_provider.Create(keySize);
        }

#if NET
        public static ECDsa Create(ECCurve curve)
        {
            return s_provider.Create(curve);
        }
#endif

        public static bool IsCurveValid(Oid oid)
        {
            return s_provider.IsCurveValid(oid);
        }

        public static bool ExplicitCurvesSupported => s_provider.ExplicitCurvesSupported;
        public static bool ExplicitCurvesSupportFailOnUseOnly => s_provider.ExplicitCurvesSupportFailOnUseOnly;
    }
}

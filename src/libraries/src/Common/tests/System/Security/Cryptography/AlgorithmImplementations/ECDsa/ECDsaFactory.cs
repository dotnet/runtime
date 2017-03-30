// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography.EcDsa.Tests
{
    public interface IECDsaProvider
    {
        ECDsa Create();
        ECDsa Create(int keySize);
#if netcoreapp
        ECDsa Create(ECCurve curve);
#endif
        bool IsCurveValid(Oid oid);
        bool ExplicitCurvesSupported { get; }
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

#if netcoreapp
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
    }
}

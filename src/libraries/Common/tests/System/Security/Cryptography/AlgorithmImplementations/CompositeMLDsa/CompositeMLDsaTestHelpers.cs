// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Security.Cryptography.Tests
{
    internal static class CompositeMLDsaTestHelpers
    {
        // TODO rename this dictionaries
        internal static Dictionary<CompositeMLDsaAlgorithm, byte[]> DomainSeparators = new()
        {
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss, Convert.FromHexString("060B6086480186FA6B50090100") },
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15, Convert.FromHexString("060B6086480186FA6B50090101") },
            { CompositeMLDsaAlgorithm.MLDsa44WithEd25519, Convert.FromHexString("060B6086480186FA6B50090102") },
            { CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256, Convert.FromHexString("060B6086480186FA6B50090103") },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss, Convert.FromHexString("060B6086480186FA6B50090104") },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15, Convert.FromHexString("060B6086480186FA6B50090105") },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss, Convert.FromHexString("060B6086480186FA6B50090106") },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15, Convert.FromHexString("060B6086480186FA6B50090107") },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256, Convert.FromHexString("060B6086480186FA6B50090108") },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384, Convert.FromHexString("060B6086480186FA6B50090109") },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1, Convert.FromHexString("060B6086480186FA6B5009010A") },
            { CompositeMLDsaAlgorithm.MLDsa65WithEd25519, Convert.FromHexString("060B6086480186FA6B5009010B") },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384, Convert.FromHexString("060B6086480186FA6B5009010C") },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1, Convert.FromHexString("060B6086480186FA6B5009010D") },
            { CompositeMLDsaAlgorithm.MLDsa87WithEd448, Convert.FromHexString("060B6086480186FA6B5009010E") },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss, Convert.FromHexString("060B6086480186FA6B5009010F") },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss, Convert.FromHexString("060B6086480186FA6B50090110") },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521, Convert.FromHexString("060B6086480186FA6B50090111") },
        };

        internal static Dictionary<CompositeMLDsaAlgorithm, HashAlgorithmName> HashAlgorithms = new()
        {
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss, HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15, HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa44WithEd25519, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256, HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithEd25519, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithEd448, HashAlgorithmName.SHA512 }, // TODO shake
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521, HashAlgorithmName.SHA512 },
        };

        internal static Dictionary<CompositeMLDsaAlgorithm, HashAlgorithmName> TradHashAlgorithms = new()
        {
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss, HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15, HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256, HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15, HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15, HashAlgorithmName.SHA384 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256, HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384, HashAlgorithmName.SHA384 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1, HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384, HashAlgorithmName.SHA384 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1, HashAlgorithmName.SHA384 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss, HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521, HashAlgorithmName.SHA512 },
        };

        internal static Dictionary<CompositeMLDsaAlgorithm, RSASignaturePadding> RsaPadding = new()
        {
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss, RSASignaturePadding.Pss },
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15, RSASignaturePadding.Pkcs1 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss, RSASignaturePadding.Pss },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15, RSASignaturePadding.Pkcs1 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss, RSASignaturePadding.Pss },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15, RSASignaturePadding.Pkcs1 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss, RSASignaturePadding.Pss },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss, RSASignaturePadding.Pss },
        };

        internal static Dictionary<CompositeMLDsaAlgorithm, MLDsaAlgorithm> MLDsaAlgorithms = new()
        {
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss, MLDsaAlgorithm.MLDsa44 },
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15, MLDsaAlgorithm.MLDsa44 },
            { CompositeMLDsaAlgorithm.MLDsa44WithEd25519, MLDsaAlgorithm.MLDsa44 },
            { CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256, MLDsaAlgorithm.MLDsa44 },

            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss, MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15, MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss, MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15, MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256, MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384, MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1, MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithEd25519, MLDsaAlgorithm.MLDsa65 },

            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384, MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1, MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithEd448, MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss, MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss, MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521, MLDsaAlgorithm.MLDsa87 },
        };

        internal static void ExecuteComponentAction(CompositeMLDsaAlgorithm algo, Action rsaFunc, Action ecdsaFunc, Action eddsaFunc)
        {
            ExecuteComponentFunc(
                algo,
                () => { rsaFunc(); return true; },
                () => { ecdsaFunc(); return true; },
                () => { eddsaFunc(); return true; });
        }

        internal static T ExecuteComponentFunc<T>(CompositeMLDsaAlgorithm algo, Func<T> rsaFunc, Func<T> ecdsaFunc, Func<T> eddsaFunc)
        {
            // TODO hardcode the algorithms instead of Contains
            if (algo.Name.Contains("RSA"))
            {
                return rsaFunc();
            }
            else if (algo.Name.Contains("ECDSA"))
            {
                return ecdsaFunc();
            }
            else if (algo.Name.Contains("Ed"))
            {
                return eddsaFunc();
            }
            else
            {
                throw new NotSupportedException($"Unsupported algorithm: {algo}");
            }
        }
    }
}

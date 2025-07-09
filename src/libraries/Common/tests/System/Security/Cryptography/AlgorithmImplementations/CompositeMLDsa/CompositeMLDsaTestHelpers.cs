// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Test.Cryptography;

namespace System.Security.Cryptography.Tests
{
    internal static class CompositeMLDsaTestHelpers
    {
        // TODO remove these dictionaries
        internal static Dictionary<CompositeMLDsaAlgorithm, byte[]> DomainSeparators = new()
        {
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss,            "060B6086480186FA6B50090100".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15,         "060B6086480186FA6B50090101".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa44WithEd25519,               "060B6086480186FA6B50090102".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256,             "060B6086480186FA6B50090103".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss,            "060B6086480186FA6B50090104".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15,         "060B6086480186FA6B50090105".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss,            "060B6086480186FA6B50090106".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15,         "060B6086480186FA6B50090107".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256,             "060B6086480186FA6B50090108".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384,             "060B6086480186FA6B50090109".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1,  "060B6086480186FA6B5009010A".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa65WithEd25519,               "060B6086480186FA6B5009010B".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384,             "060B6086480186FA6B5009010C".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1,  "060B6086480186FA6B5009010D".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa87WithEd448,                 "060B6086480186FA6B5009010E".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss,            "060B6086480186FA6B5009010F".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss,            "060B6086480186FA6B50090110".HexToByteArray() },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521,             "060B6086480186FA6B50090111".HexToByteArray() },
        };

        internal static Dictionary<CompositeMLDsaAlgorithm, HashAlgorithmName> HashAlgorithms = new()
        {
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss,            HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15,         HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa44WithEd25519,               HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256,             HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss,            HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15,         HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss,            HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15,         HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256,             HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384,             HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1,  HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithEd25519,               HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384,             HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1,  HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithEd448,                 HashAlgorithmName.SHA512 }, // TODO shake
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss,            HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss,            HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521,             HashAlgorithmName.SHA512 },
        };

        internal static Dictionary<CompositeMLDsaAlgorithm, HashAlgorithmName> TradHashAlgorithms = new()
        {
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss,            HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15,         HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256,             HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss,            HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15,         HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss,            HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15,         HashAlgorithmName.SHA384 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256,             HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384,             HashAlgorithmName.SHA384 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1,  HashAlgorithmName.SHA256 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384,             HashAlgorithmName.SHA384 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1,  HashAlgorithmName.SHA384 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss,            HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss,            HashAlgorithmName.SHA512 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521,             HashAlgorithmName.SHA512 },
        };

        internal static Dictionary<CompositeMLDsaAlgorithm, RSASignaturePadding> RsaPadding = new()
        {
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss,    RSASignaturePadding.Pss },
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15, RSASignaturePadding.Pkcs1 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss,    RSASignaturePadding.Pss },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15, RSASignaturePadding.Pkcs1 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss,    RSASignaturePadding.Pss },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15, RSASignaturePadding.Pkcs1 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss,    RSASignaturePadding.Pss },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss,    RSASignaturePadding.Pss },
        };

        internal static Dictionary<CompositeMLDsaAlgorithm, MLDsaAlgorithm> MLDsaAlgorithms = new()
        {
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pss,            MLDsaAlgorithm.MLDsa44 },
            { CompositeMLDsaAlgorithm.MLDsa44WithRSA2048Pkcs15,         MLDsaAlgorithm.MLDsa44 },
            { CompositeMLDsaAlgorithm.MLDsa44WithEd25519,               MLDsaAlgorithm.MLDsa44 },
            { CompositeMLDsaAlgorithm.MLDsa44WithECDsaP256,             MLDsaAlgorithm.MLDsa44 },

            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pss,            MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA3072Pkcs15,         MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pss,            MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithRSA4096Pkcs15,         MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP256,             MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaP384,             MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithECDsaBrainpoolP256r1,  MLDsaAlgorithm.MLDsa65 },
            { CompositeMLDsaAlgorithm.MLDsa65WithEd25519,               MLDsaAlgorithm.MLDsa65 },

            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP384,             MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaBrainpoolP384r1,  MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithEd448,                 MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA3072Pss,            MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithRSA4096Pss,            MLDsaAlgorithm.MLDsa87 },
            { CompositeMLDsaAlgorithm.MLDsa87WithECDsaP521,             MLDsaAlgorithm.MLDsa87 },
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

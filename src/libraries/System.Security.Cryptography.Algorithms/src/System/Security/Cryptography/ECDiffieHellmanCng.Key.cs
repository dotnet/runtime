// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal static partial class ECDiffieHellmanImplementation
    {
        public sealed partial class ECDiffieHellmanCng : ECDiffieHellman
        {
            private readonly ECCngKey _key = new ECCngKey(BCryptNative.AlgorithmName.ECDH);

            private string GetCurveName(out string oidValue) => _key.GetCurveName(KeySize, out oidValue);

            public override void GenerateKey(ECCurve curve)
            {
                _key.GenerateKey(curve);
                ForceSetKeySize(_key.KeySize);
            }

            private SafeNCryptKeyHandle GetDuplicatedKeyHandle() => _key.GetDuplicatedKeyHandle(KeySize);

            private void DisposeKey() => _key.DisposeKey();
            
            /// <summary>
            ///     Public key used to generate key material with the second party
            /// </summary>
            public override ECDiffieHellmanPublicKey PublicKey
            {
                get
                {
                    string curveName = GetCurveName(out _);

                    return new ECDiffieHellmanCngPublicKey(
                        curveName == null
                            ? ExportFullKeyBlob(includePrivateParameters: false)
                            : ExportKeyBlob(includePrivateParameters: false),
                        curveName);
                }
            }
        }
    }
}

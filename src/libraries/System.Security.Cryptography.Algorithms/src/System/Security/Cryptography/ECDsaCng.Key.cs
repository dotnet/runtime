// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using static Internal.NativeCrypto.BCryptNative;

namespace System.Security.Cryptography
{
    internal static partial class ECDsaImplementation
    {
        public sealed partial class ECDsaCng : ECDsa
        {
            private readonly ECCngKey _key = new ECCngKey(AlgorithmName.ECDsa);

            private string GetCurveName(out string oidValue) => _key.GetCurveName(KeySize, out oidValue);
            
            public override void GenerateKey(ECCurve curve)
            {
                _key.GenerateKey(curve);
                ForceSetKeySize(_key.KeySize);
            }

            private SafeNCryptKeyHandle GetDuplicatedKeyHandle() => _key.GetDuplicatedKeyHandle(KeySize);

            private void DisposeKey() => _key.DisposeKey();
        }
    }
}

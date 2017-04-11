// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.NativeCrypto;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace System.Security.Cryptography
{
    public partial class PasswordDeriveBytes : DeriveBytes
    {
        private SafeProvHandle _safeProvHandle = null;

        public byte[] CryptDeriveKey(string algname, string alghashname, int keySize, byte[] rgbIV)
        {
            if (keySize < 0)
                throw new CryptographicException(SR.Cryptography_InvalidKeySize);

            int algidhash = CapiHelper.NameOrOidToHashAlgId(alghashname, OidGroup.HashAlgorithm);
            if (algidhash == 0)
                throw new CryptographicException(SR.Cryptography_PasswordDerivedBytes_InvalidAlgorithm);

            int algid = CapiHelper.NameOrOidToHashAlgId(algname, OidGroup.All);
            if (algid == 0)
                throw new CryptographicException(SR.Cryptography_PasswordDerivedBytes_InvalidAlgorithm);

            if (rgbIV == null)
                throw new CryptographicException(SR.Cryptography_PasswordDerivedBytes_InvalidIV);

            byte[] key = null;
            CapiHelper.DeriveKey(ProvHandle, algid, algidhash, _password, _password.Length, keySize << 16, rgbIV, rgbIV.Length, ref key);
            return key;
        }

        private SafeProvHandle ProvHandle
        {
            get
            {
                if (_safeProvHandle == null)
                {
                    lock (this)
                    {
                        if (_safeProvHandle == null)
                        {
                            SafeProvHandle safeProvHandle = AcquireSafeProviderHandle(_cspParams);
                            System.Threading.Thread.MemoryBarrier();
                            _safeProvHandle = safeProvHandle;
                        }
                    }
                }
                return _safeProvHandle;
            }
        }

        private static SafeProvHandle AcquireSafeProviderHandle(CspParameters cspParams)
        {
            if (cspParams == null)
            {
                cspParams = new CspParameters(CapiHelper.DefaultRsaProviderType);
            }

            SafeProvHandle safeProvHandle = null;
            CapiHelper.AcquireCsp(cspParams, out safeProvHandle);
            return safeProvHandle;
        }
    }
}

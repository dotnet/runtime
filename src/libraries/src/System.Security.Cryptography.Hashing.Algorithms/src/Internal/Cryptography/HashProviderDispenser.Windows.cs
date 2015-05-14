// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;

using Internal.NativeCrypto;

namespace Internal.Cryptography
{
    //
    // Provides hash services via the native provider (CNG). 
    //
    internal static partial class HashProviderDispenser
    {
        public static HashProvider CreateHashProvider(String hashAlgorithmId)
        {
            return new HashProviderCng(hashAlgorithmId, null);
        }

        public static HashProvider CreateMacProvider(String hashAlgorithmId, byte[] key)
        {
            return new HashProviderCng(hashAlgorithmId, key);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private sealed class HashProviderCng : HashProvider
        {
            //
            //   - "hashAlgId" must be a name recognized by BCryptOpenAlgorithmProvider(). Examples: MD5, SHA1, SHA256.
            //
            //   - "key" activates MAC hashing if present. If null, this HashProvider performs a regular old hash.
            //
            public HashProviderCng(String hashAlgId, byte[] key)
            {
                Cng.OpenAlgorithmProviderFlags dwFlags = Cng.OpenAlgorithmProviderFlags.NONE;
                if (key != null)
                {
                    _key = key.CloneByteArray();
                    dwFlags |= Cng.OpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG;
                }
                _hAlgorithm = Cng.BCryptOpenAlgorithmProvider(hashAlgId, null, dwFlags);

                _hHash = _hAlgorithm.BCryptTryCreateReusableHash(_key);
                if (_hHash == null)
                {
                    // If we got here, we're running on a downlevel OS that doesn't support reusable CNG hash objects. Fall back to creating a 
                    // new HASH object each time.
                    ResetHashObject();
                }
                else
                {
                    _reusable = true;
                }

                _hashSize = _hHash.GetHashSizeInBytes();
                return;
            }

            public sealed override void AppendHashData(byte[] rgb, int ib, int cb)
            {
                unsafe
                {
                    fixed (byte* pRgb = rgb)
                    {
                        _hHash.BCryptHashData(pRgb + ib, cb);
                    }
                }
            }

            public sealed override byte[] FinalizeHashAndReset()
            {
                byte[] hash = _hHash.BCryptFinishHash(_hashSize);
                ResetHashObject();
                return hash;
            }

            public sealed override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    DestroyHash();
                    SafeAlgorithmHandle hAlgorithm = _hAlgorithm;
                    _hAlgorithm = null;
                    if (hAlgorithm != null)
                    {
                        hAlgorithm.Dispose();
                    }

                    if (_key != null)
                    {
                        byte[] key = _key;
                        _key = null;
                        Array.Clear(key, 0, key.Length);
                    }
                }
            }

            public sealed override int HashSizeInBytes
            {
                get
                {
                    return _hashSize;
                }
            }

            private void ResetHashObject()
            {
                if (_reusable)
                    return;
                DestroyHash();
                _hHash = _hAlgorithm.BCryptCreateHash(_key, 0);
            }

            private void DestroyHash()
            {
                SafeHashHandle hHash = _hHash;
                _hHash = null;
                if (hHash != null)
                {
                    hHash.Dispose();
                }
            }

            private SafeAlgorithmHandle _hAlgorithm;
            private SafeHashHandle _hHash;
            private byte[] _key;
            private readonly bool _reusable;

            private readonly int _hashSize;
        }
    }
}



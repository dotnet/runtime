// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    // ported from https://github.com/microsoft/referencesource/blob/5697c29004a34d80acdaf5742d7e699022c64ecd/mscorlib/system/security/cryptography/hmac.cs
    internal sealed class HMACManagedHashProvider : HashProvider
    {
        private bool _hashing;
        private readonly int _blockSizeValue;
        private readonly int _hashSizeValue;

        private readonly byte[] _key;
        private readonly HashProvider _hash1;
        private readonly HashProvider _hash2;

        // _inner = PaddedKey ^ {0x36,...,0x36}
        // _outer = PaddedKey ^ {0x5C,...,0x5C}
        private readonly byte[] _inner;
        private readonly byte[] _outer;

        public HMACManagedHashProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            _hash1 = HashProviderDispenser.CreateHashProvider(hashAlgorithmId);
            _hash2 = HashProviderDispenser.CreateHashProvider(hashAlgorithmId);

            (_blockSizeValue, _hashSizeValue) = hashAlgorithmId switch
            {
                HashAlgorithmNames.SHA1 => (64, 160 / 8),
                HashAlgorithmNames.SHA256 => (64, 256 / 8),
                HashAlgorithmNames.SHA384 => (128, 384 / 8),
                HashAlgorithmNames.SHA512 => (128, 512 / 8),
                _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId)),
            };

            _key = InitializeKey(key);
            (_inner, _outer) = UpdateIOPadBuffers();
        }

        private byte[] InitializeKey(ReadOnlySpan<byte> key)
        {
            if (key.Length > _blockSizeValue)
            {
                byte[] result = new byte[_hashSizeValue];
                _hash1.AppendHashData(key);
                int written = _hash1.FinalizeHashAndReset(result);
                Debug.Assert(written == result.Length);

                return result;
            }

            return key.ToArray();
        }

        private (byte[] inner, byte[] outer) UpdateIOPadBuffers()
        {
            byte[] inner = new byte[_blockSizeValue];
            byte[] outer = new byte[_blockSizeValue];

            int i;
            for (i = 0; i < _blockSizeValue; i++)
            {
                inner[i] = 0x36;
                outer[i] = 0x5C;
            }
            for (i = 0; i < _key.Length; i++)
            {
                inner[i] ^= _key[i];
                outer[i] ^= _key[i];
            }

            return (inner, outer);
        }

        public override void AppendHashData(ReadOnlySpan<byte> data)
        {
            if (_hashing == false)
            {
                _hash1.AppendHashData(_inner);
                _hashing = true;
            }

            _hash1.AppendHashData(data);
        }

        public override int FinalizeHashAndReset(Span<byte> destination)
        {
            int written = GetCurrentHash(destination);
            Reset();
            return written;
        }

        public override int GetCurrentHash(Span<byte> destination)
        {
            if (_hashing == false)
            {
                _hash1.AppendHashData(_inner);
                _hashing = true;
            }

            // finalize the original hash
            Span<byte> hashValue1 = stackalloc byte[_hashSizeValue];
            int hash1Written = _hash1.GetCurrentHash(hashValue1);
            Debug.Assert(hash1Written == hashValue1.Length);

            // write the outer array
            _hash2.AppendHashData(_outer);
            // write the inner hash and finalize the hash
            _hash2.AppendHashData(hashValue1);
            return _hash2.FinalizeHashAndReset(destination);
        }

        public override int HashSizeInBytes => _hashSizeValue;

        public override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _hash1.Dispose();
                _hash2.Dispose();
                Array.Clear(_inner, 0, _inner.Length);
                Array.Clear(_outer, 0, _outer.Length);
            }
        }

        public override void Reset()
        {
            if (_hashing)
            {
                _hash1.Reset();
                _hash2.Reset();
                _hashing = false;
            }
        }
    }
}

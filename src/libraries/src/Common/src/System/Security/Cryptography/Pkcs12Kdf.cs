// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System.Security.Cryptography.Pkcs
{
    internal static class Pkcs12Kdf
    {
        private const byte CipherKeyId = 1;
        private const byte IvId = 2;
        private const byte MacKeyId = 3;

        // This is a dictionary representation of the table in
        // https://tools.ietf.org/html/rfc7292#appendix-B.2
        private static readonly Dictionary<HashAlgorithmName, Tuple<int, int>> s_uvLookup =
            new Dictionary<HashAlgorithmName, Tuple<int, int>>
            {
                { HashAlgorithmName.MD5, Tuple.Create(128, 512) },
                { HashAlgorithmName.SHA1, Tuple.Create(160, 512) },
                { HashAlgorithmName.SHA256, Tuple.Create(256, 512) },
                { HashAlgorithmName.SHA384, Tuple.Create(384, 1024) },
                { HashAlgorithmName.SHA512, Tuple.Create(512, 1024) },
            };

        internal static void DeriveCipherKey(
            ReadOnlySpan<char> password,
            HashAlgorithmName hashAlgorithm,
            int iterationCount,
            ReadOnlySpan<byte> salt,
            Span<byte> destination)
        {
            Derive(
                password,
                hashAlgorithm,
                iterationCount,
                CipherKeyId,
                salt,
                destination);
        }

        internal static void DeriveIV(
            ReadOnlySpan<char> password,
            HashAlgorithmName hashAlgorithm,
            int iterationCount,
            ReadOnlySpan<byte> salt,
            Span<byte> destination)
        {
            Derive(
                password,
                hashAlgorithm,
                iterationCount,
                IvId,
                salt,
                destination);
        }

        internal static void DeriveMacKey(
            ReadOnlySpan<char> password,
            HashAlgorithmName hashAlgorithm,
            int iterationCount,
            ReadOnlySpan<byte> salt,
            Span<byte> destination)
        {
            Derive(
                password,
                hashAlgorithm,
                iterationCount,
                MacKeyId,
                salt,
                destination);
        }

        private static void Derive(
            ReadOnlySpan<char> password,
            HashAlgorithmName hashAlgorithm,
            int iterationCount,
            byte id,
            ReadOnlySpan<byte> salt,
            Span<byte> destination)
        {
            // https://tools.ietf.org/html/rfc7292#appendix-B.2
            Debug.Assert(iterationCount >= 1);

            if (!s_uvLookup.TryGetValue(hashAlgorithm, out Tuple<int, int> uv))
            {
                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithm.Name);
            }

            (int u, int v) = uv;

            Debug.Assert(v <= 1024);

            //  1. Construct a string, D (the "diversifier"), by concatenating v/8 copies of ID.
            int vBytes = v >> 3;
            Span<byte> D = stackalloc byte[vBytes];
            D.Fill(id);

            // 2.  Concatenate copies of the salt together to create a string S of
            // length v(ceiling(s/ v)) bits(the final copy of the salt may be
            // truncated to create S). Note that if the salt is the empty
            // string, then so is S.
            int SLen = ((salt.Length - 1 + vBytes) / vBytes) * vBytes;

            // The password is a null-terminated UTF-16BE version of the input.
            int passLen = checked((password.Length + 1) * 2);

            // If password == default then the span represents the null string (as opposed to
            // an empty string), and the P block should then have size 0 in the next step.
            if (password == default)
            {
                passLen = 0;
            }

            // 3.  Concatenate copies of the password together to create a string P
            // of length v(ceiling(p/v)) bits (the final copy of the password
            // may be truncated to create P).  Note that if the password is the
            // empty string, then so is P.
            //
            // (The RFC quote considers the trailing '\0' to be part of the string,
            // so "empty string" from this RFC means "null string" in C#, and C#'s
            // "empty string" is not 'empty' in this context.)
            int PLen = ((passLen - 1 + vBytes) / vBytes) * vBytes;

            // 4.  Set I=S||P to be the concatenation of S and P.
            int ILen = SLen + PLen;
            Span<byte> I = stackalloc byte[0];
            byte[] IRented = null;

            if (ILen <= 1024)
            {
                I = stackalloc byte[ILen];
            }
            else
            {
                IRented = CryptoPool.Rent(ILen);
                I = IRented.AsSpan(0, ILen);
            }

            IncrementalHash hash = IncrementalHash.CreateHash(hashAlgorithm);

            try
            {
                CircularCopy(salt, I.Slice(0, SLen));
                CircularCopyUtf16BE(password, I.Slice(SLen));

                int uBytes = u >> 3;

                Span<byte> hashBuf = stackalloc byte[uBytes];
                Span<byte> bBuf = stackalloc byte[vBytes];

                // 5.  Set c=ceiling(n/u).
                // 6.  For i=1, 2, ..., c, do the following:
                // (later we're going to start writing A_i values as output,
                // they mean "while work remains").
                while (true)
                {
                    // A.  Set A_i=H^r(D||I). (i.e., the r-th hash of D||I,
                    // H(H(H(... H(D || I))))
                    hash.AppendData(D);
                    hash.AppendData(I);

                    for (int j = iterationCount; j > 0; j--)
                    {
                        if (!hash.TryGetHashAndReset(hashBuf, out int bytesWritten) || bytesWritten != hashBuf.Length)
                        {
                            Debug.Fail($"Hash output wrote {bytesWritten} bytes when {hashBuf.Length} was expected");
                            throw new CryptographicException();
                        }

                        if (j != 1)
                        {
                            hash.AppendData(hashBuf);
                        }
                    }

                    // 7.  Concatenate A_1, A_2, ..., A_c together to form a pseudorandom
                    // bit string, A.
                    //
                    // 8.  Use the first n bits of A as the output of this entire process.

                    if (hashBuf.Length >= destination.Length)
                    {
                        hashBuf.Slice(0, destination.Length).CopyTo(destination);
                        return;
                    }

                    hashBuf.CopyTo(destination);
                    destination = destination.Slice(hashBuf.Length);

                    // B.  Concatenate copies of A_i to create a string B of length v
                    // bits(the final copy of Ai may be truncated to create B).
                    CircularCopy(hashBuf, bBuf);

                    // C.  Treating I as a concatenation I_0, I_1, ..., I_(k-1) of v-bit
                    // blocks, where k = ceiling(s / v) + ceiling(p / v), modify I by
                    // setting I_j = (I_j + B + 1) mod 2 ^ v for each j.
                    for (int j = (I.Length / vBytes) - 1; j >= 0; j--)
                    {
                        Span<byte> I_j = I.Slice(j * vBytes, vBytes);
                        AddPlusOne(I_j, bBuf);
                    }
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(I);

                if (IRented != null)
                {
                    CryptoPool.Return(IRented, clearSize: 0);
                }

                hash.Dispose();
            }
        }

        private static void AddPlusOne(Span<byte> into, Span<byte> addend)
        {
            Debug.Assert(into.Length == addend.Length);

            int carry = 1;

            for (int i = into.Length - 1; i >= 0; i--)
            {
                int tmp = carry + into[i] + addend[i];
                into[i] = (byte)tmp;
                carry = tmp >> 8;
            }
        }

        private static void CircularCopy(ReadOnlySpan<byte> bytes, Span<byte> destination)
        {
            Debug.Assert(bytes.Length > 0);

            while (destination.Length > 0)
            {
                if (destination.Length >= bytes.Length)
                {
                    bytes.CopyTo(destination);
                    destination = destination.Slice(bytes.Length);
                }
                else
                {
                    bytes.Slice(0, destination.Length).CopyTo(destination);
                    return;
                }
            }
        }

        private static void CircularCopyUtf16BE(ReadOnlySpan<char> password, Span<byte> destination)
        {
            int fullCopyLen = password.Length * 2;
            Encoding bigEndianUnicode = System.Text.Encoding.BigEndianUnicode;
            Debug.Assert(destination.Length % 2 == 0);

            while (destination.Length > 0)
            {
                if (destination.Length >= fullCopyLen)
                {
                    int count = bigEndianUnicode.GetBytes(password, destination);

                    if (count != fullCopyLen)
                    {
                        Debug.Fail($"Unexpected written byte count ({count} vs {fullCopyLen})");
                        throw new CryptographicException();
                    }

                    destination = destination.Slice(count);
                    Span<byte> nullTerminator = destination.Slice(0, Math.Min(2, destination.Length));
                    nullTerminator.Clear();
                    destination = destination.Slice(nullTerminator.Length);
                }
                else
                {
                    ReadOnlySpan<char> trimmed = password.Slice(0, destination.Length / 2);

                    int count = bigEndianUnicode.GetBytes(trimmed, destination);

                    if (count != destination.Length)
                    {
                        Debug.Fail($"Partial copy wrote {count} bytes of {destination.Length} expected");
                        throw new CryptographicException();
                    }

                    return;
                }
            }
        }
    }
}

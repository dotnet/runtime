// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Security;

namespace System.Reflection
{
    internal static partial class AssemblyNameHelpers
    {
        public static byte[]? ComputePublicKeyToken(byte[]? publicKey)
        {
            if (publicKey == null)
                return null;

            if (publicKey.Length == 0)
                return Array.Empty<byte>();

            if (!IsValidPublicKey(publicKey))
                throw new SecurityException(SR.Security_InvalidAssemblyPublicKey);

            Span<byte> hash = stackalloc byte[20];

            Sha1ForNonSecretPurposes sha1 = default;
            sha1.Start();
            sha1.Append(publicKey);
            sha1.Finish(hash);

            byte[] publicKeyToken = new byte[PublicKeyTokenLength];
            for (int i = 0; i < publicKeyToken.Length; i++)
                publicKeyToken[i] = hash[hash.Length - 1 - i];
            return publicKeyToken;
        }

        //
        // This validation logic is a port of StrongNameIsValidPublicKey() from src\coreclr\md\runtime\strongnameinternal.cpp
        //
        private static bool IsValidPublicKey(byte[] publicKey)
        {
            uint publicKeyLength = (uint)(publicKey.Length);

            // The buffer must be at least as large as the public key structure (for compat with desktop, we actually compare with the size of the header + 4).
            if (publicKeyLength < SizeOfPublicKeyBlob + 4)
                return false;

            // Poor man's reinterpret_cast into the PublicKeyBlob structure.
            ReadOnlySpan<byte> publicKeyBlob = new ReadOnlySpan<byte>(publicKey);
            uint sigAlgID = BinaryPrimitives.ReadUInt32LittleEndian(publicKeyBlob);
            uint hashAlgID = BinaryPrimitives.ReadUInt32LittleEndian(publicKeyBlob.Slice(4));
            uint cbPublicKey = BinaryPrimitives.ReadUInt32LittleEndian(publicKeyBlob.Slice(8));

            // The buffer must be the same size as the structure header plus the trailing key data
            if (cbPublicKey != publicKeyLength - SizeOfPublicKeyBlob)
                return false;

            // The buffer itself looks reasonable, but the public key structure needs to be validated as well

            // The ECMA key doesn't look like a valid key so it will fail the below checks. If we were passed that
            // key, then we can skip them.
            if (EcmaKey.SequenceEqual(publicKeyBlob))
                return true;

            // If a hash algorithm is specified, it must be a sensible value
            bool fHashAlgorithmValid = GetAlgClass(hashAlgID) == ALG_CLASS_HASH && GetAlgSid(hashAlgID) >= ALG_SID_SHA1;
            if (hashAlgID != 0 && !fHashAlgorithmValid)
                return false;

            // If a signature algorithm is specified, it must be a sensible value
            bool fSignatureAlgorithmValid = GetAlgClass(sigAlgID) == ALG_CLASS_SIGNATURE;
            if (sigAlgID != 0 && !fSignatureAlgorithmValid)
                return false;

            // The key blob must indicate that it is a PUBLICKEYBLOB
            if (publicKey[SizeOfPublicKeyBlob] != PUBLICKEYBLOB)
                return false;

            return true;
        }

        // Constants and macros copied from WinCrypt.h:

        private static uint GetAlgClass(uint x)
        {
            return (x & (7 << 13));
        }

        private static uint GetAlgSid(uint x)
        {
            return (x & (511));
        }

        private const uint ALG_CLASS_HASH = (4 << 13);
        private const uint ALG_SID_SHA1 = 4;
        private const uint ALG_CLASS_SIGNATURE = (1 << 13);
        private const uint PUBLICKEYBLOB = 0x6;

        private const uint SizeOfPublicKeyBlob = 12;

        private const int PublicKeyTokenLength = 8;

        private static ReadOnlySpan<byte> EcmaKey => new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
    }
}

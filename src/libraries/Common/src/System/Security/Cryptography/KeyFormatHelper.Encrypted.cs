// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Asn1;

namespace System.Security.Cryptography
{
    internal static partial class KeyFormatHelper
    {
        internal static unsafe void ReadEncryptedPkcs8<TRet>(
            string[] validOids,
            ReadOnlySpan<byte> source,
            ReadOnlySpan<char> password,
            KeyReader<TRet> keyReader,
            out int bytesRead,
            out TRet ret)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    ReadEncryptedPkcs8(validOids, manager.Memory, password, keyReader, out bytesRead, out ret);
                }
            }
        }

        internal static unsafe void ReadEncryptedPkcs8<TRet>(
            string[] validOids,
            ReadOnlySpan<byte> source,
            ReadOnlySpan<byte> passwordBytes,
            KeyReader<TRet> keyReader,
            out int bytesRead,
            out TRet ret)
        {
            fixed (byte* ptr = &MemoryMarshal.GetReference(source))
            {
                using (MemoryManager<byte> manager = new PointerMemoryManager<byte>(ptr, source.Length))
                {
                    ReadEncryptedPkcs8(
                        validOids,
                        manager.Memory,
                        passwordBytes,
                        keyReader,
                        out bytesRead,
                        out ret);
                }
            }
        }

        private static void ReadEncryptedPkcs8<TRet>(
            string[] validOids,
            ReadOnlyMemory<byte> source,
            ReadOnlySpan<char> password,
            KeyReader<TRet> keyReader,
            out int bytesRead,
            out TRet ret)
        {
            ReadEncryptedPkcs8(
                validOids,
                source,
                password,
                ReadOnlySpan<byte>.Empty,
                keyReader,
                out bytesRead,
                out ret);
        }

        private static void ReadEncryptedPkcs8<TRet>(
            string[] validOids,
            ReadOnlyMemory<byte> source,
            ReadOnlySpan<byte> passwordBytes,
            KeyReader<TRet> keyReader,
            out int bytesRead,
            out TRet ret)
        {
            ReadEncryptedPkcs8(
                validOids,
                source,
                ReadOnlySpan<char>.Empty,
                passwordBytes,
                keyReader,
                out bytesRead,
                out ret);
        }

        private static void ReadEncryptedPkcs8<TRet>(
            string[] validOids,
            ReadOnlyMemory<byte> source,
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> passwordBytes,
            KeyReader<TRet> keyReader,
            out int bytesRead,
            out TRet ret)
        {
            int read;
            EncryptedPrivateKeyInfoAsn epki;

            try
            {
                AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.BER);
                read = reader.PeekEncodedValue().Length;
                EncryptedPrivateKeyInfoAsn.Decode(ref reader, source, out epki);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            // No supported encryption algorithms produce more bytes of decryption output than there
            // were of decryption input.
            byte[] decrypted = CryptoPool.Rent(epki.EncryptedData.Length);
            Memory<byte> decryptedMemory = decrypted;

            try
            {
                int decryptedBytes = PasswordBasedEncryption.Decrypt(
                    epki.EncryptionAlgorithm,
                    password,
                    passwordBytes,
                    epki.EncryptedData.Span,
                    decrypted);

                decryptedMemory = decryptedMemory.Slice(0, decryptedBytes);

                ReadPkcs8(
                    validOids,
                    decryptedMemory,
                    keyReader,
                    out int innerRead,
                    out ret);

                if (innerRead != decryptedMemory.Length)
                {
                    ret = default!;
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                bytesRead = read;
            }
            catch (CryptographicException e)
            {
                throw new CryptographicException(SR.Cryptography_Pkcs8_EncryptedReadFailed, e);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(decryptedMemory.Span);
                CryptoPool.Return(decrypted, clearSize: 0);
            }
        }

        internal static AsnWriter WriteEncryptedPkcs8(
            ReadOnlySpan<char> password,
            AsnWriter pkcs8Writer,
            PbeParameters pbeParameters)
        {
            return WriteEncryptedPkcs8(
                password,
                ReadOnlySpan<byte>.Empty,
                pkcs8Writer,
                pbeParameters);
        }

        internal static AsnWriter WriteEncryptedPkcs8(
            ReadOnlySpan<byte> passwordBytes,
            AsnWriter pkcs8Writer,
            PbeParameters pbeParameters)
        {
            return WriteEncryptedPkcs8(
                ReadOnlySpan<char>.Empty,
                passwordBytes,
                pkcs8Writer,
                pbeParameters);
        }

        private static AsnWriter WriteEncryptedPkcs8(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> passwordBytes,
            AsnWriter pkcs8Writer,
            PbeParameters pbeParameters)
        {
            PasswordBasedEncryption.InitiateEncryption(
                pbeParameters,
                out SymmetricAlgorithm cipher,
                out string hmacOid,
                out string encryptionAlgorithmOid,
                out bool isPkcs12);

            byte[]? encryptedRent = null;
            Span<byte> encryptedSpan = default;
            AsnWriter? writer = null;

            try
            {
                Debug.Assert(cipher.BlockSize <= 128, $"Encountered unexpected block size: {cipher.BlockSize}");
                Span<byte> iv = stackalloc byte[cipher.BlockSize / 8];
                Span<byte> salt = stackalloc byte[16];

                // We need at least one block size beyond the input data size.
                encryptedRent = CryptoPool.Rent(
                    checked(pkcs8Writer.GetEncodedLength() + (cipher.BlockSize / 8)));

                RandomNumberGenerator.Fill(salt);

                int written = PasswordBasedEncryption.Encrypt(
                    password,
                    passwordBytes,
                    cipher,
                    isPkcs12,
                    pkcs8Writer,
                    pbeParameters,
                    salt,
                    encryptedRent,
                    iv);

                encryptedSpan = encryptedRent.AsSpan(0, written);

                writer = new AsnWriter(AsnEncodingRules.DER);

                // PKCS8 EncryptedPrivateKeyInfo
                writer.PushSequence();

                // EncryptedPrivateKeyInfo.encryptionAlgorithm
                PasswordBasedEncryption.WritePbeAlgorithmIdentifier(
                    writer,
                    isPkcs12,
                    encryptionAlgorithmOid,
                    salt,
                    pbeParameters.IterationCount,
                    hmacOid,
                    iv);

                // encryptedData
                writer.WriteOctetString(encryptedSpan);
                writer.PopSequence();

                return writer;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(encryptedSpan);
                CryptoPool.Return(encryptedRent!, clearSize: 0);

                cipher.Dispose();
            }
        }

        internal static ArraySegment<byte> DecryptPkcs8(
            ReadOnlySpan<char> inputPassword,
            ReadOnlyMemory<byte> source,
            out int bytesRead)
        {
            return DecryptPkcs8(
                inputPassword,
                ReadOnlySpan<byte>.Empty,
                source,
                out bytesRead);
        }

        internal static ArraySegment<byte> DecryptPkcs8(
            ReadOnlySpan<byte> inputPasswordBytes,
            ReadOnlyMemory<byte> source,
            out int bytesRead)
        {
            return DecryptPkcs8(
                ReadOnlySpan<char>.Empty,
                inputPasswordBytes,
                source,
                out bytesRead);
        }

        private static ArraySegment<byte> DecryptPkcs8(
            ReadOnlySpan<char> inputPassword,
            ReadOnlySpan<byte> inputPasswordBytes,
            ReadOnlyMemory<byte> source,
            out int bytesRead)
        {
            int localRead;
            EncryptedPrivateKeyInfoAsn epki;

            try
            {
                AsnValueReader reader = new AsnValueReader(source.Span, AsnEncodingRules.BER);
                localRead = reader.PeekEncodedValue().Length;
                EncryptedPrivateKeyInfoAsn.Decode(ref reader, source, out epki);
            }
            catch (AsnContentException e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }

            // No supported encryption algorithms produce more bytes of decryption output than there
            // were of decryption input.
            byte[] decrypted = CryptoPool.Rent(epki.EncryptedData.Length);

            try
            {
                int decryptedBytes = PasswordBasedEncryption.Decrypt(
                    epki.EncryptionAlgorithm,
                    inputPassword,
                    inputPasswordBytes,
                    epki.EncryptedData.Span,
                    decrypted);

                bytesRead = localRead;

                return new ArraySegment<byte>(decrypted, 0, decryptedBytes);
            }
            catch (CryptographicException e)
            {
                CryptoPool.Return(decrypted);
                throw new CryptographicException(SR.Cryptography_Pkcs8_EncryptedReadFailed, e);
            }
        }

        internal static AsnWriter ReencryptPkcs8(
            ReadOnlySpan<char> inputPassword,
            ReadOnlyMemory<byte> current,
            ReadOnlySpan<char> newPassword,
            PbeParameters pbeParameters)
        {
            ArraySegment<byte> decrypted = DecryptPkcs8(
                inputPassword,
                current,
                out int bytesRead);

            try
            {
                if (bytesRead != current.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                AsnWriter pkcs8Writer = new AsnWriter(AsnEncodingRules.BER);
                pkcs8Writer.WriteEncodedValueForCrypto(decrypted);

                return WriteEncryptedPkcs8(
                    newPassword,
                    pkcs8Writer,
                    pbeParameters);
            }
            catch (CryptographicException e)
            {
                throw new CryptographicException(SR.Cryptography_Pkcs8_EncryptedReadFailed, e);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(decrypted);
                CryptoPool.Return(decrypted.Array!, clearSize: 0);
            }
        }

        internal static AsnWriter ReencryptPkcs8(
            ReadOnlySpan<char> inputPassword,
            ReadOnlyMemory<byte> current,
            ReadOnlySpan<byte> newPasswordBytes,
            PbeParameters pbeParameters)
        {
            ArraySegment<byte> decrypted = DecryptPkcs8(
                inputPassword,
                current,
                out int bytesRead);

            try
            {
                if (bytesRead != current.Length)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                AsnWriter pkcs8Writer = new AsnWriter(AsnEncodingRules.BER);
                pkcs8Writer.WriteEncodedValueForCrypto(decrypted);

                return WriteEncryptedPkcs8(
                    newPasswordBytes,
                    pkcs8Writer,
                    pbeParameters);
            }
            catch (CryptographicException e)
            {
                throw new CryptographicException(SR.Cryptography_Pkcs8_EncryptedReadFailed, e);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(decrypted);
                CryptoPool.Return(decrypted.Array!, clearSize: 0);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Internal.Cryptography;
using Internal.NativeCrypto;

using ErrorCode = Interop.NCrypt.ErrorCode;
using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;
using BCRYPT_RSAKEY_BLOB = Interop.BCrypt.BCRYPT_RSAKEY_BLOB;

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        /// <summary>
        ///     <para>
        ///         ImportParameters will replace the existing key that RSACng is working with by creating a
        ///         new CngKey for the parameters structure. If the parameters structure contains only an
        ///         exponent and modulus, then only a public key will be imported. If the parameters also
        ///         contain P and Q values, then a full key pair will be imported.
        ///     </para>
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     if <paramref name="parameters" /> contains neither an exponent nor a modulus.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///     if <paramref name="parameters" /> is not a valid RSA key or if <paramref name="parameters"
        ///     /> is a full key pair and the default KSP is used.
        /// </exception>
        public override unsafe void ImportParameters(RSAParameters parameters)
        {
            if (parameters.Exponent == null || parameters.Modulus == null)
                throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);

            bool includePrivate;
            if (parameters.D == null)
            {
                includePrivate = false;

                if (parameters.P != null ||
                    parameters.DP != null ||
                    parameters.Q != null ||
                    parameters.DQ != null ||
                    parameters.InverseQ != null)
                {
                    throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);
                }
            }
            else
            {
                includePrivate = true;

                if (parameters.P == null ||
                    parameters.DP == null ||
                    parameters.Q == null ||
                    parameters.DQ == null ||
                    parameters.InverseQ == null)
                {
                    throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);
                }

                // Half, rounded up.
                int halfModulusLength = (parameters.Modulus.Length + 1) / 2;

                // The same checks are done by RSACryptoServiceProvider on import (when building the key blob)
                // Historically RSACng let CNG handle this (reporting NTE_NOT_SUPPORTED), but on RS1 CNG let the
                // import succeed, then on private key use (e.g. signing) it would report NTE_INVALID_PARAMETER.
                //
                // Doing the check here prevents the state in RS1 where the Import succeeds, but corrupts the key,
                // and makes for a friendlier exception message.
                if (parameters.D.Length != parameters.Modulus.Length ||
                    parameters.P.Length != halfModulusLength ||
                    parameters.Q.Length != halfModulusLength ||
                    parameters.DP.Length != halfModulusLength ||
                    parameters.DQ.Length != halfModulusLength ||
                    parameters.InverseQ.Length != halfModulusLength)
                {
                    throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);
                }
            }

            //
            // We need to build a key blob structured as follows:
            //
            //     BCRYPT_RSAKEY_BLOB   header
            //     byte[cbPublicExp]    publicExponent      - Exponent
            //     byte[cbModulus]      modulus             - Modulus
            //     -- Only if "includePrivate" is true --
            //     byte[cbPrime1]       prime1              - P
            //     byte[cbPrime2]       prime2              - Q
            //     ------------------
            //

            int blobSize = sizeof(BCRYPT_RSAKEY_BLOB) +
                            parameters.Exponent.Length +
                            parameters.Modulus.Length;
            if (includePrivate)
            {
                blobSize += parameters.P!.Length +
                            parameters.Q!.Length;
            }

            byte[] rsaBlob = new byte[blobSize];
            fixed (byte* pRsaBlob = &rsaBlob[0])
            {
                // Build the header
                BCRYPT_RSAKEY_BLOB* pBcryptBlob = (BCRYPT_RSAKEY_BLOB*)pRsaBlob;
                pBcryptBlob->Magic = includePrivate ? KeyBlobMagicNumber.BCRYPT_RSAPRIVATE_MAGIC : KeyBlobMagicNumber.BCRYPT_RSAPUBLIC_MAGIC;
                pBcryptBlob->BitLength = parameters.Modulus.Length * 8;
                pBcryptBlob->cbPublicExp = parameters.Exponent.Length;
                pBcryptBlob->cbModulus = parameters.Modulus.Length;

                if (includePrivate)
                {
                    pBcryptBlob->cbPrime1 = parameters.P!.Length;
                    pBcryptBlob->cbPrime2 = parameters.Q!.Length;
                }

                int offset = sizeof(BCRYPT_RSAKEY_BLOB);

                Interop.BCrypt.Emit(rsaBlob, ref offset, parameters.Exponent);
                Interop.BCrypt.Emit(rsaBlob, ref offset, parameters.Modulus);

                if (includePrivate)
                {
                    Interop.BCrypt.Emit(rsaBlob, ref offset, parameters.P!);
                    Interop.BCrypt.Emit(rsaBlob, ref offset, parameters.Q!);
                }

                // We better have computed the right allocation size above!
                Debug.Assert(offset == blobSize, "offset == blobSize");
            }

            ImportKeyBlob(rsaBlob, includePrivate);
        }

            public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
            {
                ThrowIfDisposed();

                CngPkcs8.Pkcs8Response response = CngPkcs8.ImportPkcs8PrivateKey(source, out int localRead);

                ProcessPkcs8Response(response);
                bytesRead = localRead;
            }

            public override void ImportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<byte> passwordBytes,
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                ThrowIfDisposed();

                CngPkcs8.Pkcs8Response response = CngPkcs8.ImportEncryptedPkcs8PrivateKey(
                    passwordBytes,
                    source,
                    out int localRead);

                ProcessPkcs8Response(response);
                bytesRead = localRead;
            }

            public override void ImportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                ReadOnlySpan<byte> source,
                out int bytesRead)
            {
                ThrowIfDisposed();

                CngPkcs8.Pkcs8Response response = CngPkcs8.ImportEncryptedPkcs8PrivateKey(
                    password,
                    source,
                    out int localRead);

                ProcessPkcs8Response(response);
                bytesRead = localRead;
            }

            private void ProcessPkcs8Response(CngPkcs8.Pkcs8Response response)
            {
                // Wrong algorithm?
                if (response.GetAlgorithmGroup() != BCryptNative.AlgorithmName.RSA)
                {
                    response.FreeKey();
                    throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                }

                AcceptImport(response);
            }

            public override byte[] ExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<byte> passwordBytes,
                PbeParameters pbeParameters)
            {
                ArgumentNullException.ThrowIfNull(pbeParameters);

                return CngPkcs8.ExportEncryptedPkcs8PrivateKey(
                    this,
                    passwordBytes,
                    pbeParameters);
            }

            public override byte[] ExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                PbeParameters pbeParameters)
            {
                ArgumentNullException.ThrowIfNull(pbeParameters);

                PasswordBasedEncryption.ValidatePbeParameters(
                    pbeParameters,
                    password,
                    ReadOnlySpan<byte>.Empty);

                if (CngPkcs8.IsPlatformScheme(pbeParameters))
                {
                    return ExportEncryptedPkcs8(password, pbeParameters.IterationCount);
                }

                return CngPkcs8.ExportEncryptedPkcs8PrivateKey(
                    this,
                    password,
                    pbeParameters);
            }

            public override bool TryExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<byte> passwordBytes,
                PbeParameters pbeParameters,
                Span<byte> destination,
                out int bytesWritten)
            {
                ArgumentNullException.ThrowIfNull(pbeParameters);

                PasswordBasedEncryption.ValidatePbeParameters(
                    pbeParameters,
                    ReadOnlySpan<char>.Empty,
                    passwordBytes);

                return CngPkcs8.TryExportEncryptedPkcs8PrivateKey(
                    this,
                    passwordBytes,
                    pbeParameters,
                    destination,
                    out bytesWritten);
            }

            public override bool TryExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                PbeParameters pbeParameters,
                Span<byte> destination,
                out int bytesWritten)
            {
                ArgumentNullException.ThrowIfNull(pbeParameters);

                PasswordBasedEncryption.ValidatePbeParameters(
                    pbeParameters,
                    password,
                    ReadOnlySpan<byte>.Empty);

                if (CngPkcs8.IsPlatformScheme(pbeParameters))
                {
                    return TryExportEncryptedPkcs8(
                        password,
                        pbeParameters.IterationCount,
                        destination,
                        out bytesWritten);
                }

                return CngPkcs8.TryExportEncryptedPkcs8PrivateKey(
                    this,
                    password,
                    pbeParameters,
                    destination,
                    out bytesWritten);
            }

            /// <summary>
            ///     Exports the key used by the RSA object into an RSAParameters object.
            /// </summary>
            public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            byte[] rsaBlob = ExportKeyBlob(includePrivateParameters);
            RSAParameters rsaParams = default;
            ExportParameters(ref rsaParams, rsaBlob, includePrivateParameters);
            return rsaParams;
        }

        private static void ExportParameters(ref RSAParameters rsaParams, byte[] rsaBlob, bool includePrivateParameters)
        {
            //
            // We now have a buffer laid out as follows:
            //     BCRYPT_RSAKEY_BLOB   header
            //     byte[cbPublicExp]    publicExponent      - Exponent
            //     byte[cbModulus]      modulus             - Modulus
            //     -- Private only --
            //     byte[cbPrime1]       prime1              - P
            //     byte[cbPrime2]       prime2              - Q
            //     byte[cbPrime1]       exponent1           - DP
            //     byte[cbPrime2]       exponent2           - DQ
            //     byte[cbPrime1]       coefficient         - InverseQ
            //     byte[cbModulus]      privateExponent     - D
            //
            KeyBlobMagicNumber magic = (KeyBlobMagicNumber)BitConverter.ToInt32(rsaBlob, 0);

            // Check the magic value in the key blob header. If the blob does not have the required magic,
            // then throw a CryptographicException.
            CheckMagicValueOfKey(magic, includePrivateParameters);

            unsafe
            {
                // Fail-fast if a rogue provider gave us a blob that isn't even the size of the blob header.
                if (rsaBlob.Length < sizeof(BCRYPT_RSAKEY_BLOB))
                    throw ErrorCode.E_FAIL.ToCryptographicException();

                fixed (byte* pRsaBlob = &rsaBlob[0])
                {
                    BCRYPT_RSAKEY_BLOB* pBcryptBlob = (BCRYPT_RSAKEY_BLOB*)pRsaBlob;

                    int offset = sizeof(BCRYPT_RSAKEY_BLOB);

                    // Read out the exponent
                    rsaParams.Exponent = Interop.BCrypt.Consume(rsaBlob, ref offset, pBcryptBlob->cbPublicExp);
                    rsaParams.Modulus = Interop.BCrypt.Consume(rsaBlob, ref offset, pBcryptBlob->cbModulus);

                    if (includePrivateParameters)
                    {
                        rsaParams.P = Interop.BCrypt.Consume(rsaBlob, ref offset, pBcryptBlob->cbPrime1);
                        rsaParams.Q = Interop.BCrypt.Consume(rsaBlob, ref offset, pBcryptBlob->cbPrime2);
                        rsaParams.DP = Interop.BCrypt.Consume(rsaBlob, ref offset, pBcryptBlob->cbPrime1);
                        rsaParams.DQ = Interop.BCrypt.Consume(rsaBlob, ref offset, pBcryptBlob->cbPrime2);
                        rsaParams.InverseQ = Interop.BCrypt.Consume(rsaBlob, ref offset, pBcryptBlob->cbPrime1);
                        rsaParams.D = Interop.BCrypt.Consume(rsaBlob, ref offset, pBcryptBlob->cbModulus);
                    }
                }
            }
        }

        /// <summary>
        ///     This function checks the magic value in the key blob header
        /// </summary>
        /// <param name="magic">The expected magic number.</param>
        /// <param name="includePrivateParameters">Private blob if true else public key blob</param>
        private static void CheckMagicValueOfKey(KeyBlobMagicNumber magic, bool includePrivateParameters)
        {
            if (includePrivateParameters)
            {
                if (magic != KeyBlobMagicNumber.BCRYPT_RSAPRIVATE_MAGIC && magic != KeyBlobMagicNumber.BCRYPT_RSAFULLPRIVATE_MAGIC)
                {
                    throw new CryptographicException(SR.Cryptography_NotValidPrivateKey);
                }
            }
            else
            {
                if (magic != KeyBlobMagicNumber.BCRYPT_RSAPUBLIC_MAGIC)
                {
                    // Private key magic is permissible too since the public key can be derived from the private key blob.
                    if (magic != KeyBlobMagicNumber.BCRYPT_RSAPRIVATE_MAGIC && magic != KeyBlobMagicNumber.BCRYPT_RSAFULLPRIVATE_MAGIC)
                    {
                        throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
                    }
                }
            }
        }
    }
}

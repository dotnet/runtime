// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Internal.Cryptography;

using ErrorCode = Interop.NCrypt.ErrorCode;
using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;
using BCRYPT_RSAKEY_BLOB = Interop.BCrypt.BCRYPT_RSAKEY_BLOB;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class RSAImplementation
    {
#endif
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
        public override void ImportParameters(RSAParameters parameters)
        {
            unsafe
            {
                if (parameters.Exponent == null || parameters.Modulus == null)
                    throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);

                bool includePrivate;
                if (parameters.D == null)
                {
                    includePrivate = false;
                    if (parameters.P != null || parameters.DP != null || parameters.Q != null || parameters.DQ != null || parameters.InverseQ != null)
                        throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);
                }
                else
                {
                    includePrivate = true;
                    if (parameters.P == null || parameters.DP == null || parameters.Q == null || parameters.DQ == null || parameters.InverseQ == null)
                        throw new CryptographicException(SR.Cryptography_InvalidRsaParameters);
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
                    blobSize += parameters.P.Length +
                                parameters.Q.Length;
                }

                byte[] rsaBlob = new byte[blobSize];
                fixed (byte* pRsaBlob = rsaBlob)
                {
                    // Build the header
                    BCRYPT_RSAKEY_BLOB* pBcryptBlob = (BCRYPT_RSAKEY_BLOB*)pRsaBlob;
                    pBcryptBlob->Magic = includePrivate ? KeyBlobMagicNumber.BCRYPT_RSAPRIVATE_MAGIC : KeyBlobMagicNumber.BCRYPT_RSAPUBLIC_MAGIC;
                    pBcryptBlob->BitLength = parameters.Modulus.Length * 8;
                    pBcryptBlob->cbPublicExp = parameters.Exponent.Length;
                    pBcryptBlob->cbModulus = parameters.Modulus.Length;

                    if (includePrivate)
                    {
                        pBcryptBlob->cbPrime1 = parameters.P.Length;
                        pBcryptBlob->cbPrime2 = parameters.Q.Length;
                    }

                    int offset = sizeof(BCRYPT_RSAKEY_BLOB);

                    Emit(rsaBlob, ref offset, parameters.Exponent);
                    Emit(rsaBlob, ref offset, parameters.Modulus);

                    if (includePrivate)
                    {
                        Emit(rsaBlob, ref offset, parameters.P);
                        Emit(rsaBlob, ref offset, parameters.Q);
                    }

                    // We better have computed the right allocation size above!
                    Debug.Assert(offset == blobSize, "offset == blobSize");
                }

                ImportKeyBlob(rsaBlob, includePrivate);
            }
        }

        /// <summary>
        ///     Append "value" to the data already in "rsaBlob".
        /// </summary>
        private static void Emit(byte[] rsaBlob, ref int offset, byte[] value)
        {
            Buffer.BlockCopy(value, 0, rsaBlob, offset, value.Length);
            offset += value.Length;
        }

        /// <summary>
        ///     Exports the key used by the RSA object into an RSAParameters object.
        /// </summary>
        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            byte[] rsaBlob = ExportKeyBlob(includePrivateParameters);
            RSAParameters rsaParams = new RSAParameters();
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
            byte[] tempMagic = new byte[4];
            tempMagic[0] = rsaBlob[0]; tempMagic[1] = rsaBlob[1]; tempMagic[2] = rsaBlob[2]; tempMagic[3] = rsaBlob[3];
            KeyBlobMagicNumber magic = (KeyBlobMagicNumber)BitConverter.ToInt32(tempMagic, 0);

            // Check the magic value in the key blob header. If the blob does not have the required magic,
            // then throw a CryptographicException.
            CheckMagicValueOfKey(magic, includePrivateParameters);

            unsafe
            {
                // Fail-fast if a rogue provider gave us a blob that isn't even the size of the blob header.
                if (rsaBlob.Length < sizeof(BCRYPT_RSAKEY_BLOB))
                    throw ErrorCode.E_FAIL.ToCryptographicException();

                fixed (byte* pRsaBlob = rsaBlob)
                {
                    BCRYPT_RSAKEY_BLOB* pBcryptBlob = (BCRYPT_RSAKEY_BLOB*)pRsaBlob;

                    int offset = sizeof(BCRYPT_RSAKEY_BLOB);

                    // Read out the exponent
                    rsaParams.Exponent = Consume(rsaBlob, ref offset, pBcryptBlob->cbPublicExp);
                    rsaParams.Modulus = Consume(rsaBlob, ref offset, pBcryptBlob->cbModulus);

                    if (includePrivateParameters)
                    {
                        rsaParams.P = Consume(rsaBlob, ref offset, pBcryptBlob->cbPrime1);
                        rsaParams.Q = Consume(rsaBlob, ref offset, pBcryptBlob->cbPrime2);
                        rsaParams.DP = Consume(rsaBlob, ref offset, pBcryptBlob->cbPrime1);
                        rsaParams.DQ = Consume(rsaBlob, ref offset, pBcryptBlob->cbPrime2);
                        rsaParams.InverseQ = Consume(rsaBlob, ref offset, pBcryptBlob->cbPrime1);
                        rsaParams.D = Consume(rsaBlob, ref offset, pBcryptBlob->cbModulus);
                    }
                }
            }
        }

        /// <summary>
        ///     This function checks the magic value in the key blob header
        /// </summary>
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

        /// <summary>
        ///     Peel off the next "count" bytes in "rsaBlob" and return them in a byte array.
        /// </summary>
        private static byte[] Consume(byte[] rsaBlob, ref int offset, int count)
        {
            byte[] value = new byte[count];
            Buffer.BlockCopy(rsaBlob, offset, value, 0, count);
            offset += count;
            return value;
        }
    }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}

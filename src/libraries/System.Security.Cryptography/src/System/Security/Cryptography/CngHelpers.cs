// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

using BCRYPT_RSAKEY_BLOB = Interop.BCrypt.BCRYPT_RSAKEY_BLOB;
using ErrorCode = Interop.NCrypt.ErrorCode;
using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;

namespace System.Security.Cryptography
{
    internal static class CngHelpers
    {
        private static readonly CngKeyBlobFormat s_cipherKeyBlobFormat = new CngKeyBlobFormat(Interop.NCrypt.NCRYPT_CIPHER_KEY_BLOB);

        internal static CryptographicException ToCryptographicException(this Interop.NCrypt.ErrorCode errorCode)
        {
            return ((int)errorCode).ToCryptographicException();
        }

        internal static SafeNCryptProviderHandle OpenStorageProvider(this CngProvider provider)
        {
            string providerName = provider.Provider;
            SafeNCryptProviderHandle providerHandle;
            ErrorCode errorCode = Interop.NCrypt.NCryptOpenStorageProvider(out providerHandle, providerName, 0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                providerHandle.Dispose();
                throw errorCode.ToCryptographicException();
            }

            return providerHandle;
        }

        public static void SetExportPolicy(this SafeNCryptKeyHandle keyHandle, CngExportPolicies exportPolicy)
        {
            unsafe
            {
                ErrorCode errorCode = Interop.NCrypt.NCryptSetProperty(
                    keyHandle,
                    KeyPropertyName.ExportPolicy,
                    &exportPolicy,
                    sizeof(CngExportPolicies),
                    CngPropertyOptions.Persist);

                if (errorCode != ErrorCode.ERROR_SUCCESS)
                {
                    throw errorCode.ToCryptographicException();
                }
            }
        }

        /// <summary>
        /// Returns a CNG key property.
        /// </summary>
        /// <returns>
        /// null - if property not defined on key.
        /// throws - for any other type of error.
        /// </returns>
        internal static byte[]? GetProperty(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
        {
            Debug.Assert(!ncryptHandle.IsInvalid);
            unsafe
            {
                ErrorCode errorCode = Interop.NCrypt.NCryptGetProperty(
                    ncryptHandle,
                    propertyName,
                    null,
                    0,
                    out int numBytesNeeded,
                    options);

                if (errorCode == ErrorCode.NTE_NOT_FOUND)
                {
                    return null;
                }

                if (errorCode != ErrorCode.ERROR_SUCCESS)
                {
                    throw errorCode.ToCryptographicException();
                }

                byte[] propertyValue = new byte[numBytesNeeded];

                fixed (byte* pPropertyValue = propertyValue)
                {
                    errorCode = Interop.NCrypt.NCryptGetProperty(
                        ncryptHandle,
                        propertyName,
                        pPropertyValue,
                        propertyValue.Length,
                        out numBytesNeeded,
                        options);
                }

                if (errorCode == ErrorCode.NTE_NOT_FOUND)
                {
                    return null;
                }

                if (errorCode != ErrorCode.ERROR_SUCCESS)
                {
                    throw errorCode.ToCryptographicException();
                }

                Array.Resize(ref propertyValue, numBytesNeeded);
                return propertyValue;
            }
        }

        /// <summary>
        /// Retrieve a well-known CNG string property. (Note: .NET Framework compat: this helper likes to return special
        /// values rather than throw exceptions for missing or ill-formatted property values. Only use it for well-known
        /// properties that are unlikely to be ill-formatted.)
        /// </summary>
        internal static string? GetPropertyAsString(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
        {
            Debug.Assert(!ncryptHandle.IsInvalid);
            byte[]? value = GetProperty(ncryptHandle, propertyName, options);

            if (value == null)
            {
                // .NET Framework compat: return null if key not present.
                return null;
            }

            if (value.Length == 0)
            {
                // .NET Framework compat: return empty if property value is 0-length.
                return string.Empty;
            }

            unsafe
            {
                fixed (byte* pValue = &value[0])
                {
                    string valueAsString = Marshal.PtrToStringUni((IntPtr)pValue)!;
                    return valueAsString;
                }
            }
        }

        /// <summary>
        /// Retrieve a well-known CNG dword property. (Note: .NET Framework compat: this helper likes to return special values
        /// rather than throw exceptions for missing or ill-formatted property values. Only use it for well-known properties that
        /// are unlikely to be ill-formatted.)
        /// </summary>
        public static int GetPropertyAsDword(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options) =>
            GetPropertyAsPrimitive<int>(ncryptHandle, propertyName, options);

        /// <summary>
        /// Retrieve a well-known CNG pointer property. (Note: .NET Framework compat: this helper likes to return special values
        /// rather than throw exceptions for missing or ill-formatted property values. Only use it for well-known properties that
        /// are unlikely to be ill-formatted.)
        /// </summary>
        internal static IntPtr GetPropertyAsIntPtr(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options) =>
            GetPropertyAsPrimitive<IntPtr>(ncryptHandle, propertyName, options);

        private static unsafe T GetPropertyAsPrimitive<T>(SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
            where T : unmanaged
        {
            T value;

            ErrorCode errorCode = Interop.NCrypt.NCryptGetProperty(
                ncryptHandle,
                propertyName,
                &value,
                sizeof(T),
                out _,
                options);

            if (errorCode == ErrorCode.NTE_NOT_FOUND)
            {
                return default;
            }

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            return value;
        }

        /// <summary>
        /// Note! This can and likely will throw if the algorithm was given a hardware-based key.
        /// </summary>
        internal static byte[] GetSymmetricKeyDataIfExportable(this CngKey cngKey, string algorithm)
        {
            const int SizeOf_NCRYPT_KEY_BLOB_HEADER =
                sizeof(int) + sizeof(int) + sizeof(int) + sizeof(int);

            byte[] keyBlob = cngKey.Export(s_cipherKeyBlobFormat);
            using (MemoryStream ms = new MemoryStream(keyBlob))
            {
                using (BinaryReader br = new BinaryReader(ms, Encoding.Unicode))
                {
                    // Read NCRYPT_KEY_BLOB_HEADER
                    int cbSize = br.ReadInt32();                      // NCRYPT_KEY_BLOB_HEADER.cbSize
                    if (cbSize != SizeOf_NCRYPT_KEY_BLOB_HEADER)
                        throw new CryptographicException(SR.Cryptography_KeyBlobParsingError);

                    int ncryptMagic = br.ReadInt32();                 // NCRYPT_KEY_BLOB_HEADER.dwMagic
                    if (ncryptMagic != Interop.NCrypt.NCRYPT_CIPHER_KEY_BLOB_MAGIC)
                        throw new CryptographicException(SR.Cryptography_KeyBlobParsingError);

                    int cbAlgName = br.ReadInt32();                   // NCRYPT_KEY_BLOB_HEADER.cbAlgName

                    br.ReadInt32();                                   // NCRYPT_KEY_BLOB_HEADER.cbKey

                    string algorithmName = new string(br.ReadChars((cbAlgName / 2) - 1));
                    if (algorithmName != algorithm)
                        throw new CryptographicException(SR.Format(SR.Cryptography_CngKeyWrongAlgorithm, algorithmName, algorithm));

                    char nullTerminator = br.ReadChar();
                    if (nullTerminator != 0)
                        throw new CryptographicException(SR.Cryptography_KeyBlobParsingError);

                    // Read BCRYPT_KEY_DATA_BLOB_HEADER
                    int bcryptMagic = br.ReadInt32();                 // BCRYPT_KEY_DATA_BLOB_HEADER.dwMagic
                    if (bcryptMagic != Interop.BCrypt.BCRYPT_KEY_DATA_BLOB_HEADER.BCRYPT_KEY_DATA_BLOB_MAGIC)
                        throw new CryptographicException(SR.Cryptography_KeyBlobParsingError);

                    int dwVersion = br.ReadInt32();                   // BCRYPT_KEY_DATA_BLOB_HEADER.dwVersion
                    if (dwVersion != Interop.BCrypt.BCRYPT_KEY_DATA_BLOB_HEADER.BCRYPT_KEY_DATA_BLOB_VERSION1)
                        throw new CryptographicException(SR.Cryptography_KeyBlobParsingError);

                    int keyLength = br.ReadInt32();                   // BCRYPT_KEY_DATA_BLOB_HEADER.cbKeyData
                    byte[] key = br.ReadBytes(keyLength);
                    return key;
                }
            }
        }

        internal static unsafe ArraySegment<byte> ToBCryptBlob(this in RSAParameters parameters)
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

            byte[] rsaBlob = CryptoPool.Rent(blobSize);

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
                else
                {
                    pBcryptBlob->cbPrime1 = pBcryptBlob->cbPrime2 = 0;
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
                Debug.Assert(offset == blobSize);
            }

            return new ArraySegment<byte>(rsaBlob, 0, blobSize);
        }

        internal static void FromBCryptBlob(
            this ref RSAParameters rsaParams,
            ReadOnlySpan<byte> rsaBlob,
            bool includePrivateParameters)
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

            unsafe
            {
                // Fail-fast if a rogue provider gave us a blob that isn't even the size of the blob header.
                if (rsaBlob.Length < sizeof(BCRYPT_RSAKEY_BLOB))
                    throw ErrorCode.E_FAIL.ToCryptographicException();

                fixed (byte* pRsaBlob = &rsaBlob[0])
                {
                    KeyBlobMagicNumber magic = (KeyBlobMagicNumber)Unsafe.ReadUnaligned<int>(pRsaBlob);

                    // Check the magic value in the key blob header. If the blob does not have the required magic,
                    // then throw a CryptographicException.
                    CheckMagicValueOfKey(magic, includePrivateParameters);

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

            static void CheckMagicValueOfKey(KeyBlobMagicNumber magic, bool includePrivateParameters)
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
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

using ErrorCode = Interop.NCrypt.ErrorCode;

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
        public static int GetPropertyAsDword(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
        {
            byte[]? value = ncryptHandle.GetProperty(propertyName, options);

            if (value == null)
            {
                // .NET Framework compat: return 0 if key not present.
                return 0;
            }

            return BitConverter.ToInt32(value, 0);
        }

        /// <summary>
        /// Retrieve a well-known CNG pointer property. (Note: .NET Framework compat: this helper likes to return special values
        /// rather than throw exceptions for missing or ill-formatted property values. Only use it for well-known properties that
        /// are unlikely to be ill-formatted.)
        /// </summary>
        internal static IntPtr GetPropertyAsIntPtr(this SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
        {
            unsafe
            {
                IntPtr value;

                ErrorCode errorCode = Interop.NCrypt.NCryptGetProperty(
                    ncryptHandle,
                    propertyName,
                    &value,
                    IntPtr.Size,
                    out _,
                    options);

                if (errorCode == ErrorCode.NTE_NOT_FOUND)
                {
                    return IntPtr.Zero;
                }

                if (errorCode != ErrorCode.ERROR_SUCCESS)
                {
                    throw errorCode.ToCryptographicException();
                }

                return value;
            }
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
    }
}

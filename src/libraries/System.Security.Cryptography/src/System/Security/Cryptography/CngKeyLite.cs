// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;

using Microsoft.Win32.SafeHandles;

using ErrorCode = Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography
{
    internal static class KeyPropertyName
    {
        internal const string Algorithm = "Algorithm Name";                 // NCRYPT_ALGORITHM_PROPERTY
        internal const string AlgorithmGroup = "Algorithm Group";           // NCRYPT_ALGORITHM_GROUP_PROPERTY
        internal const string ChainingMode = "Chaining Mode";               // NCRYPT_CHAINING_MODE_PROPERTY
        internal const string ECCCurveName = "ECCCurveName";                // NCRYPT_ECC_CURVE_NAME
        internal const string ECCParameters = "ECCParameters";              // BCRYPT_ECC_PARAMETERS
        internal const string ExportPolicy = "Export Policy";               // NCRYPT_EXPORT_POLICY_PROPERTY
        internal const string InitializationVector = "IV";                  // NCRYPT_INITIALIZATION_VECTOR
        internal const string KeyType = "Key Type";                         // NCRYPT_KEY_TYPE_PROPERTY
        internal const string KeyUsage = "Key Usage";                       // NCRYPT_KEY_USAGE_PROPERTY
        internal const string Length = "Length";                            // NCRYPT_LENGTH_PROPERTY
        internal const string Name = "Name";                                // NCRYPT_NAME_PROPERTY
        internal const string ParentWindowHandle = "HWND Handle";           // NCRYPT_WINDOW_HANDLE_PROPERTY
        internal const string PublicKeyLength = "PublicKeyLength";          // NCRYPT_PUBLIC_KEY_LENGTH (Win10+)
        internal const string ProviderHandle = "Provider Handle";           // NCRYPT_PROVIDER_HANDLE_PROPERTY
        internal const string UIPolicy = "UI Policy";                       // NCRYPT_UI_POLICY_PROPERTY
        internal const string UniqueName = "Unique Name";                   // NCRYPT_UNIQUE_NAME_PROPERTY
        internal const string UseContext = "Use Context";                   // NCRYPT_USE_CONTEXT_PROPERTY


        //
        // Properties defined by the CLR
        //

        /// <summary>
        ///     Is the key a CLR created ephemeral key, it will contain a single byte with value 1 if the
        ///     key was created by the CLR as an ephemeral key.
        /// </summary>
        internal const string ClrIsEphemeral = "CLR IsEphemeral";
    }

    internal static class CngKeyLite
    {
        private static readonly SafeNCryptProviderHandle s_microsoftSoftwareProviderHandle =
            OpenNCryptProvider("Microsoft Software Key Storage Provider"); // MS_KEY_STORAGE_PROVIDER

        internal static unsafe SafeNCryptKeyHandle ImportKeyBlob(
            string blobType,
            ReadOnlySpan<byte> keyBlob,
            bool encrypted = false,
            ReadOnlySpan<char> password = default)
        {
            SafeNCryptKeyHandle keyHandle;
            ErrorCode errorCode;

            if (encrypted)
            {
                using (var stringHandle = new SafeUnicodeStringHandle(password))
                {
                    Interop.NCrypt.NCryptBuffer* buffers = stackalloc Interop.NCrypt.NCryptBuffer[1];

                    buffers[0] = new Interop.NCrypt.NCryptBuffer
                    {
                        BufferType = Interop.NCrypt.BufferType.PkcsSecret,
                        cbBuffer = checked(2 * (password.Length + 1)),
                        pvBuffer = stringHandle.DangerousGetHandle(),
                    };

                    if (buffers[0].pvBuffer == IntPtr.Zero)
                    {
                        buffers[0].cbBuffer = 0;
                    }

                    Interop.NCrypt.NCryptBufferDesc desc = new Interop.NCrypt.NCryptBufferDesc
                    {
                        cBuffers = 1,
                        pBuffers = (IntPtr)buffers,
                        ulVersion = 0,
                    };

                    errorCode = Interop.NCrypt.NCryptImportKey(
                        s_microsoftSoftwareProviderHandle,
                        IntPtr.Zero,
                        blobType,
                        ref desc,
                        out keyHandle,
                        ref MemoryMarshal.GetReference(keyBlob),
                        keyBlob.Length,
                        0);
                }
            }
            else
            {
                errorCode = Interop.NCrypt.NCryptImportKey(
                    s_microsoftSoftwareProviderHandle,
                    IntPtr.Zero,
                    blobType,
                    IntPtr.Zero,
                    out keyHandle,
                    ref MemoryMarshal.GetReference(keyBlob),
                    keyBlob.Length,
                    0);
            }

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            Debug.Assert(keyHandle != null);

            SetExportable(keyHandle);
            return keyHandle;
        }

        internal static SafeNCryptKeyHandle ImportKeyBlob(string blobType, byte[] keyBlob, string curveName)
        {
            SafeNCryptKeyHandle keyHandle;

            keyHandle = ECCng.ImportKeyBlob(blobType, keyBlob, curveName, s_microsoftSoftwareProviderHandle);

            Debug.Assert(keyHandle != null);

            SetExportable(keyHandle);
            return keyHandle;
        }

        internal static byte[] ExportKeyBlob(SafeNCryptKeyHandle keyHandle, string blobType)
        {
            Debug.Assert(!keyHandle.IsInvalid);

            int numBytesNeeded;

            ErrorCode errorCode = Interop.NCrypt.NCryptExportKey(
                keyHandle,
                IntPtr.Zero,
                blobType,
                IntPtr.Zero,
                null,
                0,
                out numBytesNeeded,
                0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            if (numBytesNeeded == 0)
            {
                // This is rather unlikely, but prevents an error from ref buffer[0].
                return Array.Empty<byte>();
            }

            byte[] buffer = new byte[numBytesNeeded];

            errorCode = Interop.NCrypt.NCryptExportKey(
                keyHandle,
                IntPtr.Zero,
                blobType,
                IntPtr.Zero,
                ref buffer[0],
                buffer.Length,
                out numBytesNeeded,
                0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            if (buffer.Length != numBytesNeeded)
            {
                Span<byte> writtenPortion = buffer.AsSpan(0, numBytesNeeded);
                byte[] tmp = writtenPortion.ToArray();
                CryptographicOperations.ZeroMemory(writtenPortion);
                return tmp;
            }

            return buffer;
        }

        internal static bool TryExportKeyBlob(
            SafeNCryptKeyHandle keyHandle,
            string blobType,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (destination.IsEmpty)
            {
                bytesWritten = 0;
                return false;
            }

            // Sanity check the current bounds
            Span<byte> empty = default;

            ErrorCode errorCode = Interop.NCrypt.NCryptExportKey(
                keyHandle,
                IntPtr.Zero,
                blobType,
                IntPtr.Zero,
                ref MemoryMarshal.GetReference(empty),
                empty.Length,
                out int written,
                0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            if (written > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            if (written == 0)
            {
                bytesWritten = 0;
                return true;
            }

            errorCode = Interop.NCrypt.NCryptExportKey(
                keyHandle,
                IntPtr.Zero,
                blobType,
                IntPtr.Zero,
                ref MemoryMarshal.GetReference(destination),
                destination.Length,
                out written,
                0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            bytesWritten = written;
            return true;
        }

        internal static byte[] ExportPkcs8KeyBlob(
            SafeNCryptKeyHandle keyHandle,
            ReadOnlySpan<char> password,
            int kdfCount)
        {
            bool ret = ExportPkcs8KeyBlob(
                true,
                keyHandle,
                password,
                kdfCount,
                Span<byte>.Empty,
                out _,
                out byte[]? allocated);

            Debug.Assert(ret);
            return allocated!;
        }

        internal static bool TryExportPkcs8KeyBlob(
            SafeNCryptKeyHandle keyHandle,
            ReadOnlySpan<char> password,
            int kdfCount,
            Span<byte> destination,
            out int bytesWritten)
        {
            return ExportPkcs8KeyBlob(
                false,
                keyHandle,
                password,
                kdfCount,
                destination,
                out bytesWritten,
                out _);
        }

        internal static unsafe bool ExportPkcs8KeyBlob(
            bool allocate,
            SafeNCryptKeyHandle keyHandle,
            ReadOnlySpan<char> password,
            int kdfCount,
            Span<byte> destination,
            out int bytesWritten,
            out byte[]? allocated)
        {
            using (SafeUnicodeStringHandle stringHandle = new SafeUnicodeStringHandle(password))
            {
                ReadOnlySpan<byte> pkcs12TripleDesOidBytes = "1.2.840.113549.1.12.1.3\0"u8; // the Windows APIs for OID strings are ASCII-only
                fixed (byte* oidPtr = &MemoryMarshal.GetReference(pkcs12TripleDesOidBytes))
                {
                    Interop.NCrypt.NCryptBuffer* buffers = stackalloc Interop.NCrypt.NCryptBuffer[3];

                    Interop.NCrypt.PBE_PARAMS pbeParams = default;
                    Span<byte> salt = new Span<byte>(pbeParams.rgbSalt, Interop.NCrypt.PBE_PARAMS.RgbSaltSize);
                    RandomNumberGenerator.Fill(salt);
                    pbeParams.Params.cbSalt = salt.Length;
                    pbeParams.Params.iIterations = kdfCount;

                    buffers[0] = new Interop.NCrypt.NCryptBuffer
                    {
                        BufferType = Interop.NCrypt.BufferType.PkcsSecret,
                        cbBuffer = checked(2 * (password.Length + 1)),
                        pvBuffer = stringHandle.DangerousGetHandle(),
                    };

                    if (buffers[0].pvBuffer == IntPtr.Zero)
                    {
                        buffers[0].cbBuffer = 0;
                    }

                    buffers[1] = new Interop.NCrypt.NCryptBuffer
                    {
                        BufferType = Interop.NCrypt.BufferType.PkcsAlgOid,
                        cbBuffer = pkcs12TripleDesOidBytes.Length,
                        pvBuffer = (IntPtr)oidPtr,
                    };

                    buffers[2] = new Interop.NCrypt.NCryptBuffer
                    {
                        BufferType = Interop.NCrypt.BufferType.PkcsAlgParam,
                        cbBuffer = sizeof(Interop.NCrypt.PBE_PARAMS),
                        pvBuffer = (IntPtr)(&pbeParams),
                    };

                    Interop.NCrypt.NCryptBufferDesc desc = new Interop.NCrypt.NCryptBufferDesc
                    {
                        cBuffers = 3,
                        pBuffers = (IntPtr)buffers,
                        ulVersion = 0,
                    };

                    Span<byte> empty = default;

                    ErrorCode errorCode = Interop.NCrypt.NCryptExportKey(
                        keyHandle,
                        IntPtr.Zero,
                        Interop.NCrypt.NCRYPT_PKCS8_PRIVATE_KEY_BLOB,
                        ref desc,
                        ref MemoryMarshal.GetReference(empty),
                        0,
                        out int numBytesNeeded,
                        0);

                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                    {
                        throw errorCode.ToCryptographicException();
                    }

                    allocated = null;

                    if (allocate)
                    {
                        allocated = new byte[numBytesNeeded];
                        destination = allocated;
                    }
                    else if (numBytesNeeded > destination.Length)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    errorCode = Interop.NCrypt.NCryptExportKey(
                        keyHandle,
                        IntPtr.Zero,
                        Interop.NCrypt.NCRYPT_PKCS8_PRIVATE_KEY_BLOB,
                        ref desc,
                        ref MemoryMarshal.GetReference(destination),
                        destination.Length,
                        out numBytesNeeded,
                        0);

                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                    {
                        throw errorCode.ToCryptographicException();
                    }

                    if (allocate && numBytesNeeded != destination.Length)
                    {
                        byte[] trimmed = new byte[numBytesNeeded];
                        destination.Slice(0, numBytesNeeded).CopyTo(trimmed);
                        CryptographicOperations.ZeroMemory(allocated.AsSpan(0, numBytesNeeded));
                        allocated = trimmed;
                    }

                    bytesWritten = numBytesNeeded;
                    return true;
                }
            }
        }

        internal static SafeNCryptKeyHandle GenerateNewExportableKey(string algorithm, int keySize)
        {
            // Despite the function being create "persisted" key, since we pass a null name it's
            // actually ephemeral.
            SafeNCryptKeyHandle keyHandle;
            ErrorCode errorCode = Interop.NCrypt.NCryptCreatePersistedKey(
                s_microsoftSoftwareProviderHandle,
                out keyHandle,
                algorithm,
                null,
                0,
                CngKeyCreationOptions.None);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            Debug.Assert(!keyHandle.IsInvalid);

            SetExportable(keyHandle);
            SetKeyLength(keyHandle, keySize);

            errorCode = Interop.NCrypt.NCryptFinalizeKey(keyHandle, 0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            return keyHandle;
        }

        internal static SafeNCryptKeyHandle GenerateNewExportableKey(string algorithm, string curveName)
        {
            // Despite the function being create "persisted" key, since we pass a null name it's
            // actually ephemeral.
            SafeNCryptKeyHandle keyHandle;
            ErrorCode errorCode = Interop.NCrypt.NCryptCreatePersistedKey(
                s_microsoftSoftwareProviderHandle,
                out keyHandle,
                algorithm,
                null,
                0,
                CngKeyCreationOptions.None);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            Debug.Assert(!keyHandle.IsInvalid);

            SetExportable(keyHandle);
            SetCurveName(keyHandle, curveName);

            errorCode = Interop.NCrypt.NCryptFinalizeKey(keyHandle, 0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            return keyHandle;
        }

        internal static SafeNCryptKeyHandle GenerateNewExportableKey(string algorithm, ref ECCurve explicitCurve)
        {
            // Despite the function being create "persisted" key, since we pass a null name it's
            // actually ephemeral.
            SafeNCryptKeyHandle keyHandle;
            ErrorCode errorCode = Interop.NCrypt.NCryptCreatePersistedKey(
                s_microsoftSoftwareProviderHandle,
                out keyHandle,
                algorithm,
                null,
                0,
                CngKeyCreationOptions.None);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            Debug.Assert(!keyHandle.IsInvalid);

            SetExportable(keyHandle);
            byte[] parametersBlob = ECCng.GetPrimeCurveParameterBlob(ref explicitCurve);
            SetProperty(keyHandle, KeyPropertyName.ECCParameters, parametersBlob);

            errorCode = Interop.NCrypt.NCryptFinalizeKey(keyHandle, 0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            return keyHandle;
        }

        private static void SetExportable(SafeNCryptKeyHandle keyHandle)
        {
            Debug.Assert(!keyHandle.IsInvalid);
            CngExportPolicies exportPolicy = CngExportPolicies.AllowPlaintextExport;

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

        private static void SetKeyLength(SafeNCryptKeyHandle keyHandle, int keySize)
        {
            Debug.Assert(!keyHandle.IsInvalid);
            unsafe
            {
                ErrorCode errorCode = Interop.NCrypt.NCryptSetProperty(
                    keyHandle,
                    KeyPropertyName.Length,
                    &keySize,
                    sizeof(int),
                    CngPropertyOptions.Persist);

                if (errorCode != ErrorCode.ERROR_SUCCESS)
                {
                    throw errorCode.ToCryptographicException();
                }
            }
        }

        internal static unsafe int GetKeyLength(SafeNCryptKeyHandle keyHandle)
        {
            Debug.Assert(!keyHandle.IsInvalid);
            int keySize = 0;

            // Attempt to use PublicKeyLength first as it returns the correct value for ECC keys
            ErrorCode errorCode = Interop.NCrypt.NCryptGetIntProperty(
                keyHandle,
                KeyPropertyName.PublicKeyLength,
                ref keySize);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                // Fall back to Length (< Windows 10)
                errorCode = Interop.NCrypt.NCryptGetIntProperty(
                    keyHandle,
                    KeyPropertyName.Length,
                    ref keySize);
            }

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            return keySize;
        }

        private static SafeNCryptProviderHandle OpenNCryptProvider(string providerName)
        {
            SafeNCryptProviderHandle providerHandle;
            ErrorCode errorCode = Interop.NCrypt.NCryptOpenStorageProvider(out providerHandle, providerName, 0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            Debug.Assert(!providerHandle.IsInvalid);
            return providerHandle;
        }

        /// <summary>
        /// Returns a CNG key property.
        /// </summary>
        /// <returns>
        /// null - if property not defined on key.
        /// throws - for any other type of error.
        /// </returns>
        private static byte[]? GetProperty(SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
        {
            return CngHelpers.GetProperty(ncryptHandle, propertyName, options);
        }

        internal static string? GetCurveName(SafeNCryptHandle ncryptHandle)
        {
            Debug.Assert(!ncryptHandle.IsInvalid);
            return ncryptHandle.GetPropertyAsString(KeyPropertyName.ECCCurveName, CngPropertyOptions.None);
        }

        internal static void SetCurveName(SafeNCryptHandle keyHandle, string curveName)
        {
            unsafe
            {
                byte[] curveNameBytes = new byte[(curveName.Length + 1) * sizeof(char)]; // +1 to add trailing null
                System.Text.Encoding.Unicode.GetBytes(curveName, 0, curveName.Length, curveNameBytes, 0);
                SetProperty(keyHandle, KeyPropertyName.ECCCurveName, curveNameBytes);
            }
        }

        private static void SetProperty(SafeNCryptHandle ncryptHandle, string propertyName, byte[] value)
        {
            Debug.Assert(!ncryptHandle.IsInvalid);
            unsafe
            {
                fixed (byte* pBlob = value)
                {
                    ErrorCode errorCode = Interop.NCrypt.NCryptSetProperty(
                        ncryptHandle,
                        propertyName,
                        pBlob,
                        value.Length,
                        CngPropertyOptions.None);

                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                    {
                        throw errorCode.ToCryptographicException();
                    }
                }
            }
        }
    }
}

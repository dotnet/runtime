// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;
using ErrorCode = Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography
{
    internal static partial class CngHelpers
    {
        internal static CryptographicException ToCryptographicException(this Interop.NCrypt.ErrorCode errorCode)
        {
            return ((int)errorCode).ToCryptographicException();
        }

        internal static void SetExportPolicy(this SafeNCryptKeyHandle keyHandle, CngExportPolicies exportPolicy)
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

        internal static bool TryExportKeyBlob(
            this SafeNCryptKeyHandle handle,
            string blobType,
            Span<byte> destination,
            out int bytesWritten)
        {
            // Sanity check the current bounds
            Span<byte> empty = default;

            ErrorCode errorCode = Interop.NCrypt.NCryptExportKey(
                handle,
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

            errorCode = Interop.NCrypt.NCryptExportKey(
                handle,
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
#if NET
                    RandomNumberGenerator.Fill(salt);
#else
                    CngHelpers.GetRandomBytes(salt);
#endif
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
                        Array.Clear(allocated!, 0, numBytesNeeded);
                        allocated = trimmed;
                    }

                    bytesWritten = numBytesNeeded;
                    return true;
                }
            }
        }

        [SupportedOSPlatform("windows")]
        internal static CngKey Duplicate(this SafeNCryptKeyHandle keyHandle, bool isEphemeral)
        {
            return CngKey.Open(keyHandle, isEphemeral ? CngKeyHandleOpenOptions.EphemeralKey : CngKeyHandleOpenOptions.None);
        }
    }
}

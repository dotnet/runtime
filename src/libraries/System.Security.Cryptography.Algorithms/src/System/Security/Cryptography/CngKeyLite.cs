// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;

using Microsoft.Win32.SafeHandles;

using ErrorCode=Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography
{
    internal static class CngKeyLite
    {
        private static class KeyPropertyName
        {
            internal const string ExportPolicy = "Export Policy";               // NCRYPT_EXPORT_POLICY_PROPERTY
            internal const string Length = "Length";                            // NCRYPT_LENGTH_PROPERTY
        }

        private static readonly SafeNCryptProviderHandle s_microsoftSoftwareProviderHandle =
            OpenNCryptProvider("Microsoft Software Key Storage Provider"); // MS_KEY_STORAGE_PROVIDER

        internal static SafeNCryptKeyHandle ImportKeyBlob(string blobType, byte[] keyBlob)
        {
            SafeNCryptKeyHandle keyHandle;

            ErrorCode errorCode = Interop.NCrypt.NCryptImportKey(
                s_microsoftSoftwareProviderHandle,
                IntPtr.Zero,
                blobType,
                IntPtr.Zero,
                out keyHandle,
                keyBlob,
                keyBlob.Length,
                0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

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

            byte[] buffer = new byte[numBytesNeeded];

            errorCode = Interop.NCrypt.NCryptExportKey(
                keyHandle,
                IntPtr.Zero,
                blobType,
                IntPtr.Zero,
                buffer,
                buffer.Length,
                out numBytesNeeded,
                0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            Array.Resize(ref buffer, numBytesNeeded);
            return buffer;
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

        private static void SetExportable(SafeNCryptKeyHandle keyHandle)
        {
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
            int keySize = 0;
            int bytesWritten;

            ErrorCode errorCode = Interop.NCrypt.NCryptGetProperty(
                keyHandle,
                KeyPropertyName.Length,
                &keySize,
                sizeof(int),
                out bytesWritten,
                CngPropertyOptions.None);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            Debug.Assert(bytesWritten == sizeof(int));
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
    }

    // Limited version of CngExportPolicies from the Cng contract.
    [Flags]
    internal enum CngPropertyOptions : int
    {
        None = 0,
        Persist = unchecked((int)0x80000000),     //NCRYPT_PERSIST_FLAG (The property should be persisted.)
    }

    // Limited version of CngKeyCreationOptions from the Cng contract.
    [Flags]
    internal enum CngKeyCreationOptions : int
    {
        None = 0x00000000,
    }

    // Limited version of CngKeyOpenOptions from the Cng contract.
    [Flags]
    internal enum CngKeyOpenOptions : int
    {
        None = 0x00000000,
    }

    // Limited version of CngExportPolicies from the Cng contract.
    [Flags]
    internal enum CngExportPolicies : int
    {
        None = 0x00000000,
        AllowPlaintextExport = 0x00000002,      // NCRYPT_ALLOW_PLAINTEXT_EXPORT_FLAG
    }
}

// Internal, lightweight versions of the SafeNCryptHandle types which are public in CNG.
namespace Microsoft.Win32.SafeHandles
{
    internal class SafeNCryptHandle : SafeHandle
    {
        public SafeNCryptHandle()
            : base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            ErrorCode errorCode = Interop.NCrypt.NCryptFreeObject(handle);
            bool success = (errorCode == ErrorCode.ERROR_SUCCESS);
            Debug.Assert(success);
            handle = IntPtr.Zero;
            return success;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }

    internal class SafeNCryptKeyHandle : SafeNCryptHandle
    {
    }

    internal class SafeNCryptProviderHandle : SafeNCryptHandle
    {
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Internal.Cryptography;

using Microsoft.Win32.SafeHandles;

using ErrorCode=Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography
{
    internal static class CngKeyLite
    {
        private static class KeyPropertyName
        {
            internal const string ECCCurveName = "ECCCurveName";                // NCRYPT_ECC_CURVE_NAME
            internal const string ECCParameters = "ECCParameters";              // BCRYPT_ECC_PARAMETERS
            internal const string ExportPolicy = "Export Policy";               // NCRYPT_EXPORT_POLICY_PROPERTY
            internal const string Length = "Length";                            // NCRYPT_LENGTH_PROPERTY
            internal const string PublicKeyLength = "PublicKeyLength";          // NCRYPT_PUBLIC_KEY_LENGTH (Win10+)
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
        private static byte[] GetProperty(SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
        {
            Debug.Assert(!ncryptHandle.IsInvalid);
            unsafe
            {
                int numBytesNeeded;
                ErrorCode errorCode = Interop.NCrypt.NCryptGetProperty(ncryptHandle, propertyName, null, 0, out numBytesNeeded, options);
                if (errorCode == ErrorCode.NTE_NOT_FOUND)
                    return null;
                if (errorCode != ErrorCode.ERROR_SUCCESS)
                    throw errorCode.ToCryptographicException();

                byte[] propertyValue = new byte[numBytesNeeded];
                fixed (byte* pPropertyValue = propertyValue)
                {
                    errorCode = Interop.NCrypt.NCryptGetProperty(ncryptHandle, propertyName, pPropertyValue, propertyValue.Length, out numBytesNeeded, options);
                }
                if (errorCode == ErrorCode.NTE_NOT_FOUND)
                    return null;
                if (errorCode != ErrorCode.ERROR_SUCCESS)
                    throw errorCode.ToCryptographicException();

                Array.Resize(ref propertyValue, numBytesNeeded);
                return propertyValue;
            }
        }

        /// <summary>
        /// Retrieve a well-known CNG string property. (Note: desktop compat: this helper likes to return special values rather than throw exceptions for missing
        /// or ill-formatted property values. Only use it for well-known properties that are unlikely to be ill-formatted.) 
        /// </summary>
        private static string GetPropertyAsString(SafeNCryptHandle ncryptHandle, string propertyName, CngPropertyOptions options)
        {
            Debug.Assert(!ncryptHandle.IsInvalid);
            byte[] value = GetProperty(ncryptHandle, propertyName, options);
            if (value == null)
                return null;   // Desktop compat: return null if key not present.
            if (value.Length == 0)
                return string.Empty; // Desktop compat: return empty if property value is 0-length.

            unsafe
            {
                fixed (byte* pValue = &value[0])
                {
                    string valueAsString = Marshal.PtrToStringUni((IntPtr)pValue);
                    return valueAsString;
                }
            }
        }

        internal static string GetCurveName(SafeNCryptHandle ncryptHandle)
        {
            Debug.Assert(!ncryptHandle.IsInvalid);
            return GetPropertyAsString(ncryptHandle, KeyPropertyName.ECCCurveName, CngPropertyOptions.None);
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

    internal class DuplicateSafeNCryptKeyHandle : SafeNCryptKeyHandle
    {
        public DuplicateSafeNCryptKeyHandle(SafeNCryptKeyHandle original)
            : base()
        {
            bool success = false;
            original.DangerousAddRef(ref success);
            if (!success)
                throw new CryptographicException(); // DangerousAddRef() never actually sets success to false, so no need to expend a resource string here.
            SetHandle(original.DangerousGetHandle());
            _original = original;
        }

        protected override bool ReleaseHandle()
        {
            _original.DangerousRelease();
            SetHandle(IntPtr.Zero);
            return true;
        }

        private readonly SafeNCryptKeyHandle _original;
    }
}

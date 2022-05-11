// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

using ErrorCode = Interop.NCrypt.ErrorCode;
using NCRYPT_UI_POLICY = Interop.NCrypt.NCRYPT_UI_POLICY;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Managed representation of an NCrypt key
    /// </summary>
    public sealed partial class CngKey : IDisposable
    {
        //
        // Creation factory methods
        //

        [SupportedOSPlatform("windows")]
        public static CngKey Create(CngAlgorithm algorithm)
        {
            return Create(algorithm, keyName: null);
        }

        [SupportedOSPlatform("windows")]
        public static CngKey Create(CngAlgorithm algorithm, string? keyName)
        {
            return Create(algorithm, keyName, creationParameters: null);
        }

        [SupportedOSPlatform("windows")]
        public static CngKey Create(CngAlgorithm algorithm, string? keyName, CngKeyCreationParameters? creationParameters)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (creationParameters == null)
                creationParameters = new CngKeyCreationParameters();

            SafeNCryptProviderHandle providerHandle = creationParameters.Provider!.OpenStorageProvider();
            SafeNCryptKeyHandle keyHandle;
            ErrorCode errorCode = Interop.NCrypt.NCryptCreatePersistedKey(providerHandle, out keyHandle, algorithm.Algorithm, keyName, 0, creationParameters.KeyCreationOptions);
            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                // For ecc, the exception may be caught and re-thrown as PlatformNotSupportedException
                throw errorCode.ToCryptographicException();
            }

            InitializeKeyProperties(keyHandle, creationParameters);

            errorCode = Interop.NCrypt.NCryptFinalizeKey(keyHandle, 0);
            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                // For ecc, the exception may be caught and re-thrown as PlatformNotSupportedException
                throw errorCode.ToCryptographicException();
            }

            CngKey key = new CngKey(providerHandle, keyHandle);

            // No name translates to an ephemeral key
            if (keyName == null)
            {
                key.IsEphemeral = true;
            }

            return key;
        }

        /// <summary>
        ///     Setup the key properties specified in the key creation parameters
        /// </summary>
        private static void InitializeKeyProperties(SafeNCryptKeyHandle keyHandle, CngKeyCreationParameters creationParameters)
        {
            unsafe
            {
                if (creationParameters.ExportPolicy.HasValue)
                {
                    CngExportPolicies exportPolicy = creationParameters.ExportPolicy.Value;
                    keyHandle.SetExportPolicy(exportPolicy);
                }

                if (creationParameters.KeyUsage.HasValue)
                {
                    CngKeyUsages keyUsage = creationParameters.KeyUsage.Value;
                    ErrorCode errorCode = Interop.NCrypt.NCryptSetProperty(keyHandle, KeyPropertyName.KeyUsage, &keyUsage, sizeof(CngKeyUsages), CngPropertyOptions.Persist);
                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                        throw errorCode.ToCryptographicException();
                }

                if (creationParameters.ParentWindowHandle != IntPtr.Zero)
                {
                    IntPtr parentWindowHandle = creationParameters.ParentWindowHandle;
                    ErrorCode errorCode = Interop.NCrypt.NCryptSetProperty(keyHandle, KeyPropertyName.ParentWindowHandle, &parentWindowHandle, sizeof(IntPtr), CngPropertyOptions.None);
                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                        throw errorCode.ToCryptographicException();
                }

                CngUIPolicy? uiPolicy = creationParameters.UIPolicy;
                if (uiPolicy != null)
                {
                    InitializeKeyUiPolicyProperties(keyHandle, uiPolicy);
                }

                // Iterate over the custom properties, setting those as well.
                foreach (CngProperty property in creationParameters.Parameters)
                {
                    byte[]? value = property.GetValueWithoutCopying();
                    int valueLength = (value == null) ? 0 : value.Length;
                    fixed (byte* pValue = MapZeroLengthArrayToNonNullPointer(value))
                    {
                        ErrorCode errorCode = Interop.NCrypt.NCryptSetProperty(keyHandle, property.Name, pValue, valueLength, property.Options);
                        if (errorCode != ErrorCode.ERROR_SUCCESS)
                            throw errorCode.ToCryptographicException();
                    }
                }
            }
        }

        /// <summary>
        ///     Setup the UIPolicy key properties specified in the key creation parameters
        /// </summary>
        private static void InitializeKeyUiPolicyProperties(SafeNCryptKeyHandle keyHandle, CngUIPolicy uiPolicy)
        {
            unsafe
            {
                fixed (char* pinnedCreationTitle = uiPolicy.CreationTitle,
                             pinnedFriendlyName = uiPolicy.FriendlyName,
                             pinnedDescription = uiPolicy.Description)
                {
                    NCRYPT_UI_POLICY ncryptUiPolicy = new NCRYPT_UI_POLICY()
                    {
                        dwVersion = 1,
                        dwFlags = uiPolicy.ProtectionLevel,
                        pszCreationTitle = new IntPtr(pinnedCreationTitle),
                        pszFriendlyName = new IntPtr(pinnedFriendlyName),
                        pszDescription = new IntPtr(pinnedDescription),
                    };

                    ErrorCode errorCode = Interop.NCrypt.NCryptSetProperty(keyHandle, KeyPropertyName.UIPolicy, &ncryptUiPolicy, sizeof(NCRYPT_UI_POLICY), CngPropertyOptions.Persist);
                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                        throw errorCode.ToCryptographicException();
                }

                string? useContext = uiPolicy.UseContext;
                if (useContext != null)
                {
                    int useContextByteLength = checked((useContext.Length + 1) * sizeof(char));
                    fixed (char* pinnedUseContext = useContext)
                    {
                        ErrorCode errorCode = Interop.NCrypt.NCryptSetProperty(keyHandle, KeyPropertyName.UseContext, pinnedUseContext, useContextByteLength, CngPropertyOptions.Persist);
                        if (errorCode != ErrorCode.ERROR_SUCCESS)
                            throw errorCode.ToCryptographicException();
                    }
                }
            }
        }
    }
}

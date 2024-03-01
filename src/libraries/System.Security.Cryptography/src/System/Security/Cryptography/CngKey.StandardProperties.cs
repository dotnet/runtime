// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
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
        private const int CachedKeySizeUninitializedSentinel = -1;
        private volatile int _cachedKeySize = CachedKeySizeUninitializedSentinel;

        private volatile CngAlgorithm? _cachedAlgorithm;
        private volatile bool _hasCachedAlgorithmGroup;
        private volatile CngAlgorithmGroup? _cachedAlgorithmGroup;
        private volatile bool _hasCachedProvider;
        private volatile CngProvider? _cachedProvider;

        /// <summary>
        ///     Algorithm group this key can be used with
        /// </summary>
        public CngAlgorithm Algorithm
        {
            get
            {
                if (_cachedAlgorithm is null || _keyHandle.IsClosed)
                {
                    string algorithm = _keyHandle.GetPropertyAsString(KeyPropertyName.Algorithm, CngPropertyOptions.None)!;

                    // .NET Framework compat: Don't check for null. Just let CngAlgorithm handle it.
                    _cachedAlgorithm = new CngAlgorithm(algorithm);
                }

                return _cachedAlgorithm;

            }

        }

        /// <summary>
        ///     Name of the algorithm this key can be used with
        /// </summary>
        public CngAlgorithmGroup? AlgorithmGroup
        {
            get
            {
                if (!_hasCachedAlgorithmGroup || _keyHandle.IsClosed)
                {
                    string? algorithmGroup = _keyHandle.GetPropertyAsString(KeyPropertyName.AlgorithmGroup, CngPropertyOptions.None);

                    if (algorithmGroup is not null)
                    {
                        _cachedAlgorithmGroup = new CngAlgorithmGroup(algorithmGroup);
                    }

                    _hasCachedAlgorithmGroup = true;
                }

                return _cachedAlgorithmGroup;
            }
        }

        /// <summary>
        ///     Export restrictions on the key
        /// </summary>
        public CngExportPolicies ExportPolicy
        {
            get
            {
                CngExportPolicies policy = (CngExportPolicies)_keyHandle.GetPropertyAsDword(KeyPropertyName.ExportPolicy, CngPropertyOptions.None);
                return policy;
            }

            internal set
            {
                _keyHandle.SetExportPolicy(value);
            }
        }

        /// <summary>
        ///     Native handle for the key
        /// </summary>
        public SafeNCryptKeyHandle Handle
        {
            get
            {
                return _keyHandle.Duplicate();
            }
        }

        internal SafeNCryptKeyHandle HandleNoDuplicate
        {
            get
            {
                return _keyHandle;
            }
        }

        /// <summary>
        ///     Is this key ephemeral or persisted
        /// </summary>
        /// <remarks>
        ///     Any ephemeral key created by the CLR will have a property 'CLR IsEphemeral' which consists
        ///     of a single byte containing the value 1. We cannot detect ephemeral keys created by other
        ///     APIs and imported via handle.
        /// </remarks>
        public bool IsEphemeral
        {
            get
            {
                unsafe
                {
                    byte propertyValue;
                    int cbResult;
                    ErrorCode errorCode = Interop.NCrypt.NCryptGetProperty(_keyHandle, KeyPropertyName.ClrIsEphemeral, &propertyValue, sizeof(byte), out cbResult, CngPropertyOptions.CustomProperty);
                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                    {
                        // Third party Key providers, and Windows PCP KSP won't recognize this property;
                        // and Win32 layer does not enforce error return contract.
                        // Therefore, they can return whatever error code they think appropriate.
                        return false;
                    }

                    if (cbResult != sizeof(byte))
                        return false;

                    if (propertyValue != 1)
                        return false;

                    return true;
                }
            }

            private set
            {
                unsafe
                {
                    byte isEphemeral = value ? (byte)1 : (byte)0;
                    ErrorCode errorCode = Interop.NCrypt.NCryptSetProperty(_keyHandle, KeyPropertyName.ClrIsEphemeral, &isEphemeral, sizeof(byte), CngPropertyOptions.CustomProperty);
                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                        throw errorCode.ToCryptographicException();
                }
            }
        }

        /// <summary>
        ///     Is this a machine key or a user key
        /// </summary>
        public bool IsMachineKey
        {
            get
            {
                CngKeyOpenOptions keyType = (CngKeyOpenOptions)_keyHandle.GetPropertyAsDword(KeyPropertyName.KeyType, CngPropertyOptions.None);
                bool isMachineKey = (keyType & CngKeyOpenOptions.MachineKey) == CngKeyOpenOptions.MachineKey;
                return isMachineKey;
            }
        }

        /// <summary>
        ///     The name of the key, null if it is ephemeral. We can only detect ephemeral keys created by
        ///     the CLR. Other ephemeral keys, such as those imported by handle, will get a CryptographicException
        ///     if they read this property.
        /// </summary>
        public string? KeyName
        {
            get
            {
                if (IsEphemeral)
                    return null;

                string? keyName = _keyHandle.GetPropertyAsString(KeyPropertyName.Name, CngPropertyOptions.None);
                return keyName;
            }
        }

        /// <summary>
        ///     Size, in bits, of the key
        /// </summary>
        public int KeySize
        {
            get
            {
                // Key size lookup is a common operation, and we don't want to incur the
                // LRPC overhead to query lsass for the information. Since the key size
                // cannot be changed on an existing key, we'll cache it. For consistency
                // with other properties, we'll still throw if the underlying handle has
                // been closed.
                if (_cachedKeySize == CachedKeySizeUninitializedSentinel || _keyHandle.IsClosed)
                {
                    _cachedKeySize = ComputeKeySize();
                }
                return _cachedKeySize;

                int ComputeKeySize()
                {
                    int keySize = 0;

                    // Attempt to use PublicKeyLength first as it returns the correct value for ECC keys
                    ErrorCode errorCode = Interop.NCrypt.NCryptGetIntProperty(
                        _keyHandle,
                        KeyPropertyName.PublicKeyLength,
                        ref keySize);

                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                    {
                        // Fall back to Length (< Windows 10)
                        errorCode = Interop.NCrypt.NCryptGetIntProperty(
                            _keyHandle,
                            KeyPropertyName.Length,
                            ref keySize);
                    }

                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                    {
                        throw errorCode.ToCryptographicException();
                    }

                    if (keySize == 0 && Provider == CngProvider.MicrosoftPlatformCryptoProvider)
                    {
                        // The platform crypto provider always returns "0" for EC keys when asked for a key size. This
                        // has been observed in Windows 10 and most recently observed in Windows 11 22H2.
                        // The Algorithm NCrypt Property only returns the Algorithm Group, so that doesn't work either.
                        // What does work is the ECCCurveName.
                        // Accessing the AlgorithmGroup property is expensive which is why this is broken in to a separate
                        // if block. We don't want to read from it unless we don't know the key size.
                        CngAlgorithmGroup? algorithmGroup = AlgorithmGroup;

                        if (algorithmGroup == CngAlgorithmGroup.ECDiffieHellman || algorithmGroup == CngAlgorithmGroup.ECDsa)
                        {
                            string? curve = _keyHandle.GetPropertyAsString(KeyPropertyName.ECCCurveName, CngPropertyOptions.None);

                            switch (curve)
                            {
                                // nistP192 and nistP224 don't have named curve accelerators but we can handle them.
                                // These string values match the names in https://learn.microsoft.com/en-us/windows/win32/seccng/cng-named-elliptic-curves
                                case "nistP192": return 192;
                                case "nistP224": return 224;
                                case nameof(ECCurve.NamedCurves.nistP256): return 256;
                                case nameof(ECCurve.NamedCurves.nistP384): return 384;
                                case nameof(ECCurve.NamedCurves.nistP521): return 521;
                            }
                        }
                    }

                    return keySize;
                }
            }
        }

        /// <summary>
        ///     Usage restrictions on the key
        /// </summary>
        public CngKeyUsages KeyUsage

        {
            get
            {
                CngKeyUsages keyUsage = (CngKeyUsages)(_keyHandle.GetPropertyAsDword(KeyPropertyName.KeyUsage, CngPropertyOptions.None));
                return keyUsage;
            }
        }

        /// <summary>
        ///     HWND of the window to use as a parent for any UI
        /// </summary>
        public IntPtr ParentWindowHandle
        {
            get
            {
                IntPtr parentWindowHandle = _keyHandle.GetPropertyAsIntPtr(KeyPropertyName.ParentWindowHandle, CngPropertyOptions.None);
                return parentWindowHandle;
            }

            set
            {
                unsafe
                {
                    Interop.NCrypt.NCryptSetProperty(_keyHandle, KeyPropertyName.ParentWindowHandle, &value, IntPtr.Size, CngPropertyOptions.None);
                }
            }
        }


        /// <summary>
        ///     KSP which holds this key
        /// </summary>
        public CngProvider? Provider
        {
            get
            {
                if (!_hasCachedProvider || _keyHandle.IsClosed)
                {
                    string? provider = _providerHandle.GetPropertyAsString(ProviderPropertyName.Name, CngPropertyOptions.None);

                    if (provider is not null)
                    {
                        _cachedProvider = new CngProvider(provider);
                    }

                    _hasCachedProvider = true;
                }

                return _cachedProvider;
            }
        }

        /// <summary>
        ///     Native handle to the KSP associated with this key
        /// </summary>
        public SafeNCryptProviderHandle ProviderHandle
        {
            get
            {
                return _providerHandle.Duplicate();
            }
        }

        /// <summary>
        ///     UI strings associated with a key
        /// </summary>
        public CngUIPolicy UIPolicy
        {
            get
            {
                CngUIProtectionLevels uiProtectionLevel;
                string? friendlyName;
                string? description;
                string? creationTitle;
                unsafe
                {
                    int numBytesNeeded;
                    ErrorCode errorCode = Interop.NCrypt.NCryptGetProperty(_keyHandle, KeyPropertyName.UIPolicy, null, 0, out numBytesNeeded, CngPropertyOptions.None);
                    if (errorCode != ErrorCode.ERROR_SUCCESS && errorCode != ErrorCode.NTE_NOT_FOUND)
                        throw errorCode.ToCryptographicException();

                    if (errorCode != ErrorCode.ERROR_SUCCESS || numBytesNeeded == 0)
                    {
                        // No UI policy set. Our defined behavior is to return a non-null CngUIPolicy always, so set the UI policy components to the default values.
                        uiProtectionLevel = CngUIProtectionLevels.None;
                        friendlyName = null;
                        description = null;
                        creationTitle = null;
                    }
                    else
                    {
                        // The returned property must be at least the size of NCRYPT_UI_POLICY (plus the extra size of the UI policy strings if any.)
                        // If by any chance, a rogue provider passed us something smaller, fail-fast now rather risk dereferencing "pointers" that were actually
                        // constructed from memory garbage.
                        if (numBytesNeeded < sizeof(NCRYPT_UI_POLICY))
                            throw ErrorCode.E_FAIL.ToCryptographicException();

                        // ! We must keep this byte array pinned until NCryptGetProperty() has returned *and* we've marshaled all of the inner native strings into managed String
                        // ! objects. Otherwise, a badly timed GC will move the native strings in memory and invalidate the pointers to them before we dereference them.
                        byte[] ncryptUiPolicyAndStrings = new byte[numBytesNeeded];
                        fixed (byte* pNcryptUiPolicyAndStrings = &ncryptUiPolicyAndStrings[0])
                        {
                            errorCode = Interop.NCrypt.NCryptGetProperty(_keyHandle, KeyPropertyName.UIPolicy, pNcryptUiPolicyAndStrings, ncryptUiPolicyAndStrings.Length, out numBytesNeeded, CngPropertyOptions.None);
                            if (errorCode != ErrorCode.ERROR_SUCCESS)
                                throw errorCode.ToCryptographicException();

                            NCRYPT_UI_POLICY* pNcryptUiPolicy = (NCRYPT_UI_POLICY*)pNcryptUiPolicyAndStrings;
                            uiProtectionLevel = pNcryptUiPolicy->dwFlags;
                            friendlyName = Marshal.PtrToStringUni(pNcryptUiPolicy->pszFriendlyName);
                            description = Marshal.PtrToStringUni(pNcryptUiPolicy->pszDescription);
                            creationTitle = Marshal.PtrToStringUni(pNcryptUiPolicy->pszCreationTitle);
                        }
                    }
                }

                string? useContext = _keyHandle.GetPropertyAsString(KeyPropertyName.UseContext, CngPropertyOptions.None);

                return new CngUIPolicy(uiProtectionLevel, friendlyName, description, useContext, creationTitle);
            }
        }

        /// <summary>
        ///     Unique name of the key, null if it is ephemeral. See the comments on the Name property for
        ///     details about names of ephemeral keys.
        /// </summary>
        public string? UniqueName
        {
            get
            {
                if (IsEphemeral)
                    return null;

                string? uniqueName = _keyHandle.GetPropertyAsString(KeyPropertyName.UniqueName, CngPropertyOptions.None);
                return uniqueName;
            }
        }
    }
}

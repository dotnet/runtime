// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    [MinimumOSPlatform("windows7.0")]
    public sealed class CspKeyContainerInfo
    {
        private readonly CspParameters _parameters;
        private readonly bool _randomKeyContainer;

        //Public Constructor will call internal constructor.
        public CspKeyContainerInfo(CspParameters parameters)
            : this(parameters, false)
        {
        }

        /// <summary>
        ///Internal constructor for creating the CspKeyContainerInfo object
        /// </summary>
        /// <param name="parameters">CSP parameters</param>
        /// <param name="randomKeyContainer">Is it random container</param>
        internal CspKeyContainerInfo(CspParameters parameters, bool randomKeyContainer)
        {
            _parameters = new CspParameters(parameters);
            if (_parameters.KeyNumber == -1)
            {
                if (_parameters.ProviderType == (int)CapiHelper.ProviderType.PROV_RSA_FULL ||
                    _parameters.ProviderType == (int)CapiHelper.ProviderType.PROV_RSA_AES)
                {
                    _parameters.KeyNumber = (int)KeyNumber.Exchange;
                }
                else if (_parameters.ProviderType == (int)CapiHelper.ProviderType.PROV_DSS_DH)
                {
                    _parameters.KeyNumber = (int)KeyNumber.Signature;
                }
            }
            _randomKeyContainer = randomKeyContainer;
        }

        /// <summary>
        /// Check the key is accessible
        /// </summary>
        public bool Accessible
        {
            get
            {
                object? retVal = ReadKeyParameterSilent(Constants.CLR_ACCESSIBLE, throwOnNotFound: false);

                if (retVal == null)
                {
                    // The key wasn't found, so consider it to be not accessible.
                    return false;
                }

                return (bool)retVal;
            }
        }

        /// <summary>
        /// Check the key is exportable
        /// </summary>
        public bool Exportable
        {
            get
            {
                // Assume hardware keys are not exportable.
                if (HardwareDevice)
                {
                    return false;
                }

                return (bool)ReadKeyParameterSilent(Constants.CLR_EXPORTABLE)!;
            }
        }

        /// <summary>
        /// Check if device with key is HW device
        /// </summary>
        public bool HardwareDevice
        {
            get
            {
                return (bool)ReadDeviceParameterVerifyContext(Constants.CLR_HARDWARE);
            }
        }

        /// <summary>
        /// Get Key container Name
        /// </summary>
        public string? KeyContainerName
        {
            get
            {
                return _parameters.KeyContainerName;
            }
        }

        /// <summary>
        /// Get the key number
        /// </summary>
        public KeyNumber KeyNumber
        {
            get
            {
                return (KeyNumber)_parameters.KeyNumber;
            }
        }

        /// <summary>
        /// Check if machine key store is in flag or not
        /// </summary>
        public bool MachineKeyStore
        {
            get
            {
                return CapiHelper.IsFlagBitSet((uint)_parameters.Flags, (uint)CspProviderFlags.UseMachineKeyStore);
            }
        }

        /// <summary>
        /// Check if key is protected
        /// </summary>
        public bool Protected
        {
            get
            {
                // Assume hardware keys are protected.
                if (HardwareDevice)
                {
                    return true;
                }

                return (bool)ReadKeyParameterSilent(Constants.CLR_PROTECTED)!;
            }
        }

        /// <summary>
        /// Gets the provider name
        /// </summary>
        public string? ProviderName
        {
            get
            {
                return _parameters.ProviderName;
            }
        }

        /// <summary>
        /// Gets the provider type
        /// </summary>
        public int ProviderType
        {
            get
            {
                return _parameters.ProviderType;
            }
        }

        /// <summary>
        /// Check if key container is randomly generated
        /// </summary>
        public bool RandomlyGenerated
        {
            get
            {
                return _randomKeyContainer;
            }
        }

        /// <summary>
        /// Check if container is removable
        /// </summary>
        public bool Removable
        {
            get
            {
                return (bool)ReadDeviceParameterVerifyContext(Constants.CLR_REMOVABLE);
            }
        }

        /// <summary>
        /// Get the container name
        /// </summary>
        public string UniqueKeyContainerName
        {
            get
            {
                return (string)ReadKeyParameterSilent(Constants.CLR_UNIQUE_CONTAINER)!;
            }
        }

        /// <summary>
        /// Read a parameter from the current key using CRYPT_SILENT, to avoid any potential UI prompts.
        /// </summary>
        private object? ReadKeyParameterSilent(int keyParam, bool throwOnNotFound = true)
        {
            const uint SilentFlags = (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_SILENT;

            SafeProvHandle safeProvHandle;
            int hr = CapiHelper.OpenCSP(_parameters, SilentFlags, out safeProvHandle);

            using (safeProvHandle)
            {
                if (hr != CapiHelper.S_OK)
                {
                    if (throwOnNotFound)
                    {
                        throw new CryptographicException(SR.Format(SR.Cryptography_CSP_NotFound, "Error"));
                    }

                    return null;
                }

                object retVal = CapiHelper.GetProviderParameter(safeProvHandle, _parameters.KeyNumber, keyParam);
                return retVal;
            }
        }

        /// <summary>
        /// Read a parameter using VERIFY_CONTEXT to read from the device being targeted by _parameters
        /// </summary>
        private object ReadDeviceParameterVerifyContext(int keyParam)
        {
            CspParameters parameters = new CspParameters(_parameters);

            // We're asking questions of the device container, the only flag that makes sense is Machine vs User.
            parameters.Flags &= CspProviderFlags.UseMachineKeyStore;

            // In order to ask about the device, instead of a key, we need to ensure that no key is named.
            parameters.KeyContainerName = null;

            const uint OpenDeviceFlags = (uint)Interop.Advapi32.CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT;

            SafeProvHandle safeProvHandle;
            int hr = CapiHelper.OpenCSP(parameters, OpenDeviceFlags, out safeProvHandle);

            using (safeProvHandle)
            {
                if (hr != CapiHelper.S_OK)
                {
                    throw new CryptographicException(SR.Format(SR.Cryptography_CSP_NotFound, "Error"));
                }

                object retVal = CapiHelper.GetProviderParameter(safeProvHandle, parameters.KeyNumber, keyParam);
                return retVal;
            }
        }
    }
}

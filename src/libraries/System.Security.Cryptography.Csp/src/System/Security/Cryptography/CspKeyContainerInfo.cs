// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Security;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace System.Security.Cryptography
{
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
        /// <param name="randomKeyContainer">Is it ranndom container</param>
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
                // This method will pop-up a UI for hardware keys.
                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                int hr = CapiHelper.OpenCSP(_parameters, (uint)CapiHelper.CryptAcquireContextFlags.CRYPT_SILENT, ref safeProvHandle);
                if (hr != CapiHelper.S_OK)
                {
                    return false;
                }
                bool isAccessible = (bool)CapiHelper.GetProviderParameter(safeProvHandle, _parameters.KeyNumber, Constants.CLR_ACCESSIBLE);
                safeProvHandle.Dispose();
                return isAccessible;
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
                if (this.HardwareDevice)
                {
                    return false;
                }
                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                int hr = CapiHelper.OpenCSP(_parameters, (uint)CapiHelper.CryptAcquireContextFlags.CRYPT_SILENT, ref safeProvHandle);
                if (hr != CapiHelper.S_OK)
                {
                    throw new CryptographicException(SR.Format(SR.Cryptography_CSP_NotFound, "Error"));
                }

                bool isExportable = (bool)CapiHelper.GetProviderParameter(safeProvHandle, _parameters.KeyNumber, Constants.CLR_EXPORTABLE);
                safeProvHandle.Dispose();
                return isExportable;
            }
        }

        /// <summary>
        /// Check if device with key is HW device
        /// </summary>
        public bool HardwareDevice
        {
            get
            {
                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                CspParameters parameters = new CspParameters(_parameters);
                parameters.KeyContainerName = null;
                parameters.Flags = CapiHelper.IsFlagBitSet((uint)parameters.Flags,
                                                            (uint)CspProviderFlags.UseMachineKeyStore) ?
                                                            CspProviderFlags.UseMachineKeyStore : 0;

                uint flags = (uint)CapiHelper.CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT;
                int hr = CapiHelper.OpenCSP(parameters, flags, ref safeProvHandle);
                if (hr != CapiHelper.S_OK)
                {
                    throw new CryptographicException(SR.Format(SR.Cryptography_CSP_NotFound, "Error"));
                }
                bool isHardwareDevice = (bool)CapiHelper.GetProviderParameter(safeProvHandle, parameters.KeyNumber, Constants.CLR_HARDWARE);
                safeProvHandle.Dispose();
                return isHardwareDevice;
            }
        }

        /// <summary>
        /// Get Key container Name
        /// </summary>
        public string KeyContainerName
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
                return CapiHelper.IsFlagBitSet((uint)_parameters.Flags, (uint)CspProviderFlags.UseMachineKeyStore) ? true : false;
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
                if (this.HardwareDevice == true)
                {
                    return true;
                }
                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                int hr = CapiHelper.OpenCSP(_parameters, (uint)CapiHelper.CryptAcquireContextFlags.CRYPT_SILENT, ref safeProvHandle);
                if (hr != CapiHelper.S_OK)
                {
                    throw new CryptographicException(SR.Format(SR.Cryptography_CSP_NotFound, "Error"));
                }
                bool isProtected = (bool)CapiHelper.GetProviderParameter(safeProvHandle, _parameters.KeyNumber, Constants.CLR_PROTECTED);
                safeProvHandle.Dispose();
                return isProtected;
            }
        }

        /// <summary>
        /// Gets the provider name
        /// </summary>
        public string ProviderName
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
                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                CspParameters parameters = new CspParameters(_parameters);
                parameters.KeyContainerName = null;
                parameters.Flags = CapiHelper.IsFlagBitSet((uint)parameters.Flags,
                                                            (uint)CspProviderFlags.UseMachineKeyStore) ?
                                                            CspProviderFlags.UseMachineKeyStore : 0;

                uint flags = (uint)CapiHelper.CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT;
                int hr = CapiHelper.OpenCSP(parameters, flags, ref safeProvHandle);
                if (hr != CapiHelper.S_OK)
                {
                    throw new CryptographicException(SR.Format(SR.Cryptography_CSP_NotFound, "Error"));
                }
                bool isRemovable = (bool)CapiHelper.GetProviderParameter(safeProvHandle, parameters.KeyNumber, Constants.CLR_REMOVABLE);
                safeProvHandle.Dispose();
                return isRemovable;
            }
        }

        /// <summary>
        /// Get the container name 
        /// </summary>
        public string UniqueKeyContainerName
        {
            get
            {
                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                int hr = CapiHelper.OpenCSP(_parameters, (uint)CapiHelper.CryptAcquireContextFlags.CRYPT_SILENT, ref safeProvHandle);
                if (hr != CapiHelper.S_OK)
                {
                    throw new CryptographicException(SR.Format(SR.Cryptography_CSP_NotFound, "Error"));
                }
                string uniqueContainerName = (string)CapiHelper.GetProviderParameter(safeProvHandle, _parameters.KeyNumber, Constants.CLR_UNIQUE_CONTAINER);
                safeProvHandle.Dispose();
                return uniqueContainerName;
            }
        }
    }
}

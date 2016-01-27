// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Security.Cryptography {
    using System.Security.AccessControl;
    using System.Security.Permissions;

    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public enum KeyNumber {
        Exchange  = 1,
        Signature = 2
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed class CspKeyContainerInfo {
        private CspParameters m_parameters;
        private bool m_randomKeyContainer;

        private CspKeyContainerInfo () {}
        [System.Security.SecurityCritical]  // auto-generated
        internal CspKeyContainerInfo (CspParameters parameters, bool randomKeyContainer) {
            if (!CompatibilitySwitches.IsAppEarlierThanWindowsPhone8) {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(parameters, KeyContainerPermissionFlags.Open);
                kp.AccessEntries.Add(entry);
                kp.Demand();
            }

            m_parameters = new CspParameters(parameters);
            if (m_parameters.KeyNumber == -1) {
                if (m_parameters.ProviderType == Constants.PROV_RSA_FULL || m_parameters.ProviderType == Constants.PROV_RSA_AES)
                    m_parameters.KeyNumber = Constants.AT_KEYEXCHANGE;
                else if (m_parameters.ProviderType == Constants.PROV_DSS_DH)
                    m_parameters.KeyNumber = Constants.AT_SIGNATURE;
            }
            m_randomKeyContainer = randomKeyContainer;
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public CspKeyContainerInfo (CspParameters parameters) : this (parameters, false) {}

        public bool MachineKeyStore {
            get {
                return (m_parameters.Flags & CspProviderFlags.UseMachineKeyStore) == CspProviderFlags.UseMachineKeyStore ? true : false;
            }
        }

        public string ProviderName {
            get {
                return m_parameters.ProviderName;
            }
        }

        public int ProviderType {
            get {
                return m_parameters.ProviderType;
            }
        }

        public string KeyContainerName {
            get {
                return m_parameters.KeyContainerName;
            }
        }

        public string UniqueKeyContainerName {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                int hr = Utils._OpenCSP(m_parameters, Constants.CRYPT_SILENT, ref safeProvHandle);
                if (hr != Constants.S_OK)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_NotFound"));

                string uniqueContainerName = (string) Utils._GetProviderParameter(safeProvHandle, m_parameters.KeyNumber, Constants.CLR_UNIQUE_CONTAINER);
                safeProvHandle.Dispose();
                return uniqueContainerName;
            }
        }

        public KeyNumber KeyNumber {
            get {
                return (KeyNumber) m_parameters.KeyNumber;
            }
        }

        public bool Exportable {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                // Assume hardware keys are not exportable.
                if (this.HardwareDevice)
                    return false;

                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                int hr = Utils._OpenCSP(m_parameters, Constants.CRYPT_SILENT, ref safeProvHandle);
                if (hr != Constants.S_OK)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_NotFound"));

                byte[] isExportable = (byte[]) Utils._GetProviderParameter(safeProvHandle, m_parameters.KeyNumber, Constants.CLR_EXPORTABLE);
                safeProvHandle.Dispose();
                return (isExportable[0] == 1);
            }
        }

        public bool HardwareDevice {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                CspParameters parameters = new CspParameters(m_parameters);
                parameters.KeyContainerName = null;
                parameters.Flags = (parameters.Flags & CspProviderFlags.UseMachineKeyStore) != 0 ? CspProviderFlags.UseMachineKeyStore : 0;
                uint flags = Constants.CRYPT_VERIFYCONTEXT;
                int hr = Utils._OpenCSP(parameters, flags, ref safeProvHandle);
                if (hr != Constants.S_OK)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_NotFound"));

                byte[] isHardwareDevice = (byte[]) Utils._GetProviderParameter(safeProvHandle, parameters.KeyNumber, Constants.CLR_HARDWARE);
                safeProvHandle.Dispose();
                return (isHardwareDevice[0] == 1);
            }
        }

        public bool Removable {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                CspParameters parameters = new CspParameters(m_parameters);
                parameters.KeyContainerName = null;
                parameters.Flags = (parameters.Flags & CspProviderFlags.UseMachineKeyStore) != 0 ? CspProviderFlags.UseMachineKeyStore : 0;
                uint flags = Constants.CRYPT_VERIFYCONTEXT;
                int hr = Utils._OpenCSP(parameters, flags, ref safeProvHandle);
                if (hr != Constants.S_OK)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_NotFound"));

                byte[] isRemovable = (byte[]) Utils._GetProviderParameter(safeProvHandle, parameters.KeyNumber, Constants.CLR_REMOVABLE);
                safeProvHandle.Dispose();
                return (isRemovable[0] == 1);
            }
        }

        public bool Accessible {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                // This method will pop-up a UI for hardware keys.
                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                int hr = Utils._OpenCSP(m_parameters, Constants.CRYPT_SILENT, ref safeProvHandle);
                if (hr != Constants.S_OK)
                    return false;

                byte[] isAccessible = (byte[]) Utils._GetProviderParameter(safeProvHandle, m_parameters.KeyNumber, Constants.CLR_ACCESSIBLE);
                safeProvHandle.Dispose();
                return (isAccessible[0] == 1);
            }
        }

        public bool Protected {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                // Assume hardware keys are protected.
                if (this.HardwareDevice == true)
                    return true;

                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                int hr = Utils._OpenCSP(m_parameters, Constants.CRYPT_SILENT, ref safeProvHandle);
                if (hr != Constants.S_OK)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_NotFound"));

                byte[] isProtected = (byte[]) Utils._GetProviderParameter(safeProvHandle, m_parameters.KeyNumber, Constants.CLR_PROTECTED);
                safeProvHandle.Dispose();
                return (isProtected[0] == 1);
            }
        }

#if FEATURE_MACL
        public CryptoKeySecurity CryptoKeySecurity {
            [System.Security.SecuritySafeCritical]  // auto-generated
            get {
                KeyContainerPermission kp = new KeyContainerPermission(KeyContainerPermissionFlags.NoFlags);
                KeyContainerPermissionAccessEntry entry = new KeyContainerPermissionAccessEntry(m_parameters,
                                                                                                KeyContainerPermissionFlags.ChangeAcl |
                                                                                                KeyContainerPermissionFlags.ViewAcl);
                kp.AccessEntries.Add(entry);
                kp.Demand();

                SafeProvHandle safeProvHandle = SafeProvHandle.InvalidHandle;
                int hr = Utils._OpenCSP(m_parameters, Constants.CRYPT_SILENT, ref safeProvHandle);
                if (hr != Constants.S_OK)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_CSP_NotFound"));

                using (safeProvHandle) {
                    return Utils.GetKeySetSecurityInfo(safeProvHandle, AccessControlSections.All);
                }
            }
        }
#endif //FEATURE_MACL

        public bool RandomlyGenerated {
            get {
                return m_randomKeyContainer;
            }
        }
    }

    [System.Runtime.InteropServices.ComVisible(true)]
    public interface ICspAsymmetricAlgorithm {
        CspKeyContainerInfo CspKeyContainerInfo { get; }
#if FEATURE_LEGACYNETCFCRYPTO
        [SecurityCritical]
#endif
        byte[] ExportCspBlob (bool includePrivateParameters);

#if FEATURE_LEGACYNETCFCRYPTO
        [SecurityCritical]
#endif
        void ImportCspBlob (byte[] rawData);
    }
}

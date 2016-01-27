// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    using Microsoft.Win32;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

#if !FEATURE_CORECLR
[System.Runtime.InteropServices.ComVisible(true)]
#endif // !FEATURE_CORECLR
    public sealed class RNGCryptoServiceProvider : RandomNumberGenerator {
#if !FEATURE_CORECLR 
        [System.Security.SecurityCritical] // auto-generated
        SafeProvHandle m_safeProvHandle;
        bool m_ownsHandle;
#else // !FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        SafeCspHandle m_cspHandle;
#endif // !FEATURE_CORECLR

        //
        // public constructors
        //

#if !FEATURE_CORECLR

        public RNGCryptoServiceProvider() : this((CspParameters) null) {}
        public RNGCryptoServiceProvider(string str) : this((CspParameters) null) {}

        public RNGCryptoServiceProvider(byte[] rgb) : this((CspParameters) null) {}

        [System.Security.SecuritySafeCritical]  // auto-generated
        public RNGCryptoServiceProvider(CspParameters cspParams) {
            if (cspParams != null) {
                m_safeProvHandle = Utils.AcquireProvHandle(cspParams);
                m_ownsHandle = true;
            }
            else {
                m_safeProvHandle = Utils.StaticProvHandle;
                m_ownsHandle = false;
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            if (disposing && m_ownsHandle) {
                m_safeProvHandle.Dispose();
            }
        }

#else // !FEATURE_CORECLR

        [System.Security.SecuritySafeCritical] // auto-generated
        public RNGCryptoServiceProvider() {
            m_cspHandle = CapiNative.AcquireCsp(null,
                                                CapiNative.ProviderNames.MicrosoftEnhanced,
                                                CapiNative.ProviderType.RsaFull,
                                                CapiNative.CryptAcquireContextFlags.VerifyContext);
        }
#endif // !FEATURE_CORECLR

        //
        // public methods
        //
#if !FEATURE_CORECLR
        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void GetBytes(byte[] data) {
            if (data == null) throw new ArgumentNullException("data");
            Contract.EndContractBlock();
            GetBytes(m_safeProvHandle, data, data.Length);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void GetNonZeroBytes(byte[] data) {
            if (data == null)
                throw new ArgumentNullException("data");
            Contract.EndContractBlock();

            GetNonZeroBytes(m_safeProvHandle, data, data.Length);
        }

#else // !FEATURE_CORECLR

        [System.Security.SecuritySafeCritical] // auto-generated
        protected override void Dispose(bool disposing) {
            try {
                if (disposing) {
                    if (m_cspHandle != null) {
                        m_cspHandle.Dispose();
                    }
                }
            }
            finally {
                base.Dispose(disposing);
            }
        }

        [System.Security.SecuritySafeCritical] // auto-generated
        public override void GetBytes(byte[] data) {
            if (data == null) {
                throw new ArgumentNullException("data");
            }
            Contract.EndContractBlock();

            if (data.Length > 0) {
                CapiNative.GenerateRandomBytes(m_cspHandle, data);
            }
        }

        [System.Security.SecuritySafeCritical] // auto-generated
        public override void GetBytes(byte[] data, int offset, int count) {
            if (data == null) throw new ArgumentNullException("data");
            if (offset < 0) throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (count < 0) throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            if (offset + count > data.Length) throw new ArgumentException(Environment.GetResourceString("Argument_InvalidOffLen"));

            if (count > 0) {
                CapiNative.GenerateRandomBytes(m_cspHandle, data, offset, count);
            }
        }
#endif // !FEATURE_CORECLR

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private static extern void GetBytes(SafeProvHandle hProv, byte[] randomBytes, int count);

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private static extern void GetNonZeroBytes(SafeProvHandle hProv, byte[] randomBytes, int count);
    }
}

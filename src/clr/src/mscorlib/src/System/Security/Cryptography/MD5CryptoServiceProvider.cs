// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
namespace System.Security.Cryptography {
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class MD5CryptoServiceProvider : MD5 {
        [System.Security.SecurityCritical] // auto-generated
        private SafeHashHandle _safeHashHandle = null;

        //
        // public constructors
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        public MD5CryptoServiceProvider() {
            if (CryptoConfig.AllowOnlyFipsAlgorithms)
                throw new InvalidOperationException(Environment.GetResourceString("Cryptography_NonCompliantFIPSAlgorithm"));
            Contract.EndContractBlock();

            // _CreateHash will check for failures and throw the appropriate exception
            _safeHashHandle = Utils.CreateHash(Utils.StaticProvHandle, Constants.CALG_MD5);
        }

        [System.Security.SecuritySafeCritical] // overrides public transparent member
        protected override void Dispose(bool disposing)
        {
            if (_safeHashHandle != null && !_safeHashHandle.IsClosed)
                _safeHashHandle.Dispose();
            base.Dispose(disposing);
        }

        //
        // public methods
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        public override void Initialize() {
            if (_safeHashHandle != null && !_safeHashHandle.IsClosed)
                _safeHashHandle.Dispose();
            
            // _CreateHash will check for failures and throw the appropriate exception
            _safeHashHandle = Utils.CreateHash(Utils.StaticProvHandle, Constants.CALG_MD5);
        }

        [System.Security.SecuritySafeCritical] // overrides protected transparent member
        protected override void HashCore(byte[] rgb, int ibStart, int cbSize)
        {
            Utils.HashData(_safeHashHandle, rgb, ibStart, cbSize);
        }

        [System.Security.SecuritySafeCritical] // overrides protected transparent member
        protected override byte[] HashFinal() {
            return Utils.EndHash(_safeHashHandle);
        }
    }
}

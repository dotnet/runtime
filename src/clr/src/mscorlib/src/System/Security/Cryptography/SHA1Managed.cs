// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This file should only be used in coreclr builds, and not on Desktop
#if !FEATURE_CORECLR
#error This version of SHA1Managed should not be built on Desktop CLR.
#endif

namespace System.Security.Cryptography
{
    using System;
    using System.Security;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    public class SHA1Managed : SHA1
    {
        [System.Security.SecurityCritical] // auto-generated
        private SafeHashHandle _safeHashHandle = null;

        //
        // public constructors
        //
      
        [System.Security.SecuritySafeCritical]  // auto-generated
        public SHA1Managed()
        {
            // _CreateHash will check for failures and throw the appropriate exception
            _safeHashHandle = Utils.CreateHash(Utils.StaticProvHandle, Constants.CALG_SHA1);
        }

        [System.Security.SecuritySafeCritical] // overrides public transparent member
        protected override void Dispose(bool disposing)
        {
            if (_safeHashHandle != null && !_safeHashHandle.IsClosed)
                _safeHashHandle.Dispose();
            // call the base class's Dispose
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
            _safeHashHandle = Utils.CreateHash(Utils.StaticProvHandle, Constants.CALG_SHA1);
        }

        [System.Security.SecuritySafeCritical] // overrides protected transparent member
        protected override void HashCore(byte[] rgb, int ibStart, int cbSize)
        {
            Utils.HashData(_safeHashHandle, rgb, ibStart, cbSize);
        }

        [System.Security.SecuritySafeCritical] // overrides protected transparent member
        protected override byte[] HashFinal()
        {
            return Utils.EndHash(_safeHashHandle);
        }

    }
}

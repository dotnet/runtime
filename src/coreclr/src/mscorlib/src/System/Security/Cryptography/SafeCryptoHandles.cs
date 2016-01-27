// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using Microsoft.Win32.SafeHandles;

    /// <summary>
    ///     Safe handle representing a mscorwks!CRYPT_PROV_CTX
    /// </summary>
    /// <remarks>
    ///     Since we need sometimes to delete the key container created in the context of the CSP, the handle
    ///     used in this class is actually a pointer to a CRYPT_PROV_CTX unmanaged structure defined in
    ///     COMCryptography.h
    /// </remarks>
    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeProvHandle : SafeHandleZeroOrMinusOneIsInvalid {

        private SafeProvHandle() : base(true) {
            SetHandle(IntPtr.Zero);
        }

        private SafeProvHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }

        internal static SafeProvHandle InvalidHandle {
            get { return new SafeProvHandle(); }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void FreeCsp(IntPtr pProviderContext);

        [System.Security.SecurityCritical]
        protected override bool ReleaseHandle()
        {
            FreeCsp(handle);
            return true;
        }
    }

    /// <summary>
    ///     Safe handle representing a mscorkws!CRYPT_KEY_CTX
    /// </summary>
    /// <summary>
    ///     Since we need to delete the key handle before the provider is released we need to actually hold a
    ///     pointer to a CRYPT_KEY_CTX unmanaged structure whose destructor decrements a refCount. Only when
    ///     the provider refCount is 0 it is deleted. This way, we raced and lost in the critical finalization
    ///     of the key handle and provider handle. This also applies to hash handles, which point to a 
    ///     CRYPT_HASH_CTX. Those structures are defined in COMCryptography.h
    /// </summary>
    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeKeyHandle : SafeHandleZeroOrMinusOneIsInvalid {

        private SafeKeyHandle() : base(true) {
            SetHandle(IntPtr.Zero);
        }

        private SafeKeyHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }

        internal static SafeKeyHandle InvalidHandle {
            get { return new SafeKeyHandle(); }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void FreeKey(IntPtr pKeyCotext);

        [System.Security.SecurityCritical]
        protected override bool ReleaseHandle()
        {
            FreeKey(handle);
            return true;
        }
    }

    /// <summary>
    ///     SafeHandle representing a mscorwks!CRYPT_HASH_CTX
    /// </summary>
    /// <remarks>
    ///     See code:System.Security.Cryptography.SafeKeyHandle for information about the release process
    ///     for a CRYPT_HASH_CTX.
    /// </remarks>
    [System.Security.SecurityCritical]  // auto-generated
    internal sealed class SafeHashHandle : SafeHandleZeroOrMinusOneIsInvalid {

        private SafeHashHandle() : base(true) {
            SetHandle(IntPtr.Zero);
        }

        private SafeHashHandle(IntPtr handle) : base (true) {
            SetHandle(handle);
        }

        internal static SafeHashHandle InvalidHandle {
            get { return new SafeHashHandle(); }
        }

        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [SuppressUnmanagedCodeSecurity]
        private static extern void FreeHash(IntPtr pHashContext);

        [System.Security.SecurityCritical]
        protected override bool ReleaseHandle()
        {
            FreeHash(handle);
            return true;
        }
    }
}


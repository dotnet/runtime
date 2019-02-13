// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Safe handle representing a HCRYPTKEY 
    /// </summary>
    /// <summary>
    ///     Since we need to delete the key handle before the provider is released we need to actually hold a
    ///     pointer to a CRYPT_KEY_CTX unmanaged structure whose destructor decrements a refCount. Only when
    ///     the provider refCount is 0 it is deleted. This way, we loose a race in the critical finalization
    ///     of the key handle and provider handle. This also applies to hash handles, which point to a 
    ///     CRYPT_HASH_CTX. Those structures are defined in COMCryptography.h
    /// </summary>
    internal sealed class SafeKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private int _keySpec;
        private bool _fPublicOnly;
        private SafeProvHandle _parent;

        private SafeKeyHandle() : base(true)
        {
            SetHandle(IntPtr.Zero);
            _keySpec = 0;
            _fPublicOnly = false;
        }

        internal int KeySpec
        {
            get
            {
                return _keySpec;
            }
            set
            {
                _keySpec = value;
            }
        }

        internal bool PublicOnly
        {
            get
            {
                return _fPublicOnly;
            }
            set
            {
                _fPublicOnly = value;
            }
        }

        internal void SetParent(SafeProvHandle parent)
        {
            if (IsInvalid || IsClosed)
            {
                return;
            }

            Debug.Assert(_parent == null);
            Debug.Assert(!parent.IsClosed);
            Debug.Assert(!parent.IsInvalid);

            _parent = parent;

            bool ignored = false;
            _parent.DangerousAddRef(ref ignored);
        }

        internal static SafeKeyHandle InvalidHandle
        {
            get { return SafeHandleCache<SafeKeyHandle>.GetInvalidHandle(() => new SafeKeyHandle()); }
        }

        protected override void Dispose(bool disposing)
        {
            if (!SafeHandleCache<SafeKeyHandle>.IsCachedInvalidHandle(this))
            {
                base.Dispose(disposing);
            }
        }

        protected override bool ReleaseHandle()
        {
            bool successfullyFreed = Interop.Advapi32.CryptDestroyKey(handle);
            Debug.Assert(successfullyFreed);

            SafeProvHandle parent = _parent;
            _parent = null;
            parent?.DangerousRelease();

            return successfullyFreed;
        }
    }
}

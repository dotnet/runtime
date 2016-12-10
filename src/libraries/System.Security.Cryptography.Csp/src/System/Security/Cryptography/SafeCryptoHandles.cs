// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Safehandle representing HCRYPTPROV
    /// </summary>
    internal sealed class SafeProvHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private string _containerName;
        private string _providerName;
        private int _type;
        private uint _flags;
        private bool _fPersistKeyInCsp;

        private SafeProvHandle() : base(true)
        {
            SetHandle(IntPtr.Zero);
            _containerName = null;
            _providerName = null;
            _type = 0;
            _flags = 0;
            _fPersistKeyInCsp = true;
        }

        internal string ContainerName
        {
            get
            {
                return _containerName;
            }
            set
            {
                _containerName = value;
            }
        }

        internal string ProviderName
        {
            get
            {
                return _providerName;
            }
            set
            {
                _providerName = value;
            }
        }

        internal int Types
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }

        internal uint Flags
        {
            get
            {
                return _flags;
            }
            set
            {
                _flags = value;
            }
        }

        internal bool PersistKeyInCsp
        {
            get
            {
                return _fPersistKeyInCsp;
            }
            set
            {
                _fPersistKeyInCsp = value;
            }
        }

        internal static SafeProvHandle InvalidHandle
        {
            get { return SafeHandleCache<SafeProvHandle>.GetInvalidHandle(() => new SafeProvHandle()); }
        }

        protected override void Dispose(bool disposing)
        {
            if (!SafeHandleCache<SafeProvHandle>.IsCachedInvalidHandle(this))
            {
                base.Dispose(disposing);
            }
        }

        protected override bool ReleaseHandle()
        {
            // Make sure not to delete a key that we want to keep in the key container or an ephemeral key
            if (!_fPersistKeyInCsp && 0 == (_flags & (uint)CapiHelper.CryptAcquireContextFlags.CRYPT_VERIFYCONTEXT))
            {
                // Delete the key container. 

                uint flags = (_flags & (uint)CapiHelper.CryptAcquireContextFlags.CRYPT_MACHINE_KEYSET) | (uint)CapiHelper.CryptAcquireContextFlags.CRYPT_DELETEKEYSET;
                SafeProvHandle hIgnoredProv;
                bool ignoredSuccess = CapiHelper.CryptAcquireContext(out hIgnoredProv, _containerName, _providerName, _type, flags);
                hIgnoredProv.Dispose();
                // Ignoring success result code as CryptAcquireContext is being called to delete a key container rather than acquire a context.
                // If it fails, we can't do anything about it anyway as we're in a dispose method.
            }

            bool successfullyFreed = CapiHelper.CryptReleaseContext(handle, 0);
            Debug.Assert(successfullyFreed);

            SetHandle(IntPtr.Zero);
            return successfullyFreed;
        }
    }

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
            bool successfullyFreed = CapiHelper.CryptDestroyKey(handle);
            Debug.Assert(successfullyFreed);

            SafeProvHandle parent = _parent;
            _parent = null;
            parent?.DangerousRelease();

            return successfullyFreed;
        }
    }

    /// <summary>
    /// SafeHandle representing HCRYPTHASH handle
    /// </summary>
    internal sealed class SafeHashHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeProvHandle _parent;

        private SafeHashHandle() : base(true)
        {
            SetHandle(IntPtr.Zero);
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

        internal static SafeHashHandle InvalidHandle
        {
            get { return SafeHandleCache<SafeHashHandle>.GetInvalidHandle(() => new SafeHashHandle()); }
        }

        protected override void Dispose(bool disposing)
        {
            if (!SafeHandleCache<SafeHashHandle>.IsCachedInvalidHandle(this))
            {
                base.Dispose(disposing);
            }
        }

        protected override bool ReleaseHandle()
        {
            bool successfullyFreed = CapiHelper.CryptDestroyHash(handle);
            Debug.Assert(successfullyFreed);

            SafeProvHandle parent = _parent;
            _parent = null;
            parent?.DangerousRelease();

            return successfullyFreed;
        }
    }
}

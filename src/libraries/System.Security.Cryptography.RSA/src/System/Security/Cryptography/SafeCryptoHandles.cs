// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    /// <summary>
    /// This class helps validate the invalid handles. Look for example 
    /// in CapiHelper.cs VerifyValidHandle method. 
    /// </summary>
    [SecurityCritical]
    internal abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle
    {
        protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle)
            : base(IntPtr.Zero, ownsHandle)
        {
        }

        public override bool IsInvalid
        {
            [SecurityCritical]
            get { return handle == IntPtr.Zero || handle == new IntPtr(-1); }
        }
    }

    //ToDo: Remove before code review - Copied from SafeCryptoHandels.cs

    /// <summary>
    /// Safehandle representing HCRYPTPROV
    /// </summary>
    [SecurityCritical]
    internal sealed class SafeProvHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private string _containerName;
        private string _providerName;
        private int _type;
        private uint _flags;
        private bool _fPersistKeyInCsp;
        private bool _fReleaseProvider;

        private SafeProvHandle() : base(true)
        {
            SetHandle(IntPtr.Zero);
            _containerName = null;
            _providerName = null;
            _type = 0;
            _flags = 0;
            _fPersistKeyInCsp = true;
            _fReleaseProvider = true;
        }

        private SafeProvHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
            _containerName = null;
            _providerName = null;
            _type = 0;
            _flags = 0;
            _fPersistKeyInCsp = true;
            _fReleaseProvider = true;
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

        internal bool ReleaseProvider
        {
            get
            {
                return _fReleaseProvider;
            }
            set
            {
                _fReleaseProvider = value;
            }
        }

        internal static SafeProvHandle InvalidHandle
        {
            get { return new SafeProvHandle(); }
        }

        protected override bool ReleaseHandle()
        {
            SetHandle(IntPtr.Zero);
            return true;
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
    ///     CRYPT_HASH_CTX. Those strucutres are defined in COMCryptography.h
    /// </summary>
    [SecurityCritical]  // auto-generated
    internal sealed class SafeKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        //SafeProvHandle safeProvHandle;
        private int _keySpec;
        private bool _fPublicOnly;
        private SafeKeyHandle() : base(true)
        {
            SetHandle(IntPtr.Zero);
            _keySpec = 0;
            _fPublicOnly = false;
        }

        private SafeKeyHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
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

        internal static SafeKeyHandle InvalidHandle
        {
            get { return new SafeKeyHandle(); }
        }

        //[DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode)]
        //[ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        //[ResourceExposure(ResourceScope.None)]
        //[SuppressUnmanagedCodeSecurity]
        //private static extern void FreeKey(IntPtr pKeyCotext);

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            //FreeKey(handle);
            return true;
        }
    }

    /// <summary>
    /// SafeHandle representing HCRYPTHASH handle
    /// </summary>
    [SecurityCritical]
    internal sealed class SafeHashHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        private SafeHashHandle() : base(true)
        {
            SetHandle(IntPtr.Zero);
        }

        private SafeHashHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        internal static SafeHashHandle InvalidHandle
        {
            get { return new SafeHashHandle(); }
        }

        //private static extern void FreeHash(IntPtr pHashContext);

        [SecurityCritical]
        protected override bool ReleaseHandle()
        {
            //FreeHash(handle);
            return true;
        }
    }
}

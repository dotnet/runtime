// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_NewX509Stack")]
        internal static partial SafeX509StackHandle NewX509Stack();

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_RecursiveFreeX509Stack")]
        internal static partial void RecursiveFreeX509Stack(IntPtr stack);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509StackFieldCount")]
        internal static partial int GetX509StackFieldCount(SafeX509StackHandle stack);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509StackFieldCount")]
        internal static partial int GetX509StackFieldCount(SafeSharedX509StackHandle stack);

        /// <summary>
        /// Gets a pointer to a certificate within a STACK_OF(X509). This pointer will later
        /// be freed, so it should be cloned via new X509Certificate2(IntPtr)
        /// </summary>
        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509StackField")]
        internal static partial IntPtr GetX509StackField(SafeX509StackHandle stack, int loc);

        /// <summary>
        /// Gets a pointer to a certificate within a STACK_OF(X509). This pointer will later
        /// be freed, so it should be cloned via new X509Certificate2(IntPtr)
        /// </summary>
        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509StackField")]
        internal static partial IntPtr GetX509StackField(SafeSharedX509StackHandle stack, int loc);

        [GeneratedDllImport(Libraries.CryptoNative, CharSet = CharSet.Ansi)]
        private static partial int CryptoNative_X509StackAddDirectoryStore(SafeX509StackHandle stack, string storePath);

        internal static void X509StackAddDirectoryStore(SafeX509StackHandle stack, string storePath)
        {
            if (CryptoNative_X509StackAddDirectoryStore(stack, storePath) != 1)
            {
                throw CreateOpenSslCryptographicException();
            }
        }

        [GeneratedDllImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_X509StackAddMultiple(SafeX509StackHandle dest, SafeX509StackHandle src);

        internal static void X509StackAddMultiple(SafeX509StackHandle dest, SafeX509StackHandle src)
        {
            if (CryptoNative_X509StackAddMultiple(dest, src) != 1)
            {
                throw CreateOpenSslCryptographicException();
            }
        }
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeX509StackHandle : SafeHandle
    {
        public SafeX509StackHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.RecursiveFreeX509Stack(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        internal static SafeX509StackHandle InvalidHandle =>
            SafeHandleCache<SafeX509StackHandle>.GetInvalidHandle(() => new SafeX509StackHandle());

        protected override void Dispose(bool disposing)
        {
            if (!SafeHandleCache<SafeX509StackHandle>.IsCachedInvalidHandle(this))
            {
                base.Dispose(disposing);
            }
        }
    }

    /// <summary>
    /// Represents access to a STACK_OF(X509)* which is a member of a structure tracked
    /// by another SafeHandle.
    /// </summary>
    internal sealed class SafeSharedX509StackHandle : SafeInteriorHandle
    {
        internal static readonly SafeSharedX509StackHandle InvalidHandle = new SafeSharedX509StackHandle();

        public SafeSharedX509StackHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }
    }
}

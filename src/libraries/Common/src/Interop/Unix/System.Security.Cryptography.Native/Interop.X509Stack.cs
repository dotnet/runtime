// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative)]
        internal static extern SafeX509StackHandle NewX509Stack();

        [DllImport(Libraries.CryptoNative)]
        internal static extern void RecursiveFreeX509Stack(IntPtr stack);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int GetX509StackFieldCount(SafeX509StackHandle stack);

        [DllImport(Libraries.CryptoNative)]
        internal static extern int GetX509StackFieldCount(SafeSharedX509StackHandle stack);

        /// <summary>
        /// Gets a pointer to a certificate within a STACK_OF(X509). This pointer will later
        /// be freed, so it should be cloned via new X509Certificate2(IntPtr)
        /// </summary>
        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr GetX509StackField(SafeX509StackHandle stack, int loc);

        /// <summary>
        /// Gets a pointer to a certificate within a STACK_OF(X509). This pointer will later
        /// be freed, so it should be cloned via new X509Certificate2(IntPtr)
        /// </summary>
        [DllImport(Libraries.CryptoNative)]
        internal static extern IntPtr GetX509StackField(SafeSharedX509StackHandle stack, int loc);
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeX509StackHandle : SafeHandle
    {
        private SafeX509StackHandle() :
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
    }

    /// <summary>
    /// Represents access to a STACK_OF(X509)* which is a member of a structure tracked
    /// by another SafeHandle.
    /// </summary>
    internal sealed class SafeSharedX509StackHandle : SafeInteriorHandle
    {
        internal static readonly SafeSharedX509StackHandle InvalidHandle = new SafeSharedX509StackHandle();

        private SafeSharedX509StackHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }
    }
}

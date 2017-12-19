// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NameStackFieldCount")]
        internal static extern int GetX509NameStackFieldCount(SafeSharedX509NameStackHandle sk);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_PushX509NameStackField")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PushX509NameStackField(SafeX509NameStackHandle stack, SafeX509NameHandle x509_Name);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_RecursiveFreeX509NameStack")]
        internal static extern void RecursiveFreeX509NameStack(IntPtr stack);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_NewX509NameStack")]
        internal static extern SafeX509NameStackHandle NewX509NameStack();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NameStackField")]
        private static extern SafeSharedX509NameHandle GetX509NameStackField_private(SafeSharedX509NameStackHandle sk,
            int loc);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NameRawBytes")]
        private static extern int GetX509NameRawBytes(SafeSharedX509NameHandle x509Name, byte[] buf, int cBuf);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeX509Name")]
        internal static extern SafeX509NameHandle DecodeX509Name(byte[] buf, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_X509NameDestroy")]
        internal static extern void X509NameDestroy(IntPtr a);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NameEntryCount")]
        internal static extern int GetX509NameEntryCount(SafeX509NameHandle x509Name);

        internal static X500DistinguishedName LoadX500Name(SafeSharedX509NameHandle namePtr)
        {
            CheckValidOpenSslHandle(namePtr);

            byte[] buf = GetDynamicBuffer((ptr, buf1, i) => GetX509NameRawBytes(ptr, buf1, i), namePtr);
            return new X500DistinguishedName(buf);
        }

        internal static SafeSharedX509NameHandle GetX509NameStackField(SafeSharedX509NameStackHandle sk, int loc)
        {
            CheckValidOpenSslHandle(sk);

            return SafeInteriorHandle.OpenInteriorHandle(
                (handle, i) => GetX509NameStackField_private(handle, i),
                sk,
                loc);
        }
    }
}

namespace Microsoft.Win32.SafeHandles
{
    /// <summary>
    /// Represents access to a X509_NAME* which is a member of a structure tracked
    /// by another SafeHandle.
    /// </summary>
    internal sealed class SafeSharedX509NameHandle : SafeInteriorHandle
    {
        private SafeSharedX509NameHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }
    }

    /// <summary>
    /// Represents access to a STACK_OF(X509_NAME)* which is a member of a structure tracked
    /// by another SafeHandle.
    /// </summary>
    internal sealed class SafeSharedX509NameStackHandle : SafeInteriorHandle
    {
        private SafeSharedX509NameStackHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }
    }

    internal sealed class SafeX509NameStackHandle : SafeHandle
    {
        private SafeX509NameStackHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Interop.Crypto.RecursiveFreeX509NameStack(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }
    }
}


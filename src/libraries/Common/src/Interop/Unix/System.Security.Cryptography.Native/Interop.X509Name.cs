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

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NameStackField")]
        private static extern SafeSharedX509NameHandle GetX509NameStackField_private(SafeSharedX509NameStackHandle sk,
            int loc);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NameRawBytes")]
        private static extern int GetX509NameRawBytes(SafeSharedX509NameHandle x509Name, byte[] buf, int cBuf);

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
}


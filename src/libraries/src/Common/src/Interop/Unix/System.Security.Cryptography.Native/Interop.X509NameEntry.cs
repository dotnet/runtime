// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NameEntry")]
        private static extern SafeSharedX509NameEntryHandle GetX509NameEntry_private(SafeX509NameHandle x509Name, int loc);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NameEntryOid")]
        private static extern SafeSharedAsn1ObjectHandle GetX509NameEntryOid_private(SafeSharedX509NameEntryHandle nameEntry);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetX509NameEntryData")]
        private static extern SafeSharedAsn1StringHandle GetX509NameEntryData_private(SafeSharedX509NameEntryHandle nameEntry);

        internal static SafeSharedX509NameEntryHandle GetX509NameEntry(SafeX509NameHandle x509Name, int loc)
        {
            CheckValidOpenSslHandle(x509Name);

            return SafeInteriorHandle.OpenInteriorHandle(
                (nameHandle, i) => GetX509NameEntry_private(nameHandle, i),
                x509Name,
                loc);
        }

        internal static SafeSharedAsn1ObjectHandle GetX509NameEntryOid(SafeSharedX509NameEntryHandle nameEntry)
        {
            CheckValidOpenSslHandle(nameEntry);

            return SafeInteriorHandle.OpenInteriorHandle(
                handle => GetX509NameEntryOid_private(handle),
                nameEntry);
        }

        internal static SafeSharedAsn1StringHandle GetX509NameEntryData(SafeSharedX509NameEntryHandle nameEntry)
        {
            CheckValidOpenSslHandle(nameEntry);

            return SafeInteriorHandle.OpenInteriorHandle(
                handle => GetX509NameEntryData_private(handle),
                nameEntry);
        }
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeSharedX509NameEntryHandle : SafeInteriorHandle
    {
        private SafeSharedX509NameEntryHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }
    }
}

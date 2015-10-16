// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "GetX509NameEntry")]
        private static extern SafeSharedX509NameEntryHandle GetX509NameEntry_private(SafeX509NameHandle x509Name, int loc);

        [DllImport(Libraries.CryptoNative, EntryPoint = "GetX509NameEntryOid")]
        private static extern SafeSharedAsn1ObjectHandle GetX509NameEntryOid_private(SafeSharedX509NameEntryHandle nameEntry);

        [DllImport(Libraries.CryptoNative, EntryPoint = "GetX509NameEntryData")]
        private static extern SafeSharedAsn1StringHandle GetX509NameEntryData_private(SafeSharedX509NameEntryHandle nameEntry);

        internal static SafeSharedX509NameEntryHandle GetX509NameEntry(SafeX509NameHandle x509Name, int loc)
        {
            CheckValidOpenSslHandle(x509Name);

            SafeSharedX509NameEntryHandle handle = GetX509NameEntry_private(x509Name, loc);

            if (!handle.IsInvalid)
            {
                handle.SetParent(x509Name);
            }

            return handle;
        }

        internal static SafeSharedAsn1ObjectHandle GetX509NameEntryOid(SafeSharedX509NameEntryHandle nameEntry)
        {
            CheckValidOpenSslHandle(nameEntry);

            SafeSharedAsn1ObjectHandle handle = GetX509NameEntryOid_private(nameEntry);

            if (!handle.IsInvalid)
            {
                handle.SetParent(nameEntry);
            }

            return handle;
        }

        internal static SafeSharedAsn1StringHandle GetX509NameEntryData(SafeSharedX509NameEntryHandle nameEntry)
        {
            CheckValidOpenSslHandle(nameEntry);

            SafeSharedAsn1StringHandle handle = GetX509NameEntryData_private(nameEntry);

            if (!handle.IsInvalid)
            {
                handle.SetParent(nameEntry);
            }

            return handle;
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

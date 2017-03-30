// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ObjTxt2Obj", CharSet = CharSet.Ansi)]
        internal static extern SafeAsn1ObjectHandle ObjTxt2Obj(string s);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ObjObj2Txt")]
        private static extern unsafe int ObjObj2Txt(byte* buf, int buf_len, IntPtr a);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetObjectDefinitionByName", CharSet = CharSet.Ansi)]
        internal static extern IntPtr GetObjectDefinitionByName(string friendlyName);

        // Returns shared pointers, should not be tracked as a SafeHandle.
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ObjNid2Obj")]
        internal static extern IntPtr ObjNid2Obj(int nid);
        
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Asn1ObjectFree")]
        internal static extern void Asn1ObjectFree(IntPtr o);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeAsn1BitString")]
        internal static extern SafeAsn1BitStringHandle DecodeAsn1BitString(byte[] buf, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Asn1BitStringFree")]
        internal static extern void Asn1BitStringFree(IntPtr o);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DecodeAsn1OctetString")]
        internal static extern SafeAsn1OctetStringHandle DecodeAsn1OctetString(byte[] buf, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Asn1OctetStringNew")]
        internal static extern SafeAsn1OctetStringHandle Asn1OctetStringNew();

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Asn1OctetStringSet")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool Asn1OctetStringSet(SafeAsn1OctetStringHandle o, byte[] d, int len);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Asn1OctetStringFree")]
        internal static extern void Asn1OctetStringFree(IntPtr o);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_Asn1StringFree")]
        internal static extern void Asn1StringFree(IntPtr o);

        internal static string GetOidValue(SafeSharedAsn1ObjectHandle asn1Object)
        {
            Debug.Assert(asn1Object != null);

            bool added = false;
            asn1Object.DangerousAddRef(ref added);
            try
            {
                return GetOidValue(asn1Object.DangerousGetHandle());
            }
            finally
            {
                asn1Object.DangerousRelease();
            }
        }

        internal static unsafe string GetOidValue(IntPtr asn1ObjectPtr)
        {
            // OBJ_obj2txt returns the number of bytes that should have been in the answer, but it does not accept
            // a NULL buffer.  The documentation says "A buffer length of 80 should be more than enough to handle
            // any OID encountered in practice", so start with a buffer of size 80, and try again if required.
            const int StackCapacity = 80;
            byte* bufStack = stackalloc byte[StackCapacity];

            int bytesNeeded = ObjObj2Txt(bufStack, StackCapacity, asn1ObjectPtr);

            if (bytesNeeded < 0)
            {
                throw CreateOpenSslCryptographicException();
            }

            Debug.Assert(bytesNeeded != 0, "OBJ_obj2txt reported a zero-length response");

            if (bytesNeeded < StackCapacity)
            {
                return Marshal.PtrToStringAnsi((IntPtr)bufStack, bytesNeeded);
            }

            // bytesNeeded does not count the \0 which will be written on the end (based on OpenSSL 1.0.1f),
            // so make sure to leave room for it.
            int initialBytesNeeded = bytesNeeded;
            byte[] bufHeap = new byte[bytesNeeded + 1];
            fixed (byte* buf = &bufHeap[0])
            {
                bytesNeeded = ObjObj2Txt(buf, bufHeap.Length, asn1ObjectPtr);

                if (bytesNeeded < 0)
                {
                    throw CreateOpenSslCryptographicException();
                }

                Debug.Assert(
                    bytesNeeded == initialBytesNeeded,
                    "OBJ_obj2txt changed the required number of bytes for the realloc call");

                if (bytesNeeded > initialBytesNeeded)
                {
                    // OBJ_obj2txt is demanding yet more memory
                    throw new CryptographicException();
                }

                return Marshal.PtrToStringAnsi((IntPtr)buf, bytesNeeded);
            }
        }
    }
}

namespace Microsoft.Win32.SafeHandles
{
    internal class SafeSharedAsn1ObjectHandle : SafeInteriorHandle
    {
        private SafeSharedAsn1ObjectHandle() :
            base(IntPtr.Zero, ownsHandle: true)
        {
        }
    }
}

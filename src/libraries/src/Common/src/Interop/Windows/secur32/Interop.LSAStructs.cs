// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct LSA_TRANSLATED_NAME
    {
        internal int Use;
        internal UNICODE_INTPTR_STRING Name;
        internal int DomainIndex;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LSA_OBJECT_ATTRIBUTES
    {
        internal int Length;
        internal IntPtr RootDirectory;
        internal IntPtr ObjectName;
        internal int Attributes;
        internal IntPtr SecurityDescriptor;
        internal IntPtr SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LSA_TRANSLATED_SID2
    {
        internal int Use;
        internal IntPtr Sid;
        internal int DomainIndex;
        uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LSA_TRUST_INFORMATION
    {
        internal UNICODE_INTPTR_STRING Name;
        internal IntPtr Sid;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LSA_REFERENCED_DOMAIN_LIST
    {
        internal int Entries;
        internal IntPtr Domains;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct UNICODE_INTPTR_STRING
    {
        /// <remarks>
        ///     Note - this constructor extracts the raw pointer from the safe handle, so any
        ///     strings created with this version of the constructor will be unsafe to use after the buffer
        ///     has been freed.
        /// </remarks>
        [System.Security.SecurityCritical]  // auto-generated
        internal UNICODE_INTPTR_STRING(int stringBytes, SafeLocalAllocHandle buffer)
        {
            Debug.Assert(buffer == null || (stringBytes >= 0 && (ulong)stringBytes <= buffer.ByteLength),
                            "buffer == null || (stringBytes >= 0 && stringBytes <= buffer.ByteLength)");

            this.Length = (ushort)stringBytes;
            this.MaxLength = (ushort)buffer.ByteLength;

            // Marshaling with a SafePointer does not work correctly, so unfortunately we need to extract
            // the raw handle here.
            this.Buffer = buffer.DangerousGetHandle();
        }

        /// <remarks>
        ///     This constructor should be used for constructing UNICODE_STRING structures with pointers
        ///     into a block of memory managed by a SafeHandle or the GC.  It shouldn't be used to own
        ///     any memory on its own.
        /// </remarks>
        internal UNICODE_INTPTR_STRING(int stringBytes, IntPtr buffer)
        {
            Debug.Assert((stringBytes == 0 && buffer == IntPtr.Zero) || (stringBytes > 0 && stringBytes <= UInt16.MaxValue && buffer != IntPtr.Zero),
                            "(stringBytes == 0 && buffer == IntPtr.Zero) || (stringBytes > 0 && stringBytes <= UInt16.MaxValue && buffer != IntPtr.Zero)");

            this.Length = (ushort)stringBytes;
            this.MaxLength = (ushort)stringBytes;
            this.Buffer = buffer;
        }

        internal ushort Length;
        internal ushort MaxLength;
        internal IntPtr Buffer;
    }
}

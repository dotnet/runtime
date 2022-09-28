// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [LibraryImport(Interop.Libraries.Advapi32, EntryPoint = "LsaLookupNames2",  SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint LsaLookupNames2(
            SafeLsaPolicyHandle handle,
            int flags,
            int count,
            MARSHALLED_UNICODE_STRING[] names,
            out SafeLsaMemoryHandle referencedDomains,
            out SafeLsaMemoryHandle sids
        );

        [NativeMarshalling(typeof(Marshaller))]
        internal struct MARSHALLED_UNICODE_STRING
        {
            internal ushort Length;
            internal ushort MaximumLength;
            internal string Buffer;

            [CustomMarshaller(typeof(MARSHALLED_UNICODE_STRING), MarshalMode.ManagedToUnmanagedIn, typeof(Marshaller))]
            [CustomMarshaller(typeof(MARSHALLED_UNICODE_STRING), MarshalMode.ElementIn, typeof(Marshaller))]
            public static class Marshaller
            {
                public static Native ConvertToUnmanaged(MARSHALLED_UNICODE_STRING managed)
                {
                    Native n;
                    n.Length = managed.Length;
                    n.MaximumLength = managed.MaximumLength;
                    n.Buffer = Marshal.StringToCoTaskMemUni(managed.Buffer);
                    return n;
                }

                public static void Free(Native native)
                {
                    Marshal.FreeCoTaskMem(native.Buffer);
                }

                public struct Native
                {
                    internal ushort Length;
                    internal ushort MaximumLength;
                    internal IntPtr Buffer;
                }
            }
        }
    }
}

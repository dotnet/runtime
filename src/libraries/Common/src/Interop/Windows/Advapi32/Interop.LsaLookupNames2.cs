// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
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

        [NativeMarshalling(typeof(Native))]
        internal struct MARSHALLED_UNICODE_STRING
        {
            internal ushort Length;
            internal ushort MaximumLength;
            internal string Buffer;

            [CustomTypeMarshaller(typeof(MARSHALLED_UNICODE_STRING), Direction = CustomTypeMarshallerDirection.In, Features = CustomTypeMarshallerFeatures.UnmanagedResources)]
            public struct Native
            {
                internal ushort Length;
                internal ushort MaximumLength;
                internal IntPtr Buffer;

                public Native(MARSHALLED_UNICODE_STRING managed)
                {
                    Length = managed.Length;
                    MaximumLength = managed.MaximumLength;
                    Buffer = Marshal.StringToCoTaskMemUni(managed.Buffer);
                }

                public void FreeNative() => Marshal.FreeCoTaskMem(Buffer);
            }
        }
    }
}

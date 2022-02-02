// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
#pragma warning disable DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time
        [DllImport(Interop.Libraries.Advapi32, EntryPoint = "LsaLookupNames2", CharSet = CharSet.Unicode, SetLastError = true)]
        // TODO: [DllImportGenerator] Switch to use GeneratedDllImport once we support non-blittable structs.
        internal static extern uint LsaLookupNames2(
            SafeLsaPolicyHandle handle,
            int flags,
            int count,
            MARSHALLED_UNICODE_STRING[] names,
            out SafeLsaMemoryHandle referencedDomains,
            out SafeLsaMemoryHandle sids
        );
#pragma warning restore DLLIMPORTGENANALYZER015 // Use 'GeneratedDllImportAttribute' instead of 'DllImportAttribute' to generate P/Invoke marshalling code at compile time

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MARSHALLED_UNICODE_STRING
        {
            internal ushort Length;
            internal ushort MaximumLength;
            [MarshalAs(UnmanagedType.LPWStr)]
            internal string Buffer;
        }
    }
}

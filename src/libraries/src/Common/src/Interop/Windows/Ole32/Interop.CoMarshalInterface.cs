// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        [DllImport(Libraries.Ole32, PreserveSig = false)]
        internal static extern void CoMarshalInterface(
            IStream pStm,                                    // Pointer to the stream used for marshaling
            [MarshalAs(UnmanagedType.LPStruct)] Guid riid,   // Reference to the identifier of the 
            IntPtr pUnk,                                     // Pointer to the interface to be marshaled
            uint dwDestContext,                              // Destination process
            IntPtr pvDestContext,                            // Reserved for future use
            uint mshlflags                                   // Reason for marshaling
            );
    }
}

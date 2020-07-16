// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.RoBuffer, CallingConvention = CallingConvention.StdCall, PreserveSig = true)]
        internal static extern int RoGetBufferMarshaler(out IMarshal bufferMarshalerPtr);
    }
}

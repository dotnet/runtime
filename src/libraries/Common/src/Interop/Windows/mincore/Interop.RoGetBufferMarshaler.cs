// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        internal static extern Int32 RoGetBufferMarshaler(out IMarshal bufferMarshalerPtr);
    }
}
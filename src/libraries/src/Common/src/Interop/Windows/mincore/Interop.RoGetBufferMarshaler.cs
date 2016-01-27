// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

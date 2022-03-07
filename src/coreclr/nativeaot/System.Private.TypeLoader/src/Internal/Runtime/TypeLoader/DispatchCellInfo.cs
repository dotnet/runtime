// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime;
using Internal.Runtime.Augments;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Internal.TypeSystem;
using Internal.NativeFormat;

namespace Internal.Runtime.TypeLoader
{
#if SUPPORTS_NATIVE_METADATA_TYPE_LOADING
    internal enum DispatchCellType
    {
        InterfaceAndSlot = 0x0,
        MetadataToken = 0x1,
        VTableOffset = 0x2,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct DispatchCellInfo
    {
        public DispatchCellType CellType;
        public IntPtr InterfaceType;
        public ushort InterfaceSlot;
        public byte HasCache;
        public uint MetadataToken;
        public uint VTableOffset;
    }
#endif
}

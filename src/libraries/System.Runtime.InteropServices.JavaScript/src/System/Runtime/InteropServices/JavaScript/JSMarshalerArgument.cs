// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices.JavaScript
{
    /// <summary>
    /// Contains the storage and type information for an argument or return value on the native stack.
    /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
    /// </summary>
    [SupportedOSPlatform("browser")]
    [CLSCompliant(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public partial struct JSMarshalerArgument
    {
        internal JSMarshalerArgumentImpl slot;

        [StructLayout(LayoutKind.Explicit, Pack = 16, Size = 16)]
        internal struct JSMarshalerArgumentImpl
        {
            [FieldOffset(0)]
            internal bool BooleanValue;
            [FieldOffset(0)]
            internal byte ByteValue;
            [FieldOffset(0)]
            internal char CharValue;
            [FieldOffset(0)]
            internal short Int16Value;
            [FieldOffset(0)]
            internal int Int32Value;
            [FieldOffset(0)]
            internal long Int64Value;// must be aligned to 8 because of HEAPI64 alignment
            [FieldOffset(0)]
            internal float SingleValue;
            [FieldOffset(0)]
            internal double DoubleValue;// must be aligned to 8 because of Module.HEAPF64 view alignment
            [FieldOffset(0)]
            internal IntPtr IntPtrValue;

            [FieldOffset(4)]
            internal IntPtr JSHandle;
            [FieldOffset(4)]
            internal IntPtr GCHandle;
            [FieldOffset(4)]
            internal MarshalerType ElementType;

            [FieldOffset(8)]
            internal int Length;

            /// <summary>
            /// Discriminator
            /// </summary>
            [FieldOffset(12)]
            internal MarshalerType Type;
        }

        /// <summary>
        /// This API supports JSImport infrastructure and is not intended to be used directly from your code.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Initialize()
        {
            slot.Type = MarshalerType.None;
        }
    }
}

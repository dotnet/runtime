// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System
{
    //
    // Some of the Number code ported from CoreRT used internal Decimal methods that did in-place modifications on Decimal. So as to not
    // lose the original perf, we'll do the same here by using Unsafe ref casts to project Decimal's true layout onto the Decimal type.
    // This looks really dangerous but the fact is that Decimal is simply the OleAut DECIMAL structure and given that it's always been possible
    // to pass it to OleAut apis via blittable interop, it's not going to change.
    //

    [StructLayout(LayoutKind.Sequential)]
    internal struct MutableDecimal
    {
        // Do not reorder these: must match DECIMAL's layout exactly.
        public uint Flags;
        public uint High;
        public uint Low;
        public uint Mid;

        public bool IsNegative
        {
            get { return (Flags & SignMask) != 0; }
            set { Flags = (Flags & ~SignMask) | (value ? SignMask : 0); }
        }

        public int Scale
        {
            get { return (byte)(Flags >> ScaleShift); }
            set { Flags = (Flags & ~ScaleMask) | ((uint)value << ScaleShift); }
        }

        // Sign mask for the flags field. A value of zero in this bit indicates a
        // positive Decimal value, and a value of one in this bit indicates a
        // negative Decimal value.
        // 
        // Look at OleAut's DECIMAL_NEG constant to check for negative values
        // in native code.
        private const uint SignMask = 0x80000000;

        // Scale mask for the flags field. This byte in the flags field contains
        // the power of 10 to divide the Decimal value by. The scale byte must
        // contain a value between 0 and 28 inclusive.
        private const uint ScaleMask = 0x00FF0000;

        // Number of bits scale is shifted by.
        private const int ScaleShift = 16;
    }
}

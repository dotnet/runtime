// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Numerics
{
    /// <summary>
    /// A structure describing the layout of an SSE2-sized register.
    /// Contains overlapping fields representing the set of valid numeric types.
    /// Allows the generic Vector'T struct to contain an explicit field layout.
    /// </summary>
    [StructLayout(LayoutKind.Explicit)]
    internal struct Register
    {
        #region Internal Storage Fields
        // Internal System.Byte Fields
        [FieldOffset(0)]
        internal byte byte_0;
        [FieldOffset(1)]
        internal byte byte_1;
        [FieldOffset(2)]
        internal byte byte_2;
        [FieldOffset(3)]
        internal byte byte_3;
        [FieldOffset(4)]
        internal byte byte_4;
        [FieldOffset(5)]
        internal byte byte_5;
        [FieldOffset(6)]
        internal byte byte_6;
        [FieldOffset(7)]
        internal byte byte_7;
        [FieldOffset(8)]
        internal byte byte_8;
        [FieldOffset(9)]
        internal byte byte_9;
        [FieldOffset(10)]
        internal byte byte_10;
        [FieldOffset(11)]
        internal byte byte_11;
        [FieldOffset(12)]
        internal byte byte_12;
        [FieldOffset(13)]
        internal byte byte_13;
        [FieldOffset(14)]
        internal byte byte_14;
        [FieldOffset(15)]
        internal byte byte_15;

        // Internal System.SByte Fields
        [FieldOffset(0)]
        internal sbyte sbyte_0;
        [FieldOffset(1)]
        internal sbyte sbyte_1;
        [FieldOffset(2)]
        internal sbyte sbyte_2;
        [FieldOffset(3)]
        internal sbyte sbyte_3;
        [FieldOffset(4)]
        internal sbyte sbyte_4;
        [FieldOffset(5)]
        internal sbyte sbyte_5;
        [FieldOffset(6)]
        internal sbyte sbyte_6;
        [FieldOffset(7)]
        internal sbyte sbyte_7;
        [FieldOffset(8)]
        internal sbyte sbyte_8;
        [FieldOffset(9)]
        internal sbyte sbyte_9;
        [FieldOffset(10)]
        internal sbyte sbyte_10;
        [FieldOffset(11)]
        internal sbyte sbyte_11;
        [FieldOffset(12)]
        internal sbyte sbyte_12;
        [FieldOffset(13)]
        internal sbyte sbyte_13;
        [FieldOffset(14)]
        internal sbyte sbyte_14;
        [FieldOffset(15)]
        internal sbyte sbyte_15;

        // Internal System.UInt16 Fields
        [FieldOffset(0)]
        internal ushort uint16_0;
        [FieldOffset(2)]
        internal ushort uint16_1;
        [FieldOffset(4)]
        internal ushort uint16_2;
        [FieldOffset(6)]
        internal ushort uint16_3;
        [FieldOffset(8)]
        internal ushort uint16_4;
        [FieldOffset(10)]
        internal ushort uint16_5;
        [FieldOffset(12)]
        internal ushort uint16_6;
        [FieldOffset(14)]
        internal ushort uint16_7;

        // Internal System.Int16 Fields
        [FieldOffset(0)]
        internal short int16_0;
        [FieldOffset(2)]
        internal short int16_1;
        [FieldOffset(4)]
        internal short int16_2;
        [FieldOffset(6)]
        internal short int16_3;
        [FieldOffset(8)]
        internal short int16_4;
        [FieldOffset(10)]
        internal short int16_5;
        [FieldOffset(12)]
        internal short int16_6;
        [FieldOffset(14)]
        internal short int16_7;

        // Internal System.UInt32 Fields
        [FieldOffset(0)]
        internal uint uint32_0;
        [FieldOffset(4)]
        internal uint uint32_1;
        [FieldOffset(8)]
        internal uint uint32_2;
        [FieldOffset(12)]
        internal uint uint32_3;

        // Internal System.Int32 Fields
        [FieldOffset(0)]
        internal int int32_0;
        [FieldOffset(4)]
        internal int int32_1;
        [FieldOffset(8)]
        internal int int32_2;
        [FieldOffset(12)]
        internal int int32_3;

        // Internal System.UInt64 Fields
        [FieldOffset(0)]
        internal ulong uint64_0;
        [FieldOffset(8)]
        internal ulong uint64_1;

        // Internal System.Int64 Fields
        [FieldOffset(0)]
        internal long int64_0;
        [FieldOffset(8)]
        internal long int64_1;

        // Internal System.Single Fields
        [FieldOffset(0)]
        internal float single_0;
        [FieldOffset(4)]
        internal float single_1;
        [FieldOffset(8)]
        internal float single_2;
        [FieldOffset(12)]
        internal float single_3;

        // Internal System.Double Fields
        [FieldOffset(0)]
        internal double double_0;
        [FieldOffset(8)]
        internal double double_1;

        #endregion Internal Storage Fields
    }
}

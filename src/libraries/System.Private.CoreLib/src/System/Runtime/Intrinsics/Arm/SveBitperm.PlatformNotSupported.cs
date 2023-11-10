// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Numerics;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM SVE hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class SveBitperm : AdvSimd
    {
        internal SveBitperm() { }

        public static new bool IsSupported { get => IsSupported; }


        ///  GatherLowerBitsFromPositionsSelectedByBitmask : Gather lower bits from positions selected by bitmask

        /// <summary>
        /// svuint8_t svbext[_u8](svuint8_t op1, svuint8_t op2)
        ///   BEXT Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svbext[_u16](svuint16_t op1, svuint16_t op2)
        ///   BEXT Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svbext[_u32](svuint32_t op1, svuint32_t op2)
        ///   BEXT Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svbext[_u64](svuint64_t op1, svuint64_t op2)
        ///   BEXT Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> GatherLowerBitsFromPositionsSelectedByBitmask(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  GroupBitsToRightOrLeftAsSelectedByBitmask : Group bits to right or left as selected by bitmask

        /// <summary>
        /// svuint8_t svbgrp[_u8](svuint8_t op1, svuint8_t op2)
        ///   BGRP Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svbgrp[_u16](svuint16_t op1, svuint16_t op2)
        ///   BGRP Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svbgrp[_u32](svuint32_t op1, svuint32_t op2)
        ///   BGRP Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svbgrp[_u64](svuint64_t op1, svuint64_t op2)
        ///   BGRP Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> GroupBitsToRightOrLeftAsSelectedByBitmask(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }


        ///  ScatterLowerBitsIntoPositionsSelectedByBitmask : Scatter lower bits into positions selected by bitmask

        /// <summary>
        /// svuint8_t svbdep[_u8](svuint8_t op1, svuint8_t op2)
        ///   BDEP Zresult.B, Zop1.B, Zop2.B
        /// </summary>
        public static unsafe Vector<byte> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<byte> left, Vector<byte> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint16_t svbdep[_u16](svuint16_t op1, svuint16_t op2)
        ///   BDEP Zresult.H, Zop1.H, Zop2.H
        /// </summary>
        public static unsafe Vector<ushort> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<ushort> left, Vector<ushort> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint32_t svbdep[_u32](svuint32_t op1, svuint32_t op2)
        ///   BDEP Zresult.S, Zop1.S, Zop2.S
        /// </summary>
        public static unsafe Vector<uint> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<uint> left, Vector<uint> right) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// svuint64_t svbdep[_u64](svuint64_t op1, svuint64_t op2)
        ///   BDEP Zresult.D, Zop1.D, Zop2.D
        /// </summary>
        public static unsafe Vector<ulong> ScatterLowerBitsIntoPositionsSelectedByBitmask(Vector<ulong> left, Vector<ulong> right) { throw new PlatformNotSupportedException(); }

    }
}


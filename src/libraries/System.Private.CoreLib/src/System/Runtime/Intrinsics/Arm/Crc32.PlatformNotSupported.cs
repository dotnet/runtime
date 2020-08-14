// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>
    /// This class provides access to the ARM Crc32 hardware instructions via intrinsics
    /// </summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Crc32 : ArmBase
    {
        internal Crc32() { }

        public static new bool IsSupported { [Intrinsic] get { return false; } }

        [Intrinsic]
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            public static new bool IsSupported { [Intrinsic] get { return false; } }

            /// <summary>
            /// uint32_t __crc32d (uint32_t a, uint64_t b)
            ///   A64: CRC32X Wd, Wn, Xm
            /// </summary>
            public static uint ComputeCrc32(uint crc, ulong data) { throw new PlatformNotSupportedException(); }

            /// <summary>
            /// uint32_t __crc32cd (uint32_t a, uint64_t b)
            ///   A64: CRC32CX Wd, Wn, Xm
            /// </summary>
            public static uint ComputeCrc32C(uint crc, ulong data) { throw new PlatformNotSupportedException(); }
        }

        /// <summary>
        /// uint32_t __crc32b (uint32_t a, uint8_t b)
        ///   A32: CRC32B Rd, Rn, Rm
        ///   A64: CRC32B Wd, Wn, Wm
        /// </summary>
        public static uint ComputeCrc32(uint crc, byte data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t __crc32h (uint32_t a, uint16_t b)
        ///   A32: CRC32H Rd, Rn, Rm
        ///   A64: CRC32H Wd, Wn, Wm
        /// </summary>
        public static uint ComputeCrc32(uint crc, ushort data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t __crc32w (uint32_t a, uint32_t b)
        ///   A32: CRC32W Rd, Rn, Rm
        ///   A64: CRC32W Wd, Wn, Wm
        /// </summary>
        public static uint ComputeCrc32(uint crc, uint data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t __crc32cb (uint32_t a, uint8_t b)
        ///   A32: CRC32CB Rd, Rn, Rm
        ///   A64: CRC32CB Wd, Wn, Wm
        /// </summary>
        public static uint ComputeCrc32C(uint crc, byte data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t __crc32ch (uint32_t a, uint16_t b)
        ///   A32: CRC32CH Rd, Rn, Rm
        ///   A64: CRC32CH Wd, Wn, Wm
        /// </summary>
        public static uint ComputeCrc32C(uint crc, ushort data) { throw new PlatformNotSupportedException(); }

        /// <summary>
        /// uint32_t __crc32cw (uint32_t a, uint32_t b)
        ///   A32: CRC32CW Rd, Rn, Rm
        ///   A64: CRC32CW Wd, Wn, Wm
        /// </summary>
        public static uint ComputeCrc32C(uint crc, uint data) { throw new PlatformNotSupportedException(); }
    }
}

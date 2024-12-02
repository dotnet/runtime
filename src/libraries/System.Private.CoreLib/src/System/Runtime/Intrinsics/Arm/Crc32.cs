// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics.Arm
{
    /// <summary>Provides access to the ARM Crc32 hardware instructions via intrinsics.</summary>
    [Intrinsic]
    [CLSCompliant(false)]
    public abstract class Crc32 : ArmBase
    {
        internal Crc32() { }

        /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
        /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
        /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
        public static new bool IsSupported { get => IsSupported; }

        /// <summary>Provides access to the ARM Crc32 hardware instructions, that are only available to 64-bit processes, via intrinsics.</summary>
        [Intrinsic]
        public new abstract class Arm64 : ArmBase.Arm64
        {
            internal Arm64() { }

            /// <summary>Gets a value that indicates whether the APIs in this class are supported.</summary>
            /// <value><see langword="true" /> if the APIs are supported; otherwise, <see langword="false" />.</value>
            /// <remarks>A value of <see langword="false" /> indicates that the APIs will throw <see cref="PlatformNotSupportedException" />.</remarks>
            public static new bool IsSupported { get => IsSupported; }

            /// <summary>
            ///   <para>uint32_t __crc32d (uint32_t a, uint64_t b)</para>
            ///   <para>  A64: CRC32X Wd, Wn, Xm</para>
            /// </summary>
            public static uint ComputeCrc32(uint crc, ulong data) => ComputeCrc32(crc, data);

            /// <summary>
            ///   <para>uint32_t __crc32cd (uint32_t a, uint64_t b)</para>
            ///   <para>  A64: CRC32CX Wd, Wn, Xm</para>
            /// </summary>
            public static uint ComputeCrc32C(uint crc, ulong data) => ComputeCrc32C(crc, data);
        }

        /// <summary>
        ///   <para>uint32_t __crc32b (uint32_t a, uint8_t b)</para>
        ///   <para>  A32: CRC32B Rd, Rn, Rm</para>
        ///   <para>  A64: CRC32B Wd, Wn, Wm</para>
        /// </summary>
        public static uint ComputeCrc32(uint crc, byte data) => ComputeCrc32(crc, data);

        /// <summary>
        ///   <para>uint32_t __crc32h (uint32_t a, uint16_t b)</para>
        ///   <para>  A32: CRC32H Rd, Rn, Rm</para>
        ///   <para>  A64: CRC32H Wd, Wn, Wm</para>
        /// </summary>
        public static uint ComputeCrc32(uint crc, ushort data) => ComputeCrc32(crc, data);

        /// <summary>
        ///   <para>uint32_t __crc32w (uint32_t a, uint32_t b)</para>
        ///   <para>  A32: CRC32W Rd, Rn, Rm</para>
        ///   <para>  A64: CRC32W Wd, Wn, Wm</para>
        /// </summary>
        public static uint ComputeCrc32(uint crc, uint data) => ComputeCrc32(crc, data);

        /// <summary>
        ///   <para>uint32_t __crc32cb (uint32_t a, uint8_t b)</para>
        ///   <para>  A32: CRC32CB Rd, Rn, Rm</para>
        ///   <para>  A64: CRC32CB Wd, Wn, Wm</para>
        /// </summary>
        public static uint ComputeCrc32C(uint crc, byte data) => ComputeCrc32C(crc, data);

        /// <summary>
        ///   <para>uint32_t __crc32ch (uint32_t a, uint16_t b)</para>
        ///   <para>  A32: CRC32CH Rd, Rn, Rm</para>
        ///   <para>  A64: CRC32CH Wd, Wn, Wm</para>
        /// </summary>
        public static uint ComputeCrc32C(uint crc, ushort data) => ComputeCrc32C(crc, data);

        /// <summary>
        ///   <para>uint32_t __crc32cw (uint32_t a, uint32_t b)</para>
        ///   <para>  A32: CRC32CW Rd, Rn, Rm</para>
        ///   <para>  A64: CRC32CW Wd, Wn, Wm</para>
        /// </summary>
        public static uint ComputeCrc32C(uint crc, uint data) => ComputeCrc32C(crc, data);
    }
}

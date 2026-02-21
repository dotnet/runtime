// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Hashing
{
    public partial class Crc64ParameterSet
    {
        /// <summary>
        ///   Gets the parameter set for the ECMA-182 variant of CRC-64.
        /// </summary>
        /// <value>
        ///   The parameter set for the ECMA-182 variant of CRC-64.
        /// </value>
        public static Crc64ParameterSet Crc64 =>
            field ??= new Ecma182ParameterSet();

        /// <summary>
        ///   Gets the parameter set used for CRC-64 in Non-Volatile Memory Express (NVMe).
        /// </summary>
        /// <value>
        ///   The parameter set used for CRC-64 in Non-Volatile Memory Express (NVMe).
        /// </value>
        public static Crc64ParameterSet Nvme =>
            field ??= Create(0xAD93D23594C93659, 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF, true);

        private sealed class Ecma182ParameterSet : Crc64ParameterSet
        {
            public Ecma182ParameterSet()
                : base(0x42F0E1EBA9EA3693, 0x0000000000000000, 0x0000000000000000, false)
            {
            }

            internal override ulong Update(ulong value, ReadOnlySpan<byte> data) => Hashing.Crc64.Update(value, data);
        }
    }
}

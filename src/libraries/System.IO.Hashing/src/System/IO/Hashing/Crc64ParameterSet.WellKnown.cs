// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Hashing
{
    public abstract partial class Crc64ParameterSet
    {
        public static Crc64ParameterSet Crc64 =>
            field ??= new Ecma182ParameterSet();

        public static Crc64ParameterSet Nvme =>
            field ??= Create(0xAD93D23594C93659, 0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF, true, true);

        private sealed class Ecma182ParameterSet : Crc64ParameterSet
        {
            public Ecma182ParameterSet()
                : base(0x42F0E1EBA9EA3693, 0x0000000000000000, 0x0000000000000000, false, false)
            {
            }

            internal override ulong Update(ulong value, ReadOnlySpan<byte> data) => Hashing.Crc64.Update(value, data);
        }
    }
}

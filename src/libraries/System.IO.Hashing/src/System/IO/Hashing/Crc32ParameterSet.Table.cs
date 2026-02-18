// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
#if NET
using System.Runtime.Intrinsics;
#endif

namespace System.IO.Hashing
{
    public partial class Crc32ParameterSet
    {
        private static uint[] GenerateLookupTable(uint polynomial, bool reflectInput)
        {
            uint[] table = new uint[256];

            if (!reflectInput)
            {
                uint crc = 0x80000000u;

                for (int i = 1; i < 256; i <<= 1)
                {
                    if ((crc & 0x80000000u) != 0)
                    {
                        crc = (crc << 1) ^ polynomial;
                    }
                    else
                    {
                        crc <<= 1;
                    }

                    for (int j = 0; j < i; j++)
                    {
                        table[i + j] = crc ^ table[j];
                    }
                }
            }
            else
            {
                for (int i = 1; i < 256; i++)
                {
                    uint r = ReverseBits((uint)i);

                    const uint LastBit = 0x80000000u;

                    for (int j = 0; j < 8; j++)
                    {
                        if ((r & LastBit) != 0)
                        {
                            r = (r << 1) ^ polynomial;
                        }
                        else
                        {
                            r <<= 1;
                        }
                    }

                    table[i] = ReverseBits(r);
                }
            }

            return table;
        }

        private sealed partial class ReflectedTableBasedCrc32 : Crc32ParameterSet
        {
            private readonly uint[] _lookupTable;

            partial void InitializeVectorized();

            internal ReflectedTableBasedCrc32(uint polynomial, uint initialValue, uint finalXorValue)
                : base(polynomial, initialValue, finalXorValue, reflectValues: true)
            {
                _lookupTable = GenerateLookupTable(polynomial, reflectInput: true);
                InitializeVectorized();
            }

            internal override uint Update(uint value, ReadOnlySpan<byte> source)
            {
                return UpdateScalar(value, source);
            }

            private uint UpdateScalar(uint value, ReadOnlySpan<byte> source)
            {
                uint[] lookupTable = _lookupTable;
                uint crc = value;

                Debug.Assert(lookupTable.Length == 256);

                foreach (byte dataByte in source)
                {
                    byte idx = (byte)(crc ^ dataByte);
                    crc = lookupTable[idx] ^ (crc >> 8);
                }

                return crc;
            }
        }

        private sealed class ForwardTableBasedCrc32 : ForwardCrc32
        {
            private readonly uint[] _lookupTable;

            internal ForwardTableBasedCrc32(uint polynomial, uint initialValue, uint finalXorValue)
                : base(polynomial, initialValue, finalXorValue)
            {
                _lookupTable = GenerateLookupTable(polynomial, reflectInput: false);
            }

            protected override uint UpdateScalar(uint value, ReadOnlySpan<byte> source)
            {
                uint[] lookupTable = _lookupTable;
                uint crc = value;

                Debug.Assert(lookupTable.Length == 256);

                foreach (byte dataByte in source)
                {
                    byte idx = (byte)((crc >> 24) ^ dataByte);
                    crc = lookupTable[idx] ^ (crc << 8);
                }

                return crc;
            }
        }
    }
}

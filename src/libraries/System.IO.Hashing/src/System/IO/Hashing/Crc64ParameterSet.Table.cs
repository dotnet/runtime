// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
#if NET
using System.Runtime.Intrinsics;
#endif

namespace System.IO.Hashing
{
    public partial class Crc64ParameterSet
    {
        private static ulong[] GenerateLookupTable(ulong polynomial, bool reflectInput)
        {
            ulong[] table = new ulong[256];

            if (!reflectInput)
            {
                ulong crc = 0x8000000000000000ul;

                for (int i = 1; i < 256; i <<= 1)
                {
                    if ((crc & 0x8000000000000000ul) != 0)
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
                    ulong r = ReverseBits((ulong)i);

                    const ulong LastBit = 0x8000000000000000ul;

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

        private sealed partial class ReflectedTableBasedCrc64 : Crc64ParameterSet
        {
            private readonly ulong[] _lookupTable;

            partial void InitializeVectorized();

            internal ReflectedTableBasedCrc64(ulong polynomial, ulong initialValue, ulong finalXorValue)
                : base(polynomial, initialValue, finalXorValue, reflectValues: true)
            {
                _lookupTable = GenerateLookupTable(polynomial, reflectInput: true);
                InitializeVectorized();
            }

            internal override ulong Update(ulong value, ReadOnlySpan<byte> data)
            {
#if NET
                if (_canVectorize && data.Length >= Vector128<byte>.Count)
                {
                    return UpdateVectorized(value, data);
                }
#endif

                return UpdateScalar(value, data);
            }

            private ulong UpdateScalar(ulong value, ReadOnlySpan<byte> data)
            {
                ulong[] lookupTable = _lookupTable;
                ulong crc = value;

                Debug.Assert(lookupTable.Length == 256);

                foreach (byte dataByte in data)
                {
                    byte idx = (byte)(crc ^ dataByte);
                    crc = lookupTable[idx] ^ (crc >> 8);
                }

                return crc;
            }
        }

        private sealed partial class ForwardTableBasedCrc64 : Crc64ParameterSet
        {
            private readonly ulong[] _lookupTable;

            partial void InitializeVectorized();

            internal ForwardTableBasedCrc64(ulong polynomial, ulong initialValue, ulong finalXorValue)
                : base(polynomial, initialValue, finalXorValue, reflectValues: false)
            {
                _lookupTable = GenerateLookupTable(polynomial, reflectInput: false);
                InitializeVectorized();
            }

            internal override ulong Update(ulong value, ReadOnlySpan<byte> data)
            {
#if NET
                if (_canVectorize && data.Length >= Vector128<byte>.Count)
                {
                    return UpdateVectorized(value, data);
                }
#endif

                return UpdateScalar(value, data);
            }

            private ulong UpdateScalar(ulong value, ReadOnlySpan<byte> data)
            {
                ulong[] lookupTable = _lookupTable;
                ulong crc = value;

                foreach (byte dataByte in data)
                {
                    byte idx = (byte)((crc >> 56) ^ dataByte);
                    crc = lookupTable[idx] ^ (crc << 8);
                }

                return crc;
            }
        }
    }
}

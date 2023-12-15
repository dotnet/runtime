// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace CodeGenTests
{
    public static class CalculateChecksumTest
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ushort CalculateChecksum(ref byte buffer, int length)
        {
            ref var current = ref buffer;
            ulong sum = 0;

            while (length >= sizeof(ulong))
            {
                length -= sizeof(ulong);

                var ulong0 = Unsafe.As<byte, ulong>(ref current);
                current = ref Unsafe.Add(ref current, sizeof(ulong));

                // Add with carry
                sum += ulong0;
                if (sum < ulong0)
                {
                    sum++;
                }
            }

            if ((length & sizeof(uint)) != 0)
            {
                var uint0 = Unsafe.As<byte, uint>(ref current);
                current = ref Unsafe.Add(ref current, sizeof(uint));

                // Add with carry
                sum += uint0;
                if (sum < uint0)
                {
                    sum++;
                }
            }

            if ((length & sizeof(ushort)) != 0)
            {
                var ushort0 = Unsafe.As<byte, ushort>(ref current);
                current = ref Unsafe.Add(ref current, sizeof(ushort));

                // Add with carry
                sum += ushort0;
                if (sum < ushort0)
                {
                    sum++;
                }
            }

            if ((length & sizeof(byte)) != 0)
            {
                var byte0 = current;

                // Add with carry
                sum += byte0;
                if (sum < byte0)
                {
                    sum++;
                }
            }

            // Fold down to 16 bits

            var uint1 = (uint)(sum >> 32);
            var uint2 = (uint)sum;

            // Add with carry
            uint1 += uint2;
            if (uint1 < uint2)
            {
                uint1++;
            }

            var ushort2 = (ushort)uint1;
            var ushort1 = (ushort)(uint1 >> 16);

            // Add with carry
            ushort1 = (ushort)(ushort1 + ushort2);
            if (ushort1 < ushort2)
            {
                ushort1++;
            }

            // Invert to get ones-complement result 
            return (ushort)~ushort1;

            // There should not be these kinds of zero extending 'mov' instructions here.
            // example - mov eax, eax

            // X64-NOT: mov [[REG0:[a-z0-9]+]], [[REG0]]
        }

        [Fact]
        public static int TestEntryPoint()
        {
            var buffer = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            var result = CalculateChecksum(ref buffer[0], buffer.Length);

            if (result != 59115)
                return 0;

            return 100;
        }
    }
}

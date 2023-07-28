// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// GitHub 20040: operand ordering bug with GT_INDEX_ADDR
// Requires minopts/tier0 to repro

namespace GitHub_20040
{
    public class Program
    {
        [Fact]
        public static int TestEntryPoint()
        {
            var array = new byte[] {0x00, 0x01};
            var reader = new BinaryTokenStreamReader(array);

            var val = reader.ReadByte();

            if (val == 0x01)
            {
                Console.WriteLine("Pass");                
                return 100;
            }
            else
            {
                Console.WriteLine($"Fail: val=0x{val:x2}, expected 0x01");
                return 0;
            }
        }
    }

    public class BinaryTokenStreamReader
    {
        private readonly byte[] currentBuffer;

        public BinaryTokenStreamReader(byte[] input)
        {
            this.currentBuffer = input;
        }

        byte[] CheckLength(out int offset)
        {
            // In the original code, this logic is more complicated.
            // It's simplified here to demonstrate the bug.
            offset = 1;
            return currentBuffer;
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
        public byte ReadByte()
        {
            int offset;
            var buff = CheckLength(out offset);
            return buff[offset];
        }
    }
}

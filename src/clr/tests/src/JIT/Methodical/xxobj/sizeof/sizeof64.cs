

using System;
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// sizeof converted to I8 and used with arithmetic operations

namespace JitTest
{
    internal struct SimpleStruct
    {
        public int m_int;
        public uint m_uint;
        public byte m_byte;
        public sbyte m_sbyte;
        public char m_char;
        public short m_short;
        public ushort m_ushort;
        public long m_long;
        public ulong m_ulong;
    }

    internal struct RefComplexStruct
    {
        public SimpleStruct ss1;
        public SimpleStruct ss2;
    }

    internal struct Test
    {
        private static unsafe int Main()
        {
            long l = (sbyte)sizeof(RefComplexStruct);
            l += sizeof(RefComplexStruct) + new RefComplexStruct().ss1.m_sbyte;
            l -= 128L - new RefComplexStruct().ss2.m_ushort - sizeof(RefComplexStruct);
            l *= sizeof(RefComplexStruct) * (new RefComplexStruct().ss1.m_uint + 1);
            l /= sizeof(RefComplexStruct) / (int)(new RefComplexStruct().ss2.m_ulong + 1);
            l = (sizeof(RefComplexStruct) ^ 64) | l;
            l = (sizeof(RefComplexStruct) ^ (~64)) & l;
            return (int)(l + 36L);
        }
    }
}

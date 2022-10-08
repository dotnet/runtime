// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Xunit;


namespace JitTest_sizeof32_sizeof_cs
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

    public struct Test
    {
        [Fact]
        public static unsafe int TestEntryPoint()
        {
            int l = (sbyte)sizeof(RefComplexStruct);
            l += sizeof(RefComplexStruct) + new RefComplexStruct().ss1.m_sbyte;
            l -= 128 - new RefComplexStruct().ss2.m_ushort - sizeof(RefComplexStruct);
            l *= sizeof(RefComplexStruct) * (int)(new RefComplexStruct().ss1.m_uint + 1);
            l /= sizeof(RefComplexStruct) / (int)(new RefComplexStruct().ss2.m_ulong + 1);
            l = (sizeof(RefComplexStruct) ^ 64) | l;
            l = (sizeof(RefComplexStruct) ^ (~64)) & l;
            return l + 36;
        }
    }
}

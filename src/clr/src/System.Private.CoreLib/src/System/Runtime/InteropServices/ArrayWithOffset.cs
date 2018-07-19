// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public struct ArrayWithOffset
    {
        public ArrayWithOffset(object array, int offset)
        {
            m_array = array;
            m_offset = offset;
            m_count = 0;
            m_count = CalculateCount();
        }

        public object GetArray() => m_array;

        public int GetOffset() => m_offset;

        public override int GetHashCode() => m_count + m_offset;

        public override bool Equals(object obj)
        {
            return obj is ArrayWithOffset && Equals((ArrayWithOffset)obj);
        }

        public bool Equals(ArrayWithOffset obj)
        {
            return obj.m_array == m_array && obj.m_offset == m_offset && obj.m_count == m_count;
        }

        public static bool operator ==(ArrayWithOffset a, ArrayWithOffset b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(ArrayWithOffset a, ArrayWithOffset b)
        {
            return !(a == b);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern int CalculateCount();

        private object m_array;
        private int m_offset;
        private int m_count;
    }
}

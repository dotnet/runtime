// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


using System;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System.Runtime.InteropServices
{
    public struct ArrayWithOffset
    {
        //private ArrayWithOffset()
        //{
        //    throw new Exception();
        //}

        public ArrayWithOffset(Object array, int offset)
        {
            m_array = array;
            m_offset = offset;
            m_count = 0;
            m_count = CalculateCount();
        }

        public Object GetArray()
        {
            return m_array;
        }

        public int GetOffset()
        {
            return m_offset;
        }

        public override int GetHashCode()
        {
            return m_count + m_offset;
        }

        public override bool Equals(Object obj)
        {
            if (obj is ArrayWithOffset)
                return Equals((ArrayWithOffset)obj);
            else
                return false;
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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private extern int CalculateCount();

        private Object m_array;
        private int m_offset;
        private int m_count;
    }
}

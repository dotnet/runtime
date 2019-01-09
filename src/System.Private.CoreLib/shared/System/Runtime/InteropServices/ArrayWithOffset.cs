// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public struct ArrayWithOffset
    {
        // From MAX_SIZE_FOR_INTEROP in mlinfo.h
        private const int MaxSizeForInterop = 0x7ffffff0;

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

#if !CORECLR // TODO: Cleanup
        private int CalculateCount()
        {
            if (m_array == null)
            {
                if (m_offset != 0)
                {
                    throw new IndexOutOfRangeException(SR.IndexOutOfRange_ArrayWithOffset);
                }

                return 0;
            }
            else
            {
                Array arrayObj = m_array as Array;
                if (arrayObj == null)
                {
                    throw new ArgumentException(SR.Argument_NotIsomorphic);
                }

                if (arrayObj.Rank != 1)
                {
                    throw new ArgumentException(SR.Argument_NotIsomorphic);
                }

                if (!arrayObj.IsBlittable())
                {
                    throw new ArgumentException(SR.Argument_NotIsomorphic);
                }

                int totalSize = checked(arrayObj.Length * arrayObj.GetElementSize());
                if (totalSize > MaxSizeForInterop)
                {
                    throw new ArgumentException(SR.Argument_StructArrayTooLarge);
                }

                if (m_offset > totalSize)
                {
                    throw new IndexOutOfRangeException(SR.IndexOutOfRange_ArrayWithOffset);
                }

                return totalSize - m_offset;
            }
        }
#endif // !CORECLR

        private object m_array;
        private int m_offset;
        private int m_count;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#if BIT64
using nuint = System.UInt64;
#else
using nuint = System.UInt32;
#endif

namespace System.Runtime.InteropServices
{
    public struct ArrayWithOffset
    {
        private object? m_array;
        private int m_offset;
        private int m_count;

        // From MAX_SIZE_FOR_INTEROP in mlinfo.h
        private const int MaxSizeForInterop = 0x7ffffff0;

        public ArrayWithOffset(object? array, int offset)
        {
            int totalSize = 0;
            if (array != null)
            {
                if (!(array is Array arrayObj) || (arrayObj.Rank != 1) || !Marshal.IsPinnable(arrayObj))
                {
                    throw new ArgumentException(SR.ArgumentException_NotIsomorphic);
                }

                nuint nativeTotalSize = (nuint)arrayObj.LongLength * (nuint)arrayObj.GetElementSize();
                if (nativeTotalSize > MaxSizeForInterop)
                {
                    throw new ArgumentException(SR.Argument_StructArrayTooLarge);
                }

                totalSize = (int)nativeTotalSize;
            }

            if ((uint)offset > (uint)totalSize)
            {
                throw new IndexOutOfRangeException(SR.IndexOutOfRange_ArrayWithOffset);
            }

            m_array = array;
            m_offset = offset;
            m_count = totalSize - offset;
        }

        public object? GetArray() => m_array;

        public int GetOffset() => m_offset;

        public override int GetHashCode() => m_count + m_offset;

        public override bool Equals(object? obj)
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
    }
}

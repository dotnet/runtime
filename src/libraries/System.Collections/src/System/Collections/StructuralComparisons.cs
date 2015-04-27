// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


// 

using System;
using System.Collections.Generic;

namespace System.Collections
{
    public static class StructuralComparisons
    {
        private static volatile IComparer s_StructuralComparer;
        private static volatile IEqualityComparer s_StructuralEqualityComparer;

        public static IComparer StructuralComparer
        {
            get
            {
                IComparer comparer = s_StructuralComparer;
                if (comparer == null)
                {
                    comparer = new StructuralComparer();
                    s_StructuralComparer = comparer;
                }
                return comparer;
            }
        }

        public static IEqualityComparer StructuralEqualityComparer
        {
            get
            {
                IEqualityComparer comparer = s_StructuralEqualityComparer;
                if (comparer == null)
                {
                    comparer = new StructuralEqualityComparer();
                    s_StructuralEqualityComparer = comparer;
                }
                return comparer;
            }
        }
    }

    internal class StructuralEqualityComparer : IEqualityComparer
    {
        public new bool Equals(Object x, Object y)
        {
            if (x != null)
            {
                IStructuralEquatable seObj = x as IStructuralEquatable;

                if (seObj != null)
                {
                    return seObj.Equals(y, this);
                }

                if (y != null)
                {
                    return x.Equals(y);
                }
                else
                {
                    return false;
                }
            }
            if (y != null) return false;
            return true;
        }

        public int GetHashCode(Object obj)
        {
            if (obj == null) return 0;

            IStructuralEquatable seObj = obj as IStructuralEquatable;

            if (seObj != null)
            {
                return seObj.GetHashCode(this);
            }

            return obj.GetHashCode();
        }
    }

    internal class StructuralComparer : IComparer
    {
        public int Compare(Object x, Object y)
        {
            if (x == null) return y == null ? 0 : -1;
            if (y == null) return 1;

            IStructuralComparable scX = x as IStructuralComparable;

            if (scX != null)
            {
                return scX.CompareTo(y, this);
            }

            return Comparer<Object>.Default.Compare(x, y);
        }
    }
}

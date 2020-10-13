// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections
{
    public static class StructuralComparisons
    {
        private static volatile IComparer? s_StructuralComparer;
        private static volatile IEqualityComparer? s_StructuralEqualityComparer;

        public static IComparer StructuralComparer
        {
            get
            {
                IComparer? comparer = s_StructuralComparer;
                if (comparer is null)
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
                IEqualityComparer? comparer = s_StructuralEqualityComparer;
                if (comparer is null)
                {
                    comparer = new StructuralEqualityComparer();
                    s_StructuralEqualityComparer = comparer;
                }
                return comparer;
            }
        }
    }

    internal sealed class StructuralEqualityComparer : IEqualityComparer
    {
        public new bool Equals(object? x, object? y)
        {
            if (x is not null)
            {
                IStructuralEquatable? seObj = x as IStructuralEquatable;

                if (seObj is not null)
                {
                    return seObj.Equals(y, this);
                }

                if (y is not null)
                {
                    return x.Equals(y);
                }
                else
                {
                    return false;
                }
            }
            if (y is not null) return false;
            return true;
        }

        public int GetHashCode(object obj)
        {
            if (obj is null) return 0;

            IStructuralEquatable? seObj = obj as IStructuralEquatable;

            if (seObj is not null)
            {
                return seObj.GetHashCode(this);
            }

            return obj.GetHashCode();
        }
    }

    internal sealed class StructuralComparer : IComparer
    {
        public int Compare(object? x, object? y)
        {
            if (x is null) return y is null ? 0 : -1;
            if (y is null) return 1;

            IStructuralComparable? scX = x as IStructuralComparable;

            if (scX is not null)
            {
                return scX.CompareTo(y, this);
            }

            return Comparer<object>.Default.Compare(x, y);
        }
    }
}

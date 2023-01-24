// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Collections
{
    public static class StructuralComparisons
    {
        private static volatile IComparer? s_StructuralComparer;
        private static volatile IEqualityComparer? s_StructuralEqualityComparer;

        public static IComparer StructuralComparer => s_StructuralComparer ??= new StructuralComparer();

        public static IEqualityComparer StructuralEqualityComparer => s_StructuralEqualityComparer ??= new StructuralEqualityComparer();
    }

    internal sealed class StructuralEqualityComparer : IEqualityComparer
    {
        public new bool Equals(object? x, object? y)
        {
            if (x != null)
            {
                IStructuralEquatable? seObj = x as IStructuralEquatable;

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

        public int GetHashCode(object obj)
        {
            if (obj == null) return 0;

            IStructuralEquatable? seObj = obj as IStructuralEquatable;

            if (seObj != null)
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
            if (x == null) return y == null ? 0 : -1;
            if (y == null) return 1;

            IStructuralComparable? scX = x as IStructuralComparable;

            if (scX != null)
            {
                return scX.CompareTo(y, this);
            }

            return Comparer<object>.Default.Compare(x, y);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Data
{
    /// <summary>
    /// This class implements IEqualityComparer using value based semantics
    /// when comparing DataRows.
    /// </summary>
    public static class DataRowComparer
    {
        /// <summary>
        /// Gets the singleton instance of the data row comparer.
        /// </summary>
        public static DataRowComparer<DataRow> Default { get { return DataRowComparer<DataRow>.Default; } }

        internal static bool AreEqual(object? a, object? b)
        {
            if (ReferenceEquals(a, b))
            {
                // same reference or (null, null) or (DBNull.Value, DBNull.Value)
                return true;
            }
            if (a is null || ReferenceEquals(a, DBNull.Value) ||
                b is null || ReferenceEquals(b, DBNull.Value))
            {
                // (null, non-null) or (null, DBNull.Value) or vice versa
                return false;
            }
            return (a.Equals(b) || (a.GetType().IsArray && CompareArray((Array)a, b as Array)));
        }

        private static bool AreElementEqual(object? a, object? b)
        {
            if (ReferenceEquals(a, b))
            {
                // same reference or (null, null) or (DBNull.Value, DBNull.Value)
                return true;
            }
            if (a is null || ReferenceEquals(a, DBNull.Value) ||
                b is null || ReferenceEquals(b, DBNull.Value))
            {
                // (null, non-null) or (null, DBNull.Value) or vice versa
                return false;
            }
            return a.Equals(b);
        }

        private static bool CompareArray(Array a, Array? b)
        {
            if ((null == b) ||
                (1 != a.Rank) ||
                (1 != b.Rank) ||
                (a.Length != b.Length))
            {
                // automatically consider array's with Rank>1 not-equal
                return false;
            }

            int index1 = a.GetLowerBound(0);
            int index2 = b.GetLowerBound(0);
            if (a.GetType() == b.GetType() && (0 == index1) && (0 == index2))
            {
                switch (Type.GetTypeCode(a.GetType().GetElementType()))
                {
                    case TypeCode.Byte:
                        return CompareEquatableArray((byte[])a, (byte[])b);
                    case TypeCode.Int16:
                        return CompareEquatableArray((short[])a, (short[])b);
                    case TypeCode.Int32:
                        return CompareEquatableArray((int[])a, (int[])b);
                    case TypeCode.Int64:
                        return CompareEquatableArray((long[])a, (long[])b);
                    case TypeCode.String:
                        return CompareEquatableArray((string[])a, (string[])b);
                }
            }

            // Compare every element. But don't recurse if we have Array of array.
            int length = index1 + a.Length;
            for (; index1 < length; ++index1, ++index2)
            {
                if (!AreElementEqual(a.GetValue(index1), b.GetValue(index2)))
                {
                    return false;
                }
            }
            return true;
        }

        private static bool CompareEquatableArray<TElem>(TElem[] a, TElem[] b) where TElem : IEquatable<TElem>
        {
            Debug.Assert(!ReferenceEquals(a, b));
            Debug.Assert(a != null);
            Debug.Assert(b != null);
            Debug.Assert(a.Length == b.Length);

            for (int i = 0; i < a.Length; ++i)
            {
                if (!a[i]?.Equals(b[i]) ?? b[i] != null)
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// This class implements IEqualityComparer using value based semantics
    /// when comparing DataRows.
    /// </summary>
    public sealed class DataRowComparer<TRow> : IEqualityComparer<TRow> where TRow : DataRow
    {
        /// <summary>
        /// Private constructor to prevent initialization outside of Default singleton instance.
        /// </summary>
        private DataRowComparer() { }

        private static readonly DataRowComparer<TRow> s_instance = new DataRowComparer<TRow>();

        /// <summary>
        /// Gets the singleton instance of the data row comparer.
        /// </summary>
        public static DataRowComparer<TRow> Default { get { return s_instance; } }

        /// <summary>
        /// This method compares to DataRows by doing a column by column value based
        /// comparison.
        /// </summary>
        /// <param name="leftRow">The first input DataRow</param>
        /// <param name="rightRow">The second input DataRow</param>
        /// <returns>True if rows are equal, false if not.</returns>
        public bool Equals(TRow? leftRow, TRow? rightRow)
        {
            if (ReferenceEquals(leftRow, rightRow))
            {
                return true;
            }

            if (leftRow is null ||
                rightRow is null)
            {
                return false;
            }

            if (leftRow.RowState == DataRowState.Deleted || rightRow.RowState == DataRowState.Deleted)
            {
                throw DataSetUtil.InvalidOperation(SR.DataSetLinq_CannotCompareDeletedRow);
            }

            int count = leftRow.Table.Columns.Count;
            if (count != rightRow.Table.Columns.Count)
            {
                return false;
            }

            for (int i = 0; i < count; ++i)
            {
                if (!DataRowComparer.AreEqual(leftRow[i], rightRow[i]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// This method retrieves a hash code for the source row.
        /// </summary>
        /// <param name="row">The source DataRow</param>
        /// <returns>HashCode for row based on values in the row.</returns>
        public int GetHashCode(TRow row)
        {
            DataSetUtil.CheckArgumentNull(row, nameof(row));

            if (row.RowState == DataRowState.Deleted)
            {
                throw DataSetUtil.InvalidOperation(SR.DataSetLinq_CannotCompareDeletedRow);
            }

            int hash = 0;
            Debug.Assert(row.Table != null);
            if (row.Table.Columns.Count > 0)
            {
                // if the row has at least one column, then use the first column value
                object value = row[0];

                Type valueType = value.GetType();
                if (valueType.IsArray)
                {
                    Array array = (Array)value;

                    if (array.Rank > 1)
                    {
                        hash = value.GetHashCode();
                    }
                    else if (array.Length > 0)
                    {
                        hash = array.GetValue(array.GetLowerBound(0))!.GetHashCode();
                    }
                }
                else
                {
                    // have to unbox value types.
                    if (value is ValueType vt)
                    {
                        hash = vt.GetHashCode();
                    }
                    else
                    {
                        hash = value.GetHashCode();
                    }
                }
            }

            // if table has no columns, the hash code is 0
            return hash;
        }
    }
}

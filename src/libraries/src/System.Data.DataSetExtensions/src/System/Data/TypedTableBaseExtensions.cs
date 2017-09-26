// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Data
{
    /// <summary>
    /// This static class defines the extension methods that add LINQ operator functionality
    /// within IEnumerableDT and IOrderedEnumerableDT.
    /// </summary>
    public static class TypedTableBaseExtensions
    {
        /// <summary>
        /// LINQ's Where operator for generic EnumerableRowCollection.
        /// </summary>
        public static EnumerableRowCollection<TRow> Where<TRow>(this TypedTableBase<TRow> source, Func<TRow, bool> predicate) where TRow : DataRow
        {
            DataSetUtil.CheckArgumentNull(source, nameof(source));
            EnumerableRowCollection<TRow> erc = new EnumerableRowCollection<TRow>(source);
            return erc.Where(predicate);
        }

        /// <summary>
        /// LINQ's OrderBy operator for generic EnumerableRowCollection.
        /// </summary>
        public static OrderedEnumerableRowCollection<TRow> OrderBy<TRow, TKey>(this TypedTableBase<TRow> source, Func<TRow, TKey> keySelector) where TRow : DataRow
        {
            DataSetUtil.CheckArgumentNull(source, nameof(source));
            EnumerableRowCollection<TRow> erc = new EnumerableRowCollection<TRow>(source);
            return erc.OrderBy(keySelector);
        }

        /// <summary>
        /// LINQ's OrderBy operator for generic EnumerableRowCollection.
        /// </summary>
        public static OrderedEnumerableRowCollection<TRow> OrderBy<TRow, TKey>(
            this TypedTableBase<TRow> source,
            Func<TRow, TKey> keySelector,
            IComparer<TKey> comparer) where TRow : DataRow
        {
            DataSetUtil.CheckArgumentNull(source, nameof(source));
            EnumerableRowCollection<TRow> erc = new EnumerableRowCollection<TRow>(source);
            return erc.OrderBy(keySelector, comparer);
        }

        /// <summary>
        /// LINQ's OrderByDescending operator for generic EnumerableRowCollection.
        /// </summary>
        public static OrderedEnumerableRowCollection<TRow> OrderByDescending<TRow, TKey>(this TypedTableBase<TRow> source, Func<TRow, TKey> keySelector) where TRow : DataRow
        {
            DataSetUtil.CheckArgumentNull(source, nameof(source));
            EnumerableRowCollection<TRow> erc = new EnumerableRowCollection<TRow>(source);
            return erc.OrderByDescending(keySelector);
        }

        /// <summary>
        /// LINQ's OrderByDescending operator for generic EnumerableRowCollection.
        /// </summary>
        public static OrderedEnumerableRowCollection<TRow> OrderByDescending<TRow, TKey>(
            this TypedTableBase<TRow> source,
            Func<TRow, TKey> keySelector,
            IComparer<TKey> comparer) where TRow : DataRow
        {
            DataSetUtil.CheckArgumentNull(source, nameof(source));
            EnumerableRowCollection<TRow> erc = new EnumerableRowCollection<TRow>(source);
            return erc.OrderByDescending(keySelector, comparer);
        }


        /// <summary>
        /// Executes a Select (Projection) on EnumerableDataTable. If the selector returns a different
        /// type than the type of rows, then AsLinqDataView is disabled, and the returning EnumerableDataTable
        /// represents an enumerable over the LINQ Query.
        /// </summary>
        public static EnumerableRowCollection<S> Select<TRow, S>(this TypedTableBase<TRow> source, Func<TRow, S> selector) where TRow : DataRow
        {
            DataSetUtil.CheckArgumentNull(source, nameof(source));
            EnumerableRowCollection<TRow> erc = new EnumerableRowCollection<TRow>(source);
            return erc.Select(selector);
        }

        /// <summary>
        /// This method returns a IEnumerable of TRow.
        /// </summary>
        /// <param name="source">The source DataTable to make enumerable.</param>
        /// <returns>IEnumerable of datarows.</returns>
        public static EnumerableRowCollection<TRow> AsEnumerable<TRow>(this TypedTableBase<TRow> source) where TRow : DataRow
        {
            DataSetUtil.CheckArgumentNull(source, nameof(source));
            return new EnumerableRowCollection<TRow>(source as DataTable);
        }

        public static TRow ElementAtOrDefault<TRow>(this TypedTableBase<TRow> source, int index) where TRow : DataRow
        {
            if ((index >= 0) && (index < source.Rows.Count))
            {
                return (TRow)source.Rows[index];
            }
            else
            {
                return default(TRow);
            }
        }
    } 
}
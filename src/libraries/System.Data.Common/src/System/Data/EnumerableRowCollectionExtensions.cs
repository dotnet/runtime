// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace System.Data
{
    /// <summary>
    /// This static class defines the extension methods that add LINQ operator functionality
    /// within IEnumerableDT and IOrderedEnumerableDT.
    /// </summary>
    public static class EnumerableRowCollectionExtensions
    {
        /// <summary>
        /// LINQ's Where operator for generic EnumerableRowCollection.
        /// </summary>
        public static EnumerableRowCollection<TRow> Where<TRow>(this EnumerableRowCollection<TRow> source, Func<TRow, bool> predicate)
        {
            //copy constructor
            EnumerableRowCollection<TRow> edt = new EnumerableRowCollection<TRow>(source, Enumerable.Where(source, predicate), selector: null);
            edt.AddPredicate(predicate);
            return edt;
        }

        /// <summary>
        /// LINQ's OrderBy operator for generic EnumerableRowCollection.
        /// </summary>
        public static OrderedEnumerableRowCollection<TRow> OrderBy<TRow, TKey>(this EnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector)
        {
            IEnumerable<TRow> ie = Enumerable.OrderBy(source, keySelector);

            OrderedEnumerableRowCollection<TRow> edt = new OrderedEnumerableRowCollection<TRow>(source, ie);
            edt.AddSortExpression(keySelector, isDescending: false, isOrderBy: true);
            return edt;
        }

        /// <summary>
        /// LINQ's OrderBy operator for generic EnumerableRowCollection.
        /// </summary>
        public static OrderedEnumerableRowCollection<TRow> OrderBy<TRow, TKey>(this EnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector, IComparer<TKey> comparer)
        {
            IEnumerable<TRow> ie = Enumerable.OrderBy(source, keySelector, comparer);
            OrderedEnumerableRowCollection<TRow> edt = new OrderedEnumerableRowCollection<TRow>(source, ie);
            edt.AddSortExpression(keySelector, comparer, isDescending: false, isOrderBy: true);
            return edt;
        }

        /// <summary>
        /// LINQ's OrderByDescending operator for generic EnumerableRowCollection.
        /// </summary>
        public static OrderedEnumerableRowCollection<TRow> OrderByDescending<TRow, TKey>(this EnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector)
        {
            IEnumerable<TRow> ie = Enumerable.OrderByDescending(source, keySelector);

            OrderedEnumerableRowCollection<TRow> edt = new OrderedEnumerableRowCollection<TRow>(source, ie);
            edt.AddSortExpression(keySelector, isDescending: true, isOrderBy: true);
            return edt;
        }

        /// <summary>
        /// LINQ's OrderByDescending operator for generic EnumerableRowCollection.
        /// </summary>
        public static OrderedEnumerableRowCollection<TRow> OrderByDescending<TRow, TKey>(this EnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector, IComparer<TKey> comparer)
        {
            IEnumerable<TRow> ie = Enumerable.OrderByDescending(source, keySelector, comparer);

            OrderedEnumerableRowCollection<TRow> edt = new OrderedEnumerableRowCollection<TRow>(source, ie);
            edt.AddSortExpression(keySelector, comparer, isDescending: true, isOrderBy: true);
            return edt;
        }

        /// <summary>
        /// LINQ's ThenBy operator for generic EnumerableRowCollection.
        /// </summary>
        public static OrderedEnumerableRowCollection<TRow> ThenBy<TRow, TKey>(this OrderedEnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector)
        {
            IEnumerable<TRow> ie = Enumerable.ThenBy((IOrderedEnumerable<TRow>)source.EnumerableRows, keySelector);
            OrderedEnumerableRowCollection<TRow> edt = new OrderedEnumerableRowCollection<TRow>(source, ie);

            edt.AddSortExpression(keySelector, isDescending: false, isOrderBy: false);
            return edt;
        }

        /// <summary>
        /// LINQ's ThenBy operator for generic EnumerableRowCollection.
        /// </summary>
        public static OrderedEnumerableRowCollection<TRow> ThenBy<TRow, TKey>(this OrderedEnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector, IComparer<TKey> comparer)
        {
            IEnumerable<TRow> ie = Enumerable.ThenBy((IOrderedEnumerable<TRow>)source.EnumerableRows, keySelector, comparer);
            OrderedEnumerableRowCollection<TRow> edt = new OrderedEnumerableRowCollection<TRow>(source, ie);

            edt.AddSortExpression(keySelector, comparer, isDescending: false, isOrderBy: false);
            return edt;
        }

        /// <summary>
        /// LINQ's ThenByDescending operator for generic EnumerableRowCollection.
        /// </summary>
        public static OrderedEnumerableRowCollection<TRow> ThenByDescending<TRow, TKey>(this OrderedEnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector)
        {
            IEnumerable<TRow> ie = Enumerable.ThenByDescending((IOrderedEnumerable<TRow>)source.EnumerableRows, keySelector);
            OrderedEnumerableRowCollection<TRow> edt = new OrderedEnumerableRowCollection<TRow>(source, ie);

            edt.AddSortExpression(keySelector, isDescending: true, isOrderBy: false);
            return edt;
        }

        /// <summary>
        /// LINQ's ThenByDescending operator for generic EnumerableRowCollection.
        /// </summary>
        public static OrderedEnumerableRowCollection<TRow> ThenByDescending<TRow, TKey>(this OrderedEnumerableRowCollection<TRow> source, Func<TRow, TKey> keySelector, IComparer<TKey> comparer)
        {
            IEnumerable<TRow> ie = Enumerable.ThenByDescending((IOrderedEnumerable<TRow>)source.EnumerableRows, keySelector, comparer);
            OrderedEnumerableRowCollection<TRow> edt = new OrderedEnumerableRowCollection<TRow>(source, ie);

            edt.AddSortExpression(keySelector, comparer, isDescending: true, isOrderBy: false);
            return edt;
        }

        /// <summary>
        /// Executes a Select (Projection) on EnumerableDataTable. If the selector returns a different
        /// type than the type of rows, then AsLinqDataView is disabled, and the returning EnumerableDataTable
        /// represents an enumerable over the LINQ Query.
        /// </summary>
        public static EnumerableRowCollection<S> Select<TRow, S>(this EnumerableRowCollection<TRow> source, Func<TRow, S> selector)
        {
            //Anonymous type or some other type
            //The only thing that matters from this point on is _enumerableRows
            IEnumerable<S> typedEnumerable = Enumerable.Select(source, selector);

            // Dont need predicates or sort expression from this point on since we know AsLinqDataView is disabled.
            return new EnumerableRowCollection<S>(((object)source) as EnumerableRowCollection<S>, typedEnumerable, ((object)selector) as Func<S, S>);
        }

        /// <summary>
        /// Casts an EnumerableDataTable_TSource into EnumerableDataTable_TResult
        /// </summary>
        public static EnumerableRowCollection<TResult> Cast<TResult>(this EnumerableRowCollection source)
        {
            // Since Cast does not have the signature Cast_T_R(..) this call is routed through the non-generic base class EnumerableDataTable

            if ((null != source) && source.ElementType.Equals(typeof(TResult)))
            {
                return (EnumerableRowCollection<TResult>)source;
            }
            else
            {
                //Anonymous type or some other type
                //The only thing that matters from this point on is _enumerableRows

                IEnumerable<TResult> typedEnumerable = Enumerable.Cast<TResult>(source);

                EnumerableRowCollection<TResult> newEdt = new EnumerableRowCollection<TResult>(
                    typedEnumerable,
                    typeof(TResult).IsAssignableFrom(source.ElementType) && typeof(DataRow).IsAssignableFrom(typeof(TResult)),
                    source.Table);

                return newEdt;
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace System.Data
{
    /// <summary>
    /// Provides an entry point so that Cast operator call can be intercepted within an extension method.
    /// </summary>
    public abstract class EnumerableRowCollection : IEnumerable
    {
        internal abstract Type ElementType { get; }
        internal abstract DataTable? Table { get; }

        internal EnumerableRowCollection()
        {
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return null!;
        }
    }

    /// <summary>
    /// This class provides a wrapper for DataTables to allow for querying via LINQ.
    /// </summary>
    public class EnumerableRowCollection<TRow> : EnumerableRowCollection, IEnumerable<TRow>
    {
        private readonly DataTable? _table;
        private readonly IEnumerable<TRow> _enumerableRows;
        private readonly List<Func<TRow, bool>> _listOfPredicates;

        // Stores list of sort expression in the order provided by user. E.g. order by, thenby, thenby descending..
        private readonly SortExpressionBuilder<TRow> _sortExpression;

        private readonly Func<TRow, TRow>? _selector;

        internal override Type ElementType
        {
            get
            {
                return typeof(TRow);
            }

        }

        internal IEnumerable<TRow> EnumerableRows
        {
            get
            {
                return _enumerableRows;
            }
        }

        internal override DataTable? Table
        {
            get
            {
                return _table;
            }
        }

        /// <summary>
        /// This constructor is used when Select operator is called with output Type other than input row Type.
        /// Basically fail on GetLDV(), but other LINQ operators must work.
        /// </summary>
        internal EnumerableRowCollection(IEnumerable<TRow> enumerableRows, bool isDataViewable, DataTable? table)
        {
            Debug.Assert(!isDataViewable || table != null, "isDataViewable bug table is null");

            _enumerableRows = enumerableRows;
            if (isDataViewable)
            {
                _table = table;
            }
            _listOfPredicates = new List<Func<TRow, bool>>();
            _sortExpression = new SortExpressionBuilder<TRow>();
        }

        /// <summary>
        /// Basic Constructor
        /// </summary>
        internal EnumerableRowCollection(DataTable table)
        {
            _table = table;
            _enumerableRows = table.Rows.Cast<TRow>();
            _listOfPredicates = new List<Func<TRow, bool>>();
            _sortExpression = new SortExpressionBuilder<TRow>();
        }

        /// <summary>
        /// Copy Constructor that sets the input IEnumerable as enumerableRows
        /// Used to maintain IEnumerable that has linq operators executed in the same order as the user
        /// </summary>
        internal EnumerableRowCollection(EnumerableRowCollection<TRow>? source, IEnumerable<TRow> enumerableRows, Func<TRow, TRow>? selector)
        {
            Debug.Assert(null != enumerableRows, "null enumerableRows");

            _enumerableRows = enumerableRows;
            _selector = selector;
            if (null != source)
            {
                if (null == source._selector)
                {
                    _table = source._table;
                }
                _listOfPredicates = new List<Func<TRow, bool>>(source._listOfPredicates);
                //deep copy the List
                _sortExpression = source._sortExpression.Clone();
            }
            else
            {
                _listOfPredicates = new List<Func<TRow, bool>>();
                _sortExpression = new SortExpressionBuilder<TRow>();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// This method returns an strongly typed iterator
        /// for the underlying DataRow collection.
        /// </summary>
        /// <returns>A strongly typed iterator.</returns>
        public IEnumerator<TRow> GetEnumerator()
        {
            return _enumerableRows.GetEnumerator();
        }

        /// <summary>
        /// Evaluates filter and sort if necessary and returns
        /// a LinqDataView representing the LINQ query this class has collected.
        /// </summary>
        /// <returns>LinqDataView repesenting the LINQ query</returns>
        internal LinqDataView GetLinqDataView() // Called by AsLinqDataView
        {
            if ((null == _table) || !typeof(DataRow).IsAssignableFrom(typeof(TRow)))
            {
                throw DataSetUtil.NotSupported(SR.ToLDVUnsupported);
            }

            LinqDataView? view = null;

            #region BuildSinglePredicate

            Func<DataRow, bool>? finalPredicate = null; // Conjunction of all .Where(..) predicates
            if ((null != _selector) && (0 < _listOfPredicates.Count))
            {
                // Hook up all individual predicates into one predicate
                // This lambda is a conjunction of multiple predicates set by the user
                // Note: This is a Short-Circuit Conjunction
                finalPredicate =
                    (DataRow row) =>
                    {
                        if (!object.ReferenceEquals(row, _selector((TRow)(object)row)))
                        {
                            throw DataSetUtil.NotSupported(SR.ToLDVUnsupported);
                        }

                        foreach (Func<TRow, bool> pred in _listOfPredicates)
                        {
                            if (!pred((TRow)(object)row))
                            {
                                return false;
                            }
                        }
                        return true;
                    };
            }
            else if (null != _selector)
            {
                finalPredicate =
                    (DataRow row) =>
                    {
                        if (!object.ReferenceEquals(row, _selector((TRow)(object)row)))
                        {
                            throw DataSetUtil.NotSupported(SR.ToLDVUnsupported);
                        }
                        return true;
                    };
            }
            else if (0 < _listOfPredicates.Count)
            {
                finalPredicate =
                    (DataRow row) =>
                    {
                        foreach (Func<TRow, bool> pred in _listOfPredicates)
                        {
                            if (!pred((TRow)(object)row))
                            {
                                return false;
                            }
                        }
                        return true;
                    };
            }
            #endregion BuildSinglePredicate

            #region Evaluate Filter/Sort
            //  All of this complexity below is because we want to create index only once.
            //

            //  If we only have filter, we set _view.Predicate       - 1 index creation
            //  If we only have sort, we set _view.SortExpression()  - 1 index creation
            //  If we have BOTH, we set them through the constructor - 1 index creation
            //


            // Filter AND Sort
            if ((null != finalPredicate) && (0 < _sortExpression.Count))
            {
                // A lot more work here because constructor does not know type K,
                // so the responsibility to create appropriate delegate comparers
                // is outside of the constructor.

                view = new LinqDataView(
                               _table,
                               row => finalPredicate(row),          // System.Predicate
                               (DataRow a, DataRow b) =>            // Comparison for DV for Index creation
                                   _sortExpression.Compare(
                                       _sortExpression.Select((TRow)(object)a),
                                       _sortExpression.Select((TRow)(object)b)),
                               (object key, DataRow row) =>         // Comparison_K_T for DV's Find()
                                   _sortExpression.Compare(
                                       (List<object>)key,
                                       _sortExpression.Select((TRow)(object)row)),
                                _sortExpression.CloneCast<DataRow>());
            }
            else if (null != finalPredicate)
            {
                // Only Filtering
                view = new LinqDataView(
                                    _table,
                                    row => finalPredicate(row),     // System.Predicate
                                    null,
                                    null,
                                    _sortExpression.CloneCast<DataRow>());
            }
            else if (0 < _sortExpression.Count)
            {
                // Only Sorting
                view = new LinqDataView(
                            _table,
                            null,
                            (DataRow a, DataRow b) =>
                                _sortExpression.Compare(
                                    _sortExpression.Select((TRow)(object)a),
                                    _sortExpression.Select((TRow)(object)b)),
                            (object key, DataRow row) =>
                                _sortExpression.Compare(
                                    (List<object>)key,
                                    _sortExpression.Select((TRow)(object)row)),
                            _sortExpression.CloneCast<DataRow>());
            }
            else
            {
                view = new LinqDataView(_table, _sortExpression.CloneCast<DataRow>());
            }
            #endregion Evaluate Filter and Sort

            return view;
        }

        /// <summary>
        /// Used to add a filter predicate.
        /// A conjunction of all predicates are evaluated in LinqDataView
        /// </summary>
        internal void AddPredicate(Func<TRow, bool> pred)
        {
            Debug.Assert(pred != null);
            _listOfPredicates.Add(pred);
        }

        /// <summary>
        /// Adds a sort expression when Keyselector is provided but not Comparer
        /// </summary>
        internal void AddSortExpression<TKey>(Func<TRow, TKey> keySelector, bool isDescending, bool isOrderBy)
        {
            AddSortExpression<TKey>(keySelector, Comparer<TKey>.Default, isDescending, isOrderBy);
        }

        /// <summary>
        /// Adds a sort expression when Keyselector and Comparer are provided.
        /// </summary>
        internal void AddSortExpression<TKey>(Func<TRow, TKey> keySelector, IComparer<TKey> comparer, bool isDescending, bool isOrderBy)
        {
            DataSetUtil.CheckArgumentNull(keySelector, nameof(keySelector));
            DataSetUtil.CheckArgumentNull(comparer, nameof(comparer));

            _sortExpression.Add(
                    delegate (TRow input)
                    {
                        return keySelector(input)!;
                    },
                    delegate (object val1, object val2)
                    {
                        return (isDescending ? -1 : 1) * comparer.Compare((TKey)val1, (TKey)val2);
                    },
                      isOrderBy);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{
    /// <summary>
    /// Represents a bindable, queryable DataView of DataRow, that can be created from from LINQ queries over DataTable
    /// and from DataTable.
    /// </summary>
    internal sealed class LinqDataView : DataView, IBindingList, IBindingListView
    {
        /// <summary>
        /// A Comparer that compares a Key and a Row.
        /// </summary>
        internal Func<object, DataRow, int>? comparerKeyRow;  // comparer for DataView.Find(..

        /// <summary>
        /// Builds the sort expression in case multiple selector/comparers are added.
        /// </summary>
        internal readonly SortExpressionBuilder<DataRow>? sortExpressionBuilder;

        /// <summary>
        /// Constructs a LinkDataView and its parent DataView.
        /// Does not create index on the DataView since filter and sort expressions are not yet provided.
        /// </summary>
        /// <param name="table">The input table from which LinkDataView is to be created.</param>
        /// <param name="sortExpressionBuilder">The sort expression builder in case multiple selectors/comparers are added.</param>
        internal LinqDataView(DataTable table, SortExpressionBuilder<DataRow>? sortExpressionBuilder)
            : base(table)
        {
            Debug.Assert(table != null, "null DataTable");
            this.sortExpressionBuilder = sortExpressionBuilder ?? new SortExpressionBuilder<DataRow>();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="table">Table from which to create the view</param>
        /// <param name="predicate_system">User-provided predicate but in the form of System.Predicate&lt;DataRow&gt;
        /// Predicates are being replicated in different forms so that nulls can be passed in.
        /// For e.g. when user provides null predicate, base.Predicate should be set to null. I cant do that in the constructor initialization
        /// if I will have to create System.Predicate delegate from Func.
        /// </param>
        /// <param name="comparison">The comparer function of DataRow to be used for sorting. </param>
        /// <param name="comparerKeyRow">A comparer function that compares a Key value to DataRow.</param>
        /// <param name="sortExpressionBuilder">Combined sort expression build using mutiple sort expressions.</param>
        internal LinqDataView(
                    DataTable table,
                    Predicate<DataRow>? predicate_system,
                    Comparison<DataRow>? comparison,
                    Func<object, DataRow, int>? comparerKeyRow,
                    SortExpressionBuilder<DataRow>? sortExpressionBuilder)
            : base(table,
                predicate_system,
                comparison,
                DataViewRowState.CurrentRows)
        {
            this.sortExpressionBuilder = sortExpressionBuilder ?? this.sortExpressionBuilder;
            this.comparerKeyRow = comparerKeyRow;
        }

        /// <summary>
        /// Gets or sets the expression used to filter which rows are viewed in the LinqDataView
        /// </summary>
        public override string? RowFilter
        {
            get
            {
                if (base.RowPredicate == null)// using string based filter or no filter
                {
                    return base.RowFilter;
                }
                else // using expression based filter
                {
                    return null;
                }
            }
            [RequiresUnreferencedCode(Select.RequiresUnreferencedCodeMessage)]
            set
            {
                if (value == null)
                {
                    base.RowPredicate = null;
                    base.RowFilter = string.Empty; // INDEX rebuild twice
                }
                else
                {
                    base.RowFilter = value;
                    base.RowPredicate = null;
                }
            }
        }

        #region Find

        /// <summary>
        /// Searches the index and finds a single row where the sort-key matches the input key
        /// </summary>
        /// <param name="key">Value of the key to find</param>
        /// <returns>Index of the first match of input key</returns>
        internal override int FindByKey(object? key)
        {
            Debug.Assert(base.Sort != null);
            Debug.Assert(!(!string.IsNullOrEmpty(base.Sort) && base.SortComparison != null),
                "string and expression based sort cannot both be set");

            if (!string.IsNullOrEmpty(base.Sort))  // use find for DV's sort string
            {
                return base.FindByKey(key);
            }
            else if (base.SortComparison == null) // neither string or expr set
            {
                // This is the exception message from DataView that we want to use
                throw ExceptionBuilder.IndexKeyLength(0, 0);
            }
            else  // find for expression based sort
            {
                if (sortExpressionBuilder!.Count != 1)
                    throw DataSetUtil.InvalidOperation(SR.Format(SR.LDV_InvalidNumOfKeys, sortExpressionBuilder.Count));

                Index.ComparisonBySelector<object, DataRow> compareDelg =
                    new Index.ComparisonBySelector<object, DataRow>(comparerKeyRow!);

                List<object?> keyList = new List<object?>();
                keyList.Add(key);
                Range range = FindRecords<object, DataRow>(compareDelg, keyList);

                return (range.Count == 0) ? -1 : range.Min;
            }
        }

        /// <summary>
        /// Since LinkDataView does not support multiple selectors/comparers, it does not make sense for
        /// them to Find using multiple keys.
        /// This overriden method prevents users calling multi-key find on dataview.
        /// </summary>
        internal override int FindByKey(object?[] key)
        {
            // must have string or expression based sort specified
            if (base.SortComparison == null && string.IsNullOrEmpty(base.Sort))
            {
                // This is the exception message from DataView that we want to use
                throw ExceptionBuilder.IndexKeyLength(0, 0);
            }
            else if (base.SortComparison != null && key.Length != sortExpressionBuilder!.Count)
            {
                throw DataSetUtil.InvalidOperation(SR.Format(SR.LDV_InvalidNumOfKeys, sortExpressionBuilder.Count));
            }

            if (base.SortComparison == null)
            {
                // using string to sort
                return base.FindByKey(key);
            }
            else
            {
                Index.ComparisonBySelector<object, DataRow> compareDelg =
                    new Index.ComparisonBySelector<object, DataRow>(comparerKeyRow!);

                List<object?> keyList = new List<object?>();
                foreach (object? singleKey in key)
                {
                    keyList.Add(singleKey);
                }

                Range range = FindRecords<object, DataRow>(compareDelg, keyList);
                return (range.Count == 0) ? -1 : range.Min;
            }

        }

        /// <summary>
        /// Searches the index and finds rows where the sort-key matches the input key.
        /// Since LinkDataView does not support multiple selectors/comparers, it does not make sense for
        /// them to Find using multiple keys. This overriden method prevents users calling multi-key find on dataview.
        /// </summary>
        internal override DataRowView[] FindRowsByKey(object?[] key)
        {
            // must have string or expression based sort specified
            if (base.SortComparison == null && string.IsNullOrEmpty(base.Sort))
            {
                // This is the exception message from DataView that we want to use
                throw ExceptionBuilder.IndexKeyLength(0, 0);
            }
            else if (base.SortComparison != null && key.Length != sortExpressionBuilder!.Count)
            {
                throw DataSetUtil.InvalidOperation(SR.Format(SR.LDV_InvalidNumOfKeys, sortExpressionBuilder.Count));
            }

            if (base.SortComparison == null)// using string to sort
            {
                return base.FindRowsByKey(key);
            }
            else
            {
                Range range = FindRecords<object, DataRow>(
                    new Index.ComparisonBySelector<object, DataRow>(comparerKeyRow!),
                    new List<object?>(key));
                return base.GetDataRowViewFromRange(range);
            }
        }
        #endregion


        #region Misc Overrides
        /// <summary>
        /// Overriding DataView's SetIndex to prevent users from setting RowState filter to anything other
        /// than CurrentRows.
        /// </summary>
        internal override void SetIndex(string newSort, DataViewRowState newRowStates, IFilter? newRowFilter)
        {
            // Throw only if expressions (filter or sort) are used and rowstate is not current rows
            if ((base.SortComparison != null || base.RowPredicate != null) && newRowStates != DataViewRowState.CurrentRows)
            {
                throw DataSetUtil.Argument(SR.LDVRowStateError);
            }
            else
            {
                base.SetIndex(newSort, newRowStates, newRowFilter);
            }
        }

        #endregion

        #region IBindingList
        /// <summary>
        /// Clears both expression-based and DataView's string-based sorting.
        /// </summary>
        void IBindingList.RemoveSort()
        {
            base.Sort = string.Empty;
            base.SortComparison = null;
        }

        /// <summary>
        /// Overrides IBindingList's SortProperty so that it returns null if expression based sort
        /// is used in the LinkDataView, otherwise it defers the result to DataView
        /// </summary>
        PropertyDescriptor? IBindingList.SortProperty
        {
            get
            {
                return (base.SortComparison == null) ? base.GetSortProperty() : null;
            }
        }

        /// <summary>
        /// Overrides IBindingList's SortDescriptions so that it returns null if expression based sort
        /// is used in the LinkDataView, otherwise it defers the result to DataView
        /// </summary>
        ListSortDescriptionCollection IBindingListView.SortDescriptions
        {
            get
            {
                if (base.SortComparison == null)
                {
                    return base.GetSortDescriptions();
                }
                else
                {
                    return new ListSortDescriptionCollection();
                }
            }
        }

        /// <summary>
        /// Tells whether the LinqDataView is sorted or not
        /// </summary>
        bool IBindingList.IsSorted
        {
            get
            {   // Sorted if either expression based sort or string based sort is set
                return !(base.SortComparison == null && base.Sort.Length == 0);
            }
        }
        #endregion
    }
}

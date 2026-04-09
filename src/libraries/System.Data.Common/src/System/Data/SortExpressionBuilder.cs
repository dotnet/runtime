// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Data
{

    /// <summary>
    /// This class represents a combined sort expression build using multiple sort expressions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class SortExpressionBuilder<T> : IComparer<List<object>>
    {
        /**
         *  This class ensures multiple orderby/thenbys are handled correctly. Its semantics is as follows:
         *
         * Query 1:
         * orderby a
         * thenby  b
         * orderby c
         * orderby d
         * thenby  e
         *
         * is equivalent to:
         *
         * Query 2:
         * orderby d
         * thenby  e
         * thenby  c
         * thenby  a
         * thenby  b
         *
         **/

        // Selectors and comparers are mapped using the index in the list.
        // E.g: _comparers[i] is used with _selectors[i]

        private readonly LinkedList<Func<T, object>> _selectors = new LinkedList<Func<T, object>>();
        private readonly LinkedList<Comparison<object>> _comparers = new LinkedList<Comparison<object>>();

        private LinkedListNode<Func<T, object>>? _currentSelector;
        private LinkedListNode<Comparison<object>>? _currentComparer;

        /// <summary>
        /// Adds a sorting selector/comparer in the correct order
        /// </summary>
        internal void Add(Func<T, object> keySelector, Comparison<object> compare, bool isOrderBy)
        {
            Debug.Assert(keySelector != null);
            Debug.Assert(compare != null);
            // Inputs are assumed to be valid. The burden for ensuring it is on the caller.

            if (isOrderBy)
            {
                _currentSelector = _selectors.AddFirst(keySelector);
                _currentComparer = _comparers.AddFirst(compare);
            }
            else
            {
                // ThenBy can only be called after OrderBy
                Debug.Assert(_currentSelector != null);
                Debug.Assert(_currentComparer != null);

                _currentSelector = _selectors.AddAfter(_currentSelector, keySelector);
                _currentComparer = _comparers.AddAfter(_currentComparer, compare);
            }
        }

        /// <summary>
        /// Represents a Combined selector of all selectors added thus far.
        /// </summary>
        /// <returns>List of 'objects returned by each selector'. This list is the combined-selector</returns>
        public List<object> Select(T row)
        {
            List<object> result = new List<object>();

            foreach (Func<T, object> selector in _selectors)
            {
                result.Add(selector(row));
            }

            return result;
        }


        /// <summary>
        /// Represents a Comparer (of IComparer) that compares two combined-selectors using
        /// provided comparers for each individual selector.
        /// Note: Comparison is done in the order it was Added.
        /// </summary>
        /// <returns>Comparison result of the combined Sort comparer expression</returns>
        public int Compare(List<object>? a, List<object>? b)
        {
            Debug.Assert(a != null && b != null && a.Count == Count);

            int i = 0;
            foreach (Comparison<object> compare in _comparers)
            {
                int result = compare(a[i], b[i]);

                if (result != 0)
                {
                    return result;
                }
                i++;
            }

            return 0;
        }

        internal int Count
        {
            get
            {
                //weak now that we have two dimensions
                Debug.Assert(_selectors.Count == _comparers.Count);
                return _selectors.Count;
            }
        }

        /// <summary>
        /// Clones the SortexpressionBuilder and returns a new object
        /// that points to same comparer and selectors (in the same order).
        /// </summary>
        internal SortExpressionBuilder<T> Clone()
        {
            SortExpressionBuilder<T> builder = new SortExpressionBuilder<T>();

            foreach (Func<T, object> selector in _selectors)
            {
                if (selector == _currentSelector!.Value)
                {
                    builder._currentSelector = builder._selectors.AddLast(selector);
                }
                else
                {
                    builder._selectors.AddLast(selector);
                }
            }


            foreach (Comparison<object> comparer in _comparers)
            {
                if (comparer == _currentComparer!.Value)
                {
                    builder._currentComparer = builder._comparers.AddLast(comparer);
                }
                else
                {
                    builder._comparers.AddLast(comparer);
                }
            }

            return builder;
        }

        /// <summary>
        /// Clones the SortExpressinBuilder and casts to type TResult.
        /// </summary>
        internal SortExpressionBuilder<TResult> CloneCast<TResult>()
        {
            SortExpressionBuilder<TResult> builder = new SortExpressionBuilder<TResult>();

            foreach (Func<T, object> selector in _selectors)
            {
                if (selector == _currentSelector!.Value)
                {
                    builder._currentSelector = builder._selectors.AddLast(r => selector((T)(object)r!));
                }
                else
                {
                    builder._selectors.AddLast(r => selector((T)(object)r!));
                }
            }


            foreach (Comparison<object> comparer in _comparers)
            {
                if (comparer == _currentComparer!.Value)
                {
                    builder._currentComparer = builder._comparers.AddLast(comparer);
                }
                else
                {
                    builder._comparers.AddLast(comparer);
                }
            }

            return builder;
        }
    }
}

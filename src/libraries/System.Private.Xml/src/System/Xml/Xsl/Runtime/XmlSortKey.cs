// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System;
using System.Diagnostics;
using System.Globalization;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// Base internal class for all sort keys.
    /// Inherits from IComparable, so that Array.Sort can perform comparison.
    /// </summary>
    internal abstract class XmlSortKey : IComparable
    {
        private int _priority;           // Original input ordering used to ensure that sort is stable
        private XmlSortKey _nextKey;     // Next sort key if there are multiple keys (null otherwise)

        /// <summary>
        /// Get or set this key's index, relative to other keys involved in a sort.  This priority will
        /// break ties.  If the priority is not set, then the sort will not be stable.
        /// </summary>
        public int Priority
        {
            //get { return this.priority; }
            set
            {
                // All linked keys have same priority
                XmlSortKey key = this;

                while (key != null)
                {
                    key._priority = value;
                    key = key._nextKey;
                }
            }
        }

        /// <summary>
        /// Sometimes a key is composed of multiple parts.  For example: (LastName, FirstName).  Multi-part
        /// keys are linked together in a list.  This method recursively adds a new key part to the end of the list.
        /// Returns the first (primary) key in the list.
        /// </summary>
        public XmlSortKey AddSortKey(XmlSortKey sortKey)
        {
            if (_nextKey != null)
            {
                // Add to end of list--this is not it
                _nextKey.AddSortKey(sortKey);
            }
            else
            {
                // This is the end of the list
                _nextKey = sortKey;
            }

            return this;
        }

        /// <summary>
        /// When two keys are compared and found to be equal, the tie must be broken.  If there is a secondary key,
        /// then use that to break the tie.  Otherwise, use the input ordering to break the tie.  Since every key
        /// has a unique index, this is guaranteed to always break the tie.
        /// </summary>
        protected int BreakSortingTie(XmlSortKey that)
        {
            if (_nextKey != null)
            {
                // There are multiple keys, so break tie using next key
                Debug.Assert(_nextKey != null && that._nextKey != null);
                return _nextKey.CompareTo(that._nextKey);
            }

            Debug.Assert(_priority != that._priority);
            return (_priority < that._priority) ? -1 : 1;
        }

        /// <summary>
        /// Compare a non-empty key (this) to an empty key (obj).  The empty sequence always sorts either before all
        /// other values, or after all other values.
        /// </summary>
        protected int CompareToEmpty(object obj)
        {
            XmlEmptySortKey that = obj as XmlEmptySortKey;
            Debug.Assert(that != null && !(this is XmlEmptySortKey));
            return that.IsEmptyGreatest ? -1 : 1;
        }

        /// <summary>
        /// Base internal class is abstract and doesn't actually implement CompareTo; derived classes must do this.
        /// </summary>
        public abstract int CompareTo(object that);
    }


    /// <summary>
    /// Sort key for the empty sequence.  Empty sequence always compares sorts either before all other values,
    /// or after all other values.
    /// </summary>
    internal sealed class XmlEmptySortKey : XmlSortKey
    {
        private readonly bool _isEmptyGreatest;

        public XmlEmptySortKey(XmlCollation collation)
        {
            // Greatest, Ascending: isEmptyGreatest = true
            // Greatest, Descending: isEmptyGreatest = false
            // Least, Ascending: isEmptyGreatest = false
            // Least, Descending: isEmptyGreatest = true
            _isEmptyGreatest = collation.EmptyGreatest != collation.DescendingOrder;
        }

        public bool IsEmptyGreatest
        {
            get { return _isEmptyGreatest; }
        }

        public override int CompareTo(object obj)
        {
            XmlEmptySortKey that = obj as XmlEmptySortKey;

            if (that == null)
            {
                // Empty compared to non-empty
                Debug.Assert(obj is XmlSortKey);
                return -(obj as XmlSortKey).CompareTo(this);
            }

            // Empty compared to empty
            return BreakSortingTie(that);
        }
    }


    /// <summary>
    /// Sort key for xs:decimal values.
    /// </summary>
    internal sealed class XmlDecimalSortKey : XmlSortKey
    {
        private readonly decimal _decVal;

        public XmlDecimalSortKey(decimal value, XmlCollation collation)
        {
            // Invert decimal if sorting in descending order
            _decVal = collation.DescendingOrder ? -value : value;
        }

        public override int CompareTo(object obj)
        {
            XmlDecimalSortKey that = obj as XmlDecimalSortKey;
            int cmp;

            if (that == null)
                return CompareToEmpty(obj);

            cmp = decimal.Compare(_decVal, that._decVal);
            if (cmp == 0)
                return BreakSortingTie(that);

            return cmp;
        }
    }


    /// <summary>
    /// Sort key for xs:integer values.
    /// </summary>
    internal class XmlIntegerSortKey : XmlSortKey
    {
        private readonly long _longVal;

        public XmlIntegerSortKey(long value, XmlCollation collation)
        {
            // Invert long if sorting in descending order
            _longVal = collation.DescendingOrder ? ~value : value;
        }

        public override int CompareTo(object obj)
        {
            XmlIntegerSortKey that = obj as XmlIntegerSortKey;

            if (that == null)
                return CompareToEmpty(obj);

            if (_longVal == that._longVal)
                return BreakSortingTie(that);

            return (_longVal < that._longVal) ? -1 : 1;
        }
    }


    /// <summary>
    /// Sort key for xs:int values.
    /// </summary>
    internal sealed class XmlIntSortKey : XmlSortKey
    {
        private readonly int _intVal;

        public XmlIntSortKey(int value, XmlCollation collation)
        {
            // Invert integer if sorting in descending order
            _intVal = collation.DescendingOrder ? ~value : value;
        }

        public override int CompareTo(object obj)
        {
            XmlIntSortKey that = obj as XmlIntSortKey;

            if (that == null)
                return CompareToEmpty(obj);

            if (_intVal == that._intVal)
                return BreakSortingTie(that);

            return (_intVal < that._intVal) ? -1 : 1;
        }
    }

    /// <summary>
    /// Sort key for xs:string values.  Strings are sorted according to a byte-wise sort key calculated by caller.
    /// </summary>
    internal sealed class XmlStringSortKey : XmlSortKey
    {
        private readonly SortKey _sortKey;
        private readonly byte[] _sortKeyBytes;
        private readonly bool _descendingOrder;

        public XmlStringSortKey(SortKey sortKey, bool descendingOrder)
        {
            _sortKey = sortKey;
            _descendingOrder = descendingOrder;
        }

        public XmlStringSortKey(byte[] sortKey, bool descendingOrder)
        {
            _sortKeyBytes = sortKey;
            _descendingOrder = descendingOrder;
        }

        public override int CompareTo(object obj)
        {
            XmlStringSortKey that = obj as XmlStringSortKey;
            int idx, cntCmp, result;

            if (that == null)
                return CompareToEmpty(obj);

            // Compare either using SortKey.Compare or byte arrays
            if (_sortKey != null)
            {
                Debug.Assert(that._sortKey != null, "Both keys must have non-null sortKey field");
                result = SortKey.Compare(_sortKey, that._sortKey);
            }
            else
            {
                Debug.Assert(_sortKeyBytes != null && that._sortKeyBytes != null, "Both keys must have non-null sortKeyBytes field");

                cntCmp = (_sortKeyBytes.Length < that._sortKeyBytes.Length) ? _sortKeyBytes.Length : that._sortKeyBytes.Length;
                for (idx = 0; idx < cntCmp; idx++)
                {
                    if (_sortKeyBytes[idx] < that._sortKeyBytes[idx])
                    {
                        result = -1;
                        goto Done;
                    }

                    if (_sortKeyBytes[idx] > that._sortKeyBytes[idx])
                    {
                        result = 1;
                        goto Done;
                    }
                }

                // So far, keys are equal, so now test length of each key
                if (_sortKeyBytes.Length < that._sortKeyBytes.Length)
                    result = -1;
                else if (_sortKeyBytes.Length > that._sortKeyBytes.Length)
                    result = 1;
                else
                    result = 0;
            }

        Done:
            // Use document order to break sorting tie
            if (result == 0)
                return BreakSortingTie(that);

            return _descendingOrder ? -result : result;
        }
    }

    /// <summary>
    /// Sort key for Double values.
    /// </summary>
    internal sealed class XmlDoubleSortKey : XmlSortKey
    {
        private readonly double _dblVal;
        private readonly bool _isNaN;

        public XmlDoubleSortKey(double value, XmlCollation collation)
        {
            if (double.IsNaN(value))
            {
                // Treat NaN as if it were the empty sequence
                _isNaN = true;

                // Greatest, Ascending: isEmptyGreatest = true
                // Greatest, Descending: isEmptyGreatest = false
                // Least, Ascending: isEmptyGreatest = false
                // Least, Descending: isEmptyGreatest = true
                _dblVal = (collation.EmptyGreatest != collation.DescendingOrder) ? double.PositiveInfinity : double.NegativeInfinity;
            }
            else
            {
                _dblVal = collation.DescendingOrder ? -value : value;
            }
        }

        public override int CompareTo(object obj)
        {
            XmlDoubleSortKey that = obj as XmlDoubleSortKey;

            if (that == null)
            {
                // Compare to empty sequence
                if (_isNaN)
                    return BreakSortingTie(obj as XmlSortKey);

                return CompareToEmpty(obj);
            }

            if (_dblVal == that._dblVal)
            {
                if (_isNaN)
                {
                    // NaN sorts equal to NaN
                    if (that._isNaN)
                        return BreakSortingTie(that);

                    // NaN sorts before or after all non-NaN values
                    Debug.Assert(_dblVal == double.NegativeInfinity || _dblVal == double.PositiveInfinity);
                    return (_dblVal == double.NegativeInfinity) ? -1 : 1;
                }
                else if (that._isNaN)
                {
                    // NaN sorts before or after all non-NaN values
                    Debug.Assert(that._dblVal == double.NegativeInfinity || that._dblVal == double.PositiveInfinity);
                    return (that._dblVal == double.NegativeInfinity) ? 1 : -1;
                }

                return BreakSortingTie(that);
            }

            return (_dblVal < that._dblVal) ? -1 : 1;
        }
    }


    /// <summary>
    /// Sort key for DateTime values (just convert DateTime to ticks and use Long sort key).
    /// </summary>
    internal sealed class XmlDateTimeSortKey : XmlIntegerSortKey
    {
        public XmlDateTimeSortKey(DateTime value, XmlCollation collation) : base(value.Ticks, collation)
        {
        }
    }
}

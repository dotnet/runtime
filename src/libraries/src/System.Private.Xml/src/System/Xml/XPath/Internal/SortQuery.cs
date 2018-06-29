// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MS.Internal.Xml.XPath
{
    internal sealed class SortQuery : Query
    {
        private List<SortKey> _results;
        private XPathSortComparer _comparer;
        private Query _qyInput;

        public SortQuery(Query qyInput)
        {
            Debug.Assert(qyInput != null, "Sort Query needs an input query tree to work on");
            _results = new List<SortKey>();
            _comparer = new XPathSortComparer();
            _qyInput = qyInput;
            count = 0;
        }
        private SortQuery(SortQuery other) : base(other)
        {
            _results = new List<SortKey>(other._results);
            _comparer = other._comparer.Clone();
            _qyInput = Clone(other._qyInput);
            count = 0;
        }

        public override void Reset() { count = 0; }

        public override void SetXsltContext(XsltContext xsltContext)
        {
            _qyInput.SetXsltContext(xsltContext);
            if (
                _qyInput.StaticType != XPathResultType.NodeSet &&
                _qyInput.StaticType != XPathResultType.Any
            )
            {
                throw XPathException.Create(SR.Xp_NodeSetExpected);
            }
        }

        private void BuildResultsList()
        {
            int numSorts = _comparer.NumSorts;

            Debug.Assert(numSorts > 0, "Why was the sort query created?");

            XPathNavigator eNext;
            while ((eNext = _qyInput.Advance()) != null)
            {
                SortKey key = new SortKey(numSorts, /*originalPosition:*/_results.Count, eNext.Clone());

                for (int j = 0; j < numSorts; j++)
                {
                    key[j] = _comparer.Expression(j).Evaluate(_qyInput);
                }

                _results.Add(key);
            }
            _results.Sort(_comparer);
        }

        public override object Evaluate(XPathNodeIterator context)
        {
            _qyInput.Evaluate(context);
            _results.Clear();
            BuildResultsList();
            count = 0;
            return this;
        }

        public override XPathNavigator Advance()
        {
            Debug.Assert(0 <= count && count <= _results.Count);
            if (count < _results.Count)
            {
                return _results[count++].Node;
            }
            return null;
        }

        public override XPathNavigator Current
        {
            get
            {
                Debug.Assert(0 <= count && count <= _results.Count);
                if (count == 0)
                {
                    return null;
                }
                return _results[count - 1].Node;
            }
        }

        internal void AddSort(Query evalQuery, IComparer comparer)
        {
            _comparer.AddSort(evalQuery, comparer);
        }

        public override XPathNodeIterator Clone() { return new SortQuery(this); }

        public override XPathResultType StaticType { get { return XPathResultType.NodeSet; } }
        public override int CurrentPosition { get { return count; } }
        public override int Count { get { return _results.Count; } }
        public override QueryProps Properties { get { return QueryProps.Cached | QueryProps.Position | QueryProps.Count; } }
    } // class SortQuery

    internal sealed class SortKey
    {
        private int _numKeys;
        private object[] _keys;
        private int _originalPosition;
        private XPathNavigator _node;

        public SortKey(int numKeys, int originalPosition, XPathNavigator node)
        {
            _numKeys = numKeys;
            _keys = new object[numKeys];
            _originalPosition = originalPosition;
            _node = node;
        }

        public object this[int index]
        {
            get { return _keys[index]; }
            set { _keys[index] = value; }
        }

        public int NumKeys { get { return _numKeys; } }
        public int OriginalPosition { get { return _originalPosition; } }
        public XPathNavigator Node { get { return _node; } }
    } // class SortKey

    internal sealed class XPathSortComparer : IComparer<SortKey>
    {
        private const int minSize = 3;
        private Query[] _expressions;
        private IComparer[] _comparers;
        private int _numSorts;

        public XPathSortComparer(int size)
        {
            if (size <= 0) size = minSize;
            _expressions = new Query[size];
            _comparers = new IComparer[size];
        }
        public XPathSortComparer() : this(minSize) { }

        public void AddSort(Query evalQuery, IComparer comparer)
        {
            Debug.Assert(_expressions.Length == _comparers.Length);
            Debug.Assert(0 < _expressions.Length);
            Debug.Assert(0 <= _numSorts && _numSorts <= _expressions.Length);
            // Adjust array sizes if needed.
            if (_numSorts == _expressions.Length)
            {
                Query[] newExpressions = new Query[_numSorts * 2];
                IComparer[] newComparers = new IComparer[_numSorts * 2];
                for (int i = 0; i < _numSorts; i++)
                {
                    newExpressions[i] = _expressions[i];
                    newComparers[i] = _comparers[i];
                }
                _expressions = newExpressions;
                _comparers = newComparers;
            }
            Debug.Assert(_numSorts < _expressions.Length);

            // Fixup expression to handle node-set return type:
            if (evalQuery.StaticType == XPathResultType.NodeSet || evalQuery.StaticType == XPathResultType.Any)
            {
                evalQuery = new StringFunctions(Function.FunctionType.FuncString, new Query[] { evalQuery });
            }

            _expressions[_numSorts] = evalQuery;
            _comparers[_numSorts] = comparer;
            _numSorts++;
        }

        public int NumSorts { get { return _numSorts; } }

        public Query Expression(int i)
        {
            return _expressions[i];
        }

        int IComparer<SortKey>.Compare(SortKey x, SortKey y)
        {
            Debug.Assert(x != null && y != null, "Oops!! what happened?");
            int result = 0;
            for (int i = 0; i < x.NumKeys; i++)
            {
                result = _comparers[i].Compare(x[i], y[i]);
                if (result != 0)
                {
                    return result;
                }
            }

            // if after all comparisons, the two sort keys are still equal, preserve the doc order
            return x.OriginalPosition - y.OriginalPosition;
        }

        internal XPathSortComparer Clone()
        {
            XPathSortComparer clone = new XPathSortComparer(_numSorts);

            for (int i = 0; i < _numSorts; i++)
            {
                clone._comparers[i] = _comparers[i];
                clone._expressions[i] = (Query)_expressions[i].Clone(); // Expressions should be cloned because Query should be cloned
            }
            clone._numSorts = _numSorts;
            return clone;
        }
    } // class XPathSortComparer
} // namespace

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        private List<SortKey> results;
        private XPathSortComparer comparer;
        private Query qyInput;

        public SortQuery(Query qyInput)
        {
            Debug.Assert(qyInput != null, "Sort Query needs an input query tree to work on");
            this.results = new List<SortKey>();
            this.comparer = new XPathSortComparer();
            this.qyInput = qyInput;
            count = 0;
        }
        private SortQuery(SortQuery other) : base(other)
        {
            this.results = new List<SortKey>(other.results);
            this.comparer = other.comparer.Clone();
            this.qyInput = Clone(other.qyInput);
            count = 0;
        }

        public override void Reset() { count = 0; }

        public override void SetXsltContext(XsltContext xsltContext)
        {
            qyInput.SetXsltContext(xsltContext);
            if (
                qyInput.StaticType != XPathResultType.NodeSet &&
                qyInput.StaticType != XPathResultType.Any
            )
            {
                throw XPathException.Create(SR.Xp_NodeSetExpected);
            }
        }

        private void BuildResultsList()
        {
            Int32 numSorts = this.comparer.NumSorts;

            Debug.Assert(numSorts > 0, "Why was the sort query created?");

            XPathNavigator eNext;
            while ((eNext = qyInput.Advance()) != null)
            {
                SortKey key = new SortKey(numSorts, /*originalPosition:*/this.results.Count, eNext.Clone());

                for (Int32 j = 0; j < numSorts; j++)
                {
                    key[j] = this.comparer.Expression(j).Evaluate(qyInput);
                }

                results.Add(key);
            }
            results.Sort(this.comparer);
        }

        public override object Evaluate(XPathNodeIterator context)
        {
            qyInput.Evaluate(context);
            this.results.Clear();
            BuildResultsList();
            count = 0;
            return this;
        }

        public override XPathNavigator Advance()
        {
            Debug.Assert(0 <= count && count <= results.Count);
            if (count < this.results.Count)
            {
                return this.results[count++].Node;
            }
            return null;
        }

        public override XPathNavigator Current
        {
            get
            {
                Debug.Assert(0 <= count && count <= results.Count);
                if (count == 0)
                {
                    return null;
                }
                return results[count - 1].Node;
            }
        }

        internal void AddSort(Query evalQuery, IComparer comparer)
        {
            this.comparer.AddSort(evalQuery, comparer);
        }

        public override XPathNodeIterator Clone() { return new SortQuery(this); }

        public override XPathResultType StaticType { get { return XPathResultType.NodeSet; } }
        public override int CurrentPosition { get { return count; } }
        public override int Count { get { return results.Count; } }
        public override QueryProps Properties { get { return QueryProps.Cached | QueryProps.Position | QueryProps.Count; } }

        public override void PrintQuery(XmlWriter w)
        {
            w.WriteStartElement(this.GetType().Name);
            qyInput.PrintQuery(w);
            w.WriteElementString("XPathSortComparer", "... PrintTree() not implemented ...");
            w.WriteEndElement();
        }
    } // class SortQuery

    internal sealed class SortKey
    {
        private Int32 numKeys;
        private object[] keys;
        private int originalPosition;
        private XPathNavigator node;

        public SortKey(int numKeys, int originalPosition, XPathNavigator node)
        {
            this.numKeys = numKeys;
            this.keys = new object[numKeys];
            this.originalPosition = originalPosition;
            this.node = node;
        }

        public object this[int index]
        {
            get { return this.keys[index]; }
            set { this.keys[index] = value; }
        }

        public int NumKeys { get { return this.numKeys; } }
        public int OriginalPosition { get { return this.originalPosition; } }
        public XPathNavigator Node { get { return this.node; } }
    } // class SortKey

    internal sealed class XPathSortComparer : IComparer<SortKey>
    {
        private const int minSize = 3;
        private Query[] expressions;
        private IComparer[] comparers;
        private int numSorts;

        public XPathSortComparer(int size)
        {
            if (size <= 0) size = minSize;
            this.expressions = new Query[size];
            this.comparers = new IComparer[size];
        }
        public XPathSortComparer() : this(minSize) { }

        public void AddSort(Query evalQuery, IComparer comparer)
        {
            Debug.Assert(this.expressions.Length == this.comparers.Length);
            Debug.Assert(0 < this.expressions.Length);
            Debug.Assert(0 <= numSorts && numSorts <= this.expressions.Length);
            // Ajust array sizes if needed.
            if (numSorts == this.expressions.Length)
            {
                Query[] newExpressions = new Query[numSorts * 2];
                IComparer[] newComparers = new IComparer[numSorts * 2];
                for (int i = 0; i < numSorts; i++)
                {
                    newExpressions[i] = this.expressions[i];
                    newComparers[i] = this.comparers[i];
                }
                this.expressions = newExpressions;
                this.comparers = newComparers;
            }
            Debug.Assert(numSorts < this.expressions.Length);

            // Fixup expression to handle node-set return type:
            if (evalQuery.StaticType == XPathResultType.NodeSet || evalQuery.StaticType == XPathResultType.Any)
            {
                evalQuery = new StringFunctions(Function.FunctionType.FuncString, new Query[] { evalQuery });
            }

            this.expressions[numSorts] = evalQuery;
            this.comparers[numSorts] = comparer;
            numSorts++;
        }

        public int NumSorts { get { return numSorts; } }

        public Query Expression(int i)
        {
            return this.expressions[i];
        }

        int IComparer<SortKey>.Compare(SortKey x, SortKey y)
        {
            Debug.Assert(x != null && y != null, "Oops!! what happened?");
            int result = 0;
            for (int i = 0; i < x.NumKeys; i++)
            {
                result = this.comparers[i].Compare(x[i], y[i]);
                if (result != 0)
                {
                    return result;
                }
            }

            // if after all comparisions, the two sort keys are still equal, preserve the doc order
            return x.OriginalPosition - y.OriginalPosition;
        }

        internal XPathSortComparer Clone()
        {
            XPathSortComparer clone = new XPathSortComparer(this.numSorts);

            for (int i = 0; i < this.numSorts; i++)
            {
                clone.comparers[i] = this.comparers[i];
                clone.expressions[i] = (Query)this.expressions[i].Clone(); // Expressions should be cloned because Query should be cloned
            }
            clone.numSorts = this.numSorts;
            return clone;
        }
    } // class XPathSortComparer
} // namespace

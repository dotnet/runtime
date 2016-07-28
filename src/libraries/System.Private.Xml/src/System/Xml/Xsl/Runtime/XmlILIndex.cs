// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// This class manages nodes from one input document, indexed by key value(s).
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class XmlILIndex
    {
        private Dictionary<string, XmlQueryNodeSequence> _table;

        /// <summary>
        /// This constructor is internal so that external users cannot construct it (and therefore we do not have to test it separately).
        /// </summary>
        internal XmlILIndex()
        {
            _table = new Dictionary<string, XmlQueryNodeSequence>();
        }

        /// <summary>
        /// Add a node indexed by the specified key value.
        /// </summary>
        public void Add(string key, XPathNavigator navigator)
        {
            XmlQueryNodeSequence seq;

            if (!_table.TryGetValue(key, out seq))
            {
                // Create a new sequence and add it to the index
                seq = new XmlQueryNodeSequence();
                seq.AddClone(navigator);
                _table.Add(key, seq);
            }
            else
            {
                // The nodes are guaranteed to be added in document order with possible duplicates.
                // Add node to the existing sequence if it differs from the last one.
                Debug.Assert(navigator.ComparePosition(seq[seq.Count - 1]) >= 0, "Index nodes must be added in document order");
                if (!navigator.IsSamePosition(seq[seq.Count - 1]))
                {
                    seq.AddClone(navigator);
                }
            }
        }

        /// <summary>
        /// Lookup a sequence of nodes that are indexed by the specified key value.
        /// Return a non-null empty sequence, if there are no nodes associated with the key.
        /// </summary>
        public XmlQueryNodeSequence Lookup(string key)
        {
            XmlQueryNodeSequence seq;

            if (!_table.TryGetValue(key, out seq))
                seq = new XmlQueryNodeSequence();

            return seq;
        }
    }
}

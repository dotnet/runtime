// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// Merges several doc-order-distinct sequences into a single doc-order-distinct sequence.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct DodSequenceMerge
    {
        private IList<XPathNavigator>? _firstSequence;
        private List<IEnumerator<XPathNavigator>>? _sequencesToMerge;
        private int _nodeCount;
        private XmlQueryRuntime _runtime;

        /// <summary>
        /// Initialize this instance of DodSequenceMerge.
        /// </summary>
        public void Create(XmlQueryRuntime runtime)
        {
            _firstSequence = null;
            _sequencesToMerge = null;
            _nodeCount = 0;
            _runtime = runtime;
        }

        /// <summary>
        /// Add a new sequence to the list of sequences to merge.
        /// </summary>
        public void AddSequence(IList<XPathNavigator> sequence)
        {
            // Ignore empty sequences
            if (sequence.Count == 0)
                return;

            if (_firstSequence == null)
            {
                _firstSequence = sequence;
            }
            else
            {
                if (_sequencesToMerge == null)
                {
                    _sequencesToMerge = new List<IEnumerator<XPathNavigator>>();
                    MoveAndInsertSequence(_firstSequence.GetEnumerator());
                    _nodeCount = _firstSequence.Count;
                }

                MoveAndInsertSequence(sequence.GetEnumerator());
                _nodeCount += sequence.Count;
            }
        }

        /// <summary>
        /// Return the fully merged sequence.
        /// </summary>
        public IList<XPathNavigator> MergeSequences()
        {
            XmlQueryNodeSequence newSequence;

            // Zero sequences to merge
            if (_firstSequence == null)
                return XmlQueryNodeSequence.Empty;

            // One sequence to merge
            if (_sequencesToMerge == null || _sequencesToMerge.Count <= 1)
                return _firstSequence;

            // Two or more sequences to merge
            newSequence = new XmlQueryNodeSequence(_nodeCount);

            while (_sequencesToMerge.Count != 1)
            {
                // Save last item in list in temp variable, and remove it from list
                IEnumerator<XPathNavigator> sequence = _sequencesToMerge[_sequencesToMerge.Count - 1];
                _sequencesToMerge.RemoveAt(_sequencesToMerge.Count - 1);

                // Add current node to merged sequence
                newSequence.Add(sequence.Current);

                // Now move to the next node, and re-insert it into the list in reverse document order
                MoveAndInsertSequence(sequence);
            }

            // Add nodes in remaining sequence to end of list
            Debug.Assert(_sequencesToMerge.Count == 1, "While loop should terminate when count == 1");
            do
            {
                newSequence.Add(_sequencesToMerge[0].Current);
            }
            while (_sequencesToMerge[0].MoveNext());

            return newSequence;
        }

        /// <summary>
        /// Move to the next item in the sequence.  If there is no next item, then do not
        /// insert the sequence.  Otherwise, call InsertSequence.
        /// </summary>
        private void MoveAndInsertSequence(IEnumerator<XPathNavigator> sequence)
        {
            if (sequence.MoveNext())
                InsertSequence(sequence);
        }

        /// <summary>
        /// Insert the specified sequence into the list of sequences to be merged.
        /// Insert it in reverse document order with respect to the current nodes in other sequences.
        /// </summary>
        private void InsertSequence(IEnumerator<XPathNavigator> sequence)
        {
            for (int i = _sequencesToMerge!.Count - 1; i >= 0; i--)
            {
                int cmp = _runtime.ComparePosition(sequence.Current, _sequencesToMerge[i].Current);

                if (cmp == -1)
                {
                    // Insert after current item
                    _sequencesToMerge.Insert(i + 1, sequence);
                    return;
                }
                else if (cmp == 0)
                {
                    // Found duplicate, so skip the duplicate
                    if (!sequence.MoveNext())
                    {
                        // No more nodes, so don't insert anything
                        return;
                    }

                    // Next node must be after current node in document order, so don't need to reset loop
                }
            }

            // Insert at beginning of list
            _sequencesToMerge.Insert(0, sequence);
        }
    }
}

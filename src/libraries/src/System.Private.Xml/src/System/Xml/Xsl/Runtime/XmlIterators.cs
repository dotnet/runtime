// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Xml;
using System.Xml.XPath;
using System.ComponentModel;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// Iterators that use containment to control a nested iterator return one of the following values from MoveNext().
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public enum IteratorResult
    {
        NoMoreNodes,                // Iteration is complete; there are no more nodes
        NeedInputNode,              // The next node needs to be fetched from the contained iterator before iteration can continue
        HaveCurrentNode,            // This iterator's Current property is set to the next node in the iteration
    };


    /// <summary>
    /// Tokenize a string containing IDREF values and deref the values in order to get a list of ID elements.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct IdIterator
    {
        private XPathNavigator _navCurrent;
        private string[] _idrefs;
        private int _idx;

        public void Create(XPathNavigator context, string value)
        {
            _navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
            _idrefs = XmlConvert.SplitString(value);
            _idx = -1;
        }

        public bool MoveNext()
        {
            do
            {
                _idx++;
                if (_idx >= _idrefs.Length)
                    return false;
            }
            while (!_navCurrent.MoveToId(_idrefs[_idx]));

            return true;
        }

        /// <summary>
        /// Return the current result navigator.  This is only defined after MoveNext() has returned true.
        /// </summary>
        public XPathNavigator Current
        {
            get { return _navCurrent; }
        }
    }
}

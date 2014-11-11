// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using CultureInfo = System.Globalization.CultureInfo;
using Debug = System.Diagnostics.Debug;
using IEnumerable = System.Collections.IEnumerable;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;
using Enumerable = System.Linq.Enumerable;
using IComparer = System.Collections.IComparer;
using IEqualityComparer = System.Collections.IEqualityComparer;
using StringBuilder = System.Text.StringBuilder;
using Encoding = System.Text.Encoding;
using Interlocked = System.Threading.Interlocked;
using System.Reflection;

namespace System.Xml.Linq
{
    /// <summary>
    /// Contains functionality to compare nodes for their document order.
    /// This class cannot be inherited.
    /// </summary>
    public sealed class XNodeDocumentOrderComparer :
        IComparer,
        IComparer<XNode>
    {
        /// <summary>
        /// Compares two nodes to determine their relative XML document order.
        /// </summary>
        /// <param name="x">The first node to compare.</param>
        /// <param name="y">The second node to compare.</param>
        /// <returns>
        /// 0 if the nodes are equal;
        /// -1 if x is before y; 
        /// 1 if x is after y.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the two nodes do not share a common ancestor.
        /// </exception>
        public int Compare(XNode x, XNode y)
        {
            return XNode.CompareDocumentOrder(x, y);
        }

        /// <summary>
        /// Compares two nodes to determine their relative XML document order.
        /// </summary>
        /// <param name="x">The first node to compare.</param>
        /// <param name="y">The second node to compare.</param>
        /// <returns>
        /// 0 if the nodes are equal;
        /// -1 if x is before y; 
        /// 1 if x is after y.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the two nodes do not share a common ancestor.
        /// </exception>        
        /// <exception cref="ArgumentException">
        /// Thrown if either of the two nodes are not derived from XNode.
        /// </exception>        
        int IComparer.Compare(object x, object y)
        {
            XNode n1 = x as XNode;
            if (n1 == null && x != null) throw new ArgumentException(SR.Format(SR.Argument_MustBeDerivedFrom, typeof(XNode)), "x");
            XNode n2 = y as XNode;
            if (n2 == null && y != null) throw new ArgumentException(SR.Format(SR.Argument_MustBeDerivedFrom, typeof(XNode)), "y");
            return Compare(n1, n2);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using IComparer = System.Collections.IComparer;

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
            if (n1 == null && x != null) throw new ArgumentException(SR.Format(SR.Argument_MustBeDerivedFrom, typeof(XNode)), nameof(x));
            XNode n2 = y as XNode;
            if (n2 == null && y != null) throw new ArgumentException(SR.Format(SR.Argument_MustBeDerivedFrom, typeof(XNode)), nameof(y));
            return Compare(n1, n2);
        }
    }
}

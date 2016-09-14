// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using IEqualityComparer = System.Collections.IEqualityComparer;

namespace System.Xml.Linq
{
    /// <summary>
    /// Contains functionality to compare nodes for value equality.
    /// This class cannot be inherited.
    /// </summary>
    public sealed class XNodeEqualityComparer :
        IEqualityComparer,
        IEqualityComparer<XNode>
    {
        /// <summary>
        /// Compares the values of two nodes.
        /// </summary>
        /// <param name="x">The first node to compare.</param>
        /// <param name="y">The second node to compare.</param>
        /// <returns>true if the nodes are equal, false otherwise.</returns>
        /// <remarks>
        /// A null node is equal to another null node but unequal to a non-null
        /// node. Two <see cref="XNode"/>s of different types are never equal. Two
        /// <see cref="XText"/> nodes are equal if they contain the same text. Two
        /// <see cref="XElement"/> nodes are equal if they have the same tag name, the same
        /// set of attributes with the same values, and, ignoring comments and processing
        /// instructions, contain two equal length sequences of pairwise equal content nodes.
        /// Two <see cref="XDocument"/>s are equal if their root nodes are equal. Two
        /// <see cref="XComment"/> nodes are equal if they contain the same comment text.
        /// Two <see cref="XProcessingInstruction"/> nodes are equal if they have the same
        /// target and data. Two <see cref="XDocumentType"/> nodes are equal if the have the
        /// same name, public id, system id, and internal subset.
        /// </remarks>
        public bool Equals(XNode x, XNode y)
        {
            return XNode.DeepEquals(x, y);
        }

        /// <summary>
        /// Returns a hash code based on an <see cref="XNode"/> objects value.
        /// </summary>
        /// <param name="obj">The node to hash.</param>
        /// <returns>A value-based hash code for the node.</returns>
        /// <remarks>
        /// The <see cref="XNode"/> class's implementation of <see cref="Object.GetHashCode"/>
        /// is based on the referential identity of the node. This method computes a
        /// hash code based on the value of the node.
        /// </remarks>
        public int GetHashCode(XNode obj)
        {
            return obj != null ? obj.GetDeepHashCode() : 0;
        }

        /// <summary>
        /// Compares the values of two nodes.
        /// </summary>
        /// <param name="x">The first node to compare.</param>
        /// <param name="y">The second node to compare.</param>
        /// <returns>true if the nodes are equal, false otherwise.</returns>
        /// <remarks>
        /// A null node is equal to another null node but unequal to a non-null
        /// node. Two <see cref="XNode"/>s of different types are never equal. Two
        /// <see cref="XText"/> nodes are equal if they contain the same text. Two
        /// <see cref="XElement"/> nodes are equal if they have the same tag name, the same
        /// set of attributes with the same values, and, ignoring comments and processing
        /// instructions, contain two equal length sequences of pairwise equal content nodes.
        /// Two <see cref="XDocument"/>s are equal if their root nodes are equal. Two
        /// <see cref="XComment"/> nodes are equal if they contain the same comment text.
        /// Two <see cref="XProcessingInstruction"/> nodes are equal if they have the same
        /// target and data. Two <see cref="XDocumentType"/> nodes are equal if the have the
        /// same name, public id, system id, and internal subset.
        /// </remarks>
        bool IEqualityComparer.Equals(object x, object y)
        {
            XNode n1 = x as XNode;
            if (n1 == null && x != null) throw new ArgumentException(SR.Format(SR.Argument_MustBeDerivedFrom, typeof(XNode)), nameof(x));
            XNode n2 = y as XNode;
            if (n2 == null && y != null) throw new ArgumentException(SR.Format(SR.Argument_MustBeDerivedFrom, typeof(XNode)), nameof(y));
            return Equals(n1, n2);
        }

        /// <summary>
        /// Returns a hash code based on a node's value.
        /// </summary>
        /// <param name="obj">The node to hash.</param>
        /// <returns>A value-based hash code for the node.</returns>
        /// <remarks>
        /// The <see cref="XNode"/> class's implementation of <see cref="Object.GetHashCode"/>
        /// is based on the referential identity of the node. This method computes a
        /// hash code based on the value of the node.
        /// </remarks>
        int IEqualityComparer.GetHashCode(object obj)
        {
            XNode n = obj as XNode;
            if (n == null && obj != null) throw new ArgumentException(SR.Format(SR.Argument_MustBeDerivedFrom, typeof(XNode)), nameof(obj));
            return GetHashCode(n);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Collections.Immutable
{
    /// <summary>
    /// An interface for binary tree nodes that allow our common enumerator to walk the graph.
    /// </summary>
    internal interface IBinaryTree
    {
        /// <summary>
        /// Gets the depth of the tree below this node.
        /// </summary>
        int Height { get; }

        /// <summary>
        /// Gets a value indicating whether this node is empty.
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Gets the number of non-empty nodes at this node and below.
        /// </summary>
        /// <exception cref="NotSupportedException">Thrown if the implementation does not store this value at the node.</exception>
        int Count { get; }
        /// <summary>
        /// Gets the left branch of this node.
        /// </summary>
        IBinaryTree Left { get; }

        /// <summary>
        /// Gets the right branch of this node.
        /// </summary>
        IBinaryTree Right { get; }
    }

    /// <summary>
    /// An interface for binary tree nodes that allow our common enumerator to walk the graph.
    /// </summary>
    /// <typeparam name="T">The type of value for each node.</typeparam>
    internal interface IBinaryTree<out T> : IBinaryTree
    {
        /// <summary>
        /// Gets the value represented by the current node.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// Gets the left branch of this node.
        /// </summary>
        new IBinaryTree<T> Left { get; }

        /// <summary>
        /// Gets the right branch of this node.
        /// </summary>
        new IBinaryTree<T> Right { get; }
    }
}

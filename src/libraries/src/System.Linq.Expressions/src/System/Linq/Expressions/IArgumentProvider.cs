// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Linq.Expressions
{
    /// <summary>
    /// Interface for accessing the arguments of multiple tree nodes (<see cref="IDynamicExpression"/>,
    /// <see cref="ElementInit"/>, <see cref="MethodCallExpression"/>, <see cref="InvocationExpression"/>,
    /// <see cref="NewExpression"/>, and <see cref="IndexExpression"/>).
    /// </summary>
    /// <remarks>
    /// This enables two optimizations which reduce the size of the trees.  The first is it enables
    /// the nodes to hold onto an <see cref="Collections.Generic.IList{T}"/> instead of a
    /// <see cref="Collections.ObjectModel.ReadOnlyCollection{T}"/>.  This saves the cost of allocating
    /// the read-only collection for each node.  The second is that it enables specialized subclasses to be
    /// created which hold onto a specific number of arguments.  These nodes can therefore avoid allocating
    /// both a <see cref="Collections.ObjectModel.ReadOnlyCollection{T}"/> and an array for storing their
    /// elements, thus saving 32 bytes per node.  This technique is used by various nodes including
    /// <see cref="BlockExpression"/>, <see cref="InvocationExpression"/>, <see cref="MethodCallExpression"/>.
    ///
    /// Meanwhile the nodes can expose properties of <see cref="Collections.ObjectModel.ReadOnlyCollection{T}"/>s.
    /// They do this by re-using one field for storing both the array or an element that would normally be stored
    /// in the array.
    ///
    /// For the array case the collection is typed to <see cref="Collections.Generic.IList{T}"/> instead
    /// of <see cref="Collections.ObjectModel.ReadOnlyCollection{T}"/>. When the node is initially constructed
    /// it is an array.  When utilities in this library access the arguments it uses this interface. If a user
    /// accesses the property the array is promoted to a <see cref="Collections.ObjectModel.ReadOnlyCollection{T}"/>.
    ///
    /// For the object case we store the first argument in a field typed to <see cref="object"/> and when
    /// the node is initially constructed this holds directly onto the <see cref="Expression"/> of the
    /// first argument.  When utilities in this library access the arguments it again uses this interface
    /// and the accessor for the first argument uses <see cref="System.Dynamic.Utils.ExpressionUtils.ReturnObject{T}"/> to return the object
    /// which handles the <see cref="Expression"/> or <see cref="Collections.ObjectModel.ReadOnlyCollection{T}"/> case.
    /// When the user accesses the property the object field is updated to hold directly onto the
    /// <see cref="Collections.ObjectModel.ReadOnlyCollection{T}"/>.
    ///
    /// It is important that <see cref="Expression"/> properties consistently return the same
    /// <see cref="Collections.ObjectModel.ReadOnlyCollection{T}"/> otherwise the rewriter used by expression
    /// visitors will be broken and it would be a breaking change from LINQ v1.  The problem is that currently
    /// users can rely on object identity to tell if the node has changed.  Storing the read-only collection in
    /// an overloaded field enables us to both reduce memory usage as well as maintain compatibility and an
    /// easy to use external API.
    /// </remarks>
    public interface IArgumentProvider
    {
        /// <summary>
        /// Gets the argument expression with the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the argument expression to get.</param>
        /// <returns>The expression representing the argument at the specified <paramref name="index"/>.</returns>
        Expression GetArgument(int index);

        /// <summary>
        /// Gets the number of argument expressions of the node.
        /// </summary>
        int ArgumentCount
        {
            get;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Linq.Expressions
{
    /// <summary>Provides an internal interface for accessing the arguments of multiple tree nodes (DynamicExpression, ElementInit, MethodCallExpression, InvocationExpression, NewExpression, and IndexExpression).  This API is for internal use only.</summary>
    /// <remarks>You should not use this API.  It is public only due to assembly refactoring, and it exists only for internal performance optimizations. It enables two optimizations that reduce the size of the trees:
    /// 1. It enables the nodes to hold onto an <see cref="System.Collections.Generic.IList{T}" /> instead of a <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" />.  This saves the cost of allocating the read-only collection for each node.
    /// 2. It enables specialized subclasses to be created that hold on to a specific number of arguments (for example, `Block2`, `Block2`, `Block4`).  Therefore, these nodes avoid allocating both a <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> and an array for storing their elements, thus saving 32 bytes per node.  This technique is used by various nodes, including <see cref="System.Linq.Expressions.BlockExpression" />, <see cref="System.Linq.Expressions.InvocationExpression" />, and <see cref="System.Linq.Expressions.MethodCallExpression" />.
    /// The expression tree nodes continue to expose the original LINQ properties of <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> objects. They do this by reusing a field for storing both the array or an element that would normally be stored in the array.
    /// For the array case, the collection is typed to <see cref="System.Collections.Generic.IList{T}" /> instead of <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" />. When the node is initially constructed, it is an array.  The compiler or utilities in this library access the elements through this interface. Accessing array elements promotes the array to a <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" />.
    /// For the object case, the first argument is stored in a field typed to <see cref="object" />. When the node is initially constructed, this field holds the <see cref="System.Linq.Expressions.Expression" /> of the first argument.  When the compiler and utilities in this library access the arguments, they again use this interface, and the accessor for the first argument uses the internal <see langword="Expression.ReturnObject{T}(object)" /> helper method to return the object that handles the <see cref="System.Linq.Expressions.Expression" /> or <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> case. When the user accesses the <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" />, the object field is updated to hold directly onto the <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" />.
    /// It is important that <see cref="System.Linq.Expressions.Expression" /> properties consistently return the same <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" />. Otherwise, the rewriter tree walker used by expression visitors will break. It is a breaking change from LINQ v1 to return different <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> from the same <see cref="System.Linq.Expressions.Expression" /> node. Currently, users can rely on object identity to tell if the node has changed.  Storing the <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> in an overloaded field both reduces memory usage and maintains compatibility for the public API.</remarks>
    public interface IArgumentProvider
    {
        /// <summary>Returns the argument at <paramref name="index" />, throwing if <paramref name="index" /> is out of bounds. This API is for internal use only.</summary>
        /// <param name="index">The index of the argument.</param>
        /// <returns>The argument at index.</returns>
        /// <remarks>You should not use this API.  It is only public due to assembly refactoring, and it is used internally for performance optimizations.</remarks>
        Expression GetArgument(int index);

        /// <summary>Returns the number of arguments to the expression tree node. This API is for internal use only.</summary>
        /// <value>The number of arguments to the expression tree node as <see cref="int" />.</value>
        /// <remarks>You should not use this API. It is public only due to assembly refactoring, and it is used internally for performance optimizations.</remarks>
        int ArgumentCount
        {
            get;
        }
    }
}

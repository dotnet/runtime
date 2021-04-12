// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Linq.Expressions
{
    /// <summary>Provides an internal interface for accessing the arguments of DynamicExpression tree nodes as well as CallSite and Rewriting functionality.  You should not use this API.  It is only public due to DLL refactoring and exists only for internal performance optimizations.</summary>
    public interface IDynamicExpression : IArgumentProvider
    {
        /// <summary>Gets the delegate type used by the CallSite, which is the type of the rules used in the dynamic expression's polymorphic inline cache.</summary>
        /// <value>The delegate type used by the CallSite.</value>
        Type DelegateType { get; }

        /// <summary>Rewrites this node replacing the dynamic expression's arguments with the provided values.  The number of <paramref name="args" /> needs to match the number of the current expression.  You should not use this type.  It is only public due to assembly refactoring, and it is used internally for performance optimizations.  This helper method allows re-writing of nodes to be independent of the specific implementation class deriving from DynamicExpression that is being used at the call site.</summary>
        /// <param name="args">The arguments used to replace this node.</param>
        /// <returns>The rewritten node, but if no changes were made, then returns the same node.</returns>
        Expression Rewrite(Expression[] args);

        /// <summary>Optionally creates the CallSite and returns the CallSite for the DynamicExpression's polymorphic inline cache.  You should not use this type.  It is only public due to assembly refactoring, and it is used internally for performance optimizations.</summary>
        /// <returns>The CallSite for the DynamicExpression's polymorphic inline cache.</returns>
        object CreateCallSite();
    }
}

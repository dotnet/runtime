// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Linq.Expressions
{
    /// <summary>
    /// Interface implemented by expression nodes that represent a dynamically bound operation.
    /// </summary>
    public interface IDynamicExpression : IArgumentProvider
    {
        /// <summary>
        /// Gets the type of the delegate used by the CallSite.
        /// </summary>
        Type DelegateType { get; }

        /// <summary>
        /// Rewrite this node replacing the args with the provided values.  The
        /// number of the args needs to match the number of the current block.
        ///
        /// This helper is provided to allow re-writing of nodes to not depend on the specific
        /// class of DynamicExpression which is being used.
        /// </summary>
        Expression Rewrite(Expression[] args);

        /// <summary>
        /// Creates a CallSite for the node.
        /// </summary>
        object CreateCallSite();
    }
}

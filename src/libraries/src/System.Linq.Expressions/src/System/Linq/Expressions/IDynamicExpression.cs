// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Linq.Expressions
{
    public interface IDynamicExpression : IArgumentProvider
    {
        /// <summary>
        /// Gets the type of the delegate used by the CallSite />.
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
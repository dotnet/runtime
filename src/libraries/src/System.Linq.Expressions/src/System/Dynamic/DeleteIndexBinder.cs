// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;

namespace System.Dynamic
{
    /// <summary>
    /// Represents the dynamic delete index operation at the call site, providing the binding semantic and the details about the operation.
    /// </summary>
    public abstract class DeleteIndexBinder : DynamicMetaObjectBinder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteIndexBinder" />.
        /// </summary>
        /// <param name="callInfo">The signature of the arguments at the call site.</param>
        protected DeleteIndexBinder(CallInfo callInfo)
        {
            ContractUtils.RequiresNotNull(callInfo, nameof(callInfo));
            CallInfo = callInfo;
        }

        /// <summary>
        /// The result type of the operation.
        /// </summary>
        public override sealed Type ReturnType => typeof(void);

        /// <summary>
        /// Gets the signature of the arguments at the call site.
        /// </summary>
        public CallInfo CallInfo { get; }

        /// <summary>
        /// Performs the binding of the dynamic delete index operation.
        /// </summary>
        /// <param name="target">The target of the dynamic delete index operation.</param>
        /// <param name="args">An array of arguments of the dynamic delete index operation.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            ContractUtils.RequiresNotNull(target, nameof(target));
            ContractUtils.RequiresNotNullItems(args, nameof(args));

            return target.BindDeleteIndex(this, args);
        }

        /// <summary>
        /// Always returns <c>true</c> because this is a standard <see cref="DynamicMetaObjectBinder"/>.
        /// </summary>
        internal override sealed bool IsStandardBinder => true;

        /// <summary>
        /// Performs the binding of the dynamic delete index operation if the target dynamic object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic delete index operation.</param>
        /// <param name="indexes">The arguments of the dynamic delete index operation.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public DynamicMetaObject FallbackDeleteIndex(DynamicMetaObject target, DynamicMetaObject[] indexes)
        {
            return FallbackDeleteIndex(target, indexes, null);
        }

        /// <summary>
        /// When overridden in the derived class, performs the binding of the dynamic delete index operation if the target dynamic object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic delete index operation.</param>
        /// <param name="indexes">The arguments of the dynamic delete index operation.</param>
        /// <param name="errorSuggestion">The binding result to use if binding fails, or null.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public abstract DynamicMetaObject FallbackDeleteIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject errorSuggestion);
    }
}

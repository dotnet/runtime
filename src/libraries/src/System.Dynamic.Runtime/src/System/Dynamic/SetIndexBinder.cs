// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;

namespace System.Dynamic
{
    /// <summary>
    /// Represents the dynamic set index operation at the call site, providing the binding semantic and the details about the operation.
    /// </summary>
    public abstract class SetIndexBinder : DynamicMetaObjectBinder
    {
        private readonly CallInfo _callInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetIndexBinder" />.
        /// </summary>
        /// <param name="callInfo">The signature of the arguments at the call site.</param>
        protected SetIndexBinder(CallInfo callInfo)
        {
            ContractUtils.RequiresNotNull(callInfo, nameof(callInfo));
            _callInfo = callInfo;
        }

        /// <summary>
        /// The result type of the operation.
        /// </summary>
        public override sealed Type ReturnType
        {
            get { return typeof(object); }
        }

        /// <summary>
        /// Gets the signature of the arguments at the call site.
        /// </summary>
        public CallInfo CallInfo
        {
            get { return _callInfo; }
        }

        /// <summary>
        /// Performs the binding of the dynamic set index operation.
        /// </summary>
        /// <param name="target">The target of the dynamic set index operation.</param>
        /// <param name="args">An array of arguments of the dynamic set index operation.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public sealed override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            ContractUtils.RequiresNotNull(target, nameof(target));
            ContractUtils.RequiresNotNull(args, nameof(args));
            ContractUtils.Requires(args.Length >= 2, nameof(args));

            DynamicMetaObject value = args[args.Length - 1];
            DynamicMetaObject[] indexes = args.RemoveLast();

            ContractUtils.RequiresNotNull(value, nameof(args));
            ContractUtils.RequiresNotNullItems(indexes, nameof(args));

            return target.BindSetIndex(this, indexes, value);
        }

        // this is a standard DynamicMetaObjectBinder
        internal override sealed bool IsStandardBinder
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Performs the binding of the dynamic set index operation if the target dynamic object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic set index operation.</param>
        /// <param name="indexes">The arguments of the dynamic set index operation.</param>
        /// <param name="value">The value to set to the collection.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value)
        {
            return FallbackSetIndex(target, indexes, value, null);
        }

        /// <summary>
        /// When overridden in the derived class, performs the binding of the dynamic set index operation if the target dynamic object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic set index operation.</param>
        /// <param name="indexes">The arguments of the dynamic set index operation.</param>
        /// <param name="value">The value to set to the collection.</param>
        /// <param name="errorSuggestion">The binding result to use if binding fails, or null.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        public abstract DynamicMetaObject FallbackSetIndex(DynamicMetaObject target, DynamicMetaObject[] indexes, DynamicMetaObject value, DynamicMetaObject errorSuggestion);
    }
}

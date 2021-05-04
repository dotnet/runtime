// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Numerics.Hashing;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder
{
    /// <summary>
    /// Used to test whether a dynamic member over which += or -= is used is an event member.
    /// </summary>
    internal sealed class CSharpIsEventBinder : DynamicMetaObjectBinder, ICSharpBinder
    {
        public BindingFlag BindingFlags => 0;

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public Expr DispatchPayload(RuntimeBinder runtimeBinder, ArgumentObject[] arguments, LocalVariableSymbol[] locals)
            => runtimeBinder.BindIsEvent(this, arguments, locals);

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public void PopulateSymbolTableWithName(Type callingType, ArgumentObject[] arguments)
            => SymbolTable.PopulateSymbolTableWithName(Name, null, arguments[0].Info.IsStaticType ? arguments[0].Value as Type : arguments[0].Type);

        public bool IsBinderThatCanHaveRefReceiver => false;

        CSharpArgumentInfo ICSharpBinder.GetArgumentInfo(int index) => CSharpArgumentInfo.None;

        public string Name { get; }

        private readonly RuntimeBinder _binder;

        private readonly Type _callingContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpIsEventBinder"/> class.
        /// </summary>
        /// <param name="name">The name of the member to test.</param>
        /// <param name="callingContext">The <see cref="System.Type"/> that indicates where this operation is defined.</param>
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public CSharpIsEventBinder(
            string name,
            Type callingContext)
        {
            Name = name;
            _callingContext = callingContext;
            _binder = new RuntimeBinder(callingContext);
        }

        public int GetGetBinderEquivalenceHash()
        {
            int hash = _callingContext?.GetHashCode() ?? 0;
            hash = HashHelpers.Combine(hash, Name.GetHashCode());
            return hash;
        }

        public bool IsEquivalentTo(ICSharpBinder other)
        {
            var otherBinder = other as CSharpIsEventBinder;
            if (otherBinder == null)
            {
                return false;
            }

            if (_callingContext != otherBinder._callingContext ||
                Name != otherBinder.Name)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// The result type of the operation.
        /// </summary>
        public override Type ReturnType => typeof(bool);

        /// <summary>
        /// Performs the binding of the binary dynamic operation if the target dynamic object cannot bind.
        /// </summary>
        /// <param name="target">The target of the dynamic binary operation.</param>
        /// <param name="args">The arguments to the dynamic event test.</param>
        /// <returns>The <see cref="DynamicMetaObject"/> representing the result of the binding.</returns>
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            BinderHelper.ValidateBindArgument(target, nameof(target));
            return BinderHelper.Bind(this, _binder, new[] { target }, null, null);
        }
    }
}

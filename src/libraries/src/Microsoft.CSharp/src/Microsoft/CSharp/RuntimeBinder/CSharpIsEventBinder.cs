// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Dynamic;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder
{
    /// <summary>
    /// Used to test whether a dynamic member over which += or -= is used is an event member.
    /// </summary>
    internal sealed class CSharpIsEventBinder : DynamicMetaObjectBinder, ICSharpBinder
    {
        public BindingFlag BindingFlags => 0;

        public Expr DispatchPayload(RuntimeBinder runtimeBinder, ArgumentObject[] arguments, LocalVariableSymbol[] locals)
            => runtimeBinder.BindIsEvent(this, arguments, locals);

        public void PopulateSymbolTableWithName(SymbolTable symbolTable, Type callingType, ArgumentObject[] arguments)
            => symbolTable.PopulateSymbolTableWithName(Name, null, arguments[0].Info.IsStaticType ? arguments[0].Value as Type : arguments[0].Type);

        public bool IsBinderThatCanHaveRefReceiver => false;

        CSharpArgumentInfo ICSharpBinder.GetArgumentInfo(int index) => CSharpArgumentInfo.None;

        public string Name { get; }

        public Type CallingContext { get; }

        public bool IsChecked => false;

        private readonly RuntimeBinder _binder;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpIsEventBinder"/> class.
        /// </summary>
        /// <param name="name">The name of the member to test.</param>
        /// <param name="callingContext">The <see cref="System.Type"/> that indicates where this operation is defined.</param>
        public CSharpIsEventBinder(
            string name,
            Type callingContext)
        {
            Name = name;
            CallingContext = callingContext;
            _binder = RuntimeBinder.GetInstance();
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
        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            BinderHelper.ValidateBindArgument(target, nameof(target));
            return BinderHelper.Bind(this, _binder, new [] { target }, null, null);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Dynamic;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal sealed class CSharpInvokeConstructorBinder : DynamicMetaObjectBinder, ICSharpInvokeOrInvokeMemberBinder
    {
        public BindingFlag BindingFlags => 0;

        public Expr DispatchPayload(RuntimeBinder runtimeBinder, ArgumentObject[] arguments, LocalVariableSymbol[] locals)
            => runtimeBinder.DispatchPayload(this, arguments, locals);

        public void PopulateSymbolTableWithName(SymbolTable symbolTable, Type callingType, ArgumentObject[] arguments)
            => RuntimeBinder.PopulateSymbolTableWithPayloadInformation(symbolTable, this, callingType, arguments);

        public bool IsBinderThatCanHaveRefReceiver => true;

        public CSharpCallFlags Flags { get; }

        public Type CallingContext { get; }

        public bool IsChecked => false;

        private readonly CSharpArgumentInfo[] _argumentInfo;

        CSharpArgumentInfo ICSharpBinder.GetArgumentInfo(int index) => _argumentInfo[index];

        public bool StaticCall => true;

        public Type[] TypeArguments => Array.Empty<Type>();

        public string Name => ".ctor";

        bool ICSharpInvokeOrInvokeMemberBinder.ResultDiscarded => false;

        private readonly RuntimeBinder _binder;

        public CSharpInvokeConstructorBinder(
            CSharpCallFlags flags,
            Type callingContext,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            Flags = flags;
            CallingContext = callingContext;
            _argumentInfo = BinderHelper.ToArray(argumentInfo);
            _binder = RuntimeBinder.GetInstance();
        }

        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            BinderHelper.ValidateBindArgument(target, nameof(target));
            BinderHelper.ValidateBindArgument(args, nameof(args));
            return BinderHelper.Bind(this, _binder, BinderHelper.Cons(target, args), _argumentInfo, null);
        }
    }
}

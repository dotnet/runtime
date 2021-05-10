// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Numerics.Hashing;
using Microsoft.CSharp.RuntimeBinder.Semantics;

namespace Microsoft.CSharp.RuntimeBinder
{
    internal sealed class CSharpInvokeConstructorBinder : DynamicMetaObjectBinder, ICSharpInvokeOrInvokeMemberBinder
    {
        public BindingFlag BindingFlags => 0;

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public Expr DispatchPayload(RuntimeBinder runtimeBinder, ArgumentObject[] arguments, LocalVariableSymbol[] locals)
            => runtimeBinder.DispatchPayload(this, arguments, locals);

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public void PopulateSymbolTableWithName(Type callingType, ArgumentObject[] arguments)
            => RuntimeBinder.PopulateSymbolTableWithPayloadInformation(this, callingType, arguments);

        public bool IsBinderThatCanHaveRefReceiver => true;

        public CSharpCallFlags Flags { get; }

        private readonly CSharpArgumentInfo[] _argumentInfo;

        CSharpArgumentInfo ICSharpBinder.GetArgumentInfo(int index) => _argumentInfo[index];

        public bool StaticCall => true;

        public Type[] TypeArguments => Type.EmptyTypes;

        public string Name => ".ctor";

        bool ICSharpInvokeOrInvokeMemberBinder.ResultDiscarded => false;

        private readonly RuntimeBinder _binder;

        private readonly Type _callingContext;

        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        public CSharpInvokeConstructorBinder(
            CSharpCallFlags flags,
            Type callingContext,
            IEnumerable<CSharpArgumentInfo> argumentInfo)
        {
            Flags = flags;
            _callingContext = callingContext;
            _argumentInfo = BinderHelper.ToArray(argumentInfo);
            _binder = new RuntimeBinder(callingContext);
        }

        public int GetGetBinderEquivalenceHash()
        {
            int hash = _callingContext?.GetHashCode() ?? 0;
            hash = HashHelpers.Combine(hash, (int)Flags);
            hash = HashHelpers.Combine(hash, Name.GetHashCode());

            hash = BinderHelper.AddArgHashes(hash, TypeArguments, _argumentInfo);

            return hash;
        }

        public bool IsEquivalentTo(ICSharpBinder other)
        {
            var otherBinder = other as CSharpInvokeConstructorBinder;
            if (otherBinder == null)
            {
                return false;
            }

            if (Flags != otherBinder.Flags ||
                _callingContext != otherBinder._callingContext ||
                Name != otherBinder.Name ||
                TypeArguments.Length != otherBinder.TypeArguments.Length ||
                _argumentInfo.Length != otherBinder._argumentInfo.Length)
            {
                return false;
            }

            return BinderHelper.CompareArgInfos(TypeArguments, otherBinder.TypeArguments, _argumentInfo, otherBinder._argumentInfo);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            BinderHelper.ValidateBindArgument(target, nameof(target));
            BinderHelper.ValidateBindArgument(args, nameof(args));
            return BinderHelper.Bind(this, _binder, BinderHelper.Cons(target, args), _argumentInfo, null);
        }
    }
}

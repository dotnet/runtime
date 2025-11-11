// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public partial class AsyncResumptionStub : ILStubMethod
    {
        private readonly MethodDesc _targetMethod;
        private readonly TypeDesc _owningType;
        private MethodSignature _signature;

        public AsyncResumptionStub(MethodDesc targetMethod, TypeDesc owningType)
        {
            Debug.Assert(targetMethod.IsAsyncCall());
            _targetMethod = targetMethod;
            _owningType = owningType;
        }

        public override ReadOnlySpan<byte> Name => _targetMethod.Name;
        public override string DiagnosticName => _targetMethod.DiagnosticName;

        public override TypeDesc OwningType => _owningType;

        public override MethodSignature Signature => _signature ??= InitializeSignature();

        public override TypeSystemContext Context => _targetMethod.Context;

        private MethodSignature InitializeSignature()
        {
            TypeDesc objectType = Context.GetWellKnownType(WellKnownType.Object);
            TypeDesc byrefByte = Context.GetWellKnownType(WellKnownType.Byte).MakeByRefType();
            return _signature = new MethodSignature(0, 0, objectType, [objectType, byrefByte]);
        }

        public override MethodIL EmitIL()
        {
            var emitter = new ILEmitter();
            ILCodeStream codeStream = emitter.NewCodeStream();

            // TODO: match getAsyncResumptionStub from CoreCLR VM
            codeStream.EmitCallThrowHelper(emitter, Context.GetHelperEntryPoint("ThrowHelpers"u8, "ThrowNotSupportedException"u8));

            return emitter.Link(this);
        }
    }
}

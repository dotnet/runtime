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
        private readonly MethodDesc _owningMethod;
        private MethodSignature _signature;

        public AsyncResumptionStub(MethodDesc owningMethod)
        {
            Debug.Assert(owningMethod.IsAsyncVariant()
                || (owningMethod.IsAsync && !owningMethod.Signature.ReturnsTaskOrValueTask()));
            _owningMethod = owningMethod;
        }

        public override ReadOnlySpan<byte> Name => _owningMethod.Name;
        public override string DiagnosticName => _owningMethod.DiagnosticName;

        public override TypeDesc OwningType => _owningMethod.OwningType;

        public override MethodSignature Signature => _signature ??= InitializeSignature();

        public override TypeSystemContext Context => _owningMethod.Context;

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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;
using ILLocalVariable = Internal.IL.Stubs.ILLocalVariable;

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

        public MethodDesc TargetMethod => _targetMethod;

        private MethodSignature InitializeSignature()
        {
            TypeDesc objectType = Context.GetWellKnownType(WellKnownType.Object);
            TypeDesc byrefByte = Context.GetWellKnownType(WellKnownType.Byte).MakeByRefType();
            return _signature = new MethodSignature(MethodSignatureFlags.Static, 0, objectType, [objectType, byrefByte]);
        }

        public override MethodIL EmitIL()
        {
            ILEmitter ilEmitter = new ILEmitter();
            ILCodeStream ilStream = ilEmitter.NewCodeStream();

            // Ported from jitinterface.cpp CEEJitInfo::getAsyncResumptionStub
            if (!_targetMethod.Signature.IsStatic)
            {
                if (_targetMethod.OwningType.IsValueType)
                {
                    ilStream.EmitLdc(0);
                    ilStream.Emit(ILOpcode.conv_u);
                }
                else
                {
                    ilStream.Emit(ILOpcode.ldnull);
                }
            }

            if (_targetMethod.RequiresInstArg())
            {
                ilStream.EmitLdc(0);
                ilStream.Emit(ILOpcode.conv_i);
                ilStream.Emit(ILOpcode.call, ilEmitter.NewToken(Context.GetCoreLibEntryPoint("System.Runtime.CompilerServices"u8, "RuntimeHelpers"u8, "SetNextCallGenericContext"u8, null)));
            }

            ilStream.EmitLdArg(0);
            ilStream.Emit(ILOpcode.call, ilEmitter.NewToken(Context.GetCoreLibEntryPoint("System.Runtime.CompilerServices"u8, "RuntimeHelpers"u8, "SetNextCallAsyncContinuation"u8, null)));

            foreach (var param in _targetMethod.Signature)
            {
                var local = ilEmitter.NewLocal(param);
                ilStream.EmitLdLoca(local);
                ilStream.Emit(ILOpcode.initobj, ilEmitter.NewToken(param));
                ilStream.EmitLdLoc(local);
            }

            ilStream.Emit(ILOpcode.call, ilEmitter.NewToken(_targetMethod));

            bool returnsVoid = _targetMethod.Signature.ReturnType.IsVoid;
            ILLocalVariable resultLocal = default;
            if (!returnsVoid)
            {
                resultLocal = ilEmitter.NewLocal(_targetMethod.Signature.ReturnType);
                ilStream.EmitStLoc(resultLocal);
            }

            MethodDesc asyncCallContinuation = Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "AsyncHelpers"u8)
                .GetKnownMethod("AsyncCallContinuation"u8, null);
            TypeDesc continuation = Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "Continuation"u8);
            var newContinuationLocal = ilEmitter.NewLocal(continuation);
            ilStream.Emit(ILOpcode.call, ilEmitter.NewToken(asyncCallContinuation));
            ilStream.EmitStLoc(newContinuationLocal);

            if (!returnsVoid)
            {
                var doneResult = ilEmitter.NewCodeLabel();
                ilStream.EmitLdLoc(newContinuationLocal);
                ilStream.Emit(ILOpcode.brtrue, doneResult);
                ilStream.EmitLdArg(1);
                ilStream.EmitLdLoc(resultLocal);
                ilStream.Emit(ILOpcode.stobj, ilEmitter.NewToken(_targetMethod.Signature.ReturnType));
                ilStream.EmitLabel(doneResult);
            }
            ilStream.EmitLdLoc(newContinuationLocal);
            ilStream.Emit(ILOpcode.ret);

            return ilEmitter.Link(this);
        }
    }
}

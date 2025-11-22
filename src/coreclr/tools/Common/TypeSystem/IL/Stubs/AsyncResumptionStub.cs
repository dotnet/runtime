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

            if (_targetMethod.RequiresInstArg())
            {
                MethodDesc setNextCallGenericContext = Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "RuntimeHelpers"u8)
                    .GetKnownMethod("SetNextCallGenericContext"u8, null);
                ilStream.EmitLdc(0);
                ilStream.Emit(ILOpcode.conv_i);
                ilStream.Emit(ILOpcode.call, ilEmitter.NewToken(setNextCallGenericContext));
            }

            MethodDesc setNextCallAsyncContinuation = Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "RuntimeHelpers"u8)
                .GetKnownMethod("SetNextCallAsyncContinuation"u8, null);
            ilStream.EmitLdArg(0);
            ilStream.Emit(ILOpcode.call, ilEmitter.NewToken(setNextCallAsyncContinuation));

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

            foreach (var param in _targetMethod.Signature)
            {
                var local = ilEmitter.NewLocal(param);
                ilStream.EmitLdLoca(local);
                ilStream.Emit(ILOpcode.initobj, ilEmitter.NewToken(param));
                ilStream.EmitLdLoc(local);
            }

            MethodDesc resumingMethod = new ExplicitContinuationAsyncMethod(_targetMethod);
            // TODO: Can be direct call?
            ilStream.Emit(ILOpcode.call, ilEmitter.NewToken(resumingMethod));

            bool returnsVoid = resumingMethod.Signature.ReturnType.IsVoid;
            ILLocalVariable resultLocal = default;
            if (!returnsVoid)
            {
                resultLocal = ilEmitter.NewLocal(resumingMethod.Signature.ReturnType);
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
                ilStream.Emit(ILOpcode.stobj, ilEmitter.NewToken(resumingMethod.Signature.ReturnType));
                ilStream.EmitLabel(doneResult);
            }
            ilStream.EmitLdLoc(newContinuationLocal);
            ilStream.Emit(ILOpcode.ret);

            return ilEmitter.Link(this);
        }
    }

    /// <summary>
    /// A dummy method used to tell the jit that we want to explicitly pass the hidden Continuation parameter
    /// as well as the generic context parameter (if any) for async resumption methods.
    /// This method should be marked HasInstantiation=false. This is the default
    /// for MethodDesc and so isn't explicitly set in the code below.
    /// </summary>
    internal sealed partial class ExplicitContinuationAsyncMethod : MethodDesc
    {
        private MethodSignature _signature;
        private readonly MethodDesc _wrappedMethod;
        private readonly MethodDesc _typicalDefinition;

        public ExplicitContinuationAsyncMethod(MethodDesc target)
        {
            _wrappedMethod = target;

            MethodDesc targetTypicalDefinition = target.GetTypicalMethodDefinition();
            if (targetTypicalDefinition != target)
                _typicalDefinition = new ExplicitContinuationAsyncMethod(targetTypicalDefinition);
            else
                _typicalDefinition = this;
        }

        public override bool IsAsync => true;

        public MethodDesc Target => _wrappedMethod;

        /// <summary>
        /// To explicitly pass the hidden parameters for async resumption methods,
        /// we need to add explicit Continuation and generic context parameters to the signature.
        /// </summary>
        private MethodSignature InitializeSignature()
        {
            var parameters = new TypeDesc[_wrappedMethod.Signature.Length];

            int i = 0;
            foreach (var param in _wrappedMethod.Signature)
            {
                parameters[i++] = param;
            }

            // Get the return type from the Task-returning variant
            TypeDesc returnType;
            if (_wrappedMethod is AsyncMethodVariant variant)
            {
                Debug.Assert(variant.Target.Signature.ReturnsTaskOrValueTask());
                if (variant.Target.Signature.ReturnType is { HasInstantiation: true } wrappedReturnType)
                {
                    returnType = wrappedReturnType.Instantiation[0];
                }
                else
                {
                    returnType = Context.GetWellKnownType(WellKnownType.Void);
                }
            }
            else
            {
                // AsyncExplicitImpl
                returnType = _wrappedMethod.Signature.ReturnType;
            }

            return _signature = new MethodSignature(
                _wrappedMethod.Signature.Flags,
                0,
                returnType,
                parameters);
        }

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName) => throw new NotImplementedException();

        public override MethodSignature Signature
        {
            get
            {
                if (_signature is null)
                    return InitializeSignature();

                return _signature;
            }
        }

        public override string DiagnosticName => $"ExplicitContinuationAsyncMethod({_wrappedMethod.DiagnosticName})";

        public override TypeDesc OwningType => _wrappedMethod.OwningType;

        public override TypeSystemContext Context => _wrappedMethod.Context;

        public override bool IsInternalCall => true;

        public override MethodDesc GetTypicalMethodDefinition() => _typicalDefinition;
    }

    public static class AsyncResumptionStubExtensions
    {
        public static bool IsExplicitContinuationAsyncMethod(this MethodDesc method)
        {
            return method is ExplicitContinuationAsyncMethod;
        }
        public static MethodDesc GetExplicitContinuationAsyncMethodTarget(this MethodDesc method)
        {
            return ((ExplicitContinuationAsyncMethod)method).Target;
        }
    }
}

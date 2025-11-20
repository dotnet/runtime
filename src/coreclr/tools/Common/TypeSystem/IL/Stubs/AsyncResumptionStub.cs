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
            return _signature = new MethodSignature(0, 0, objectType, [objectType, byrefByte]);
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

            if (Context.Target.Architecture != TargetArchitecture.X86)
            {
                if (_targetMethod.RequiresInstArg())
                {
                    ilStream.EmitLdc(0);
                    ilStream.Emit(ILOpcode.conv_i);
                }
                ilStream.EmitLdArg(0);
            }

            foreach (var param in _targetMethod.Signature)
            {
                var local = ilEmitter.NewLocal(param);
                ilStream.EmitLdLoca(local);
                ilStream.Emit(ILOpcode.initobj, ilEmitter.NewToken(param));
                ilStream.EmitLdLoc(local);
            }

            if (Context.Target.Architecture == TargetArchitecture.X86)
            {
                ilStream.EmitLdArg(0);
                if (_targetMethod.RequiresInstArg())
                {
                    ilStream.EmitLdc(0);
                    ilStream.Emit(ILOpcode.conv_i);
                }
            }

            MethodDesc resumingMethod = new ExplicitContinuationAsyncMethod(_targetMethod);
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
    /// This method should be marked IsAsync=false and HasInstantiation=false. These are defaults
    /// for MethodDesc and so aren't explicitly set in the code below.
    /// </summary>
    internal sealed partial class ExplicitContinuationAsyncMethod : MethodDesc
    {
        private MethodSignature _signature;
        private MethodDesc _wrappedMethod;

        public ExplicitContinuationAsyncMethod(MethodDesc target)
        {
            _wrappedMethod = target;
        }

        public MethodDesc Target => _wrappedMethod;

        /// <summary>
        /// To explicitly pass the hidden parameters for async resumption methods,
        /// we need to add explicit Continuation and generic context parameters to the signature.
        /// </summary>
        private MethodSignature InitializeSignature()
        {
            // Async methods have an implicit Continuation parameter
            // The order of parameters depends on the architecture
            // non-x86: this?, genericCtx?, continuation, params...
            // x86: this?, params, continuation, genericCtx?
            // To make the jit pass arguments in this order, we can add the continuation parameter
            // at the end for x86 and at the beginning for other architectures.
            // The 'this' parameter and generic context parameter (if any) can be handled by the jit.

            var parameters = new TypeDesc[_wrappedMethod.Signature.Length + 1 + (_wrappedMethod.RequiresInstArg() ? 1 : 0)];

            TypeDesc continuation = Context.SystemModule.GetKnownType("System.Runtime.CompilerServices"u8, "Continuation"u8);
            if (Context.Target.Architecture == TargetArchitecture.X86)
            {
                int i = 0;
                for (; i < _wrappedMethod.Signature.Length; i++)
                {
                    parameters[i] = _wrappedMethod.Signature[i];
                }
                parameters[i++] = continuation;
                if (_wrappedMethod.RequiresInstArg())
                {
                    parameters[i] = Context.GetWellKnownType(WellKnownType.Void).MakePointerType();
                }
            }
            else
            {
                int i = 0;
                if (_wrappedMethod.RequiresInstArg())
                {
                    parameters[i++] = Context.GetWellKnownType(WellKnownType.Void).MakePointerType();
                }
                parameters[i++] = continuation;
                foreach (var param in _wrappedMethod.Signature)
                {
                    parameters[i++] = param;
                }
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

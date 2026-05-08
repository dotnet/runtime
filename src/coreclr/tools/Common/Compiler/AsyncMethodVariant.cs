// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler
{
    /// <summary>
    /// MethodDesc that represents async calling convention entrypoint of a Task-returning method.
    /// </summary>
    public partial class AsyncMethodVariant : MethodDelegator
    {
        private MethodSignature _asyncSignature;

        public AsyncMethodVariant(EcmaMethod wrappedMethod)
            : base(wrappedMethod)
        {
            Debug.Assert(wrappedMethod.Signature.ReturnsTaskOrValueTask());
        }

        public EcmaMethod Target => (EcmaMethod)_wrappedMethod;

        public override MethodSignature Signature
        {
            get
            {
                return _asyncSignature ?? InitializeSignature();
            }
        }

        private MethodSignature InitializeSignature()
        {
            var signature = _wrappedMethod.Signature;
            Debug.Assert(signature.ReturnsTaskOrValueTask());
            TypeDesc md = signature.ReturnType;
            MethodSignatureBuilder builder = new MethodSignatureBuilder(signature);
            builder.ReturnType = md.HasInstantiation ? md.Instantiation[0] : this.Context.GetWellKnownType(WellKnownType.Void);
            return (_asyncSignature = builder.ToSignature());
        }

        public override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            return this;
        }

        public override MethodDesc GetMethodDefinition()
        {
            return this;
        }

        public override MethodDesc GetTypicalMethodDefinition()
        {
            return this;
        }

        public override MethodDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            return this;
        }

        public override string ToString() => $"Async variant: " + _wrappedMethod.ToString();

        protected override int ClassCode => unchecked((int)0xd0fd1c1fu);

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var asyncOther = (AsyncMethodVariant)other;
            return comparer.Compare(_wrappedMethod, asyncOther._wrappedMethod);
        }
    }

    /// <summary>
    /// A special void-returning async variant that calls the T returning async variant and drops the result.
    /// Used when a base class method returns Task and the derived class overrides it with Task&lt;T&gt;.
    /// The base's async variant is void-returning, while the derived's async variant is T-returning.
    /// This thunk bridges the mismatch.
    /// </summary>
    public partial class ReturnDroppingAsyncThunk : MethodDelegator
    {
        private readonly AsyncMethodVariant _asyncVariant;
        private MethodSignature _voidSignature;

        public ReturnDroppingAsyncThunk(AsyncMethodVariant asyncVariant)
            : base(asyncVariant)
        {
            Debug.Assert(!asyncVariant.Signature.ReturnType.IsVoid);
            _asyncVariant = asyncVariant;
        }

        public AsyncMethodVariant AsyncVariantTarget => _asyncVariant;

        public override MethodSignature Signature
        {
            get
            {
                return _voidSignature ?? InitializeSignature();
            }
        }

        private MethodSignature InitializeSignature()
        {
            MethodSignatureBuilder builder = new MethodSignatureBuilder(_asyncVariant.Signature);
            builder.ReturnType = Context.GetWellKnownType(WellKnownType.Void);
            return (_voidSignature = builder.ToSignature());
        }

        public override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            return this;
        }

        public override MethodDesc GetMethodDefinition()
        {
            return this;
        }

        public override MethodDesc GetTypicalMethodDefinition()
        {
            return this;
        }

        public override MethodDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            return this;
        }

        public override string ToString() => $"Return-dropping async variant: " + _asyncVariant.Target.ToString();

        protected override int ClassCode => unchecked((int)0xa3c2b7e5u);

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var rdOther = (ReturnDroppingAsyncThunk)other;
            return comparer.Compare(_asyncVariant, rdOther._asyncVariant);
        }
    }

    public static class AsyncMethodVariantExtensions
    {
        public static bool IsAsyncVariant(this MethodDesc method)
        {
            return method.GetTypicalMethodDefinition() is AsyncMethodVariant;
        }

        public static bool IsReturnDroppingAsyncThunk(this MethodDesc method)
        {
            return method.GetTypicalMethodDefinition() is ReturnDroppingAsyncThunk;
        }

        public static bool IsAsyncThunk(this MethodDesc method)
        {
            return (method.IsAsyncVariant() ^ method.IsAsync) || method.IsReturnDroppingAsyncThunk();
        }

        public static bool IsCompilerGeneratedILBodyForAsync(this MethodDesc method)
        {
            return method.IsAsyncThunk() || method is AsyncResumptionStub;
        }

        public static MethodDesc GetAsyncVariant(this MethodDesc method)
        {
            Debug.Assert(!method.IsAsyncVariant());
            return ((CompilerTypeSystemContext)method.Context).GetAsyncVariantMethod(method);
        }

        public static MethodDesc GetTargetOfAsyncVariant(this MethodDesc method)
        {
            Debug.Assert(method.IsAsyncVariant());
            return ((CompilerTypeSystemContext)method.Context).GetTargetOfAsyncVariantMethod(method);
        }
    }
}

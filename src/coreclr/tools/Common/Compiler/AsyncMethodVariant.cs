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

    public static class AsyncMethodVariantExtensions
    {
        public static bool IsAsyncVariant(this MethodDesc method)
        {
            return method.GetTypicalMethodDefinition() is AsyncMethodVariant;
        }

        public static bool IsAsyncThunk(this MethodDesc method)
        {
            return method.IsAsyncVariant() ^ method.IsAsync;
        }
    }
}

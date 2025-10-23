// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.TypeSystem;

namespace Internal.JitInterface
{
    /// <summary>
    /// Represents the async-callable (CORINFO_CALLCONV_ASYNCCALL) variant of a Task/ValueTask returning method.
    /// The wrapper should be short‑lived and only used while interacting with the JIT interface.
    /// </summary>
    internal sealed class AsyncMethodDesc : MethodDelegator, IJitHashableOnly
    {
        private readonly AsyncMethodDescFactory _factory;
        private readonly int _jitVisibleHashCode;

        public MethodDesc Target => _wrappedMethod;

        public AsyncMethodDesc(MethodDesc wrappedMethod, AsyncMethodDescFactory factory)
            : base(wrappedMethod)
        {
            Debug.Assert(wrappedMethod.IsTaskReturning());
            _factory = factory;
            // Salt with arbitrary constant so hash space differs from underlying method.
            _jitVisibleHashCode = HashCode.Combine(wrappedMethod.GetHashCode(), 0x51C0A54);
        }

        public override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            MethodDesc realCanonTarget = _wrappedMethod.GetCanonMethodTarget(kind);
            if (realCanonTarget != _wrappedMethod)
                return _factory.GetAsyncMethod(realCanonTarget);
            return this;
        }

        public override MethodDesc GetMethodDefinition()
        {
            MethodDesc real = _wrappedMethod.GetMethodDefinition();
            if (real != _wrappedMethod)
                return _factory.GetAsyncMethod(real);
            return this;
        }

        public override MethodDesc GetTypicalMethodDefinition()
        {
            MethodDesc real = _wrappedMethod.GetTypicalMethodDefinition();
            if (real != _wrappedMethod)
                return _factory.GetAsyncMethod(real);
            return this;
        }

        public override MethodDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            MethodDesc real = _wrappedMethod.InstantiateSignature(typeInstantiation, methodInstantiation);
            if (real != _wrappedMethod)
                return _factory.GetAsyncMethod(real);
            return this;
        }

        public override MethodSignature Signature
        {
            get
            {
                MethodSignature wrappedSignature = _wrappedMethod.Signature;
                MetadataType md = (MetadataType)wrappedSignature.ReturnType;
                MethodSignatureBuilder builder = new MethodSignatureBuilder(wrappedSignature);
                builder.ReturnType = md.HasInstantiation ? md.Instantiation[0] : this.Context.GetWellKnownType(WellKnownType.Void);
                builder.Flags = wrappedSignature.Flags | MethodSignatureFlags.AsyncCallConv;
                return builder.ToSignature();
            }
        }

#if !SUPPORT_JIT
        // Same pattern as UnboxingMethodDesc: these should not escape JIT hashing scope.
        protected override int ClassCode => throw new NotImplementedException();
        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer) => throw new NotImplementedException();
        protected override int ComputeHashCode() => _jitVisibleHashCode;
        int IJitHashableOnly.GetJitVisibleHashCode() => _jitVisibleHashCode;
#else
        int IJitHashableOnly.GetJitVisibleHashCode() => _jitVisibleHashCode;
#endif
    }

    internal static class AsyncMethodDescExtensions
    {
        /// <summary>
        /// Returns true if the method returns Task, Task&lt;T&gt;, ValueTask, or ValueTask&lt;T&gt;, otherwise false.
        /// </summary>
        public static bool IsTaskReturning(this MethodDesc method)
        {
            TypeDesc ret = method.GetTypicalMethodDefinition().Signature.ReturnType;

            if (ret is MetadataType md
                && md.Module == method.Context.SystemModule
                && md.Namespace.SequenceEqual("System.Threading.Tasks"u8))
            {
                ReadOnlySpan<byte> name = md.Name;
                if (name.SequenceEqual("Task"u8) || name.SequenceEqual("Task`1"u8)
                    || name.SequenceEqual("ValueTask"u8) || name.SequenceEqual("ValueTask`1"u8))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.JitInterface;


namespace Internal.TypeSystem
{
    /// <summary>
    /// Either the AsyncMethodImplVariant or AsyncMethodThunkVariant of a method marked .IsAsync.
    /// </summary>
    public partial class AsyncMethodVariant : MethodDelegator, IJitHashableOnly
    {
        private readonly AsyncMethodVariantFactory _factory;
        private readonly AsyncMethodKind _asyncMethodKind;
        private readonly int _jitVisibleHashCode;
        private MethodSignature _asyncSignature;

        public AsyncMethodVariant(MethodDesc wrappedMethod, AsyncMethodVariantFactory factory, AsyncMethodKind kind)
            : base(wrappedMethod)
        {
            Debug.Assert(wrappedMethod.IsTaskReturning);
            Debug.Assert(kind switch
            {
                AsyncMethodKind.AsyncVariantThunk => !wrappedMethod.IsAsync,
                AsyncMethodKind.AsyncVariantImpl => wrappedMethod.IsAsync,
                _ => false,
            });
            _factory = factory;
            _asyncMethodKind = kind;
            _jitVisibleHashCode = HashCode.Combine(wrappedMethod.GetHashCode(), 0x310bb74f);
        }

        public MethodDesc Target => _wrappedMethod;

        public override AsyncMethodKind AsyncMethodKind => _asyncMethodKind;

        public override MethodSignature Signature
        {
            get
            {
                return _asyncSignature ??= _wrappedMethod.Signature.CreateAsyncSignature();
            }
        }

        public override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            return _factory.GetOrCreateAsyncMethodImplVariant(_wrappedMethod.GetCanonMethodTarget(kind), _asyncMethodKind);
        }

        public override MethodDesc GetMethodDefinition()
        {
            return _wrappedMethod.GetMethodDefinition();
        }

        public override MethodDesc GetTypicalMethodDefinition()
        {
            return _wrappedMethod.GetTypicalMethodDefinition();
        }

        public override MethodDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            var real = _wrappedMethod.InstantiateSignature(typeInstantiation, methodInstantiation);
            if (real == _wrappedMethod)
                return this;
            return _factory.GetOrCreateAsyncMethodImplVariant(real, _asyncMethodKind);
        }

        public override string ToString() => $"Async variant ({_asyncMethodKind}): " + _wrappedMethod.ToString();

        protected override int ClassCode => throw new NotImplementedException();

        protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            throw new NotImplementedException();
        }

        protected override int ComputeHashCode()
        {
            throw new NotSupportedException("This method may not be stored as it is expected to only be used transiently in the JIT");
        }

        int IJitHashableOnly.GetJitVisibleHashCode() => _jitVisibleHashCode;
    }

    public sealed class AsyncMethodVariantFactory : Dictionary<(MethodDesc, AsyncMethodKind), AsyncMethodVariant>
    {
        public AsyncMethodVariant GetOrCreateAsyncMethodImplVariant(MethodDesc wrappedMethod, AsyncMethodKind kind)
        {
            Debug.Assert(wrappedMethod.IsAsync);
            if (!TryGetValue((wrappedMethod, kind), out AsyncMethodVariant variant))
            {
                variant = new AsyncMethodVariant(wrappedMethod, this, kind);
                this[(wrappedMethod, kind)] = variant;
            }
            return variant;
        }

        public AsyncMethodVariant GetOrCreateAsyncThunk(MethodDesc wrappedMethod)
        {
            return GetOrCreateAsyncMethodImplVariant(wrappedMethod, AsyncMethodKind.AsyncVariantThunk);
        }

        public AsyncMethodVariant GetOrCreateAsyncImpl(MethodDesc wrappedMethod)
        {
            return GetOrCreateAsyncMethodImplVariant(wrappedMethod, AsyncMethodKind.AsyncVariantImpl);
        }
    }

    public static class AsyncMethodVariantExtensions
    {
        /// <summary>
        /// Returns true if this MethodDesc is an AsyncMethodVariant, which should not escape the jit interface.
        /// </summary>
        public static bool IsAsyncVariant(this MethodDesc method)
        {
            return method is AsyncMethodVariant;
        }

        /// <summary>
        /// Gets the wrapped method of the AsyncMethodVariant. This method is Task-returning.
        /// </summary>
        public static MethodDesc GetAsyncVariantDefinition(this MethodDesc method)
        {
            return ((AsyncMethodVariant)method).Target;
        }
    }
}

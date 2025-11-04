// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.TypeSystem.Ecma;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Either the AsyncMethodImplVariant or AsyncMethodThunkVariant of a method marked .IsAsync.
    /// </summary>
    public partial class AsyncMethodVariant : MethodDelegator
    {
        private readonly int _jitVisibleHashCode;
        private MethodSignature _asyncSignature;

        public AsyncMethodVariant(MethodDesc wrappedMethod)
            : base(wrappedMethod)
        {
            Debug.Assert(wrappedMethod.IsTaskReturning);
            _jitVisibleHashCode = HashCode.Combine(wrappedMethod.GetHashCode(), 0x310bb74f);
        }

        public MethodDesc Target => _wrappedMethod;

        public override AsyncMethodKind AsyncMethodKind => _wrappedMethod.IsAsync ? AsyncMethodKind.AsyncVariantImpl : AsyncMethodKind.AsyncVariantThunk;

        public override MethodSignature Signature
        {
            get
            {
                return _asyncSignature ??= _wrappedMethod.Signature.CreateAsyncSignature();
            }
        }

        public override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            return this;
        }

        public override MethodDesc GetMethodDefinition()
        {
            var real = _wrappedMethod.GetMethodDefinition();
            if (real == _wrappedMethod)
                return this;

            return _wrappedMethod.Context.GetAsyncVariant(real);
        }

        public override MethodDesc GetTypicalMethodDefinition()
        {
            var real = _wrappedMethod.GetTypicalMethodDefinition();
            if (real == _wrappedMethod)
                return this;

            return _wrappedMethod.Context.GetAsyncVariant(real);
        }

        public override MethodDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            var real = _wrappedMethod.InstantiateSignature(typeInstantiation, methodInstantiation);
            if (real == _wrappedMethod)
                return this;

            return _wrappedMethod.Context.GetAsyncVariant(real);
        }

        public override string ToString() => $"Async variant ({AsyncMethodKind}): " + _wrappedMethod.ToString();

        protected internal override int ClassCode => unchecked((int)0xd0fd1c1fu);

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            var asyncOther = (AsyncMethodVariant)other;
            return comparer.Compare(this._wrappedMethod, asyncOther._wrappedMethod);
        }

        protected override int ComputeHashCode()
        {
            return _jitVisibleHashCode;
        }
    }

    public sealed class AsyncMethodVariantFactory : ConcurrentDictionary<MethodDesc, AsyncMethodVariant>
    {
        public AsyncMethodVariant GetOrCreateAsyncMethodImplVariant(MethodDesc wrappedMethod)
        {
            return GetOrAdd(wrappedMethod, static (x) => new AsyncMethodVariant(x));
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Internal.TypeSystem;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents the async-callable (CORINFO_CALLCONV_ASYNCCALL) variant of a Task/ValueTask returning method.
    /// </summary>
    public sealed class AsyncMethodThunk : MethodDelegator
    {
        private readonly AsyncMethodData _asyncMethodData;

        public AsyncMethodThunk(MethodDesc wrappedMethod)
            : base(wrappedMethod)
        {
            Debug.Assert(wrappedMethod.IsTaskReturning);
            Debug.Assert(!wrappedMethod.IsAsync);
            _asyncMethodData = new AsyncMethodData()
            {
                Kind = AsyncMethodKind.AsyncVariantThunk,
                Signature = _wrappedMethod.Signature.CreateAsyncSignature()
            };
        }

        public override AsyncMethodData AsyncMethodData
        {
            get
            {
                return _asyncMethodData;
            }
        }

        public override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            return _wrappedMethod.GetCanonMethodTarget(kind).GetAsyncOtherVariant();
        }

        public override MethodDesc GetMethodDefinition()
        {
            return _wrappedMethod.GetMethodDefinition();
        }

        public override MethodDesc GetTypicalMethodDefinition()
        {
            return _wrappedMethod.GetTypicalMethodDefinition();
        }

        public override MethodDesc GetAsyncOtherVariant() => _wrappedMethod;

        public override MethodDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            MethodDesc real = _wrappedMethod.InstantiateSignature(typeInstantiation, methodInstantiation);
            return real.GetAsyncOtherVariant();
        }

        public override MethodSignature Signature
        {
            get
            {
                return _asyncMethodData.Signature;
            }
        }

        public override string DiagnosticName => "Async thunk: " + _wrappedMethod.DiagnosticName;

        protected internal override int ClassCode => 0x554d08b9;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            if (other is AsyncMethodThunk otherAsync)
            {
                return comparer.Compare(_wrappedMethod, otherAsync._wrappedMethod);
            }
            return -1;
        }

        public override string ToString()
        {
            return "Async thunk: " + _wrappedMethod.ToString();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents the async-callable (CORINFO_CALLCONV_ASYNCCALL) variant of a Task/ValueTask returning method.
    /// The wrapper should be short‑lived and only used while interacting with the JIT interface.
    /// NOPE: These things aren't short lived in R2R scenarios. Please make a normal method, and give them normal, long lifetimes
    /// </summary>
    public sealed class TaskReturningAsyncThunk : MethodDelegator
    {
        private readonly AsyncMethodData _asyncMethodData;

        public TaskReturningAsyncThunk(MethodDesc asyncMethodImplVariant) : base(asyncMethodImplVariant)
        {
            Debug.Assert(asyncMethodImplVariant.IsTaskReturning);
            MethodSignature signature;
            if (asyncMethodImplVariant.HasInstantiation)
            {
                signature = ((EcmaMethod)asyncMethodImplVariant.GetTypicalMethodDefinition()).MetadataSignature.ApplySubstitution(asyncMethodImplVariant.Instantiation);
            }
            else
            {
                signature = ((EcmaMethod)asyncMethodImplVariant).MetadataSignature;
            }
            _asyncMethodData = new() { Kind = AsyncMethodKind.RuntimeAsync, Signature = signature };
        }

        public override MethodDesc GetAsyncOtherVariant() => _wrappedMethod;

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

        public override MethodDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            MethodDesc real = _wrappedMethod.InstantiateSignature(typeInstantiation, methodInstantiation);
            if (real != _wrappedMethod)
                return real.GetAsyncOtherVariant();
            return this;
        }

        public override MethodSignature Signature
        {
            get
            {
                return _asyncMethodData.Signature;
            }
        }

        protected internal override int ClassCode => 0x554d08b9;

        public override string DiagnosticName => "TaskReturningVariant: " + _wrappedMethod.DiagnosticName;

        protected internal override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer)
        {
            if (other is TaskReturningAsyncThunk otherAsync)
            {
                return comparer.Compare(_wrappedMethod, otherAsync._wrappedMethod);
            }
            return -1;
        }

        public override string ToString()
        {
            return "Task returning thunk: " + _wrappedMethod.ToString();
        }
    }
}

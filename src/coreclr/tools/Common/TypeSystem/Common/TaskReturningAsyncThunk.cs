// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents the Task-returning variant of an async call convention method.
    /// </summary>
    public sealed partial class TaskReturningAsyncThunk : MethodDelegator
    {
        private readonly AsyncMethodData _asyncMethodData;

        public TaskReturningAsyncThunk(MethodDesc asyncMethodImplVariant, MethodSignature signature) : base(asyncMethodImplVariant)
        {
            Debug.Assert(asyncMethodImplVariant.IsAsync);
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

        public override string ToString()
        {
            return "Task returning thunk: " + _wrappedMethod.ToString();
        }
    }
}

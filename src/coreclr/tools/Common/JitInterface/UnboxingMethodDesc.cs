// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using Internal.TypeSystem;

namespace Internal.JitInterface
{
    /// <summary>
    /// Represents the unboxing entrypoint of a valuetype instance method.
    /// This class is for internal purposes within the JitInterface. It's not expected
    /// for it to escape the JitInterface.
    /// </summary>
    internal sealed class UnboxingMethodDesc : MethodDelegator, IJitHashableOnly
    {
        private readonly UnboxingMethodDescFactory _factory;
        private readonly int _jitVisibleHashCode;

        public MethodDesc Target => _wrappedMethod;

        public UnboxingMethodDesc(MethodDesc wrappedMethod, UnboxingMethodDescFactory factory)
            : base(wrappedMethod)
        {
            Debug.Assert(wrappedMethod.OwningType.IsValueType);
            Debug.Assert(!wrappedMethod.Signature.IsStatic);
            _factory = factory;
            _jitVisibleHashCode = HashCode.Combine(wrappedMethod.GetHashCode(), 401752602);
        }

        public override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            MethodDesc realCanonTarget = _wrappedMethod.GetCanonMethodTarget(kind);
            if (realCanonTarget != _wrappedMethod)
                return _factory.GetUnboxingMethod(realCanonTarget);

            return this;
        }

        public override MethodDesc GetMethodDefinition()
        {
            MethodDesc realMethodDefinition = _wrappedMethod.GetMethodDefinition();
            if (realMethodDefinition != _wrappedMethod)
                return _factory.GetUnboxingMethod(realMethodDefinition);

            return this;
        }

        public override MethodDesc GetTypicalMethodDefinition()
        {
            MethodDesc realTypicalMethodDefinition = _wrappedMethod.GetTypicalMethodDefinition();
            if (realTypicalMethodDefinition != _wrappedMethod)
                return _factory.GetUnboxingMethod(realTypicalMethodDefinition);

            return this;
        }

        public override MethodDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            MethodDesc realInstantiateSignature = _wrappedMethod.InstantiateSignature(typeInstantiation, methodInstantiation);
            if (realInstantiateSignature != _wrappedMethod)
                return _factory.GetUnboxingMethod(realInstantiateSignature);

            return this;
        }

        public override string ToString()
        {
            return "Unboxing MethodDesc: " + _wrappedMethod.ToString();
        }

#if !SUPPORT_JIT
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
#endif
    }

    internal static class UnboxingMethodDescExtensions
    {
        public static bool IsUnboxingThunk(this MethodDesc method)
        {
            return method is UnboxingMethodDesc;
        }

        public static MethodDesc GetUnboxedMethod(this MethodDesc method)
        {
            return ((UnboxingMethodDesc)method).Target;
        }
    }
}

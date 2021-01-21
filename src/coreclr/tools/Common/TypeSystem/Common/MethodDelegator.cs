// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    /// <summary>
    /// Wraps a <see cref="MethodDesc"/> object and delegates methods to that <see cref="MethodDesc"/>.
    /// </summary>
    public abstract partial class MethodDelegator : MethodDesc
    {
        protected readonly MethodDesc _wrappedMethod;

        public MethodDelegator(MethodDesc wrappedMethod)
        {
            _wrappedMethod = wrappedMethod;
        }

        public override TypeSystemContext Context => _wrappedMethod.Context;

        public override TypeDesc OwningType => _wrappedMethod.OwningType;

        public override MethodSignature Signature => _wrappedMethod.Signature;

        public override Instantiation Instantiation => _wrappedMethod.Instantiation;

        public override bool IsDefaultConstructor => _wrappedMethod.IsDefaultConstructor;

        public override string Name => _wrappedMethod.Name;

        public override bool IsVirtual => _wrappedMethod.IsVirtual;

        public override bool IsNewSlot => _wrappedMethod.IsNewSlot;

        public override bool IsAbstract => _wrappedMethod.IsAbstract;

        public override bool IsFinal => _wrappedMethod.IsFinal;

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return _wrappedMethod.HasCustomAttribute(attributeNamespace, attributeName);
        }

        // For this method, delegating to the wrapped MethodDesc would likely be the wrong thing.
        public abstract override MethodDesc GetMethodDefinition();

        // For this method, delegating to the wrapped MethodDesc would likely be the wrong thing.
        public abstract override MethodDesc GetTypicalMethodDefinition();

        // For this method, delegating to the wrapped MethodDesc would likely be the wrong thing.
        public abstract override MethodDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation);
    }
}

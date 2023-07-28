// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Internal.TypeSystem
{
    public sealed partial class MethodForRuntimeDeterminedType : MethodDesc
    {
        private MethodDesc _typicalMethodDef;
        private RuntimeDeterminedType _rdType;

        internal MethodForRuntimeDeterminedType(MethodDesc typicalMethodDef, RuntimeDeterminedType rdType)
        {
            Debug.Assert(typicalMethodDef.IsTypicalMethodDefinition);

            _typicalMethodDef = typicalMethodDef;
            _rdType = rdType;
        }

        // This constructor is a performance optimization - it allows supplying the hash code if it has already
        // been computed prior to the allocation of this type. The supplied hash code still has to match the
        // hash code this type would compute on it's own (and we assert to enforce that).
        internal MethodForRuntimeDeterminedType(MethodDesc typicalMethodDef, RuntimeDeterminedType rdType, int hashcode)
            : this(typicalMethodDef, rdType)
        {
            SetHashCode(hashcode);
        }

        public override TypeSystemContext Context => _typicalMethodDef.Context;
        public override TypeDesc OwningType => _rdType;
        public override MethodSignature Signature => _typicalMethodDef.Signature;
        public override bool IsVirtual => _typicalMethodDef.IsVirtual;
        public override bool IsNewSlot => _typicalMethodDef.IsNewSlot;
        public override bool IsAbstract => _typicalMethodDef.IsAbstract;
        public override bool IsFinal => _typicalMethodDef.IsFinal;
        public override bool IsDefaultConstructor => _typicalMethodDef.IsDefaultConstructor;
        public override string Name => _typicalMethodDef.Name;
        public override MethodDesc GetTypicalMethodDefinition() => _typicalMethodDef;
        public override Instantiation Instantiation => _typicalMethodDef.Instantiation;

        public override bool HasCustomAttribute(string attributeNamespace, string attributeName)
        {
            return _typicalMethodDef.HasCustomAttribute(attributeNamespace, attributeName);
        }

        public override bool IsCanonicalMethod(CanonicalFormKind policy)
        {
            // Owning type is a RuntimeDeterminedType, so it can never be canonical.
            // Instantiation for the method can also never be canonical since it's a typical method definition.
            return false;
        }

        public override MethodDesc GetCanonMethodTarget(CanonicalFormKind kind)
        {
            TypeDesc canonicalizedTypeOfTargetMethod = _rdType.CanonicalType.ConvertToCanonForm(kind);
            return Context.GetMethodForInstantiatedType(_typicalMethodDef, (InstantiatedType)canonicalizedTypeOfTargetMethod);
        }
    }
}

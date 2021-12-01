// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents an unmanaged pointer type.
    /// </summary>
    public sealed partial class PointerType : ParameterizedType
    {
        internal PointerType(TypeDesc parameterType)
            : base(parameterType)
        {
        }

        public override int GetHashCode()
        {
            return Internal.NativeFormat.TypeHashingAlgorithms.ComputePointerTypeHashCode(this.ParameterType.GetHashCode());
        }

        public override TypeDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            TypeDesc parameterType = this.ParameterType;
            TypeDesc instantiatedParameterType = parameterType.InstantiateSignature(typeInstantiation, methodInstantiation);
            if (instantiatedParameterType != parameterType)
                return Context.GetPointerType(instantiatedParameterType);

            return this;
        }

        protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = TypeFlags.Pointer;

            flags |= TypeFlags.HasGenericVarianceComputed;
            flags |= TypeFlags.HasFinalizerComputed;
            flags |= TypeFlags.AttributeCacheComputed;

            return flags;
        }
    }
}

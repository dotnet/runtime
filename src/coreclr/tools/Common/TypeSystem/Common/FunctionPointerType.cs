// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.TypeSystem
{
    /// <summary>
    /// Represents an unmanaged pointer to a method with a signature compatible with the signature of the pointer.
    /// </summary>
    public sealed partial class FunctionPointerType : TypeDesc
    {
        private MethodSignature _signature;
        private int _hashCode;

        internal FunctionPointerType(MethodSignature signature)
        {
            _signature = signature;
        }

        /// <summary>
        /// Gets the signature of the method this pointer points to.
        /// </summary>
        public MethodSignature Signature
        {
            get
            {
                return _signature;
            }
        }

        public override TypeSystemContext Context
        {
            get
            {
                return _signature.ReturnType.Context;
            }
        }

        public override int GetHashCode()
        {
            if (_hashCode == 0)
                _hashCode = _signature.GetHashCode();
            return _hashCode;
        }

        public override TypeDesc InstantiateSignature(Instantiation typeInstantiation, Instantiation methodInstantiation)
        {
            MethodSignatureBuilder sigBuilder = new MethodSignatureBuilder(_signature);
            sigBuilder.ReturnType = _signature.ReturnType.InstantiateSignature(typeInstantiation, methodInstantiation);
            for (int i = 0; i < _signature.Length; i++)
                sigBuilder[i] = _signature[i].InstantiateSignature(typeInstantiation, methodInstantiation);

            MethodSignature instantiatedSignature = sigBuilder.ToSignature();
            if (instantiatedSignature != _signature)
                return Context.GetFunctionPointerType(instantiatedSignature);

            return this;
        }

        protected override TypeFlags ComputeTypeFlags(TypeFlags mask)
        {
            TypeFlags flags = TypeFlags.FunctionPointer;

            flags |= TypeFlags.HasGenericVarianceComputed;

            flags |= TypeFlags.HasFinalizerComputed;

            flags |= TypeFlags.AttributeCacheComputed;

            return flags;
        }
    }
}

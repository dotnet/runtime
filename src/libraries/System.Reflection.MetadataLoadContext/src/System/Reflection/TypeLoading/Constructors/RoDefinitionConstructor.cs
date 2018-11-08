// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    /// <summary>
    /// Class for all RoConstructor objects created by a MetadataLoadContext that has a MethodDef token associated with it.
    /// </summary>
    internal sealed class RoDefinitionConstructor<TMethodDecoder> : RoConstructor where TMethodDecoder : IMethodDecoder
    {
        private readonly RoInstantiationProviderType _declaringType;
        private readonly TMethodDecoder _decoder;

        internal RoDefinitionConstructor(RoInstantiationProviderType declaringType, TMethodDecoder decoder) 
            : base()
        {
            Debug.Assert(declaringType != null);
            _declaringType = declaringType;
            _decoder = decoder;
        }

        internal sealed override RoType GetRoDeclaringType() => _declaringType;
        internal sealed override RoModule GetRoModule() => _decoder.GetRoModule();
        protected sealed override string ComputeName() => _decoder.ComputeName();
        public sealed override int MetadataToken => _decoder.MetadataToken;
        public sealed override IEnumerable<CustomAttributeData> CustomAttributes => _decoder.ComputeTrueCustomAttributes();
        protected sealed override MethodAttributes ComputeAttributes() => _decoder.ComputeAttributes();
        protected sealed override CallingConventions ComputeCallingConvention() => _decoder.ComputeCallingConvention();
        protected sealed override MethodImplAttributes ComputeMethodImplementationFlags() => _decoder.ComputeMethodImplementationFlags();
        protected sealed override MethodSig<RoParameter> ComputeMethodSig() => _decoder.SpecializeMethodSig(this);
        public sealed override MethodBody GetMethodBody() => _decoder.SpecializeMethodBody(this);
        protected sealed override MethodSig<string> ComputeMethodSigStrings() => _decoder.SpecializeMethodSigStrings(TypeContext);
        protected sealed override MethodSig<RoType> ComputeCustomModifiers() => _decoder.SpecializeCustomModifiers(TypeContext);

        public sealed override bool Equals(object obj)
        {
            if (!(obj is RoDefinitionConstructor<TMethodDecoder> other))
                return false;

            if (MetadataToken != other.MetadataToken)
                return false;

            if (DeclaringType != other.DeclaringType)
                return false;

            // Constructors are never inherited or acquirable from derived classes so their ReflectedType is hard-wired to their DeclaringType.
            // There is no need to compare it separately.
            Debug.Assert(ReflectedType == other.ReflectedType); 

            return true;
        }

        public sealed override int GetHashCode() => MetadataToken.GetHashCode() ^ DeclaringType.GetHashCode();

        public sealed override TypeContext TypeContext => _declaringType.Instantiation.ToTypeContext();
    }
}

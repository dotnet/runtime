// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    /// <summary>
    /// RoTypes that return true for IsGenericTypeParameter and get its metadata from a PEReader.
    /// </summary>
    internal sealed class EcmaGenericTypeParameterType : EcmaGenericParameterType
    {
        internal EcmaGenericTypeParameterType(GenericParameterHandle handle, EcmaModule module) 
            : base(handle, module)
        {
        }

        public sealed override bool IsGenericTypeParameter => true;
        public sealed override bool IsGenericMethodParameter => false;

        protected sealed override RoType ComputeDeclaringType()
        {
            MetadataReader reader = Reader;
            TypeDefinitionHandle declaringTypeHandle = (TypeDefinitionHandle)(GenericParameter.Parent);
            EcmaDefinitionType declaringType = declaringTypeHandle.ResolveTypeDef(GetEcmaModule());
            return declaringType;
        }

        public sealed override MethodBase DeclaringMethod => null;

        protected sealed override TypeContext TypeContext => ((RoInstantiationProviderType)GetRoDeclaringType()).Instantiation.ToTypeContext();
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    //
    // Common base type for an ISignatureTypeProvider that captures modified types and pinned types. Note that this only captures custom modifiers 
    // modifying the Type at the root of the Type expression tree. (This is all that the Reflection api will give you. They're shaped around
    // a broken understanding of custom modifiers.) Wrapped types appearing anywhere else must get peeled and thrown away before
    // passing it to a type factory method as these methods are completely unprepared to receive these ill-tempered Types.
    //
    internal abstract class EcmaWrappedTypeProvider : ISignatureTypeProvider<RoType, TypeContext>
    {
        private readonly EcmaModule _module;
        private readonly ISignatureTypeProvider<RoType, TypeContext> _typeProvider;

        protected EcmaWrappedTypeProvider(EcmaModule module)
        {
            _module = module;
            _typeProvider = module;
        }

        //
        // ISignatureTypeProvider
        //

        public RoType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => _typeProvider.GetTypeFromDefinition(reader, handle, rawTypeKind);
        public RoType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => _typeProvider.GetTypeFromReference(reader, handle, rawTypeKind);
        public RoType GetTypeFromSpecification(MetadataReader reader, TypeContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => _typeProvider.GetTypeFromSpecification(reader, genericContext, handle, rawTypeKind);

        public RoType GetSZArrayType(RoType elementType) => _typeProvider.GetSZArrayType(elementType.SkipTypeWrappers());
        public RoType GetArrayType(RoType elementType, ArrayShape shape) => _typeProvider.GetArrayType(elementType.SkipTypeWrappers(), shape);
        public RoType GetByReferenceType(RoType elementType) => _typeProvider.GetByReferenceType(elementType.SkipTypeWrappers());
        public RoType GetPointerType(RoType elementType) => _typeProvider.GetPointerType(elementType.SkipTypeWrappers());
        public RoType GetGenericInstantiation(RoType genericType, ImmutableArray<RoType> typeArguments)
        {
            genericType = genericType.SkipTypeWrappers();
            ImmutableArray<RoType> filteredTypeArguments = ImmutableArray<RoType>.Empty;
            for (int i = 0; i < typeArguments.Length; i++)
            {
                filteredTypeArguments = filteredTypeArguments.Add(typeArguments[i].SkipTypeWrappers());
            }
            return _typeProvider.GetGenericInstantiation(genericType, filteredTypeArguments);
        }

        public RoType GetGenericTypeParameter(TypeContext genericContext, int index) => _typeProvider.GetGenericTypeParameter(genericContext, index);
        public RoType GetGenericMethodParameter(TypeContext genericContext, int index) => _typeProvider.GetGenericMethodParameter(genericContext, index);

        public RoType GetFunctionPointerType(MethodSignature<RoType> signature) => _typeProvider.GetFunctionPointerType(signature);

        public abstract RoType GetModifiedType(RoType modifier, RoType unmodifiedType, bool isRequired);
        public abstract RoType GetPinnedType(RoType elementType);

        public RoType GetPrimitiveType(PrimitiveTypeCode typeCode) => _typeProvider.GetPrimitiveType(typeCode);
    }
}

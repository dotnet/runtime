// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    //
    // Main TypeProvider interface for System.Reflection.Metadata.
    //
    internal sealed partial class EcmaModule : ISignatureTypeProvider<RoType, TypeContext>, ICustomAttributeTypeProvider<RoType>
    {
        //
        // ISignatureTypeProvider
        //

        public RoType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => handle.ResolveTypeDef(this);
        public RoType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => handle.ResolveTypeRef(this);
        public RoType GetTypeFromSpecification(MetadataReader reader, TypeContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => handle.ResolveTypeSpec(this, genericContext);

        public RoType GetSZArrayType(RoType elementType) => elementType.GetUniqueArrayType();
        public RoType GetArrayType(RoType elementType, ArrayShape shape) => elementType.GetUniqueArrayType(shape.Rank);
        public RoType GetByReferenceType(RoType elementType) => elementType.GetUniqueByRefType();
        public RoType GetPointerType(RoType elementType) => elementType.GetUniquePointerType();
        public RoType GetGenericInstantiation(RoType genericType, ImmutableArray<RoType> typeArguments)
        {
            if (!(genericType is RoDefinitionType roDefinitionType))
                throw new BadImageFormatException(); // TypeSpec tried to instantiate a non-definition type as a generic type.
            return roDefinitionType.GetUniqueConstructedGenericType(typeArguments.ToArray());
        }

        public RoType GetGenericTypeParameter(TypeContext genericContext, int index) => genericContext.GetGenericTypeArgumentOrNull(index) ?? throw new BadImageFormatException(SR.Format(SR.GenericTypeParamIndexOutOfRange, index));
        public RoType GetGenericMethodParameter(TypeContext genericContext, int index) => genericContext.GetGenericMethodArgumentOrNull(index) ?? throw new BadImageFormatException(SR.Format(SR.GenericMethodParamIndexOutOfRange, index));

        public RoType GetFunctionPointerType(MethodSignature<RoType> signature) => throw new NotSupportedException(SR.NotSupported_FunctionPointers);
        public RoType GetModifiedType(RoType modifier, RoType unmodifiedType, bool isRequired) => unmodifiedType;
        public RoType GetPinnedType(RoType elementType) => elementType;

        public RoType GetPrimitiveType(PrimitiveTypeCode typeCode) => Loader.GetCoreType(typeCode.ToCoreType());

        //
        // ICustomAttributeTypeProvider
        //
        public RoType GetSystemType() => Loader.GetCoreType(CoreType.Type);
        public bool IsSystemType(RoType type) => type == Loader.TryGetCoreType(CoreType.Type);
        public PrimitiveTypeCode GetUnderlyingEnumType(RoType type) => type.GetEnumUnderlyingPrimitiveTypeCode(Loader);

        public RoType GetTypeFromSerializedName(string name)
        {
            // Called when an attribute argument is of type System.Type ([MyAttribute(typeof(Foo))]
            // Parse an assembly-qualified name as Assembly.GetType() does. If the assembly part is missing, search in _module (the
            // module in which the custom attribute metadata was found.)
            if (name == null)
                return null; // This gets hit if the custom attribute passes "(Type)null"
            return Helpers.LoadTypeFromAssemblyQualifiedName(name, GetRoAssembly(), ignoreCase: false, throwOnError: true);
        }
    }
}

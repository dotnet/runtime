// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using Internal.TypeSystem;

namespace Microsoft.Diagnostics.Tools.Pgo.TypeRefTypeSystem
{
    partial class TypeRefTypeSystemContext
    {
        // Used only to ensure that the various type flags are set properly on TypeRef types
        private class TypeRefSignatureParserProvider : ISignatureTypeProvider<TypeDesc, object>
        {
            private TypeSystemContext _tsc;
            private Dictionary<TypeReferenceHandle, TypeRefTypeSystemType> _resolver;
            public TypeRefSignatureParserProvider(TypeSystemContext tsc, Dictionary<TypeReferenceHandle, TypeRefTypeSystemType> resolver)
            {
                _tsc = tsc;
                _resolver = resolver;
            }

            public TypeDesc GetArrayType(TypeDesc elementType, ArrayShape shape)
            {
                if (elementType == null)
                    return null;
                return elementType.MakeArrayType(shape.Rank);
            }

            public TypeDesc GetByReferenceType(TypeDesc elementType)
            {
                if (elementType == null)
                    return null;
                return elementType.MakeByRefType();
            }

            public TypeDesc GetFunctionPointerType(MethodSignature<TypeDesc> signature) => null;

            public TypeDesc GetGenericInstantiation(TypeDesc genericType, ImmutableArray<TypeDesc> typeArguments)
            {
                if (genericType is TypeRefTypeSystemType typeRefType)
                {
                    typeRefType.SetGenericParameterCount(typeArguments.Length);
                }

                if (genericType != null)
                {
                    TypeDesc[] instance = new TypeDesc[typeArguments.Length];
                    for (int i = 0; i < instance.Length; i++)
                    {
                        if (typeArguments[i] == null)
                            return null;
                        instance[i] = typeArguments[i];
                    }

                    return _tsc.GetInstantiatedType((MetadataType)genericType, new Instantiation(instance));
                }
                return null;
            }
            public TypeDesc GetGenericMethodParameter(object genericContext, int index) => _tsc.GetSignatureVariable(index, method: true);
            public TypeDesc GetGenericTypeParameter(object genericContext, int index) => _tsc.GetSignatureVariable(index, method: false);
            public TypeDesc GetModifiedType(TypeDesc modifier, TypeDesc unmodifiedType, bool isRequired) => unmodifiedType;
            public TypeDesc GetPinnedType(TypeDesc elementType) => elementType;
            public TypeDesc GetPointerType(TypeDesc elementType) => (elementType != null) ? elementType.MakePointerType() : null;
            public TypeDesc GetPrimitiveType(PrimitiveTypeCode typeCode)
            {
                WellKnownType wkt = 0;
                switch (typeCode)
                {
                    case PrimitiveTypeCode.Void:
                        wkt = WellKnownType.Void;
                        break;
                    case PrimitiveTypeCode.Boolean:
                        wkt = WellKnownType.Boolean;
                        break;
                    case PrimitiveTypeCode.Char:
                        wkt = WellKnownType.Char;
                        break;
                    case PrimitiveTypeCode.SByte:
                        wkt = WellKnownType.SByte;
                        break;
                    case PrimitiveTypeCode.Byte:
                        wkt = WellKnownType.Byte;
                        break;
                    case PrimitiveTypeCode.Int16:
                        wkt = WellKnownType.Int16;
                        break;
                    case PrimitiveTypeCode.UInt16:
                        wkt = WellKnownType.UInt16;
                        break;
                    case PrimitiveTypeCode.Int32:
                        wkt = WellKnownType.Int32;
                        break;
                    case PrimitiveTypeCode.UInt32:
                        wkt = WellKnownType.UInt32;
                        break;
                    case PrimitiveTypeCode.Int64:
                        wkt = WellKnownType.Int64;
                        break;
                    case PrimitiveTypeCode.UInt64:
                        wkt = WellKnownType.UInt64;
                        break;
                    case PrimitiveTypeCode.Single:
                        wkt = WellKnownType.Single;
                        break;
                    case PrimitiveTypeCode.Double:
                        wkt = WellKnownType.Double;
                        break;
                    case PrimitiveTypeCode.String:
                        wkt = WellKnownType.String;
                        break;
                    case PrimitiveTypeCode.TypedReference:
                        wkt = WellKnownType.TypedReference;
                        break;
                    case PrimitiveTypeCode.IntPtr:
                        wkt = WellKnownType.IntPtr;
                        break;
                    case PrimitiveTypeCode.UIntPtr:
                        wkt = WellKnownType.UIntPtr;
                        break;
                    case PrimitiveTypeCode.Object:
                        wkt = WellKnownType.Object;
                        break;
                }

                return _tsc.GetWellKnownType(wkt);
            }
            public TypeDesc GetSZArrayType(TypeDesc elementType) => (elementType != null) ? elementType.MakeArrayType() : null;
            public TypeDesc GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => null;
            public TypeDesc GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            {
                bool isValueType = rawTypeKind == 0x11;
                _resolver.TryGetValue(handle, out TypeRefTypeSystemType type);
                if (type != null)
                {
                    type.SetIsValueType(isValueType);
                }
                return type;
            }
            public TypeDesc GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
            {
                var typeSpec = reader.GetTypeSpecification(handle);
                return typeSpec.DecodeSignature(this, null);
            }
        }
    }
}

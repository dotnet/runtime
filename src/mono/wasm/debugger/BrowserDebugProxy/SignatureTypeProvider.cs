// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WebAssembly.Diagnostics;

namespace Microsoft.WebAssembly.Diagnostics;

internal sealed class SignatureTypeProvider : ISignatureTypeProvider<ElementType, object>
{
    public ElementType GetPrimitiveType(PrimitiveTypeCode typeCode)
        => typeCode switch
        {
            PrimitiveTypeCode.Boolean => ElementType.Boolean,
            PrimitiveTypeCode.Byte => ElementType.U1,
            PrimitiveTypeCode.Char => ElementType.Char,
            PrimitiveTypeCode.Double => ElementType.R8,
            PrimitiveTypeCode.Int16 => ElementType.I2,
            PrimitiveTypeCode.Int32 => ElementType.I4,
            PrimitiveTypeCode.Int64 => ElementType.I8,
            PrimitiveTypeCode.IntPtr => ElementType.Ptr,
            PrimitiveTypeCode.Object => ElementType.Object,
            PrimitiveTypeCode.SByte => ElementType.I1,
            PrimitiveTypeCode.Single => ElementType.R4,
            PrimitiveTypeCode.String => ElementType.String,
            PrimitiveTypeCode.TypedReference => ElementType.ValueType,
            PrimitiveTypeCode.UInt16 => ElementType.U2,
            PrimitiveTypeCode.UInt32 => ElementType.U4,
            PrimitiveTypeCode.UInt64 => ElementType.U8,
            PrimitiveTypeCode.UIntPtr => ElementType.Ptr,
            PrimitiveTypeCode.Void => ElementType.Void,
            _ => ElementType.End,
        };

    ElementType ISignatureTypeProvider<ElementType, object>.GetFunctionPointerType(MethodSignature<ElementType> signature) => ElementType.FnPtr;
    ElementType ISignatureTypeProvider<ElementType, object>.GetModifiedType(ElementType modifier, ElementType unmodifiedType, bool isRequired) => ElementType.Object;
    ElementType ISignatureTypeProvider<ElementType, object>.GetPinnedType(ElementType elementType) => ElementType.Object;
    ElementType IConstructedTypeProvider<ElementType>.GetArrayType(ElementType elementType, ArrayShape shape) => ElementType.Array;
    ElementType IConstructedTypeProvider<ElementType>.GetByReferenceType(ElementType elementType) => ElementType.Object;
    ElementType IConstructedTypeProvider<ElementType>.GetGenericInstantiation(ElementType genericType, ImmutableArray<ElementType> typeArguments) => ElementType.Object;
    ElementType IConstructedTypeProvider<ElementType>.GetPointerType(ElementType elementType) => ElementType.Ptr;
    ElementType ISZArrayTypeProvider<ElementType>.GetSZArrayType(ElementType elementType) => ElementType.SzArray;
    ElementType ISignatureTypeProvider<ElementType, object>.GetGenericMethodParameter(object genericContext, int index) => ElementType.Object;
    ElementType ISignatureTypeProvider<ElementType, object>.GetGenericTypeParameter(object genericContext, int index) => ElementType.Object;
    ElementType ISignatureTypeProvider<ElementType, object>.GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind) => ElementType.Object;
    ElementType ISimpleTypeProvider<ElementType>.GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) => ElementType.Object;
    ElementType ISimpleTypeProvider<ElementType>.GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) => ElementType.Object;
}

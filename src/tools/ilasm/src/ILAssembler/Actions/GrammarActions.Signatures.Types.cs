// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private BlobBuilder MaterializeType(TypeValue type)
    {
        const int DefaultSignatureElementBlobSize = 10;

        BlobBuilder elementType = MaterializeElementType(type.ElementType);
        BlobBuilder prefix = new(DefaultSignatureElementBlobSize);
        BlobBuilder suffix = new(DefaultSignatureElementBlobSize);

        for (int i = type.Modifiers.Length - 1; i >= 0; i--)
        {
            switch (type.Modifiers[i])
            {
                case SimpleTypeModifierValue { Kind: SimpleTypeModifierKind.SzArray }:
                    prefix.WriteByte((byte)SignatureTypeCode.SZArray);
                    break;
                case ArrayTypeModifierValue:
                    prefix.WriteByte((byte)SignatureTypeCode.Array);
                    break;
                case SimpleTypeModifierValue { Kind: SimpleTypeModifierKind.ByReference }:
                    prefix.WriteByte((byte)SignatureTypeCode.ByReference);
                    break;
                case SimpleTypeModifierValue { Kind: SimpleTypeModifierKind.Pointer }:
                    prefix.WriteByte((byte)SignatureTypeCode.Pointer);
                    break;
                case SimpleTypeModifierValue { Kind: SimpleTypeModifierKind.Pinned }:
                    prefix.WriteByte((byte)SignatureTypeCode.Pinned);
                    break;
                case CustomTypeModifierValue customModifier:
                    prefix.WriteByte((byte)(customModifier.IsRequired
                        ? SignatureTypeCode.RequiredModifier
                        : SignatureTypeCode.OptionalModifier));
                    prefix.WriteTypeEntity(ResolveTypeSpecification(customModifier.Type));
                    break;
                case GenericArgumentsTypeModifierValue:
                    prefix.WriteByte((byte)SignatureTypeCode.GenericTypeInstance);
                    break;
            }
        }

        foreach (TypeModifierValue modifier in type.Modifiers)
        {
            switch (modifier)
            {
                case ArrayTypeModifierValue array:
                    WriteArrayShape(suffix, array.Bounds);
                    break;
                case GenericArgumentsTypeModifierValue genericArguments:
                    MaterializeTypeArguments(genericArguments.Arguments).WriteContentTo(suffix);
                    break;
            }
        }

        // Work around https://github.com/dotnet/runtime/issues/127243 by writing to a separate blob.
        BlobBuilder fullBlob = new(elementType.Count + prefix.Count + suffix.Count);
        prefix.WriteContentTo(fullBlob);
        elementType.WriteContentTo(fullBlob);
        suffix.WriteContentTo(fullBlob);
        return fullBlob;
    }

    private BlobBuilder MaterializeElementType(ElementTypeValue elementType)
    {
        BlobBuilder blob = new(5);
        switch (elementType)
        {
            case CILParser.ErrorElementTypeValue:
                blob.WriteByte((byte)SignatureTypeCode.Object);
                break;
            case PrimitiveElementTypeValue primitive:
                blob.WriteByte(primitive.TypeCode);
                break;
            case ClassElementTypeValue classType:
                EntityRegistry.TypeEntity typeEntity = ResolveClassName(classType.ClassName);
                if (TryGetPrimitiveTypeCode(typeEntity, classType.IsValueType) is { } primitiveTypeCode)
                {
                    blob.WriteByte((byte)primitiveTypeCode);
                }
                else
                {
                    blob.WriteByte((byte)(classType.IsValueType ? SignatureTypeKind.ValueType : SignatureTypeKind.Class));
                    blob.WriteTypeEntity(typeEntity);
                }
                break;
            case FunctionPointerElementTypeValue functionPointer:
                blob.WriteByte((byte)SignatureTypeCode.FunctionPointer);
                blob.WriteByte(functionPointer.CallingConvention);
                ImmutableArray<SignatureArg> arguments = MaterializeSignatureArguments(functionPointer.Arguments);
                blob.WriteCompressedInteger(arguments.Count(argument => !argument.IsSentinel));
                blob.LinkSuffix(MaterializeType(functionPointer.ReturnType));
                foreach (SignatureArg argument in arguments)
                {
                    blob.LinkSuffix(argument.SignatureBlob);
                }
                break;
            case IndexedGenericParameterElementTypeValue indexedGenericParameter:
                blob.WriteByte((byte)(indexedGenericParameter.IsMethodParameter
                    ? SignatureTypeCode.GenericMethodParameter
                    : SignatureTypeCode.GenericTypeParameter));
                // Always emit indexed generic parameters, including intentionally invalid IL.
                blob.WriteCompressedInteger(indexedGenericParameter.Index);
                break;
            case NamedGenericParameterElementTypeValue namedGenericParameter:
                WriteNamedGenericParameter(blob, namedGenericParameter);
                break;
            case TypedefElementTypeValue typedef:
                if (TryResolveTypedefAsTypeBlob(typedef.Alias) is { } resolved)
                {
                    resolved.WriteContentTo(blob);
                }
                else
                {
                    ReportError(
                        DiagnosticIds.TypedefNotFound,
                        string.Format(DiagnosticMessageTemplates.TypedefNotFound, typedef.Alias),
                        typedef.Token);
                }
                break;
            case SentinelElementTypeValue sentinel:
                blob.WriteByte((byte)SignatureTypeCode.Sentinel);
                blob.LinkSuffix(MaterializeType(sentinel.Type));
                break;
        }

        return blob;
    }

    private void WriteNamedGenericParameter(BlobBuilder blob, NamedGenericParameterElementTypeValue genericParameter)
    {
        blob.WriteByte((byte)(genericParameter.IsMethodParameter
            ? SignatureTypeCode.GenericMethodParameter
            : SignatureTypeCode.GenericTypeParameter));

        string name = genericParameter.Name;
        if (genericParameter.IsMethodParameter)
        {
            if (_currentMethod is null)
            {
                ReportError(
                    DiagnosticIds.MethodTypeParameterOutsideMethod,
                    string.Format(DiagnosticMessageTemplates.MethodTypeParameterOutsideMethod, name),
                    genericParameter.Token);
                blob.WriteCompressedInteger(0);
                return;
            }

            for (int i = 0; i < _currentMethod.Definition.GenericParameters.Count; i++)
            {
                if (_currentMethod.Definition.GenericParameters[i].Name == name)
                {
                    blob.WriteCompressedInteger(i);
                    return;
                }
            }
        }
        else
        {
            if (_currentTypeDefinition.Count == 0)
            {
                ReportError(
                    DiagnosticIds.TypeParameterOutsideType,
                    string.Format(DiagnosticMessageTemplates.TypeParameterOutsideType, name),
                    genericParameter.Token);
                blob.WriteCompressedInteger(0);
                return;
            }

            for (int i = 0; i < _currentTypeDefinition.Peek().GenericParameters.Count; i++)
            {
                if (_currentTypeDefinition.Peek().GenericParameters[i].Name == name)
                {
                    blob.WriteCompressedInteger(i);
                    return;
                }
            }
        }

        ReportError(
            DiagnosticIds.GenericParameterNotFound,
            string.Format(DiagnosticMessageTemplates.GenericParameterNotFound, name),
            genericParameter.Token);
        blob.WriteCompressedInteger(0);
    }

    private ImmutableArray<SignatureArg> MaterializeSignatureArguments(ImmutableArray<SignatureArgumentValue> arguments)
    {
        ImmutableArray<SignatureArg>.Builder builder = ImmutableArray.CreateBuilder<SignatureArg>(arguments.Length);
        foreach (SignatureArgumentValue argument in arguments)
        {
            builder.Add(MaterializeSignatureArgument(argument));
        }
        return builder.MoveToImmutable();
    }

    private SignatureArg MaterializeSignatureArgument(SignatureArgumentValue argument)
    {
        if (argument.IsSentinel)
        {
            return SignatureArg.CreateSentinelArgument();
        }

        return new SignatureArg(
            (System.Reflection.ParameterAttributes)argument.Attributes,
            MaterializeType(argument.Type ?? TypeValue.Error),
            MaterializeMarshallingDescriptor(argument.Marshalling),
            argument.Name);
    }

    private BlobBuilder MaterializeTypeArguments(ImmutableArray<TypeValue> arguments)
    {
        BlobBuilder blob = new(4);
        blob.WriteCompressedInteger(arguments.Length);
        foreach (TypeValue argument in arguments)
        {
            blob.LinkSuffix(MaterializeType(argument));
        }
        return blob;
    }

    private static void WriteArrayShape(BlobBuilder suffix, ImmutableArray<ArrayBoundValue> bounds)
    {
        suffix.WriteCompressedInteger(bounds.Length);

        int sizeCount = 0;
        while (sizeCount < bounds.Length && bounds[sizeCount].Upper is not null)
        {
            sizeCount++;
        }

        suffix.WriteCompressedInteger(sizeCount);
        for (int i = 0; i < sizeCount; i++)
        {
            suffix.WriteCompressedInteger(bounds[i].Upper.GetValueOrDefault());
        }

        int lowerBoundCount = 0;
        while (lowerBoundCount < bounds.Length && bounds[lowerBoundCount].Lower is not null)
        {
            lowerBoundCount++;
        }

        suffix.WriteCompressedInteger(lowerBoundCount);
        for (int i = 0; i < lowerBoundCount; i++)
        {
            suffix.WriteCompressedSignedInteger(bounds[i].Lower.GetValueOrDefault());
        }
    }

    private static SignatureTypeCode? TryGetPrimitiveTypeCode(EntityRegistry.TypeEntity typeEntity, bool isValueType)
    {
        if (typeEntity is not EntityRegistry.TypeReferenceEntity typeReference || typeReference.Namespace != "System")
        {
            return null;
        }

        if (isValueType)
        {
            return typeReference.Name switch
            {
                "Boolean" => SignatureTypeCode.Boolean,
                "Char" => SignatureTypeCode.Char,
                "SByte" => SignatureTypeCode.SByte,
                "Byte" => SignatureTypeCode.Byte,
                "Int16" => SignatureTypeCode.Int16,
                "UInt16" => SignatureTypeCode.UInt16,
                "Int32" => SignatureTypeCode.Int32,
                "UInt32" => SignatureTypeCode.UInt32,
                "Int64" => SignatureTypeCode.Int64,
                "UInt64" => SignatureTypeCode.UInt64,
                "Single" => SignatureTypeCode.Single,
                "Double" => SignatureTypeCode.Double,
                "IntPtr" => SignatureTypeCode.IntPtr,
                "UIntPtr" => SignatureTypeCode.UIntPtr,
                "TypedReference" => SignatureTypeCode.TypedReference,
                _ => null
            };
        }

        return typeReference.Name switch
        {
            "String" => SignatureTypeCode.String,
            "Object" => SignatureTypeCode.Object,
            _ => null
        };
    }
}

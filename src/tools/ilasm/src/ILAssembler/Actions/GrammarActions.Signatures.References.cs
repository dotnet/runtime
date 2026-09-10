// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal MethodReferenceValue CreateMethodReference(
        IToken token,
        byte callingConvention,
        TypeValue returnType,
        TypeSpecificationValue? owner,
        string name,
        ImmutableArray<TypeValue>? genericArguments,
        int? genericArity,
        ImmutableArray<SignatureArgumentValue> arguments)
        => new ParsedMethodReferenceValue(
            token,
            callingConvention,
            returnType,
            owner,
            name,
            genericArguments,
            genericArity.GetValueOrDefault(),
            arguments);

    internal MethodReferenceValue CreateTokenMethodReference(int token)
        => new TokenMethodReferenceValue(token);

    internal MethodReferenceValue CreateTypedefMethodReference(IToken token, string alias)
        => new TypedefMethodReferenceValue(token, alias);

    internal FieldReferenceValue CreateFieldReference(
        TypeValue fieldType,
        TypeSpecificationValue? owner,
        string name)
        => new ParsedFieldReferenceValue(
            fieldType,
            owner,
            name);

    internal FieldReferenceValue CreateTypedefFieldReference(IToken token, string alias)
        => new TypedefFieldReferenceValue(token, alias);

    internal MemberReferenceValue CreateMethodMemberReference(MethodReferenceValue method)
        => new MethodMemberReferenceValue(method);

    internal MemberReferenceValue CreateFieldMemberReference(FieldReferenceValue field)
        => new FieldMemberReferenceValue(field);

    internal MemberReferenceValue CreateTokenMemberReference(int token)
        => new TokenMemberReferenceValue(token);

    internal OwnerTypeValue CreateTypeOwner(TypeSpecificationValue type)
        => new TypeOwnerValue(type);

    internal OwnerTypeValue CreateMemberOwner(MemberReferenceValue member)
        => new MemberOwnerValue(member);

    private EntityRegistry.EntityBase MaterializeMethodReference(MethodReferenceValue methodReference)
    {
        switch (methodReference)
        {
            case TokenMethodReferenceValue token:
                return ResolveMetadataToken(token.Token);
            case TypedefMethodReferenceValue typedef:
                if (TryResolveTypedefAsMember(typedef.Alias) is { } resolved)
                {
                    return resolved;
                }
                ReportError(
                    DiagnosticIds.TypedefNotFound,
                    string.Format(DiagnosticMessageTemplates.TypedefNotFound, typedef.Alias),
                    typedef.Token);
                return CreateErrorMethodReference(typedef.Alias);
            case ParsedMethodReferenceValue parsed:
                return MaterializeParsedMethodReference(parsed);
            default:
                return CreateErrorMethodReference("<error>");
        }
    }

    private EntityRegistry.EntityBase MaterializeParsedMethodReference(ParsedMethodReferenceValue methodReference)
    {
        byte callingConvention = methodReference.CallingConvention;
        EntityRegistry.TypeEntity owner = methodReference.Owner is null
            ? _entityRegistry.ModuleType
            : ResolveTypeSpecification(methodReference.Owner);

        BlobBuilder? methodSpecificationSignature = null;
        int genericArity = methodReference.GenericArity;
        if (methodReference.GenericArguments is { } genericArguments)
        {
            genericArity = genericArguments.Length;
            if (genericArity != 0)
            {
                methodSpecificationSignature = new BlobBuilder();
                methodSpecificationSignature.WriteByte((byte)SignatureKind.MethodSpecification);
                MaterializeTypeArguments(genericArguments).WriteContentTo(methodSpecificationSignature);
            }
        }

        if (genericArity != 0)
        {
            callingConvention |= (byte)SignatureAttributes.Generic;
        }

        if (_expectInstance && (callingConvention & (byte)SignatureAttributes.Instance) == 0)
        {
            ReportWarning(
                DiagnosticIds.MissingInstanceCallConv,
                DiagnosticMessageTemplates.MissingInstanceCallConv,
                methodReference.Token);
            callingConvention |= (byte)SignatureAttributes.Instance;
        }

        BlobBuilder signature = new();
        signature.WriteByte(callingConvention);
        if (genericArity != 0)
        {
            signature.WriteCompressedInteger(genericArity);
        }

        ImmutableArray<SignatureArg> arguments = MaterializeSignatureArguments(methodReference.Arguments);
        signature.WriteCompressedInteger(arguments.Count(argument => !argument.IsSentinel));
        MaterializeType(methodReference.ReturnType).WriteContentTo(signature);
        foreach (SignatureArg argument in arguments)
        {
            argument.SignatureBlob.WriteContentTo(signature);
        }

        EntityRegistry.MemberReferenceEntity memberReference =
            _entityRegistry.CreateLazilyRecordedMemberReference(owner, methodReference.Name, signature);
        return methodSpecificationSignature is null
            ? memberReference
            : _entityRegistry.GetOrCreateMethodSpecification(memberReference, methodSpecificationSignature);
    }

    private EntityRegistry.EntityBase MaterializeFieldReference(FieldReferenceValue fieldReference)
    {
        switch (fieldReference)
        {
            case TypedefFieldReferenceValue typedef:
                if (TryResolveTypedefAsMember(typedef.Alias) is { } resolved)
                {
                    return resolved;
                }
                ReportError(
                    DiagnosticIds.TypedefNotFound,
                    string.Format(DiagnosticMessageTemplates.TypedefNotFound, typedef.Alias),
                    typedef.Token);
                return CreateErrorFieldReference(typedef.Alias);
            case ParsedFieldReferenceValue parsed:
                BlobBuilder fieldType = MaterializeType(parsed.FieldType);
                EntityRegistry.TypeEntity owner = parsed.Owner is null
                    ? _entityRegistry.ModuleType
                    : ResolveTypeSpecification(parsed.Owner);
                BlobBuilder signature = new(fieldType.Count + 1);
                signature.WriteByte((byte)SignatureKind.Field);
                fieldType.WriteContentTo(signature);
                return _entityRegistry.CreateLazilyRecordedMemberReference(owner, parsed.Name, signature);
            default:
                return CreateErrorFieldReference("<error>");
        }
    }

    private EntityRegistry.EntityBase MaterializeMemberReference(MemberReferenceValue memberReference)
        => memberReference switch
        {
            MethodMemberReferenceValue method => MaterializeMethodReference(method.Method),
            FieldMemberReferenceValue field => MaterializeFieldReference(field.Field),
            TokenMemberReferenceValue token => ResolveMetadataToken(token.Token),
            _ => CreateErrorMethodReference("<error>")
        };

    private EntityRegistry.MemberReferenceEntity CreateErrorMethodReference(string name)
    {
        BlobBuilder signature = new(3);
        signature.WriteByte((byte)SignatureCallingConvention.Default);
        signature.WriteCompressedInteger(0);
        signature.WriteByte((byte)SignatureTypeCode.Void);
        return _entityRegistry.CreateLazilyRecordedMemberReference(
            _entityRegistry.ModuleType,
            name,
            signature);
    }

    private EntityRegistry.MemberReferenceEntity CreateErrorFieldReference(string name)
    {
        BlobBuilder signature = new(2);
        signature.WriteByte((byte)SignatureKind.Field);
        signature.WriteByte((byte)SignatureTypeCode.Object);
        return _entityRegistry.CreateLazilyRecordedMemberReference(
            _entityRegistry.ModuleType,
            name,
            signature);
    }

    private EntityRegistry.EntityBase MaterializeOwnerType(OwnerTypeValue owner)
        => owner switch
        {
            TypeOwnerValue type => ResolveTypeSpecification(type.Type),
            MemberOwnerValue member => MaterializeMemberReference(member.Member),
            _ => new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle))
        };
}

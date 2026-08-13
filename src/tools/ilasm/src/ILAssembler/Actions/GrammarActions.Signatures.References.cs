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
    internal object CreateMethodReference(
        IToken token,
        byte callingConvention,
        object? returnType,
        object? owner,
        string name,
        CILParser.TypeArgsContext? genericArguments,
        int? genericArity,
        object? arguments)
        => new ParsedMethodReferenceValue(
            token,
            callingConvention,
            GetTypeValue(returnType),
            owner is null ? null : GetTypeSpecificationValue(owner),
            name,
            genericArguments is null ? null : GetTypeArgumentsValue(genericArguments.Value),
            genericArity.GetValueOrDefault(),
            GetSignatureArgumentsValue(arguments));

    internal object CreateTokenMethodReference(int token)
        => new TokenMethodReferenceValue(token);

    internal object CreateTypedefMethodReference(IToken token, string alias)
        => new TypedefMethodReferenceValue(token, alias);

    internal object CreateFieldReference(object? fieldType, object? owner, string name)
        => new ParsedFieldReferenceValue(
            GetTypeValue(fieldType),
            owner is null ? null : GetTypeSpecificationValue(owner),
            name);

    internal object CreateTypedefFieldReference(IToken token, string alias)
        => new TypedefFieldReferenceValue(token, alias);

    internal object CreateMethodMemberReference(object? method)
        => new MethodMemberReferenceValue(GetMethodReferenceValue(method));

    internal object CreateFieldMemberReference(object? field)
        => new FieldMemberReferenceValue(GetFieldReferenceValue(field));

    internal object CreateTokenMemberReference(int token)
        => new TokenMemberReferenceValue(token);

    internal object CreateTypeOwner(object? type)
        => new TypeOwnerValue(GetTypeSpecificationValue(type));

    internal object CreateMemberOwner(object? member)
        => new MemberOwnerValue(GetMemberReferenceValue(member));

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
                return _entityRegistry.CreateLazilyRecordedMemberReference(
                    _entityRegistry.ModuleType,
                    typedef.Alias,
                    new BlobBuilder());
            case ParsedMethodReferenceValue parsed:
                return MaterializeParsedMethodReference(parsed);
            default:
                return _entityRegistry.CreateLazilyRecordedMemberReference(
                    _entityRegistry.ModuleType,
                    "<error>",
                    new BlobBuilder());
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
                return _entityRegistry.CreateLazilyRecordedMemberReference(
                    _entityRegistry.ModuleType,
                    typedef.Alias,
                    new BlobBuilder());
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
                return _entityRegistry.CreateLazilyRecordedMemberReference(
                    _entityRegistry.ModuleType,
                    "<error>",
                    new BlobBuilder());
        }
    }

    private EntityRegistry.EntityBase MaterializeMemberReference(MemberReferenceValue memberReference)
        => memberReference switch
        {
            MethodMemberReferenceValue method => MaterializeMethodReference(method.Method),
            FieldMemberReferenceValue field => MaterializeFieldReference(field.Field),
            TokenMemberReferenceValue token => ResolveMetadataToken(token.Token),
            _ => _entityRegistry.CreateLazilyRecordedMemberReference(
                _entityRegistry.ModuleType,
                "<error>",
                new BlobBuilder())
        };

    private EntityRegistry.EntityBase MaterializeOwnerType(OwnerTypeValue owner)
        => owner switch
        {
            TypeOwnerValue type => ResolveTypeSpecification(type.Type),
            MemberOwnerValue member => MaterializeMemberReference(member.Member),
            _ => new EntityRegistry.FakeTypeEntity(default(TypeDefinitionHandle))
        };
}

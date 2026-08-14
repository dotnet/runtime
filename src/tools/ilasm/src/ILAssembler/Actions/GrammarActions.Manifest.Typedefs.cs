// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly Dictionary<string, TypedefEntry> _typedefs = new();

    internal object CreateTypeSignatureTypedef(object? type, string alias)
        => new TypeSignatureTypedefDeclarationValue(GetTypeValue(type), alias);

    internal object CreateClassTypedef(object? type, string alias)
        => new ClassTypedefDeclarationValue(GetClassNameValue(type), alias);

    internal object CreateMemberTypedef(object? member, string alias)
        => new MemberTypedefDeclarationValue(GetMemberReferenceValue(member), alias);

    internal object CreateCustomAttributeTypedefDeclaration(
        object? attribute,
        IToken location,
        string alias)
        => new CustomAttributeTypedefDeclarationValue(
            GetCustomAttributeDescriptorValue(attribute),
            location,
            alias);

    private void MaterializeTypedef(TypedefDeclarationValue declaration)
    {
        switch (declaration)
        {
            case TypeSignatureTypedefDeclarationValue type:
                BlobBuilder typeBlob = MaterializeType(type.Type);
                BlobBuilder copy = new(typeBlob.Count);
                typeBlob.WriteContentTo(copy);
                _typedefs[type.Alias] = new TypedefEntry.TypeBlob(copy);
                break;
            case ClassTypedefDeclarationValue type:
                _typedefs[type.Alias] = new TypedefEntry.Type(ResolveClassName(type.Type));
                break;
            case MemberTypedefDeclarationValue member:
                _typedefs[member.Alias] =
                    new TypedefEntry.Member(MaterializeMemberReference(member.Member));
                break;
            case CustomAttributeTypedefDeclarationValue customAttribute:
                EntityRegistry.CustomAttributeEntity attribute =
                    MaterializeCustomAttribute(
                        customAttribute.Attribute,
                        customAttribute.Location);
                _typedefs[customAttribute.Alias] =
                    new TypedefEntry.CustomAttribute(attribute.Constructor, attribute.Value);
                break;
        }
    }

    internal void MaterializeTypedef(CILParser.TypedefDeclContext context)
    {
        if (context.Value is TypedefDeclarationValue declaration)
        {
            MaterializeTypedef(declaration);
        }
    }

    private EntityRegistry.TypeEntity? TryResolveTypedefAsType(string alias)
    {
        if (_typedefs.TryGetValue(alias, out TypedefEntry? entry) &&
            entry is TypedefEntry.Type type)
        {
            return type.Entity;
        }

        return null;
    }

    private BlobBuilder? TryResolveTypedefAsTypeBlob(string alias)
    {
        if (!_typedefs.TryGetValue(alias, out TypedefEntry? entry))
        {
            return null;
        }

        if (entry is TypedefEntry.TypeBlob blob)
        {
            return blob.Blob;
        }

        if (entry is TypedefEntry.Type type)
        {
            BlobBuilder result = new(5);
            result.WriteByte((byte)SignatureTypeKind.Class);
            result.WriteTypeEntity(type.Entity);
            return result;
        }

        return null;
    }

    private EntityRegistry.EntityBase? TryResolveTypedefAsMember(string alias)
    {
        if (_typedefs.TryGetValue(alias, out TypedefEntry? entry) &&
            entry is TypedefEntry.Member member)
        {
            return member.Entity;
        }

        return null;
    }

    private (EntityRegistry.EntityBase Constructor, BlobBuilder Value)?
        TryResolveTypedefAsCustomAttribute(string alias)
    {
        if (_typedefs.TryGetValue(alias, out TypedefEntry? entry) &&
            entry is TypedefEntry.CustomAttribute customAttribute)
        {
            return (customAttribute.Constructor, customAttribute.Value);
        }

        return null;
    }
}

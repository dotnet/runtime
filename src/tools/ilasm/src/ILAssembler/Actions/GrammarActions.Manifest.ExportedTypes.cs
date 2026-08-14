// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal void SetExportedTypeAttribute(CILParser.ExptAttrContext context)
    {
        string attribute = context.Start.Text == "nested"
            ? $"nested{context.Stop?.Text}"
            : context.Start.Text;
        (TypeAttributes value, TypeAttributes mask) = attribute switch
        {
            "private" => (TypeAttributes.NotPublic, TypeAttributes.VisibilityMask),
            "public" => (TypeAttributes.Public, TypeAttributes.VisibilityMask),
            "forwarder" => (TypeAttributes.Forwarder, (TypeAttributes)0),
            "nestedpublic" => (TypeAttributes.NestedPublic, TypeAttributes.VisibilityMask),
            "nestedprivate" => (TypeAttributes.NestedPrivate, TypeAttributes.VisibilityMask),
            "nestedfamily" => (TypeAttributes.NestedFamily, TypeAttributes.VisibilityMask),
            "nestedassembly" => (TypeAttributes.NestedAssembly, TypeAttributes.VisibilityMask),
            "nestedfamandassem" => (TypeAttributes.NestedFamANDAssem, TypeAttributes.VisibilityMask),
            "nestedfamorassem" => (TypeAttributes.NestedFamORAssem, TypeAttributes.VisibilityMask),
            _ => throw new UnreachableException()
        };
        context.Value = value;
        context.Mask = mask;
    }

    internal TypeAttributes AddExportedTypeAttribute(
        TypeAttributes attributes,
        TypeAttributes value,
        TypeAttributes mask)
        => mask == 0 ? attributes | value : (attributes & ~mask) | value;

    internal ExportedTypeHeaderValue CreateExportedTypeHeader(
        TypeAttributes attributes,
        string name,
        IToken location)
        => new ExportedTypeHeaderValue(true, attributes, name, location);

    internal ExportedTypeValue CreateExportedType(
        ExportedTypeHeaderValue header,
        ImmutableArray<ExportedTypeDeclarationValue> declarations)
        => new ExportedTypeValue(
            header,
            declarations);

    internal ExportedTypeDeclarationValue CreateExportedTypeFileDeclaration(
        string name,
        IToken location)
        => new ExportedTypeFileDirectiveValue(name, location);

    internal ExportedTypeDeclarationValue CreateNestedExportedTypeDeclaration(
        TypeName name,
        IToken location)
        => new NestedExportedTypeDirectiveValue(name, location);

    internal ExportedTypeDeclarationValue CreateExportedTypeAssemblyDeclaration(
        string name,
        IToken location)
        => new ExportedTypeAssemblyDirectiveValue(name, location);

    internal ExportedTypeDeclarationValue CreateExportedTypeMetadataTokenDeclaration(
        int token,
        IToken location)
        => new ExportedTypeMetadataTokenDirectiveValue(token, location);

    internal ExportedTypeDeclarationValue CreateExportedTypeDefinitionIdDeclaration(
        IToken value)
        => new ExportedTypeDefinitionIdDirectiveValue(ParseInt32(value));

    internal ExportedTypeDeclarationValue CreateExportedTypeCustomAttributeDeclaration(
        CustomAttributeDeclarationValue? value,
        IToken location)
        => new ExportedTypeCustomAttributeDirectiveValue(value, location);

    private void MaterializeExportedType(ExportedTypeValue value)
    {
        ExportedTypeHeaderValue header = value.Header;
        if (!header.IsValid ||
            header.Location is not IToken location)
        {
            return;
        }

        (string typeNamespace, string name) =
            NameHelpers.SplitDottedNameToNamespaceAndName(header.Name);
        (
            EntityRegistry.EntityBase? implementation,
            int typeDefinitionId,
            ImmutableArray<EntityRegistry.CustomAttributeEntity> customAttributes
        ) = MaterializeExportedTypeDeclarations(value.Declarations);

        if (implementation is null)
        {
            ReportWarning(
                DiagnosticIds.MissingExportedTypeImplementation,
                string.Format(
                    DiagnosticMessageTemplates.MissingExportedTypeImplementation,
                    header.Name),
                location);
            return;
        }

        EntityRegistry.ExportedTypeEntity exportedType =
            _entityRegistry.GetOrCreateExportedType(
                implementation,
                typeNamespace,
                name,
                entity =>
                {
                    entity.Attributes = header.Attributes;
                    entity.TypeDefinitionId = typeDefinitionId;
                });
        foreach (EntityRegistry.CustomAttributeEntity attribute in customAttributes)
        {
            attribute.Owner = exportedType;
        }
    }

    private (
        EntityRegistry.EntityBase? Implementation,
        int TypeDefinitionId,
        ImmutableArray<EntityRegistry.CustomAttributeEntity> CustomAttributes
    ) MaterializeExportedTypeDeclarations(
        ImmutableArray<ExportedTypeDeclarationValue> declarations)
    {
        EntityRegistry.EntityBase? implementation = null;
        int typeDefinitionId = 0;
        ImmutableArray<EntityRegistry.CustomAttributeEntity>.Builder customAttributes =
            ImmutableArray.CreateBuilder<EntityRegistry.CustomAttributeEntity>();

        foreach (ExportedTypeDeclarationValue declaration in declarations)
        {
            switch (declaration)
            {
                case ExportedTypeCustomAttributeDirectiveValue customAttribute:
                    if (MaterializeCustomAttributeDeclaration(
                        customAttribute.Value,
                        customAttribute.Location) is { } attribute)
                    {
                        customAttributes.Add(attribute);
                    }
                    break;
                case ExportedTypeMetadataTokenDirectiveValue metadataToken:
                    EntityRegistry.EntityBase entity = ResolveMetadataToken(metadataToken.Token);
                    if (entity is EntityRegistry.FakeTypeEntity)
                    {
                        ReportError(
                            DiagnosticIds.InvalidMetadataToken,
                            DiagnosticMessageTemplates.InvalidMetadataToken,
                            metadataToken.Location);
                    }
                    implementation = ResolveBetterExportedTypeImplementation(
                        implementation,
                        entity);
                    break;
                case ExportedTypeFileDirectiveValue file:
                    implementation = _entityRegistry.FindFile(file.Name);
                    if (implementation is null)
                    {
                        ReportError(
                            DiagnosticIds.FileNotFound,
                            string.Format(DiagnosticMessageTemplates.FileNotFound, file.Name),
                            file.Location);
                    }
                    break;
                case ExportedTypeAssemblyDirectiveValue assembly:
                    implementation = _entityRegistry.FindAssemblyReference(assembly.Name);
                    if (implementation is null)
                    {
                        ReportError(
                            DiagnosticIds.AssemblyNotFound,
                            string.Format(
                                DiagnosticMessageTemplates.AssemblyNotFound,
                                assembly.Name),
                            assembly.Location);
                    }
                    break;
                case NestedExportedTypeDirectiveValue nested:
                    EntityRegistry.ExportedTypeEntity? containingType =
                        ResolveExportedType(nested.Name, nested.Location);
                    if (containingType is null)
                    {
                        ReportError(
                            DiagnosticIds.ExportedTypeNotFound,
                            string.Format(
                                DiagnosticMessageTemplates.ExportedTypeNotFound,
                                GetExportedTypeDisplayName(nested.Name)),
                            nested.Location);
                    }
                    else
                    {
                        implementation = ResolveBetterExportedTypeImplementation(
                            implementation,
                            containingType);
                    }
                    break;
                case ExportedTypeDefinitionIdDirectiveValue definitionId:
                    typeDefinitionId = definitionId.Value;
                    break;
            }
        }

        return (implementation, typeDefinitionId, customAttributes.ToImmutable());
    }

    private static EntityRegistry.EntityBase? ResolveBetterExportedTypeImplementation(
        EntityRegistry.EntityBase? current,
        EntityRegistry.EntityBase? candidate)
    {
        if (candidate is null)
        {
            return current;
        }

        if (current is null)
        {
            return candidate;
        }

        return GetImplementationPriority(candidate) >= GetImplementationPriority(current)
            ? candidate
            : current;

        static int GetImplementationPriority(EntityRegistry.EntityBase entity)
            => entity switch
            {
                EntityRegistry.FileEntity => 4,
                EntityRegistry.AssemblyReferenceEntity => 3,
                EntityRegistry.ExportedTypeEntity => 2,
                _ => 1
            };
    }

    private EntityRegistry.ExportedTypeEntity? ResolveExportedType(
        TypeName typeName,
        IToken location)
    {
        Stack<TypeName> containingTypes = new();
        for (TypeName? containingType = typeName;
             containingType is not null;
             containingType = containingType.ContainingTypeName)
        {
            containingTypes.Push(containingType);
        }

        EntityRegistry.ExportedTypeEntity? exportedType = null;
        while (containingTypes.Count != 0)
        {
            TypeName containingType = containingTypes.Pop();
            (string typeNamespace, string name) =
                NameHelpers.SplitDottedNameToNamespaceAndName(containingType.DottedName);
            exportedType = _entityRegistry.FindExportedType(
                exportedType,
                typeNamespace,
                name);
            if (exportedType is null)
            {
                ReportError(
                    DiagnosticIds.ExportedTypeNotFound,
                    string.Format(
                        DiagnosticMessageTemplates.ExportedTypeNotFound,
                        containingType.DottedName),
                    location);
                return null;
            }
        }

        return exportedType;
    }

    private static string GetExportedTypeDisplayName(TypeName typeName)
    {
        Stack<string> names = new();
        for (TypeName? current = typeName;
             current is not null;
             current = current.ContainingTypeName)
        {
            names.Push(current.DottedName);
        }

        return string.Join("/", names);
    }

    internal void MaterializeExportedType(CILParser.ExptypeBlockContext context)
    {
        if (context.Value is ExportedTypeValue value)
        {
            MaterializeExportedType(value);
        }
    }

}

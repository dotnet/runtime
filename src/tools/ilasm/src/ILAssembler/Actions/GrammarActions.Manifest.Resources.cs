// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal ManifestResourceAttributes AddManifestResourceAttribute(
        ManifestResourceAttributes attributes,
        ManifestResourceAttributes value)
        => attributes | value;

    internal ManifestResourceAttributes ParseManifestResourceAttribute(IToken token)
        => token.Text switch
        {
            "public" => ManifestResourceAttributes.Public,
            "private" => ManifestResourceAttributes.Private,
            _ => throw new UnreachableException()
        };

    internal ManifestResourceHeaderValue CreateManifestResourceHeader(
        ManifestResourceAttributes attributes,
        string name,
        string alias,
        IToken location)
        => new ManifestResourceHeaderValue(true, attributes, name, alias, location);

    internal ManifestResourceValue CreateManifestResource(
        ManifestResourceHeaderValue header,
        ImmutableArray<ManifestResourceDeclarationValue> declarations)
        => new ManifestResourceValue(
            header,
            declarations);

    internal ManifestResourceDeclarationValue CreateManifestResourceFileDeclaration(
        string name,
        IToken offset,
        IToken location)
        => new ManifestResourceFileDirectiveValue(name, (uint)ParseInt32(offset), location);

    internal ManifestResourceDeclarationValue CreateManifestResourceAssemblyDeclaration(
        string name)
        => new ManifestResourceAssemblyDirectiveValue(name);

    internal ManifestResourceDeclarationValue CreateManifestResourceCustomAttributeDeclaration(
        CustomAttributeDeclarationValue? value,
        IToken location)
        => new ManifestResourceCustomAttributeDirectiveValue(value, location);

    private void MaterializeManifestResource(ManifestResourceValue value)
    {
        ManifestResourceHeaderValue header = value.Header;
        if (!header.IsValid ||
            header.Location is not IToken location)
        {
            return;
        }

        (
            EntityRegistry.EntityBase? implementation,
            uint offset,
            ImmutableArray<EntityRegistry.CustomAttributeEntity> customAttributes
        ) = MaterializeManifestResourceDeclarations(value.Declarations);

        if (implementation is null)
        {
            offset = (uint)_manifestResources.Count;
            byte[] resourceData = _resourceLocator(header.Alias);
            if (resourceData is null)
            {
                ReportError(
                    DiagnosticIds.FileNotFound,
                    string.Format(
                        DiagnosticMessageTemplates.FileNotFound,
                        header.Alias),
                    location);
            }
            else
            {
                _manifestResources.WriteInt32(resourceData.Length);
                _manifestResources.WriteBytes(resourceData);
            }
        }

        EntityRegistry.ManifestResourceEntity resource =
            _entityRegistry.CreateManifestResource(header.Name, offset);
        resource.Attributes = header.Attributes;
        resource.Implementation = implementation;
        foreach (EntityRegistry.CustomAttributeEntity customAttribute in customAttributes)
        {
            customAttribute.Owner = resource;
        }
    }

    private (
        EntityRegistry.EntityBase? Implementation,
        uint Offset,
        ImmutableArray<EntityRegistry.CustomAttributeEntity> CustomAttributes
    ) MaterializeManifestResourceDeclarations(
        ImmutableArray<ManifestResourceDeclarationValue> declarations)
    {
        EntityRegistry.EntityBase? implementation = null;
        uint offset = 0;
        ImmutableArray<EntityRegistry.CustomAttributeEntity>.Builder customAttributes =
            ImmutableArray.CreateBuilder<EntityRegistry.CustomAttributeEntity>();

        foreach (ManifestResourceDeclarationValue declaration in declarations)
        {
            switch (declaration)
            {
                case ManifestResourceCustomAttributeDirectiveValue customAttribute:
                    if (MaterializeCustomAttributeDeclaration(
                        customAttribute.Value,
                        customAttribute.Location) is { } attribute)
                    {
                        customAttributes.Add(attribute);
                    }
                    break;
                case ManifestResourceFileDirectiveValue file
                    when implementation is not EntityRegistry.AssemblyReferenceEntity:
                    EntityRegistry.FileEntity? fileEntity = _entityRegistry.FindFile(file.Name);
                    if (fileEntity is null)
                    {
                        ReportError(
                            DiagnosticIds.FileNotFound,
                            string.Format(DiagnosticMessageTemplates.FileNotFound, file.Name),
                            file.Location);
                    }
                    else
                    {
                        implementation = fileEntity;
                        offset = file.Offset;
                    }
                    break;
                case ManifestResourceAssemblyDirectiveValue assembly:
                    implementation =
                        _entityRegistry.GetOrCreateAssemblyReference(assembly.Name, _ => { });
                    break;
            }
        }

        return (implementation, offset, customAttributes.ToImmutable());
    }

    internal void MaterializeManifestResource(CILParser.ManifestResBlockContext context)
    {
        if (context.Value is ManifestResourceValue value)
        {
            MaterializeManifestResource(value);
        }
    }

}

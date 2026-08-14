// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly Stack<ManifestValueListFrame<CILParser.ManifestResDeclsContext>>
        _manifestResourceDeclarationFrames = new();

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

    internal object CreateManifestResourceHeader(
        ManifestResourceAttributes attributes,
        string name,
        string alias,
        IToken location)
        => new ManifestResourceHeaderValue(attributes, name, alias, location);

    internal void BeginManifestResourceDeclarations(CILParser.ManifestResDeclsContext context)
        => _manifestResourceDeclarationFrames.Push(new(context));

    internal void AddManifestResourceDeclaration(
        CILParser.ManifestResDeclsContext context,
        object? value)
        => AddManifestValue(_manifestResourceDeclarationFrames, context, value);

    internal object EndManifestResourceDeclarations(CILParser.ManifestResDeclsContext context)
        => EndManifestValues(_manifestResourceDeclarationFrames, context);

    internal object CreateManifestResource(
        object? header,
        object? declarations,
        IToken location)
        => new ManifestResourceValue(
            GetManifestResourceHeader(header, location),
            GetManifestValues(declarations));

    internal object CreateManifestResourceFileDeclaration(
        string name,
        IToken offset,
        IToken location)
        => new ManifestResourceFileDirectiveValue(name, (uint)ParseInt32(offset), location);

    internal object CreateManifestResourceAssemblyDeclaration(string name)
        => new ManifestResourceAssemblyDirectiveValue(name);

    internal object CreateManifestResourceCustomAttributeDeclaration(
        object? value,
        IToken location)
        => new ManifestResourceCustomAttributeDirectiveValue(value, location);

    private static ManifestResourceHeaderValue GetManifestResourceHeader(
        object? value,
        IToken location)
        => value as ManifestResourceHeaderValue
            ?? new(0, string.Empty, string.Empty, location);

    private void MaterializeManifestResource(ManifestResourceValue value)
    {
        (
            EntityRegistry.EntityBase? implementation,
            uint offset,
            ImmutableArray<EntityRegistry.CustomAttributeEntity> customAttributes
        ) = MaterializeManifestResourceDeclarations(value.Declarations);

        if (implementation is null)
        {
            offset = (uint)_manifestResources.Count;
            byte[] resourceData = _resourceLocator(value.Header.Alias);
            if (resourceData is null)
            {
                ReportError(
                    DiagnosticIds.FileNotFound,
                    string.Format(
                        DiagnosticMessageTemplates.FileNotFound,
                        value.Header.Alias),
                    value.Header.Location);
            }
            else
            {
                _manifestResources.WriteInt32(resourceData.Length);
                _manifestResources.WriteBytes(resourceData);
            }
        }

        EntityRegistry.ManifestResourceEntity resource =
            _entityRegistry.CreateManifestResource(value.Header.Name, offset);
        resource.Attributes = value.Header.Attributes;
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
    ) MaterializeManifestResourceDeclarations(ImmutableArray<object> declarations)
    {
        EntityRegistry.EntityBase? implementation = null;
        uint offset = 0;
        ImmutableArray<EntityRegistry.CustomAttributeEntity>.Builder customAttributes =
            ImmutableArray.CreateBuilder<EntityRegistry.CustomAttributeEntity>();

        foreach (object declaration in declarations)
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

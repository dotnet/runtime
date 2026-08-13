// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.PortableExecutable;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal void BeginTopLevelDirective() => PrepareTopLevelDeclaration();

    internal void ProcessTopLevelDataDeclaration(CILParser.DataDeclContext context)
    {
        if (!context.HasSyntaxError)
        {
            _ = VisitDataDecl(context);
        }
    }

    internal void ProcessTopLevelVTableDeclaration(CILParser.VtableDeclContext context)
    {
        if (!context.HasSyntaxError)
        {
            _ = VisitVtableDecl(context);
        }
    }

    internal void ProcessTopLevelVTableFixupDeclaration(CILParser.VtfixupDeclContext context)
    {
        if (!context.HasSyntaxError)
        {
            _ = VisitVtfixupDecl(context);
        }
    }

    internal void ProcessTopLevelSourceDirective(CILParser.ExtSourceSpecContext context)
    {
        if (!context.HasSyntaxError)
        {
            _ = VisitExtSourceSpec(context);
        }
    }

    internal void ProcessTopLevelFileDeclaration(CILParser.FileDeclContext context)
    {
        if (!context.HasSyntaxError)
        {
            _ = VisitFileDecl(context);
        }
    }

    internal void ProcessTopLevelAssembly(CILParser.AssemblyBlockContext context)
    {
        if (!context.HasSyntaxError)
        {
            _ = VisitAssemblyBlock(context);
        }
    }

    internal void ProcessTopLevelAssemblyReference(CILParser.AssemblyRefBlockContext context)
    {
        if (context.HasSyntaxError)
        {
            return;
        }

        _currentAssemblyOrRef = VisitAssemblyRefHead(context.assemblyRefHead()).Value;
        try
        {
            foreach (CILParser.AssemblyRefDeclContext declaration in
                context.assemblyRefDecls().assemblyRefDecl())
            {
                _ = VisitAssemblyRefDecl(declaration);
            }
        }
        finally
        {
            _currentAssemblyOrRef = null;
        }
    }

    internal void ProcessTopLevelExportedType(CILParser.ExptypeBlockContext context)
    {
        if (context.HasSyntaxError)
        {
            return;
        }

        (System.Reflection.TypeAttributes attributes, string dottedName) =
            VisitExptypeHead(context.exptypeHead()).Value;
        (string typeNamespace, string name) =
            NameHelpers.SplitDottedNameToNamespaceAndName(dottedName);
        (EntityRegistry.EntityBase? implementation, int typeDefinitionId,
            ImmutableArray<EntityRegistry.CustomAttributeEntity> customAttributes) =
            VisitExptypeDecls(context.exptypeDecls()).Value;
        if (implementation is null)
        {
            ReportWarning(
                DiagnosticIds.MissingExportedTypeImplementation,
                string.Format(
                    DiagnosticMessageTemplates.MissingExportedTypeImplementation,
                    dottedName),
                context.exptypeHead());
            return;
        }

        EntityRegistry.ExportedTypeEntity exportedType =
            _entityRegistry.GetOrCreateExportedType(
                implementation,
                typeNamespace,
                name,
                entity =>
                {
                    entity.Attributes = attributes;
                    entity.TypeDefinitionId = typeDefinitionId;
                });
        foreach (EntityRegistry.CustomAttributeEntity attribute in customAttributes)
        {
            attribute.Owner = exportedType;
        }
    }

    internal void ProcessTopLevelManifestResource(CILParser.ManifestResBlockContext context)
    {
        if (context.HasSyntaxError)
        {
            return;
        }

        (string name, string alias, ManifestResourceAttributes attributes) =
            VisitManifestResHead(context.manifestResHead()).Value;
        (EntityRegistry.EntityBase? implementation, uint offset,
            ImmutableArray<EntityRegistry.CustomAttributeEntity> customAttributes) =
            VisitManifestResDecls(context.manifestResDecls()).Value;
        if (implementation is null)
        {
            offset = (uint)_manifestResources.Count;
            byte[] resourceData = _resourceLocator(alias);
            if (resourceData is null)
            {
                ReportError(
                    DiagnosticIds.FileNotFound,
                    string.Format(DiagnosticMessageTemplates.FileNotFound, alias),
                    context);
            }
            else
            {
                _manifestResources.WriteInt32(resourceData.Length);
                _manifestResources.WriteBytes(resourceData);
            }
        }

        EntityRegistry.ManifestResourceEntity resource =
            _entityRegistry.CreateManifestResource(name, offset);
        resource.Attributes = attributes;
        resource.Implementation = implementation;
        foreach (EntityRegistry.CustomAttributeEntity attribute in customAttributes)
        {
            attribute.Owner = resource;
        }
    }

    internal void SetModuleHeader(
        CILParser.ModuleHeadContext context,
        string name,
        bool isExternal)
    {
        context.Value = name;
        context.HasName = true;
        context.IsExternal = isExternal;
    }

    internal void SetEmptyModuleHeader(CILParser.ModuleHeadContext context)
    {
        context.Value = string.Empty;
        context.HasName = false;
        context.IsExternal = false;
    }

    internal void ProcessTopLevelModule(string? name, bool hasName, bool isExternal)
    {
        if (!hasName)
        {
            _entityRegistry.Module.Name = null;
        }
        else if (isExternal)
        {
            _entityRegistry.GetOrCreateModuleReference(name ?? string.Empty, _ => { });
        }
        else
        {
            _entityRegistry.Module.Name = name;
        }
    }

    internal void ProcessTopLevelSecurityDeclaration(CILParser.SecDeclContext context)
    {
        if (!context.HasSyntaxError)
        {
            EntityRegistry.DeclarativeSecurityAttributeEntity? security =
                VisitSecDecl(context).Value;
            security?.Parent = _entityRegistry.Assembly;
        }
    }

    internal void ProcessTopLevelCustomAttribute(CILParser.CustomAttrDeclContext context)
    {
        if (!context.HasSyntaxError &&
            VisitCustomAttrDecl(context).Value is { } customAttribute)
        {
            customAttribute.Owner =
                (EntityRegistry.EntityBase?)_lastFieldDefinition ?? _entityRegistry.Module;
        }
    }

    internal void ProcessTopLevelSubsystem(IToken value)
    {
        _subsystem = (Subsystem)ParseInt32(value);
    }

    internal void ProcessTopLevelCorFlags(IToken value)
    {
        _corflags = (CorFlags)ParseInt32(value);
    }

    internal void ProcessTopLevelAlignment(IToken value)
    {
        _alignment = ParseInt32(value);
    }

    internal void ProcessTopLevelImageBase(IToken value)
    {
        _imageBase = ParseInt64(value);
    }

    internal void ProcessTopLevelStackReserve(IToken value)
    {
        _stackReserve = ParseInt64(value);
    }

    internal void ProcessTopLevelLanguageDirective(CILParser.LanguageDeclContext context)
    {
        if (!context.HasSyntaxError)
        {
            _ = VisitLanguageDecl(context);
        }
    }

    internal void ProcessTopLevelTypedef(CILParser.TypedefDeclContext context)
    {
        if (!context.HasSyntaxError)
        {
            _ = VisitTypedefDecl(context);
        }
    }

    internal void BeginTopLevelTypeList() => PrepareTopLevelDeclaration();

    internal void ProcessTopLevelTypeListEntry(object? value)
        => _ = ResolveClassName(GetClassNameValue(value));

    private void PrepareTopLevelDeclaration() => ClearPendingCustomAttributeOwners();
}
#pragma warning restore CA1822

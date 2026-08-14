// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.PortableExecutable;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal void BeginTopLevelDirective() => PrepareTopLevelDeclaration();

    internal void ProcessTopLevelDataDeclaration(CILParser.DataDeclContext context)
        => _ = context;

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
        => _ = context;

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
        if (!context.HasSyntaxError)
        {
            _ = VisitAssemblyRefBlock(context);
        }
    }

    internal void ProcessTopLevelExportedType(CILParser.ExptypeBlockContext context)
    {
        if (!context.HasSyntaxError)
        {
            _ = VisitExptypeBlock(context);
        }
    }

    internal void ProcessTopLevelManifestResource(CILParser.ManifestResBlockContext context)
    {
        if (!context.HasSyntaxError)
        {
            _ = VisitManifestResBlock(context);
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
        => _ = context;

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

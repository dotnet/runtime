// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    internal void OnDeclaration(CILParser.DeclContext context)
    {
        if (context.classHead() is not null ||
            context.nameSpaceHead() is not null ||
            context.methodHead() is not null ||
            context.fieldDecl() is not null)
        {
            return;
        }

        VisitDecl(context);
    }

    public GrammarResult VisitDecl(CILParser.DeclContext context)
    {
        bool isTrailingCustomAttribute = context.customAttrDecl() is not null;
        if (context.fieldDecl() is null && !isTrailingCustomAttribute)
        {
            _lastFieldDefinition = null;
        }

        if (context.nameSpaceHead() is not null ||
            context.classHead() is not null ||
            context.methodHead() is not null)
        {
            throw new UnreachableException(StructuralNodeIsDrivenByParserActions);
        }

        if (context.fieldDecl() is not null)
        {
            throw new UnreachableException(StructuralNodeIsDrivenByParserActions);
        }
        if (context.dataDecl() is { } dataDecl)
        {
            _ = VisitDataDecl(dataDecl);
            return GrammarResult.SentinelValue.Result;
        }
        if (context.vtableDecl() is { } vtable)
        {
            _ = VisitVtableDecl(vtable);
            return GrammarResult.SentinelValue.Result;
        }
        if (context.vtfixupDecl() is { } vtFixup)
        {
            _ = VisitVtfixupDecl(vtFixup);
            return GrammarResult.SentinelValue.Result;
        }
        if (context.extSourceSpec() is { } extSourceSpec)
        {
            _ = VisitExtSourceSpec(extSourceSpec);
            return GrammarResult.SentinelValue.Result;
        }
        if (context.fileDecl() is { } fileDecl)
        {
            _ = VisitFileDecl(fileDecl);
            return GrammarResult.SentinelValue.Result;
        }
        if (context.assemblyBlock() is { } assemblyBlock)
        {
            _ = VisitAssemblyBlock(assemblyBlock);
            return GrammarResult.SentinelValue.Result;
        }
        if (context.assemblyRefHead() is { } assemblyRef)
        {
            var asmRef = VisitAssemblyRefHead(assemblyRef).Value;
            _currentAssemblyOrRef = asmRef;
            foreach (var decl in context.assemblyRefDecls().assemblyRefDecl())
            {
                _ = VisitAssemblyRefDecl(decl);
            }
            _currentAssemblyOrRef = null;
        }
        if (context.exptypeHead() is { } exptypeHead)
        {
            var (attrs, dottedName) = VisitExptypeHead(exptypeHead).Value;
            (string typeNamespace, string name) = NameHelpers.SplitDottedNameToNamespaceAndName(dottedName);
            var (impl, typeDefId, customAttrs) = VisitExptypeDecls(context.exptypeDecls()).Value;
            if (impl is null)
            {
                // COMPAT: Like native ilasm, warn and skip the exported type when implementation is not specified
                ReportWarning(DiagnosticIds.MissingExportedTypeImplementation,
                    string.Format(DiagnosticMessageTemplates.MissingExportedTypeImplementation, dottedName),
                    exptypeHead);
                return GrammarResult.SentinelValue.Result;
            }
            var exp = _entityRegistry.GetOrCreateExportedType(impl, typeNamespace, name, exp =>
            {
                exp.Attributes = attrs;
                exp.TypeDefinitionId = typeDefId;
            });
            foreach (var attr in customAttrs)
            {
                attr.Owner = exp;
            }
            return GrammarResult.SentinelValue.Result;
        }
        if (context.manifestResHead() is { } manifestResHead)
        {
            var (name, alias, flags) = VisitManifestResHead(manifestResHead).Value;
            var (implementation, offset, attrs) = VisitManifestResDecls(context.manifestResDecls()).Value;
            if (implementation is null)
            {
                offset = (uint)_manifestResources.Count;
                byte[] resourceData = _resourceLocator(alias);
                if (resourceData is null)
                {
                    ReportError(DiagnosticIds.FileNotFound,
                        string.Format(DiagnosticMessageTemplates.FileNotFound, alias),
                        context);
                }
                else
                {
                    // ECMA-335: Each resource is prefixed with a 4-byte length
                    _manifestResources.WriteInt32(resourceData.Length);
                    _manifestResources.WriteBytes(resourceData);
                }
            }
            var res = _entityRegistry.CreateManifestResource(name, offset);
            res.Attributes = flags;
            res.Implementation = implementation;
            foreach (var attr in attrs)
            {
                attr.Owner = res;
            }
            return GrammarResult.SentinelValue.Result;
        }
        if (context.moduleHead() is { } moduleHead)
        {
            if (moduleHead.dottedName() is null)
            {
                _entityRegistry.Module.Name = null;
            }
            else if (moduleHead.ChildCount == 2)
            {
                _entityRegistry.Module.Name = VisitDottedName(moduleHead.dottedName()).Value;
            }
            else
            {
                var name = VisitDottedName(moduleHead.dottedName()).Value;
                _entityRegistry.GetOrCreateModuleReference(name, _ => { });
            }
            return GrammarResult.SentinelValue.Result;
        }
        if (context.subsystem() is { } subsystem)
        {
            _subsystem = (Subsystem)VisitSubsystem(subsystem).Value;
        }
        if (context.corflags() is { } corflags)
        {
            _corflags = (CorFlags)VisitCorflags(corflags).Value;
        }
        if (context.alignment() is { } alignment)
        {
            _alignment = VisitAlignment(alignment).Value;
        }
        if (context.imagebase() is { } imagebase)
        {
            _imageBase = VisitImagebase(imagebase).Value;
        }
        if (context.stackreserve() is { } stackreserve)
        {
            _stackReserve = VisitStackreserve(stackreserve).Value;
        }
        if (context.languageDecl() is { } languageDecl)
        {
            VisitLanguageDecl(languageDecl);
        }
        if (context.customAttrDecl() is { } topLevelCustomAttr)
        {
            if (VisitCustomAttrDecl(topLevelCustomAttr).Value is { } customAttr)
            {
                customAttr.Owner = (EntityRegistry.EntityBase?)_lastFieldDefinition ?? _entityRegistry.Module;
            }
        }
        if (context.secDecl() is { } topSecDecl)
        {
            var declarativeSecurity = VisitSecDecl(topSecDecl).Value;
            declarativeSecurity?.Parent = _entityRegistry.Assembly;
        }
        if (context.typedefDecl() is { } typedefDecl)
        {
            VisitTypedefDecl(typedefDecl);
        }
        if (context.typelist() is { } typelist)
        {
            foreach (var name in typelist.className())
            {
                _ = VisitClassName(name);
            }
        }
        if (context.mscorlib() is { } mscorlib)
        {
            VisitMscorlib(mscorlib);
        }
        return GrammarResult.SentinelValue.Result;
    }

#pragma warning disable CA1822 // Mark members as static
        public GrammarResult VisitDecls(CILParser.DeclsContext context) => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

#pragma warning restore CA1822 // Mark members as static
}

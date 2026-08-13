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

namespace ILAssembler
{
#pragma warning disable CA1822 // Mark members as static
    internal sealed partial class GrammarActions : ICILVisitor<GrammarResult>
    {
        GrammarResult ICILVisitor<GrammarResult>.VisitAlignment(CILParser.AlignmentContext context) => VisitAlignment(context);
        public GrammarResult VisitAlignment(CILParser.AlignmentContext context)
            => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitAsmAttr(CILParser.AsmAttrContext context) => VisitAsmAttr(context);
        public GrammarResult.Literal<AssemblyFlags> VisitAsmAttr(CILParser.AsmAttrContext context)
            => new(context.asmAttrAny().Select(VisitAsmAttrAny).Aggregate((AssemblyFlags)0, (lhs, rhs) => lhs | rhs));
        GrammarResult ICILVisitor<GrammarResult>.VisitAsmAttrAny(CILParser.AsmAttrAnyContext context) => VisitAsmAttrAny(context);
        public GrammarResult.Flag<AssemblyFlags> VisitAsmAttrAny(CILParser.AsmAttrAnyContext context)
        {
            return context.GetText() switch
            {
                "retargetable" => new(AssemblyFlags.Retargetable),
                "windowsruntime" => new(AssemblyFlags.WindowsRuntime),
                "noplatform" => new(AssemblyFlags.NoPlatform),
                "legacy library" => new(0),
                "cil" => new(GetFlagForArch(ProcessorArchitecture.MSIL), AssemblyFlags.ArchitectureMask),
                "x86" => new(GetFlagForArch(ProcessorArchitecture.X86), AssemblyFlags.ArchitectureMask),
                "amd64" => new(GetFlagForArch(ProcessorArchitecture.Amd64), AssemblyFlags.ArchitectureMask),
                "arm" => new(GetFlagForArch(ProcessorArchitecture.Arm), AssemblyFlags.ArchitectureMask),
                "arm64" => new(GetFlagForArch((ProcessorArchitecture)6), AssemblyFlags.ArchitectureMask),
                _ => throw new UnreachableException()
            };
        }

        private static AssemblyFlags GetFlagForArch(ProcessorArchitecture arch)
        {
            return (AssemblyFlags)((int)arch << 4);
        }

        private static (ProcessorArchitecture, AssemblyFlags) GetArchAndFlags(AssemblyFlags flags)
        {
            var arch = (ProcessorArchitecture)(((int)flags & 0xF0) >> 4);
            var newFlags = flags & ~((AssemblyFlags)((int)arch << 4));
            return (arch, newFlags);
        }
        public GrammarResult VisitAsmOrRefDecl(CILParser.AsmOrRefDeclContext context)
        {
            Debug.Assert(_currentAssemblyOrRef is not null);

            if (context.customAttrDecl() is { } attr)
            {
                var customAttr = VisitCustomAttrDecl(attr).Value;
                customAttr?.Owner = _currentAssemblyOrRef;
                return GrammarResult.SentinelValue.Result;
            }

            string decl = context.GetChild(0).GetText();
            if (decl is ".publickey" or ".publicKey")
            {
                BlobBuilder blob = new();
                blob.WriteBytes(VisitBytes(context.bytes()));
                // COMPAT: Native ilasm gives a public key token precedence regardless of declaration order.
                if (_currentAssemblyOrRef is not EntityRegistry.AssemblyReferenceEntity assemblyReference
                    || assemblyReference.PublicKeyOrToken is null
                    || assemblyReference.Flags.HasFlag(AssemblyFlags.PublicKey))
                {
                    _currentAssemblyOrRef!.PublicKeyOrToken = blob;
                    _currentAssemblyOrRef.Flags |= AssemblyFlags.PublicKey;
                }
            }
            else if (decl == ".ver")
            {
                var versionComponents = context.intOrWildcard();
                _currentAssemblyOrRef!.Version = new Version(
                    VisitIntOrWildcard(versionComponents[0]).Value ?? 0,
                    VisitIntOrWildcard(versionComponents[1]).Value ?? 0,
                    VisitIntOrWildcard(versionComponents[2]).Value ?? 0,
                    VisitIntOrWildcard(versionComponents[3]).Value ?? 0);
            }
            else if (decl == ".locale")
            {
                _currentAssemblyOrRef!.Culture = context.compQstring() is { } compQstring
                    ? VisitCompQstring(compQstring).Value
                    : Encoding.Unicode.GetString([.. VisitBytes(context.bytes())]);
            }
            return GrammarResult.SentinelValue.Result;
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitAssemblyBlock(CILParser.AssemblyBlockContext context) => VisitAssemblyBlock(context);
        public GrammarResult VisitAssemblyBlock(CILParser.AssemblyBlockContext context)
        {
            // Use command-line override if specified, otherwise use the name from the .assembly directive
            string assemblyName = _options.AssemblyName ?? VisitDottedName(context.dottedName()).Value;
            _entityRegistry.Assembly ??= new EntityRegistry.AssemblyEntity(assemblyName);
            var attr = VisitAsmAttr(context.asmAttr()).Value;
            (_entityRegistry.Assembly.ProcessorArchitecture, _entityRegistry.Assembly.Flags) = GetArchAndFlags(attr);
            foreach (var decl in context.assemblyDecls().assemblyDecl())
            {
                VisitAssemblyDecl(decl);
            }

            // Apply command-line key file override (overrides .publickey directive)
            if (_options.KeyFile is not null)
            {
                ApplyKeyFile(_options.KeyFile);
            }

            // DebuggableAttribute is applied in BuildImage() after all source declarations
            // have been processed, so the correct corelib assembly ref can be found.

            return GrammarResult.SentinelValue.Result;
        }

        private void ApplyKeyFile(string keyFilePath)
        {
            if (_entityRegistry.Assembly is null)
            {
                return;
            }

            try
            {
                byte[] keyBytes = File.ReadAllBytes(keyFilePath);
                BlobBuilder blob = new();
                blob.WriteBytes(keyBytes);
                _entityRegistry.Assembly.PublicKeyOrToken = blob;
                _entityRegistry.Assembly.Flags |= AssemblyFlags.PublicKey;
            }
            catch (Exception ex)
            {
                // Create a location pointing to the first document (if available)
                var firstDoc = _documents.Values.FirstOrDefault();
                var location = firstDoc is not null
                    ? new Location(new SourceSpan(0, 0), firstDoc)
                    : new Location(new SourceSpan(0, 0), new SourceText(string.Empty, keyFilePath));
                _diagnostics.Add(new Diagnostic(DiagnosticIds.KeyFileError, DiagnosticSeverity.Error, $"Failed to read key file '{keyFilePath}': {ex.Message}", location));
            }
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitAssemblyDecl(CILParser.AssemblyDeclContext context) => VisitAssemblyDecl(context);
        public GrammarResult VisitAssemblyDecl(CILParser.AssemblyDeclContext context)
        {
            if (context.secDecl() is { } secDecl)
            {
                var declarativeSecurity = VisitSecDecl(secDecl);
                if (declarativeSecurity.Value is { } sec)
                {
                    sec.Parent = _entityRegistry.Assembly;
                }
            }
            else if (context.int32() is { } hashAlg)
            {
                _entityRegistry.Assembly!.HashAlgorithm = (AssemblyHashAlgorithm)VisitInt32(hashAlg).Value;
            }
            else if (context.asmOrRefDecl() is { } asmOrRef)
            {
                _currentAssemblyOrRef = _entityRegistry.Assembly;
                try
                {
                    VisitAsmOrRefDecl(asmOrRef);
                }
                finally
                {
                    _currentAssemblyOrRef = null;
                }
            }
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitAssemblyDecls(CILParser.AssemblyDeclsContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        public GrammarResult VisitAssemblyRefDecl(CILParser.AssemblyRefDeclContext context)
        {
            if (context.asmOrRefDecl() is { } asmOrRef)
            {
                VisitAsmOrRefDecl(asmOrRef);
            }
            string decl = context.GetChild(0).GetText();
            if (decl == ".hash")
            {
                var blob = new BlobBuilder();
                blob.WriteBytes(VisitBytes(context.bytes()));
                ((EntityRegistry.AssemblyReferenceEntity)_currentAssemblyOrRef!).Hash = blob;
            }
            if (decl == ".publickeytoken")
            {
                var blob = new BlobBuilder();
                blob.WriteBytes(VisitBytes(context.bytes()));
                _currentAssemblyOrRef!.PublicKeyOrToken = blob;
                _currentAssemblyOrRef.Flags &= ~AssemblyFlags.PublicKey;
            }
            return GrammarResult.SentinelValue.Result;
        }
        public GrammarResult VisitAssemblyRefDecls(CILParser.AssemblyRefDeclsContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitAssemblyRefHead(CILParser.AssemblyRefHeadContext context) => VisitAssemblyRefHead(context);
        public GrammarResult.Literal<EntityRegistry.AssemblyReferenceEntity> VisitAssemblyRefHead(CILParser.AssemblyRefHeadContext context)
        {
            var (arch, flags) = GetArchAndFlags(VisitAsmAttr(context.asmAttr()).Value);
            var dottedNames = context.dottedName();
            string name = VisitDottedName(dottedNames[0]).Value;
            string alias = name;
            if (dottedNames.Length > 1)
            {
                alias = VisitDottedName(dottedNames[1]).Value;
            }
            return new(_entityRegistry.GetOrCreateAssemblyReference(alias, asmref =>
            {
                asmref.Name = name;
                asmref.Flags = flags;
                asmref.ProcessorArchitecture = arch;
            }));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitCorflags(CILParser.CorflagsContext context) => VisitCorflags(context);
        public GrammarResult VisitCorflags(CILParser.CorflagsContext context)
            => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        public GrammarResult VisitExportHead(CILParser.ExportHeadContext context) => throw new NotImplementedException("Obsolete syntax");
        GrammarResult ICILVisitor<GrammarResult>.VisitExptAttr(CILParser.ExptAttrContext context) => VisitExptAttr(context);
        public static GrammarResult.Flag<TypeAttributes> VisitExptAttr(CILParser.ExptAttrContext context)
        {
            return context.GetText() switch
            {
                "private" => new(TypeAttributes.NotPublic, TypeAttributes.VisibilityMask),
                "public" => new(TypeAttributes.Public, TypeAttributes.VisibilityMask),
                "forwarder" => new(TypeAttributes.Forwarder),
                "nestedpublic" => new(TypeAttributes.NestedPublic, TypeAttributes.VisibilityMask),
                "nestedprivate" => new(TypeAttributes.NestedPrivate, TypeAttributes.VisibilityMask),
                "nestedfamily" => new(TypeAttributes.NestedFamily, TypeAttributes.VisibilityMask),
                "nestedassembly" => new(TypeAttributes.NestedAssembly, TypeAttributes.VisibilityMask),
                "nestedfamandassem" => new(TypeAttributes.NestedFamANDAssem, TypeAttributes.VisibilityMask),
                "nestedfamorassem" => new(TypeAttributes.NestedFamORAssem, TypeAttributes.VisibilityMask),
                _ => throw new UnreachableException(),
            };
        }

        // Type exports and forwarders are implemented via VisitExptypeDecls
        public GrammarResult VisitExptypeDecl(CILParser.ExptypeDeclContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);

        GrammarResult ICILVisitor<GrammarResult>.VisitExptypeDecls(CILParser.ExptypeDeclsContext context) => VisitExptypeDecls(context);
        public GrammarResult.Literal<(EntityRegistry.EntityBase? implementation, int typedefId, ImmutableArray<EntityRegistry.CustomAttributeEntity> attrs)> VisitExptypeDecls(CILParser.ExptypeDeclsContext context)
        {
            // COMPAT: The following order specifies the precedence of the various export kinds.
            // File, Assembly, Class (enclosing type), invalid token.
            // We'll process through all of the options here and then return the one that is valid.
            // We'll also record custom attributes here.
            EntityRegistry.EntityBase? implementationEntity = null;
            int typedefId = 0;
            var attrs = ImmutableArray.CreateBuilder<EntityRegistry.CustomAttributeEntity>();
            var declarations = context.exptypeDecl();
            for (int i = 0; i < declarations.Length; i++)
            {
                if (declarations[i].customAttrDecl() is { } attr)
                {
                    if (VisitCustomAttrDecl(attr).Value is EntityRegistry.CustomAttributeEntity customAttribute)
                    {
                        attrs.Add(customAttribute);
                    }
                    continue;
                }
                if (declarations[i].mdtoken() is { } mdToken)
                {
                    var entity = VisitMdtoken(mdToken).Value;
                    if (entity is null or EntityRegistry.FakeTypeEntity)
                    {
                        ReportError(DiagnosticIds.InvalidMetadataToken, DiagnosticMessageTemplates.InvalidMetadataToken, declarations[i]);
                    }
                    implementationEntity = ResolveBetterEntity(entity);
                    continue;
                }
                string kind = declarations[i].GetText();
                if (kind.StartsWith(".file"))
                {
                    string fileName = VisitDottedName(declarations[i].dottedName()).Value;
                    implementationEntity = _entityRegistry.FindFile(fileName);
                    if (implementationEntity is null)
                    {
                        ReportError(DiagnosticIds.FileNotFound, string.Format(DiagnosticMessageTemplates.FileNotFound, fileName), declarations[i]);
                    }
                }
                else if (kind.StartsWith(".assembly"))
                {
                    string assemblyName = VisitDottedName(declarations[i].dottedName()).Value;
                    implementationEntity = _entityRegistry.FindAssemblyReference(assemblyName);
                    if (implementationEntity is null)
                    {
                        ReportError(DiagnosticIds.AssemblyNotFound, string.Format(DiagnosticMessageTemplates.AssemblyNotFound, assemblyName), declarations[i]);
                    }
                }
                else if (kind.StartsWith(".class"))
                {
                    if (declarations[i].int32() is CILParser.Int32Context int32)
                    {
                        typedefId = VisitInt32(int32).Value;
                    }
                    else
                    {
                        _ = VisitSlashedName(declarations[i].slashedName());
                        var containing = ResolveExportedType(declarations[i].slashedName());
                        if (containing is null)
                        {
                            ReportError(DiagnosticIds.ExportedTypeNotFound, string.Format(DiagnosticMessageTemplates.ExportedTypeNotFound, declarations[i].slashedName().GetText()), declarations[i]);
                        }
                        else
                        {
                            implementationEntity = ResolveBetterEntity(containing);
                        }
                    }
                }
            }

            return new((implementationEntity, typedefId, attrs.ToImmutable()));

            EntityRegistry.EntityBase? ResolveBetterEntity(EntityRegistry.EntityBase? newImplementation)
            {
                return (implementationEntity, newImplementation) switch
                {
                    (null, _) => newImplementation,
                    (_, null) => implementationEntity,
                    (_, EntityRegistry.FileEntity) => newImplementation,
                    (EntityRegistry.FileEntity, _) => implementationEntity,
                    (_, EntityRegistry.AssemblyEntity) => newImplementation,
                    (EntityRegistry.AssemblyEntity, _) => implementationEntity,
                    (_, EntityRegistry.TypeEntity) => newImplementation,
                    (EntityRegistry.TypeEntity, _) => implementationEntity,
                    _ => throw new UnreachableException(),
                };
            }

            // Resolve ExportedType reference
            EntityRegistry.ExportedTypeEntity? ResolveExportedType(CILParser.SlashedNameContext slashedName)
            {
                TypeName typeName = VisitSlashedName(slashedName).Value;
                if (typeName.ContainingTypeName is null)
                {
                    // Check for typedef - typedefs resolve to TypeEntity, not ExportedTypeEntity
                    // so we skip the typedef check for exported type resolution
                }
                Stack<TypeName> containingTypes = new();
                for (TypeName? containingType = typeName; containingType is not null; containingType = containingType.ContainingTypeName)
                {
                    containingTypes.Push(containingType);
                }
                EntityRegistry.ExportedTypeEntity? exportedType = null;
                while (containingTypes.Count != 0)
                {
                    TypeName containingType = containingTypes.Pop();

                    (string ns, string name) = NameHelpers.SplitDottedNameToNamespaceAndName(containingType.DottedName);

                    exportedType = _entityRegistry.FindExportedType(
                        exportedType,
                        ns,
                        name);

                    if (exportedType is null)
                    {
                        ReportError(DiagnosticIds.ExportedTypeNotFound, string.Format(DiagnosticMessageTemplates.ExportedTypeNotFound, containingType.DottedName), slashedName);
                        return null;
                    }
                }

                return exportedType!;
            }
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitExptypeHead(CILParser.ExptypeHeadContext context) => VisitExptypeHead(context);
        public GrammarResult.Literal<(TypeAttributes attrs, string dottedName)> VisitExptypeHead(CILParser.ExptypeHeadContext context)
        {
            var attrs = context.exptAttr().Select(VisitExptAttr).Aggregate((TypeAttributes)0, (a, b) => a | b);
            return new((attrs, VisitDottedName(context.dottedName()).Value));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitFileAttr(CILParser.FileAttrContext context) => VisitFileAttr(context);
        public GrammarResult.Literal<bool> VisitFileAttr(CILParser.FileAttrContext context)
            => context.ChildCount != 0 ? new(false) : new(true);
        GrammarResult ICILVisitor<GrammarResult>.VisitFileDecl(CILParser.FileDeclContext context) => VisitFileDecl(context);
        public GrammarResult.Literal<EntityRegistry.FileEntity> VisitFileDecl(CILParser.FileDeclContext context)
        {
            string dottedName = VisitDottedName(context.dottedName()).Value;
            ImmutableArray<byte>? hash = context.HASH() is not null ? VisitBytes(context.bytes()) : null;
            var hashBlob = hash is not null ? new BlobBuilder() : null;
            hashBlob?.WriteBytes(hash!.Value);

            bool hasMetadata = context.fileAttr().Aggregate(true, (acc, attr) => acc && VisitFileAttr(attr).Value);
            bool isEntrypoint = context.fileEntry().Aggregate(false, (acc, attr) => acc || VisitFileEntry(attr).Value);
            var entity = _entityRegistry.GetOrCreateFile(dottedName, hasMetadata, hashBlob);
            if (isEntrypoint)
            {
                _entityRegistry.EntryPoint = entity;
            }
            return new(entity);
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitFileEntry(CILParser.FileEntryContext context) => VisitFileEntry(context);
        public GrammarResult.Literal<bool> VisitFileEntry(CILParser.FileEntryContext context)
            => context.ChildCount != 0 ? new(true) : new(false);

        GrammarResult ICILVisitor<GrammarResult>.VisitImagebase(CILParser.ImagebaseContext context) => VisitImagebase(context);
        public GrammarResult VisitImagebase(CILParser.ImagebaseContext context)
            => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        public GrammarResult VisitManifestResDecl(CILParser.ManifestResDeclContext context) => throw new UnreachableException(NodeShouldNeverBeDirectlyVisited);
        GrammarResult ICILVisitor<GrammarResult>.VisitManifestResDecls(CILParser.ManifestResDeclsContext context) => VisitManifestResDecls(context);
        public GrammarResult.Literal<(EntityRegistry.EntityBase? implementation, uint offset, ImmutableArray<EntityRegistry.CustomAttributeEntity> attributes)> VisitManifestResDecls(CILParser.ManifestResDeclsContext context)
        {
            EntityRegistry.EntityBase? implementation = null;
            uint offset = 0;
            var attributes = ImmutableArray.CreateBuilder<EntityRegistry.CustomAttributeEntity>();
            // COMPAT: Priority order for implementation is the following
            // AssemblyRef, File, nil
            foreach (var decl in context.manifestResDecl())
            {
                if (decl.customAttrDecl() is CILParser.CustomAttrDeclContext customAttrDecl)
                {
                    if (VisitCustomAttrDecl(customAttrDecl).Value is { } attr)
                    {
                        attributes.Add(attr);
                    }
                }
                string kind = decl.GetChild(0).GetText();
                if (kind == ".file" && implementation is not EntityRegistry.AssemblyReferenceEntity)
                {
                    string fileName = VisitDottedName(decl.dottedName()).Value;
                    var file = _entityRegistry.FindFile(fileName);
                    if (file is null)
                    {
                        ReportError(DiagnosticIds.FileNotFound, string.Format(DiagnosticMessageTemplates.FileNotFound, fileName), decl);
                    }
                    else
                    {
                        implementation = file;
                        offset = (uint)VisitInt32(decl.int32()).Value;
                    }
                }
                else if (kind == ".assembly")
                {
                    string assemblyName = VisitDottedName(decl.dottedName()).Value;
                    implementation = _entityRegistry.GetOrCreateAssemblyReference(assemblyName, _ => { });
                }
            }

            return new((implementation, offset, attributes.ToImmutable()));
        }
        GrammarResult ICILVisitor<GrammarResult>.VisitManifestResHead(CILParser.ManifestResHeadContext context) => VisitManifestResHead(context);
        public GrammarResult.Literal<(string name, string alias, ManifestResourceAttributes attr)> VisitManifestResHead(CILParser.ManifestResHeadContext context)
        {
            var dottedNames = context.dottedName();
            string name = VisitDottedName(dottedNames[0]).Value;
            string alias = dottedNames.Length == 2 ? VisitDottedName(dottedNames[1]).Value : name;
            ManifestResourceAttributes attr = 0;
            foreach (var attrContext in context.manresAttr())
            {
                attr |= VisitManresAttr(attrContext).Value;
            }

            return new((name, alias, attr));
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitManresAttr(CILParser.ManresAttrContext context) => VisitManresAttr(context);
        public GrammarResult.Flag<ManifestResourceAttributes> VisitManresAttr(CILParser.ManresAttrContext context)
        {
            return context.GetText() switch
            {
                "public" => new(ManifestResourceAttributes.Public),
                "private" => new(ManifestResourceAttributes.Private),
                _ => throw new UnreachableException()
            };
        }

        public GrammarResult VisitModuleHead(CILParser.ModuleHeadContext context)
            => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        // .mscorlib directive indicates the assembly being compiled is mscorlib itself.
        // This is currently a no-op; the flag would be used to affect type resolution
        // when support for compiling mscorlib is added.
        public GrammarResult VisitMscorlib(CILParser.MscorlibContext context)
            => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitStackreserve(CILParser.StackreserveContext context) => VisitStackreserve(context);
        public GrammarResult VisitStackreserve(CILParser.StackreserveContext context)
            => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        GrammarResult ICILVisitor<GrammarResult>.VisitSubsystem(CILParser.SubsystemContext context) => VisitSubsystem(context);
        public GrammarResult VisitSubsystem(CILParser.SubsystemContext context)
            => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        public GrammarResult VisitTypedefDecl(CILParser.TypedefDeclContext context)
        {
            string alias = VisitDottedName(context.dottedName()).Value;

            if (context.type() is CILParser.TypeContext type)
            {
                // .typedef type as alias
                // This creates an alias for a complete type signature (blob)
                var typeBlob = VisitType(type).Value;
                // Create a copy of the blob to avoid issues with linked BlobBuilders
                var copy = new BlobBuilder(typeBlob.Count);
                typeBlob.WriteContentTo(copy);
                _typedefs[alias] = new TypedefEntry.TypeBlob(copy);
            }
            else if (context.className() is CILParser.ClassNameContext className)
            {
                // .typedef className as alias
                var typeEntity = VisitClassName(className).Value;
                _typedefs[alias] = new TypedefEntry.Type(typeEntity);
            }
            else if (context.memberRef() is CILParser.MemberRefContext memberRef)
            {
                // .typedef memberRef as alias
                var member = VisitMemberRef(memberRef).Value;
                _typedefs[alias] = new TypedefEntry.Member(member);
            }
            else if (context.customDescr() is CILParser.CustomDescrContext customDescr)
            {
                // .typedef customDescr as alias
                var attr = VisitCustomDescr(customDescr).Value;
                if (attr is not null)
                {
                    _typedefs[alias] = new TypedefEntry.CustomAttribute(attr.Constructor, attr.Value);
                }
            }
            else if (context.customDescrWithOwner() is CILParser.CustomDescrWithOwnerContext customDescrWithOwner)
            {
                // .typedef customDescrWithOwner as alias
                var attr = VisitCustomDescrWithOwner(customDescrWithOwner).Value;
                if (attr is not null)
                {
                    _typedefs[alias] = new TypedefEntry.CustomAttribute(attr.Constructor, attr.Value);
                }
            }

            return GrammarResult.SentinelValue.Result;
        }

        /// <summary>
        /// Tries to resolve a typedef alias to a type entity.
        /// </summary>
        private EntityRegistry.TypeEntity? TryResolveTypedefAsType(string alias)
        {
            if (_typedefs.TryGetValue(alias, out var entry) && entry is TypedefEntry.Type typeEntry)
            {
                return typeEntry.Entity;
            }
            return null;
        }

        /// <summary>
        /// Tries to resolve a typedef alias to a type blob (complete type signature).
        /// </summary>
        private BlobBuilder? TryResolveTypedefAsTypeBlob(string alias)
        {
            if (_typedefs.TryGetValue(alias, out var entry))
            {
                if (entry is TypedefEntry.TypeBlob blobEntry)
                {
                    return blobEntry.Blob;
                }
                if (entry is TypedefEntry.Type typeEntry)
                {
                    // Encode the type entity as a CLASS reference for the blob
                    var blob = new BlobBuilder(5);
                    blob.WriteByte((byte)SignatureTypeKind.Class);
                    blob.WriteTypeEntity(typeEntry.Entity);
                    return blob;
                }
            }
            return null;
        }

        /// <summary>
        /// Tries to resolve a typedef alias to a member reference.
        /// </summary>
        private EntityRegistry.EntityBase? TryResolveTypedefAsMember(string alias)
        {
            if (_typedefs.TryGetValue(alias, out var entry) && entry is TypedefEntry.Member memberEntry)
            {
                return memberEntry.Entity;
            }
            return null;
        }

        /// <summary>
        /// Tries to resolve a typedef alias to a custom attribute.
        /// </summary>
        private (EntityRegistry.EntityBase Constructor, BlobBuilder Value)? TryResolveTypedefAsCustomAttribute(string alias)
        {
            if (_typedefs.TryGetValue(alias, out var entry) && entry is TypedefEntry.CustomAttribute attrEntry)
            {
                return (attrEntry.Constructor, attrEntry.Value);
            }
            return null;
        }

        public GrammarResult VisitTypelist(CILParser.TypelistContext context)
            => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

        public GrammarResult VisitVtableDecl(CILParser.VtableDeclContext context)
        {
            // Raw .vtable directive with bytes - not commonly used
            // For now, we don't support this legacy syntax
            throw new NotImplementedException("raw vtable fixups blob (.vtable) not supported - use .vtfixup instead");
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitVtfixupAttr(CILParser.VtfixupAttrContext context) => VisitVtfixupAttr(context);
        public GrammarResult.Literal<ushort> VisitVtfixupAttr(CILParser.VtfixupAttrContext context)
        {
            // vtfixupAttr: | vtfixupAttr INT32_ | vtfixupAttr INT64_ | vtfixupAttr 'fromunmanaged' | vtfixupAttr 'callmostderived' | vtfixupAttr 'retainappdomain'
            ushort flags = 0;
            foreach (var child in context.children ?? [])
            {
                string text = child.GetText();
                flags |= text switch
                {
                    "int32" => VTableFixupSupport.COR_VTABLE_32BIT,
                    "int64" => VTableFixupSupport.COR_VTABLE_64BIT,
                    "fromunmanaged" => VTableFixupSupport.COR_VTABLE_FROM_UNMANAGED,
                    "callmostderived" => VTableFixupSupport.COR_VTABLE_CALL_MOST_DERIVED,
                    "retainappdomain" => VTableFixupSupport.COR_VTABLE_FROM_UNMANAGED_RETAIN_APPDOMAIN,
                    _ => 0
                };
            }

            // Default to 32-bit if neither 32 nor 64 is specified
            if ((flags & (VTableFixupSupport.COR_VTABLE_32BIT | VTableFixupSupport.COR_VTABLE_64BIT)) == 0)
            {
                flags |= VTableFixupSupport.COR_VTABLE_32BIT;
            }

            return new(flags);
        }

        GrammarResult ICILVisitor<GrammarResult>.VisitVtfixupDecl(CILParser.VtfixupDeclContext context) => VisitVtfixupDecl(context);
        public GrammarResult VisitVtfixupDecl(CILParser.VtfixupDeclContext context)
        {
            // vtfixupDecl: '.vtfixup' '[' int32 ']' vtfixupAttr 'at' id;
            int slotCount = VisitInt32(context.int32()).Value;
            ushort flags = VisitVtfixupAttr(context.vtfixupAttr()).Value;
            string dataLabel = VisitId(context.id()).Value;

            _vtableFixups.Add(new VTableFixupSupport.VTableFixupEntry(slotCount, flags, dataLabel));

            return GrammarResult.SentinelValue.Result;
        }

    }
}

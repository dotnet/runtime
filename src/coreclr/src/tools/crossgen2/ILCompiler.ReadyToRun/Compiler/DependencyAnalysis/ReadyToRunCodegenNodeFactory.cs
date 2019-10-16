// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Win32Resources;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.Text;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    // TODO-REFACTOR: rename this
    public interface IEETypeNode
    {
        TypeDesc Type { get; }
    }

    public struct NodeCache<TKey, TValue>
    {
        private Func<TKey, TValue> _creator;
        private ConcurrentDictionary<TKey, TValue> _cache;

        public NodeCache(Func<TKey, TValue> creator, IEqualityComparer<TKey> comparer)
        {
            _creator = creator;
            _cache = new ConcurrentDictionary<TKey, TValue>(comparer);
        }

        public NodeCache(Func<TKey, TValue> creator)
        {
            _creator = creator;
            _cache = new ConcurrentDictionary<TKey, TValue>();
        }

        public TValue GetOrAdd(TKey key)
        {
            return _cache.GetOrAdd(key, _creator);
        }
    }

    // TODO-REFACTOR: merge with the ReadyToRunCodegen factory
    public abstract class NodeFactory
    {
        private bool _markingComplete;

        public NodeFactory(CompilerTypeSystemContext context,
            CompilationModuleGroup compilationModuleGroup,
            NameMangler nameMangler,
            MetadataManager metadataManager)
        {
            TypeSystemContext = context;
            CompilationModuleGroup = compilationModuleGroup;
            Target = context.Target;
            NameMangler = nameMangler;
            MetadataManager = metadataManager;

            CreateNodeCaches();
        }

        public CompilerTypeSystemContext TypeSystemContext { get; }

        public TargetDetails Target { get; }

        public CompilationModuleGroup CompilationModuleGroup { get; }

        public NameMangler NameMangler { get; }

        public MetadataManager MetadataManager { get; }

        public bool MarkingComplete => _markingComplete;

        public void SetMarkingComplete()
        {
            _markingComplete = true;
        }

        private void CreateNodeCaches()
        {
            _typeSymbols = new NodeCache<TypeDesc, IEETypeNode>(CreateNecessaryTypeNode);
            _constructedTypeSymbols = new NodeCache<TypeDesc, IEETypeNode>(CreateConstructedTypeNode);
            _methodEntrypoints = new NodeCache<MethodDesc, IMethodNode>(CreateMethodEntrypointNode);
            _genericReadyToRunHelpersFromDict = new NodeCache<ReadyToRunGenericHelperKey, ISymbolNode>(CreateGenericLookupFromDictionaryNode);
            _genericReadyToRunHelpersFromType = new NodeCache<ReadyToRunGenericHelperKey, ISymbolNode>(CreateGenericLookupFromTypeNode);

            _readOnlyDataBlobs = new NodeCache<ReadOnlyDataBlobKey, BlobNode>(key =>
            {
                return new BlobNode(key.Name, ObjectNodeSection.ReadOnlyDataSection, key.Data, key.Alignment);
            });
        }

        public abstract void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph);

        protected abstract IMethodNode CreateMethodEntrypointNode(MethodDesc method);

        protected abstract IEETypeNode CreateNecessaryTypeNode(TypeDesc type);

        protected abstract IEETypeNode CreateConstructedTypeNode(TypeDesc type);

        protected abstract ISymbolNode CreateGenericLookupFromDictionaryNode(ReadyToRunGenericHelperKey helperKey);

        protected abstract ISymbolNode CreateGenericLookupFromTypeNode(ReadyToRunGenericHelperKey helperKey);

        private NodeCache<TypeDesc, IEETypeNode> _typeSymbols;

        public IEETypeNode NecessaryTypeSymbol(TypeDesc type)
        {
            return _typeSymbols.GetOrAdd(type);
        }

        private NodeCache<TypeDesc, IEETypeNode> _constructedTypeSymbols;

        public IEETypeNode ConstructedTypeSymbol(TypeDesc type)
        {
            return _constructedTypeSymbols.GetOrAdd(type);
        }

        protected NodeCache<MethodDesc, IMethodNode> _methodEntrypoints;

        // TODO-REFACTOR: we should try and get rid of this
        public IMethodNode MethodEntrypoint(MethodDesc method)
        {
            return _methodEntrypoints.GetOrAdd(method);
        }

        private NodeCache<ReadyToRunGenericHelperKey, ISymbolNode> _genericReadyToRunHelpersFromDict;

        public ISymbolNode ReadyToRunHelperFromDictionaryLookup(ReadyToRunHelperId id, Object target, TypeSystemEntity dictionaryOwner)
        {
            return _genericReadyToRunHelpersFromDict.GetOrAdd(new ReadyToRunGenericHelperKey(id, target, dictionaryOwner));
        }

        private NodeCache<ReadyToRunGenericHelperKey, ISymbolNode> _genericReadyToRunHelpersFromType;

        public ISymbolNode ReadyToRunHelperFromTypeLookup(ReadyToRunHelperId id, Object target, TypeSystemEntity dictionaryOwner)
        {
            return _genericReadyToRunHelpersFromType.GetOrAdd(new ReadyToRunGenericHelperKey(id, target, dictionaryOwner));
        }

        private NodeCache<ReadOnlyDataBlobKey, BlobNode> _readOnlyDataBlobs;

        public BlobNode ReadOnlyDataBlob(Utf8String name, byte[] blobData, int alignment)
        {
            return _readOnlyDataBlobs.GetOrAdd(new ReadOnlyDataBlobKey(name, blobData, alignment));
        }

        protected struct ReadyToRunGenericHelperKey : IEquatable<ReadyToRunGenericHelperKey>
        {
            public readonly object Target;
            public readonly TypeSystemEntity DictionaryOwner;
            public readonly ReadyToRunHelperId HelperId;

            public ReadyToRunGenericHelperKey(ReadyToRunHelperId helperId, object target, TypeSystemEntity dictionaryOwner)
            {
                HelperId = helperId;
                Target = target;
                DictionaryOwner = dictionaryOwner;
            }

            public bool Equals(ReadyToRunGenericHelperKey other)
                => HelperId == other.HelperId && DictionaryOwner == other.DictionaryOwner && Target.Equals(other.Target);
            public override bool Equals(object obj) => obj is ReadyToRunGenericHelperKey && Equals((ReadyToRunGenericHelperKey)obj);
            public override int GetHashCode()
            {
                int hashCode = (int)HelperId * 0x5498341 + 0x832424;
                hashCode = hashCode * 23 + Target.GetHashCode();
                hashCode = hashCode * 23 + DictionaryOwner.GetHashCode();
                return hashCode;
            }
        }

        protected struct ReadOnlyDataBlobKey : IEquatable<ReadOnlyDataBlobKey>
        {
            public readonly Utf8String Name;
            public readonly byte[] Data;
            public readonly int Alignment;

            public ReadOnlyDataBlobKey(Utf8String name, byte[] data, int alignment)
            {
                Name = name;
                Data = data;
                Alignment = alignment;
            }

            // The assumption here is that the name of the blob is unique.
            // We can't emit two blobs with the same name and different contents.
            // The name is part of the symbolic name and we don't do any mangling on it.
            public bool Equals(ReadOnlyDataBlobKey other) => Name.Equals(other.Name);
            public override bool Equals(object obj) => obj is ReadOnlyDataBlobKey && Equals((ReadOnlyDataBlobKey)obj);
            public override int GetHashCode() => Name.GetHashCode();
        }
    }

    // To make the code future compatible to the composite R2R story
    // do NOT attempt to pass and store _inputModule here
    public sealed class ReadyToRunCodegenNodeFactory : NodeFactory
    {
        public ReadyToRunCodegenNodeFactory(
            CompilerTypeSystemContext context,
            CompilationModuleGroup compilationModuleGroup,
            NameMangler nameMangler,
            ModuleTokenResolver moduleTokenResolver,
            SignatureContext signatureContext,
            CopiedCorHeaderNode corHeaderNode,
            ResourceData win32Resources,
            AttributePresenceFilterNode attributePresenceFilterNode)
            : base(context,
                  compilationModuleGroup,
                  nameMangler,
                  new ReadyToRunTableManager(context))
        {
            Resolver = moduleTokenResolver;
            InputModuleContext = signatureContext;
            CopiedCorHeaderNode = corHeaderNode;
            AttributePresenceFilter = attributePresenceFilterNode;
            if (!win32Resources.IsEmpty)
                Win32ResourcesNode = new Win32ResourcesNode(win32Resources);

            CreateNodeCaches();
        }

        private void CreateNodeCaches()
        {
            // Create node caches
            _constructedHelpers = new NodeCache<ReadyToRunHelper, ISymbolNode>(CreateReadyToRunHelperCell);

            _importMethods = new NodeCache<TypeAndMethod, IMethodNode>(CreateMethodEntrypoint);

            _localMethodCache = new NodeCache<TypeAndMethod, MethodWithGCInfo>(key =>
            {
                return new MethodWithGCInfo(key.Method.Method, key.SignatureContext);
            });

            _methodSignatures = new NodeCache<MethodFixupKey, MethodFixupSignature>(key =>
            {
                return new MethodFixupSignature(
                    key.FixupKind,
                    key.TypeAndMethod.Method,
                    key.TypeAndMethod.SignatureContext,
                    key.TypeAndMethod.IsUnboxingStub,
                    key.TypeAndMethod.IsInstantiatingStub
                );
            });

            _typeSignatures = new NodeCache<TypeFixupKey, TypeFixupSignature>(key =>
            {
                return new TypeFixupSignature(key.FixupKind, key.TypeDesc, InputModuleContext);
            });

            _dynamicHelperCellCache = new NodeCache<DynamicHelperCellKey, ISymbolNode>(key =>
            {
                return new DelayLoadHelperMethodImport(
                    this,
                    DispatchImports,
                    ILCompiler.ReadyToRunHelper.DelayLoad_Helper_Obj,
                    key.Method,
                    useVirtualCall: false,
                    useInstantiatingStub: true,
                    MethodSignature(
                        ReadyToRunFixupKind.READYTORUN_FIXUP_VirtualEntry,
                        key.Method,
                        signatureContext: key.SignatureContext,
                        isUnboxingStub: key.IsUnboxingStub,
                        isInstantiatingStub: key.IsInstantiatingStub),
                    key.SignatureContext);
            });

            _copiedCorHeaders = new NodeCache<EcmaModule, CopiedCorHeaderNode>(module =>
            {
                return new CopiedCorHeaderNode(module);
            });

            _copiedMetadataBlobs = new NodeCache<EcmaModule, CopiedMetadataBlobNode>(module =>
            {
                return new CopiedMetadataBlobNode(module);
            });

            _copiedMethodIL = new NodeCache<MethodDesc, CopiedMethodILNode>(method =>
            {
                return new CopiedMethodILNode((EcmaMethod)method);
            });

            _copiedFieldRvas = new NodeCache<EcmaField, CopiedFieldRvaNode>(ecmaField =>
            {
                return new CopiedFieldRvaNode(ecmaField);
            });

            _copiedStrongNameSignatures = new NodeCache<EcmaModule, CopiedStrongNameSignatureNode>(module =>
            {
                return new CopiedStrongNameSignatureNode(module);
            });

            _copiedManagedResources = new NodeCache<EcmaModule, CopiedManagedResourcesNode>(module =>
            {
                return new CopiedManagedResourcesNode(module);
            });

            _profileDataCountsNodes = new NodeCache<MethodWithGCInfo, ProfileDataNode>(method =>
            {
                return new ProfileDataNode(method, Target);
            });
        }

        public SignatureContext InputModuleContext;

        public ModuleTokenResolver Resolver;

        public CopiedCorHeaderNode CopiedCorHeaderNode;

        public Win32ResourcesNode Win32ResourcesNode;

        public HeaderNode Header;

        public RuntimeFunctionsTableNode RuntimeFunctionsTable;

        public RuntimeFunctionsGCInfoNode RuntimeFunctionsGCInfo;

        public ProfileDataSectionNode ProfileDataSection;

        public MethodEntryPointTableNode MethodEntryPointTable;

        public InstanceEntryPointTableNode InstanceEntryPointTable;

        public ManifestMetadataTableNode ManifestMetadataTable;

        public TypesTableNode TypesTable;

        public ImportSectionsTableNode ImportSectionsTable;

        public Import ModuleImport;

        public ISymbolNode PersonalityRoutine;

        public ISymbolNode FilterFuncletPersonalityRoutine;

        public DebugInfoTableNode DebugInfoTable;

        public AttributePresenceFilterNode AttributePresenceFilter;

        public ImportSectionNode EagerImports;

        public ImportSectionNode MethodImports;

        public ImportSectionNode DispatchImports;

        public ImportSectionNode StringImports;

        public ImportSectionNode HelperImports;

        public ImportSectionNode PrecodeImports;

        private NodeCache<ReadyToRunHelper, ISymbolNode> _constructedHelpers;

        public ISymbolNode GetReadyToRunHelperCell(ReadyToRunHelper helperId)
        {
            return _constructedHelpers.GetOrAdd(helperId);
        }

        private ISymbolNode CreateReadyToRunHelperCell(ReadyToRunHelper helperId)
        {
            return new Import(EagerImports, new ReadyToRunHelperSignature(helperId));
        }


        private NodeCache<TypeAndMethod, IMethodNode> _importMethods;

        private IMethodNode CreateMethodEntrypoint(TypeAndMethod key)
        {
            MethodWithToken method = key.Method;
            bool isUnboxingStub = key.IsUnboxingStub;
            bool isInstantiatingStub = key.IsInstantiatingStub;
            bool isPrecodeImportRequired = key.IsPrecodeImportRequired;
            SignatureContext signatureContext = key.SignatureContext;
            if (CompilationModuleGroup.ContainsMethodBody(method.Method, false))
            {
                if (isPrecodeImportRequired)
                {
                    return new PrecodeMethodImport(
                        this,
                        ReadyToRunFixupKind.READYTORUN_FIXUP_MethodEntry,
                        method,
                        CreateMethodEntrypointNodeHelper(method, isUnboxingStub, isInstantiatingStub, signatureContext),
                        isUnboxingStub,
                        isInstantiatingStub,
                        signatureContext);
                }
                else
                {
                    return new LocalMethodImport(
                        this,
                        ReadyToRunFixupKind.READYTORUN_FIXUP_MethodEntry,
                        method,
                        CreateMethodEntrypointNodeHelper(method, isUnboxingStub, isInstantiatingStub, signatureContext),
                        isUnboxingStub,
                        isInstantiatingStub,
                        signatureContext);
                }
            }
            else
            {
                // First time we see a given external method - emit indirection cell and the import entry
                return new ExternalMethodImport(
                    this,
                    ReadyToRunFixupKind.READYTORUN_FIXUP_MethodEntry,
                    method,
                    isUnboxingStub,
                    isInstantiatingStub,
                    signatureContext);
            }
        }

        public IMethodNode MethodEntrypoint(
            MethodWithToken method,
            bool isUnboxingStub,
            bool isInstantiatingStub,
            bool isPrecodeImportRequired,
            SignatureContext signatureContext)
        {
            TypeAndMethod key = new TypeAndMethod(method.ConstrainedType, method, isUnboxingStub, isInstantiatingStub, isPrecodeImportRequired, signatureContext);
            return _importMethods.GetOrAdd(key);
        }

        private NodeCache<TypeAndMethod, MethodWithGCInfo> _localMethodCache = new NodeCache<TypeAndMethod, MethodWithGCInfo>();

        private MethodWithGCInfo CreateMethodEntrypointNodeHelper(MethodWithToken targetMethod, bool isUnboxingStub, bool isInstantiatingStub, SignatureContext signatureContext)
        {
            Debug.Assert(CompilationModuleGroup.ContainsMethodBody(targetMethod.Method, false));

            MethodDesc localMethod = targetMethod.Method.GetCanonMethodTarget(CanonicalFormKind.Specific);

            TypeAndMethod localMethodKey = new TypeAndMethod(localMethod.OwningType,
                new MethodWithToken(localMethod, default(ModuleToken), constrainedType: null),
                isUnboxingStub: false,
                isInstantiatingStub: false,
                isPrecodeImportRequired: false,
                signatureContext);
            return _localMethodCache.GetOrAdd(localMethodKey);
        }

        public IEnumerable<MethodWithGCInfo> EnumerateCompiledMethods()
        {
            foreach (MethodDesc method in MetadataManager.GetCompiledMethods())
            {
                IMethodNode methodNode = MethodEntrypoint(method);
                MethodWithGCInfo methodCodeNode = methodNode as MethodWithGCInfo;
                if (methodCodeNode == null && methodNode is LocalMethodImport localMethodImport)
                {
                    methodCodeNode = localMethodImport.MethodCodeNode;
                }
                if (methodCodeNode == null && methodNode is PrecodeMethodImport PrecodeMethodImport)
                {
                    methodCodeNode = PrecodeMethodImport.MethodCodeNode;
                }

                if (methodCodeNode != null && !methodCodeNode.IsEmpty)
                {
                    yield return methodCodeNode;
                }
            }
        }

        private struct MethodFixupKey : IEquatable<MethodFixupKey>
        {
            public readonly ReadyToRunFixupKind FixupKind;
            public readonly TypeAndMethod TypeAndMethod;

            public MethodFixupKey(ReadyToRunFixupKind fixupKind, TypeAndMethod typeAndMethod)
            {
                FixupKind = fixupKind;
                TypeAndMethod = typeAndMethod;
            }

            public bool Equals(MethodFixupKey other)
            {
                return FixupKind == other.FixupKind && TypeAndMethod.Equals(other.TypeAndMethod);
            }

            public override bool Equals(object obj)
            {
                return obj is MethodFixupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return FixupKind.GetHashCode() ^ TypeAndMethod.GetHashCode();
            }
        }

        private NodeCache<MethodFixupKey, MethodFixupSignature> _methodSignatures;

        public MethodFixupSignature MethodSignature(
            ReadyToRunFixupKind fixupKind,
            MethodWithToken method,
            bool isUnboxingStub,
            bool isInstantiatingStub,
            SignatureContext signatureContext)
        {
            TypeAndMethod key = new TypeAndMethod(method.ConstrainedType, method, isUnboxingStub, isInstantiatingStub, false, signatureContext);
            return _methodSignatures.GetOrAdd(new MethodFixupKey(fixupKind, key));
        }

        private struct TypeFixupKey : IEquatable<TypeFixupKey>
        {
            public readonly ReadyToRunFixupKind FixupKind;
            public readonly TypeDesc TypeDesc;
            public readonly SignatureContext SignatureContext;
            public TypeFixupKey(ReadyToRunFixupKind fixupKind, TypeDesc typeDesc, SignatureContext signatureContext)
            {
                FixupKind = fixupKind;
                TypeDesc = typeDesc;
                SignatureContext = signatureContext;
            }

            public bool Equals(TypeFixupKey other)
            {
                return FixupKind == other.FixupKind 
                    && TypeDesc == other.TypeDesc 
                    && SignatureContext.Equals(other.SignatureContext);
            }

            public override bool Equals(object obj)
            {
                return obj is TypeFixupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return FixupKind.GetHashCode()
                    ^ (31 * TypeDesc.GetHashCode())
                    ^ (23 * SignatureContext.GetHashCode());
            }
        }

        private NodeCache<TypeFixupKey, TypeFixupSignature> _typeSignatures;

        public TypeFixupSignature TypeSignature(ReadyToRunFixupKind fixupKind, TypeDesc typeDesc, SignatureContext signatureContext)
        {
            TypeFixupKey fixupKey = new TypeFixupKey(fixupKind, typeDesc, signatureContext);
            return _typeSignatures.GetOrAdd(fixupKey);
        }

        public override void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph)
        {
            Header = new HeaderNode(Target);

            var compilerIdentifierNode = new CompilerIdentifierNode(Target);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.CompilerIdentifier, compilerIdentifierNode, compilerIdentifierNode);

            RuntimeFunctionsTable = new RuntimeFunctionsTableNode(this);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.RuntimeFunctions, RuntimeFunctionsTable, RuntimeFunctionsTable);

            RuntimeFunctionsGCInfo = new RuntimeFunctionsGCInfoNode();
            graph.AddRoot(RuntimeFunctionsGCInfo, "GC info is always generated");

            ProfileDataSection = new ProfileDataSectionNode();
            Header.Add(Internal.Runtime.ReadyToRunSectionType.ProfileDataInfo, ProfileDataSection, ProfileDataSection.StartSymbol);

            ExceptionInfoLookupTableNode exceptionInfoLookupTableNode = new ExceptionInfoLookupTableNode(this);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.ExceptionInfo, exceptionInfoLookupTableNode, exceptionInfoLookupTableNode);
            graph.AddRoot(exceptionInfoLookupTableNode, "ExceptionInfoLookupTable is always generated");

            MethodEntryPointTable = new MethodEntryPointTableNode(Target);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.MethodDefEntryPoints, MethodEntryPointTable, MethodEntryPointTable);

            ManifestMetadataTable = new ManifestMetadataTableNode(InputModuleContext.GlobalContext);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.ManifestMetadata, ManifestMetadataTable, ManifestMetadataTable);

            Resolver.SetModuleIndexLookup(ManifestMetadataTable.ModuleToIndex);

            InstanceEntryPointTable = new InstanceEntryPointTableNode(Target);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.InstanceMethodEntryPoints, InstanceEntryPointTable, InstanceEntryPointTable);

            TypesTable = new TypesTableNode(Target);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.AvailableTypes, TypesTable, TypesTable);

            ImportSectionsTable = new ImportSectionsTableNode(this);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.ImportSections, ImportSectionsTable, ImportSectionsTable.StartSymbol);

            DebugInfoTable = new DebugInfoTableNode(Target);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.DebugInfo, DebugInfoTable, DebugInfoTable);

            // Core library attributes are checked FAR more often than other dlls
            // attributes, so produce a highly efficient table for determining if they are
            // present. Other assemblies *MAY* benefit from this feature, but it doesn't show
            // as useful at this time.
            if (this.AttributePresenceFilter != null)
            {
                Header.Add(Internal.Runtime.ReadyToRunSectionType.AttributePresence, AttributePresenceFilter, AttributePresenceFilter);
            }

            EagerImports = new ImportSectionNode(
                "EagerImports",
                CorCompileImportType.CORCOMPILE_IMPORT_TYPE_UNKNOWN,
                CorCompileImportFlags.CORCOMPILE_IMPORT_FLAGS_EAGER,
                (byte)Target.PointerSize,
                emitPrecode: false,
                emitGCRefMap: false);
            ImportSectionsTable.AddEmbeddedObject(EagerImports);

            // All ready-to-run images have a module import helper which gets patched by the runtime on image load
            ModuleImport = new Import(EagerImports, new ReadyToRunHelperSignature(
                ILCompiler.ReadyToRunHelper.Module));
            graph.AddRoot(ModuleImport, "Module import is required by the R2R format spec");

            if (Target.Architecture != TargetArchitecture.X86)
            {
                Import personalityRoutineImport = new Import(EagerImports, new ReadyToRunHelperSignature(
                    ILCompiler.ReadyToRunHelper.PersonalityRoutine));
                PersonalityRoutine = new ImportThunk(
                    ILCompiler.ReadyToRunHelper.PersonalityRoutine, this, personalityRoutineImport, useVirtualCall: false);
                graph.AddRoot(PersonalityRoutine, "Personality routine is faster to root early rather than referencing it from each unwind info");

                Import filterFuncletPersonalityRoutineImport = new Import(EagerImports, new ReadyToRunHelperSignature(
                    ILCompiler.ReadyToRunHelper.PersonalityRoutineFilterFunclet));
                FilterFuncletPersonalityRoutine = new ImportThunk(
                    ILCompiler.ReadyToRunHelper.PersonalityRoutineFilterFunclet, this, filterFuncletPersonalityRoutineImport, useVirtualCall: false);
                graph.AddRoot(FilterFuncletPersonalityRoutine, "Filter funclet personality routine is faster to root early rather than referencing it from each unwind info");
            }

            MethodImports = new ImportSectionNode(
                "MethodImports",
                CorCompileImportType.CORCOMPILE_IMPORT_TYPE_STUB_DISPATCH,
                CorCompileImportFlags.CORCOMPILE_IMPORT_FLAGS_PCODE,
                (byte)Target.PointerSize,
                emitPrecode: false,
                emitGCRefMap: true);
            ImportSectionsTable.AddEmbeddedObject(MethodImports);

            DispatchImports = new ImportSectionNode(
                "DispatchImports",
                CorCompileImportType.CORCOMPILE_IMPORT_TYPE_STUB_DISPATCH,
                CorCompileImportFlags.CORCOMPILE_IMPORT_FLAGS_PCODE,
                (byte)Target.PointerSize,
                emitPrecode: false,
                emitGCRefMap: true);
            ImportSectionsTable.AddEmbeddedObject(DispatchImports);

            HelperImports = new ImportSectionNode(
                "HelperImports",
                CorCompileImportType.CORCOMPILE_IMPORT_TYPE_UNKNOWN,
                CorCompileImportFlags.CORCOMPILE_IMPORT_FLAGS_PCODE,
                (byte)Target.PointerSize,
                emitPrecode: false,
                emitGCRefMap: false);
            ImportSectionsTable.AddEmbeddedObject(HelperImports);

            PrecodeImports = new ImportSectionNode(
                "PrecodeImports",
                CorCompileImportType.CORCOMPILE_IMPORT_TYPE_UNKNOWN,
                CorCompileImportFlags.CORCOMPILE_IMPORT_FLAGS_PCODE,
                (byte)Target.PointerSize,
                emitPrecode: true,
                emitGCRefMap: false);
            ImportSectionsTable.AddEmbeddedObject(PrecodeImports);

            StringImports = new ImportSectionNode(
                "StringImports",
                CorCompileImportType.CORCOMPILE_IMPORT_TYPE_STRING_HANDLE,
                CorCompileImportFlags.CORCOMPILE_IMPORT_FLAGS_UNKNOWN,
                (byte)Target.PointerSize,
                emitPrecode: true,
                emitGCRefMap: false);
            ImportSectionsTable.AddEmbeddedObject(StringImports);

            graph.AddRoot(ImportSectionsTable, "Import sections table is always generated");
            graph.AddRoot(ModuleImport, "Module import is always generated");
            graph.AddRoot(EagerImports, "Eager imports are always generated");
            graph.AddRoot(MethodImports, "Method imports are always generated");
            graph.AddRoot(DispatchImports, "Dispatch imports are always generated");
            graph.AddRoot(HelperImports, "Helper imports are always generated");
            graph.AddRoot(PrecodeImports, "Precode helper imports are always generated");
            graph.AddRoot(StringImports, "String imports are always generated");
            graph.AddRoot(Header, "ReadyToRunHeader is always generated");
            graph.AddRoot(CopiedCorHeaderNode, "MSIL COR header is always generated");

            if (Win32ResourcesNode != null)
                graph.AddRoot(Win32ResourcesNode, "Win32 Resources are placed if not empty");

            MetadataManager.AttachToDependencyGraph(graph);
        }

        protected override IEETypeNode CreateNecessaryTypeNode(TypeDesc type)
        {
            if (CompilationModuleGroup.ContainsType(type))
            {
                return new AvailableType(this, type);
            }
            else
            {
                return new ExternalTypeNode(this, type);
            }
        }

        protected override IEETypeNode CreateConstructedTypeNode(TypeDesc type)
        {
            // Canonical definition types are *not* constructed types (call NecessaryTypeSymbol to get them)
            Debug.Assert(!type.IsCanonicalDefinitionType(CanonicalFormKind.Any));

            if (CompilationModuleGroup.ContainsType(type))
            {
                return new AvailableType(this, type);
            }
            else
            {
                return new ExternalTypeNode(this, type);
            }
        }

        protected override IMethodNode CreateMethodEntrypointNode(MethodDesc method)
        {
            ModuleToken moduleToken = Resolver.GetModuleTokenForMethod(method, throwIfNotFound: true);
            return MethodEntrypoint(
                new MethodWithToken(method, moduleToken, constrainedType: null),
                isUnboxingStub: false,
                isInstantiatingStub: false,
                isPrecodeImportRequired: false,
                signatureContext: InputModuleContext);
        }

        private ReadyToRunHelper GetGenericStaticHelper(ReadyToRunHelperId helperId)
        {
            ReadyToRunHelper r2rHelper;

            switch (helperId)
            {
                case ReadyToRunHelperId.GetGCStaticBase:
                    r2rHelper = ILCompiler.ReadyToRunHelper.GenericGcStaticBase;
                    break;

                case ReadyToRunHelperId.GetNonGCStaticBase:
                    r2rHelper = ILCompiler.ReadyToRunHelper.GenericNonGcStaticBase;
                    break;

                case ReadyToRunHelperId.GetThreadStaticBase:
                    r2rHelper = ILCompiler.ReadyToRunHelper.GenericGcTlsBase;
                    break;

                case ReadyToRunHelperId.GetThreadNonGcStaticBase:
                    r2rHelper = ILCompiler.ReadyToRunHelper.GenericNonGcTlsBase;
                    break;

                default:
                    throw new NotImplementedException();
            }

            return r2rHelper;
        }

        protected override ISymbolNode CreateGenericLookupFromDictionaryNode(ReadyToRunGenericHelperKey helperKey)
        {
            return new DelayLoadHelperImport(
                this,
                HelperImports,
                GetGenericStaticHelper(helperKey.HelperId),
                TypeSignature(
                    ReadyToRunFixupKind.READYTORUN_FIXUP_Invalid,
                    (TypeDesc)helperKey.Target,
                    InputModuleContext));
        }

        protected override ISymbolNode CreateGenericLookupFromTypeNode(ReadyToRunGenericHelperKey helperKey)
        {
            return new DelayLoadHelperImport(
                this,
                HelperImports,
                GetGenericStaticHelper(helperKey.HelperId),
                TypeSignature(
                    ReadyToRunFixupKind.READYTORUN_FIXUP_Invalid,
                    (TypeDesc)helperKey.Target,
                    InputModuleContext));
        }

        struct DynamicHelperCellKey : IEquatable<DynamicHelperCellKey>
        {
            public readonly MethodWithToken Method;
            public readonly bool IsUnboxingStub;
            public readonly bool IsInstantiatingStub;
            public readonly SignatureContext SignatureContext;

            public DynamicHelperCellKey(MethodWithToken method, bool isUnboxingStub, bool isInstantiatingStub, SignatureContext signatureContext)
            {
                Method = method;
                IsUnboxingStub = isUnboxingStub;
                IsInstantiatingStub = isInstantiatingStub;
                SignatureContext = signatureContext;
            }

            public bool Equals(DynamicHelperCellKey other)
            {
                return Method.Equals(other.Method)
                    && IsUnboxingStub == other.IsUnboxingStub
                    && IsInstantiatingStub == other.IsInstantiatingStub
                    && SignatureContext.Equals(other.SignatureContext);
            }

            public override bool Equals(object obj)
            {
                return obj is DynamicHelperCellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Method.GetHashCode()
                     ^ (IsUnboxingStub ? -0x80000000 : 0)
                     ^ (IsInstantiatingStub ? -0x40000000 : 0) 
                     ^ (31 * SignatureContext.GetHashCode());
            }
        }

        private NodeCache<DynamicHelperCellKey, ISymbolNode> _dynamicHelperCellCache;

        public ISymbolNode DynamicHelperCell(MethodWithToken methodWithToken, bool isInstantiatingStub, SignatureContext signatureContext)
        {
            DynamicHelperCellKey key = new DynamicHelperCellKey(methodWithToken, isUnboxingStub: false, isInstantiatingStub, signatureContext);
            return _dynamicHelperCellCache.GetOrAdd(key);
        }

        private NodeCache<EcmaModule, CopiedCorHeaderNode> _copiedCorHeaders;

        public CopiedCorHeaderNode CopiedCorHeader(EcmaModule module)
        {
            return _copiedCorHeaders.GetOrAdd(module);
        }

        private NodeCache<EcmaModule, CopiedMetadataBlobNode> _copiedMetadataBlobs;

        public CopiedMetadataBlobNode CopiedMetadataBlob(EcmaModule module)
        {
            return _copiedMetadataBlobs.GetOrAdd(module);
        }

        private NodeCache<MethodDesc, CopiedMethodILNode> _copiedMethodIL;

        public CopiedMethodILNode CopiedMethodIL(EcmaMethod method)
        {
            return _copiedMethodIL.GetOrAdd(method);
        }

        private NodeCache<EcmaField, CopiedFieldRvaNode> _copiedFieldRvas;

        public CopiedFieldRvaNode CopiedFieldRva(FieldDesc field)
        {
            Debug.Assert(field.HasRva);
            EcmaField ecmaField = (EcmaField)field.GetTypicalFieldDefinition();

            if (!CompilationModuleGroup.ContainsType(ecmaField.OwningType))
            {
                // TODO: cross-bubble RVA field
                throw new NotSupportedException($"{ecmaField} ... {ecmaField.Module.Assembly}");
            }
            if (TypeSystemContext.InputFilePaths.Count > 1)
            {
                // TODO: RVA fields in merged multi-file compilation
                throw new NotSupportedException($"{ecmaField} ... {string.Join("; ", TypeSystemContext.InputFilePaths.Keys)}");
            }

            return _copiedFieldRvas.GetOrAdd(ecmaField);
        }

        private NodeCache<EcmaModule, CopiedStrongNameSignatureNode> _copiedStrongNameSignatures;

        public CopiedStrongNameSignatureNode CopiedStrongNameSignature(EcmaModule module)
        {
            return _copiedStrongNameSignatures.GetOrAdd(module);
        }

        private NodeCache<EcmaModule, CopiedManagedResourcesNode> _copiedManagedResources;

        public CopiedManagedResourcesNode CopiedManagedResources(EcmaModule module)
        {
            return _copiedManagedResources.GetOrAdd(module);
        }

        private NodeCache<MethodWithGCInfo, ProfileDataNode> _profileDataCountsNodes;

        public ProfileDataNode ProfileData(MethodWithGCInfo method)
        {
            return _profileDataCountsNodes.GetOrAdd(method);
        }
    }
}

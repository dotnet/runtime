// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.Text;
using Internal.TypeSystem.Ecma;
using ILCompiler.Win32Resources;

namespace ILCompiler.DependencyAnalysis
{
    // TODO-REFACTOR: rename this
    public interface IEETypeNode
    {
        TypeDesc Type { get; }
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

        protected struct NodeCache<TKey, TValue>
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

            public TValue GetOrAdd(TKey key, Func<TKey, TValue> creator)
            {
                return _cache.GetOrAdd(key, creator);
            }
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

    public sealed class ReadyToRunCodegenNodeFactory : NodeFactory
    {
        private Dictionary<TypeAndMethod, IMethodNode> _importMethods;

        public ReadyToRunCodegenNodeFactory(
            CompilerTypeSystemContext context,
            CompilationModuleGroup compilationModuleGroup,
            NameMangler nameMangler,
            ModuleTokenResolver moduleTokenResolver,
            SignatureContext signatureContext,
            CopiedCorHeaderNode corHeaderNode,
            ResourceData win32Resources)
            : base(context,
                  compilationModuleGroup,
                  nameMangler,
                  new ReadyToRunTableManager(context))
        {
            _importMethods = new Dictionary<TypeAndMethod, IMethodNode>();

            Resolver = moduleTokenResolver;
            InputModuleContext = signatureContext;
            CopiedCorHeaderNode = corHeaderNode;
            if (!win32Resources.IsEmpty)
                Win32ResourcesNode = new Win32ResourcesNode(win32Resources);
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

        public ImportSectionNode EagerImports;

        public ImportSectionNode MethodImports;

        public ImportSectionNode DispatchImports;

        public ImportSectionNode StringImports;

        public ImportSectionNode HelperImports;

        public ImportSectionNode PrecodeImports;

        private readonly Dictionary<ReadyToRunHelper, ISymbolNode> _constructedHelpers = new Dictionary<ReadyToRunHelper, ISymbolNode>();

        public ISymbolNode GetReadyToRunHelperCell(ReadyToRunHelper helperId)
        {
            if (!_constructedHelpers.TryGetValue(helperId, out ISymbolNode helperCell))
            {
                helperCell = CreateReadyToRunHelperCell(helperId);
                _constructedHelpers.Add(helperId, helperCell);
            }
            return helperCell;
        }

        private ISymbolNode CreateReadyToRunHelperCell(ReadyToRunHelper helperId)
        {
            return new Import(EagerImports, new ReadyToRunHelperSignature(helperId));
        }

        public IMethodNode MethodEntrypoint(
            MethodWithToken method,
            bool isUnboxingStub,
            bool isInstantiatingStub,
            bool isPrecodeImportRequired,
            SignatureContext signatureContext)
        {
            IMethodNode methodImport;
            TypeAndMethod key = new TypeAndMethod(method.ConstrainedType, method, isUnboxingStub, isInstantiatingStub, isPrecodeImportRequired);
            if (!_importMethods.TryGetValue(key, out methodImport))
            {
                if (CompilationModuleGroup.ContainsMethodBody(method.Method, false))
                {
                    if (isPrecodeImportRequired)
                    {
                        methodImport = new PrecodeMethodImport(
                            this,
                            ReadyToRunFixupKind.READYTORUN_FIXUP_MethodEntry,
                            method,
                            CreateMethodEntrypointNode(method, isUnboxingStub, isInstantiatingStub, signatureContext),
                            isUnboxingStub,
                            isInstantiatingStub,
                            signatureContext);
                    }
                    else
                    {
                        methodImport = new LocalMethodImport(
                            this,
                            ReadyToRunFixupKind.READYTORUN_FIXUP_MethodEntry,
                            method,
                            CreateMethodEntrypointNode(method, isUnboxingStub, isInstantiatingStub, signatureContext),
                            isUnboxingStub,
                            isInstantiatingStub,
                            signatureContext);
                    }
                }
                else
                {
                    // First time we see a given external method - emit indirection cell and the import entry
                    methodImport = new ExternalMethodImport(
                        this,
                        ReadyToRunFixupKind.READYTORUN_FIXUP_MethodEntry,
                        method,
                        isUnboxingStub,
                        isInstantiatingStub,
                        signatureContext);
                }
                _importMethods.Add(key, methodImport);
            }

            return methodImport;
        }

        private readonly Dictionary<TypeAndMethod, MethodWithGCInfo> _localMethodCache = new Dictionary<TypeAndMethod, MethodWithGCInfo>();

        private MethodWithGCInfo CreateMethodEntrypointNode(MethodWithToken targetMethod, bool isUnboxingStub, bool isInstantiatingStub, SignatureContext signatureContext)
        {
            Debug.Assert(CompilationModuleGroup.ContainsMethodBody(targetMethod.Method, false));

            MethodDesc localMethod = targetMethod.Method.GetCanonMethodTarget(CanonicalFormKind.Specific);

            TypeAndMethod localMethodKey = new TypeAndMethod(localMethod.OwningType,
                new MethodWithToken(localMethod, default(ModuleToken), constrainedType: null),
                isUnboxingStub: false, isInstantiatingStub: false, isPrecodeImportRequired: false);
            MethodWithGCInfo localMethodNode;
            if (!_localMethodCache.TryGetValue(localMethodKey, out localMethodNode))
            {
                localMethodNode = new MethodWithGCInfo(localMethod, signatureContext);
                _localMethodCache.Add(localMethodKey, localMethodNode);
            }

            return localMethodNode;
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

        private readonly Dictionary<ReadyToRunFixupKind, Dictionary<TypeAndMethod, MethodFixupSignature>> _methodSignatures =
            new Dictionary<ReadyToRunFixupKind, Dictionary<TypeAndMethod, MethodFixupSignature>>();

        public MethodFixupSignature MethodSignature(
            ReadyToRunFixupKind fixupKind,
            MethodWithToken method,
            bool isUnboxingStub,
            bool isInstantiatingStub,
            SignatureContext signatureContext)
        {
            Dictionary<TypeAndMethod, MethodFixupSignature> perFixupKindMap;
            if (!_methodSignatures.TryGetValue(fixupKind, out perFixupKindMap))
            {
                perFixupKindMap = new Dictionary<TypeAndMethod, MethodFixupSignature>();
                _methodSignatures.Add(fixupKind, perFixupKindMap);
            }

            TypeAndMethod key = new TypeAndMethod(method.ConstrainedType, method, isUnboxingStub, isInstantiatingStub, false);
            MethodFixupSignature signature;
            if (!perFixupKindMap.TryGetValue(key, out signature))
            {
                signature = new MethodFixupSignature(fixupKind, method, signatureContext, isUnboxingStub, isInstantiatingStub);
                perFixupKindMap.Add(key, signature);
            }
            return signature;
        }

        private readonly Dictionary<ReadyToRunFixupKind, Dictionary<TypeDesc, TypeFixupSignature>> _typeSignatures =
            new Dictionary<ReadyToRunFixupKind, Dictionary<TypeDesc, TypeFixupSignature>>();

        public TypeFixupSignature TypeSignature(ReadyToRunFixupKind fixupKind, TypeDesc typeDesc, SignatureContext signatureContext)
        {
            Dictionary<TypeDesc, TypeFixupSignature> perFixupKindMap;
            if (!_typeSignatures.TryGetValue(fixupKind, out perFixupKindMap))
            {
                perFixupKindMap = new Dictionary<TypeDesc, TypeFixupSignature>();
                _typeSignatures.Add(fixupKind, perFixupKindMap);
            }

            TypeFixupSignature signature;
            if (!perFixupKindMap.TryGetValue(typeDesc, out signature))
            {
                signature = new TypeFixupSignature(fixupKind, typeDesc, signatureContext);
                perFixupKindMap.Add(typeDesc, signature);
            }
            return signature;
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

        private Dictionary<MethodWithToken, ISymbolNode> _dynamicHelperCellCache = new Dictionary<MethodWithToken, ISymbolNode>();

        public ISymbolNode DynamicHelperCell(MethodWithToken methodWithToken, bool isInstantiatingStub, SignatureContext signatureContext)
        {
            ISymbolNode result;
            if (!_dynamicHelperCellCache.TryGetValue(methodWithToken, out result))
            {
                result = new DelayLoadHelperMethodImport(
                    this,
                    DispatchImports,
                    ILCompiler.ReadyToRunHelper.DelayLoad_Helper_Obj,
                    methodWithToken,
                    useVirtualCall: false,
                    useInstantiatingStub: true,
                    MethodSignature(
                        ReadyToRunFixupKind.READYTORUN_FIXUP_VirtualEntry,
                        methodWithToken,
                        signatureContext: signatureContext,
                        isUnboxingStub: false,
                        isInstantiatingStub: isInstantiatingStub),
                    signatureContext);
                _dynamicHelperCellCache.Add(methodWithToken, result);
            }
            return result;
        }

        private Dictionary<EcmaModule, CopiedCorHeaderNode> _copiedCorHeaders = new Dictionary<EcmaModule, CopiedCorHeaderNode>();

        public CopiedCorHeaderNode CopiedCorHeader(EcmaModule module)
        {
            CopiedCorHeaderNode result;
            if (!_copiedCorHeaders.TryGetValue(module, out result))
            {
                result = new CopiedCorHeaderNode(module);
                _copiedCorHeaders.Add(module, result);
            }

            return result;
        }

        private Dictionary<EcmaModule, CopiedMetadataBlobNode> _copiedMetadataBlobs = new Dictionary<EcmaModule, CopiedMetadataBlobNode>();

        public CopiedMetadataBlobNode CopiedMetadataBlob(EcmaModule module)
        {
            CopiedMetadataBlobNode result;
            if (!_copiedMetadataBlobs.TryGetValue(module, out result))
            {
                result = new CopiedMetadataBlobNode(module);
                _copiedMetadataBlobs.Add(module, result);
            }

            return result;
        }

        private Dictionary<MethodDesc, CopiedMethodILNode> _copiedMethodIL = new Dictionary<MethodDesc, CopiedMethodILNode>();

        public CopiedMethodILNode CopiedMethodIL(EcmaMethod method)
        {
            CopiedMethodILNode result;
            if (!_copiedMethodIL.TryGetValue(method, out result))
            {
                result = new CopiedMethodILNode(method);
                _copiedMethodIL.Add(method, result);
            }

            return result;
        }

        private Dictionary<EcmaField, CopiedFieldRvaNode> _copiedFieldRvas = new Dictionary<EcmaField, CopiedFieldRvaNode>();

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

            CopiedFieldRvaNode result;
            if (!_copiedFieldRvas.TryGetValue(ecmaField, out result))
            {
                result = new CopiedFieldRvaNode(ecmaField);
                _copiedFieldRvas.Add(ecmaField, result);
            }

            return result;
        }

        private Dictionary<EcmaModule, CopiedStrongNameSignatureNode> _copiedStrongNameSignatures = new Dictionary<EcmaModule, CopiedStrongNameSignatureNode>();

        public CopiedStrongNameSignatureNode CopiedStrongNameSignature(EcmaModule module)
        {
            CopiedStrongNameSignatureNode result;
            if (!_copiedStrongNameSignatures.TryGetValue(module, out result))
            {
                result = new CopiedStrongNameSignatureNode(module);
                _copiedStrongNameSignatures.Add(module, result);
            }

            return result;
        }

        private Dictionary<EcmaModule, CopiedManagedResourcesNode> _copiedManagedResources = new Dictionary<EcmaModule, CopiedManagedResourcesNode>();

        public CopiedManagedResourcesNode CopiedManagedResources(EcmaModule module)
        {
            CopiedManagedResourcesNode result;
            if (!_copiedManagedResources.TryGetValue(module, out result))
            {
                result = new CopiedManagedResourcesNode(module);
                _copiedManagedResources.Add(module, result);
            }

            return result;
        }

        private readonly Dictionary<MethodWithGCInfo, ProfileDataNode> _profileDataCountsNodes = new Dictionary<MethodWithGCInfo, ProfileDataNode>();

        public ProfileDataNode ProfileDataNode(MethodWithGCInfo method)
        {
            ProfileDataNode node;
            if (!_profileDataCountsNodes.TryGetValue(method, out node))
            {
                node = new ProfileDataNode(method, Target);
                _profileDataCountsNodes.Add(method, node);
            }
            return node;
        }
    }
}

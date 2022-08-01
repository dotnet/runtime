// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using ILCompiler.DependencyAnalysis.ReadyToRun;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Win32Resources;

using Internal.IL;
using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.Text;
using Internal.TypeSystem.Ecma;
using Internal.CorConstants;
using Internal.ReadyToRunConstants;

namespace ILCompiler.DependencyAnalysis
{
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

    public sealed class NodeFactoryOptimizationFlags
    {
        public bool OptimizeAsyncMethods;
    }

    // To make the code future compatible to the composite R2R story
    // do NOT attempt to pass and store _inputModule here
    public sealed class NodeFactory
    {
        private bool _markingComplete;

        public CompilerTypeSystemContext TypeSystemContext { get; }

        public TargetDetails Target { get; }

        public ReadyToRunCompilationModuleGroupBase CompilationModuleGroup { get; }

        public ProfileDataManager ProfileDataManager { get; }

        public NameMangler NameMangler { get; }

        public MetadataManager MetadataManager { get; }

        public CompositeImageSettings CompositeImageSettings { get; set; }

        public readonly NodeFactoryOptimizationFlags OptimizationFlags;

        public ulong ImageBase;

        List<ILBodyFixupSignature> _markedILBodyFixupSignatures = new List<ILBodyFixupSignature>();

        public bool MarkingComplete => _markingComplete;

        public void SetMarkingComplete()
        {
            _markingComplete = true;
            ILCompiler.DependencyAnalysis.ReadyToRun.ILBodyFixupSignature.NotifyComplete(this, _markedILBodyFixupSignatures);
            _markedILBodyFixupSignatures = null;
        }

        public void AddMarkedILBodyFixupSignature(ILBodyFixupSignature sig)
        {
            _markedILBodyFixupSignatures.Add(sig);
        }

        private NodeCache<MethodDesc, MethodWithGCInfo> _localMethodCache;

        public MethodWithGCInfo CompiledMethodNode(MethodDesc method)
        {
            Debug.Assert(CompilationModuleGroup.ContainsMethodBody(method, false));
            Debug.Assert(method == method.GetCanonMethodTarget(CanonicalFormKind.Specific));
            return _localMethodCache.GetOrAdd(method);
        }

        private NodeCache<TypeDesc, AllMethodsOnTypeNode> _allMethodsOnType;

        public AllMethodsOnTypeNode AllMethodsOnType(TypeDesc type)
        {
            return _allMethodsOnType.GetOrAdd(type.ConvertToCanonForm(CanonicalFormKind.Specific));
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

        private struct ReadyToRunGenericHelperKey : IEquatable<ReadyToRunGenericHelperKey>
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

        private struct ModuleAndIntValueKey : IEquatable<ModuleAndIntValueKey>
        {
            public readonly int IntValue;
            public readonly EcmaModule Module;

            public ModuleAndIntValueKey(int integer, EcmaModule module)
            {
                IntValue = integer;
                Module = module;
            }

            public bool Equals(ModuleAndIntValueKey other) => IntValue == other.IntValue && ((Module == null && other.Module == null) || Module.Equals(other.Module));
            public override bool Equals(object obj) => obj is ModuleAndIntValueKey && Equals((ModuleAndIntValueKey)obj);
            public override int GetHashCode()
            {
                int hashCode = IntValue * 0x5498341 + 0x832424;
                if (Module == null)
                    return hashCode;
                return hashCode * 23 + Module.GetHashCode();
            }
        }

        public NodeFactory(
            CompilerTypeSystemContext context,
            ReadyToRunCompilationModuleGroupBase compilationModuleGroup,
            ProfileDataManager profileDataManager,
            NameMangler nameMangler,
            CopiedCorHeaderNode corHeaderNode,
            DebugDirectoryNode debugDirectoryNode,
            ResourceData win32Resources,
            ReadyToRunFlags flags,
            NodeFactoryOptimizationFlags nodeFactoryOptimizationFlags,
            ulong imageBase)
        {
            OptimizationFlags = nodeFactoryOptimizationFlags;
            TypeSystemContext = context;
            CompilationModuleGroup = compilationModuleGroup;
            ProfileDataManager = profileDataManager;
            Target = context.Target;
            NameMangler = nameMangler;
            MetadataManager = new ReadyToRunTableManager(context);
            CopiedCorHeaderNode = corHeaderNode;
            DebugDirectoryNode = debugDirectoryNode;
            Resolver = compilationModuleGroup.Resolver;
            Header = new GlobalHeaderNode(Target, flags);
            ImageBase = imageBase;
            if (!win32Resources.IsEmpty)
                Win32ResourcesNode = new Win32ResourcesNode(win32Resources);

            if (CompilationModuleGroup.IsCompositeBuildMode)
            {
                // Create a null top-level signature context to force producing module overrides for all signaturess
                SignatureContext = new SignatureContext(null, Resolver);
            }
            else
            {
                SignatureContext = new SignatureContext(CompilationModuleGroup.CompilationModuleSet.Single(), Resolver);
            }

            CreateNodeCaches();
        }

        private void CreateNodeCaches()
        {
            _allMethodsOnType = new NodeCache<TypeDesc, AllMethodsOnTypeNode>(type =>
            {
                return new AllMethodsOnTypeNode(type);
            });

            _genericReadyToRunHelpersFromDict = new NodeCache<ReadyToRunGenericHelperKey, ISymbolNode>(helperKey =>
            {
                return new DelayLoadHelperImport(
                    this,
                    HelperImports,
                    GetGenericStaticHelper(helperKey.HelperId),
                    TypeSignature(
                        ReadyToRunFixupKind.Invalid,
                        (TypeDesc)helperKey.Target));
            });

            _genericReadyToRunHelpersFromType = new NodeCache<ReadyToRunGenericHelperKey, ISymbolNode>(helperKey =>
            {
                return new DelayLoadHelperImport(
                    this,
                    HelperImports,
                    GetGenericStaticHelper(helperKey.HelperId),
                    TypeSignature(
                        ReadyToRunFixupKind.Invalid,
                        (TypeDesc)helperKey.Target));
            });

            _constructedHelpers = new NodeCache<ReadyToRunHelper, Import>(helperId =>
            {
                return new Import(EagerImports, new ReadyToRunHelperSignature(helperId));
            });

            _importThunks = new NodeCache<ImportThunkKey, ImportThunk>(key =>
            {
                return new ImportThunk(this, key.Helper, key.ContainingImportSection, key.UseVirtualCall, key.UseJumpableStub);
            });

            _importMethods = new NodeCache<TypeAndMethod, IMethodNode>(CreateMethodEntrypoint);

            _localMethodCache = new NodeCache<MethodDesc, MethodWithGCInfo>(key =>
            {
                return new MethodWithGCInfo(key);
            });

            _methodSignatures = new NodeCache<MethodFixupKey, MethodFixupSignature>(key =>
            {
                return new MethodFixupSignature(
                    key.FixupKind,
                    key.TypeAndMethod.Method,
                    key.TypeAndMethod.IsInstantiatingStub
                );
            });

            _typeSignatures = new NodeCache<TypeFixupKey, TypeFixupSignature>(key =>
            {
                return new TypeFixupSignature(key.FixupKind, key.TypeDesc);
            });

            _virtualResolutionSignatures = new NodeCache<VirtualResolutionFixupSignatureFixupKey, VirtualResolutionFixupSignature>(key =>
            {
                return new ReadyToRun.VirtualResolutionFixupSignature(key.FixupKind, key.DeclMethod, key.ImplType, key.ImplMethod);
            });

            _dynamicHelperCellCache = new NodeCache<DynamicHelperCellKey, ISymbolNode>(key =>
            {
                return new DelayLoadHelperMethodImport(
                    this,
                    DispatchImports,
                    ReadyToRunHelper.DelayLoad_Helper_Obj,
                    key.Method,
                    useVirtualCall: false,
                    useInstantiatingStub: true,
                    MethodSignature(
                        ReadyToRunFixupKind.VirtualEntry,
                        key.Method,
                        isInstantiatingStub: key.IsInstantiatingStub));
            });

            _copiedCorHeaders = new NodeCache<EcmaModule, CopiedCorHeaderNode>(module =>
            {
                return new CopiedCorHeaderNode(module);
            });

            _debugDirectoryEntries = new NodeCache<ModuleAndIntValueKey, DebugDirectoryEntryNode>(key =>
            {
                    return new CopiedDebugDirectoryEntryNode(key.Module, key.IntValue);
            });

            _copiedMetadataBlobs = new NodeCache<EcmaModule, CopiedMetadataBlobNode>(module =>
            {
                return new CopiedMetadataBlobNode(module);
            });

            _copiedMethodIL = new NodeCache<MethodDesc, CopiedMethodILNode>(method =>
            {
                return new CopiedMethodILNode((EcmaMethod)method);
            });

            _copiedFieldRvas = new NodeCache<ModuleAndIntValueKey, CopiedFieldRvaNode>(key =>
            {
                return new CopiedFieldRvaNode(key.Module, key.IntValue);
            });

            _copiedStrongNameSignatures = new NodeCache<EcmaModule, CopiedStrongNameSignatureNode>(module =>
            {
                return new CopiedStrongNameSignatureNode(module);
            });

            _copiedManagedResources = new NodeCache<EcmaModule, CopiedManagedResourcesNode>(module =>
            {
                return new CopiedManagedResourcesNode(module);
            });
        }

        public int CompilationCurrentPhase { get; private set; }

        public SignatureContext SignatureContext;

        public ModuleTokenResolver Resolver;

        public CopiedCorHeaderNode CopiedCorHeaderNode;

        public DebugDirectoryNode DebugDirectoryNode;

        public Win32ResourcesNode Win32ResourcesNode;

        public GlobalHeaderNode Header;

        public RuntimeFunctionsTableNode RuntimeFunctionsTable;

        public RuntimeFunctionsGCInfoNode RuntimeFunctionsGCInfo;

        public DelayLoadMethodCallThunkNodeRange DelayLoadMethodCallThunks;

        public InstanceEntryPointTableNode InstanceEntryPointTable;

        public ManifestMetadataTableNode ManifestMetadataTable;

        public ImportSectionsTableNode ImportSectionsTable;

        public InstrumentationDataTableNode InstrumentationDataTable;
        public InliningInfoNode CrossModuleInlningInfo;

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

        public ImportSectionNode ILBodyPrecodeImports;

        private NodeCache<ReadyToRunHelper, Import> _constructedHelpers;

        public Import GetReadyToRunHelperCell(ReadyToRunHelper helperId)
        {
            return _constructedHelpers.GetOrAdd(helperId);
        }

        private NodeCache<TypeAndMethod, IMethodNode> _importMethods;

        private IMethodNode CreateMethodEntrypoint(TypeAndMethod key)
        {
            MethodWithToken method = key.Method;
            bool isInstantiatingStub = key.IsInstantiatingStub;
            bool isPrecodeImportRequired = key.IsPrecodeImportRequired;
            MethodDesc compilableMethod = method.Method.GetCanonMethodTarget(CanonicalFormKind.Specific);
            MethodWithGCInfo methodWithGCInfo = null;

            if (CompilationModuleGroup.ContainsMethodBody(compilableMethod, false))
            {
                methodWithGCInfo = CompiledMethodNode(compilableMethod);
            }

            if (isPrecodeImportRequired)
            {
                Debug.Assert(!key.IsJumpableImportRequired);
                return new PrecodeMethodImport(
                    this,
                    ReadyToRunFixupKind.MethodEntry,
                    method,
                    methodWithGCInfo,
                    isInstantiatingStub);
            }
            else
            {
                return new DelayLoadMethodImport(
                    this,
                    ReadyToRunFixupKind.MethodEntry,
                    method,
                    methodWithGCInfo,
                    isInstantiatingStub,
                    isJump: key.IsJumpableImportRequired);
            }
        }

        public IMethodNode MethodEntrypoint(MethodWithToken method, bool isInstantiatingStub, bool isPrecodeImportRequired, bool isJumpableImportRequired)
        {
            Debug.Assert(!isJumpableImportRequired || !isPrecodeImportRequired);
            TypeAndMethod key = new TypeAndMethod(method.ConstrainedType, method, isInstantiatingStub, isPrecodeImportRequired, isJumpableImportRequired);
            return _importMethods.GetOrAdd(key);
        }

        public IEnumerable<MethodWithGCInfo> EnumerateCompiledMethods()
        {
            return EnumerateCompiledMethods(null, CompiledMethodCategory.All);
        }

        public IEnumerable<MethodWithGCInfo> EnumerateCompiledMethods(EcmaModule moduleToEnumerate, CompiledMethodCategory methodCategory)
        {
            foreach (IMethodNode methodNode in MetadataManager.GetCompiledMethods(moduleToEnumerate, methodCategory))
            {
                MethodDesc method = methodNode.Method;
                MethodWithGCInfo methodCodeNode = methodNode as MethodWithGCInfo;
#if DEBUG
                if (!methodCodeNode.IsEmpty || CompilationModuleGroup.VersionsWithMethodBody(method))
                {
                    EcmaModule module = ((EcmaMethod)method.GetTypicalMethodDefinition()).Module;
                    ModuleToken moduleToken = Resolver.GetModuleTokenForMethod(method, allowDynamicallyCreatedReference: true, throwIfNotFound: true);

                    IMethodNode methodNodeDebug = MethodEntrypoint(new MethodWithToken(method, moduleToken, constrainedType: null, unboxing: false, context: null), false, false, false);
                    MethodWithGCInfo methodCodeNodeDebug = methodNodeDebug as MethodWithGCInfo;
                    if (methodCodeNodeDebug == null && methodNodeDebug is DelayLoadMethodImport DelayLoadMethodImport)
                    {
                        methodCodeNodeDebug = DelayLoadMethodImport.MethodCodeNode;
                    }
                    if (methodCodeNodeDebug == null && methodNodeDebug is PrecodeMethodImport precodeMethodImport)
                    {
                        methodCodeNodeDebug = precodeMethodImport.MethodCodeNode;
                    }
                    Debug.Assert(methodCodeNodeDebug == methodCodeNode);
                }
#endif

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
            bool isInstantiatingStub)
        {
            TypeAndMethod key = new TypeAndMethod(method.ConstrainedType, method, isInstantiatingStub, false, false);
            return _methodSignatures.GetOrAdd(new MethodFixupKey(fixupKind, key));
        }

        private struct TypeFixupKey : IEquatable<TypeFixupKey>
        {
            public readonly ReadyToRunFixupKind FixupKind;
            public readonly TypeDesc TypeDesc;

            public TypeFixupKey(ReadyToRunFixupKind fixupKind, TypeDesc typeDesc)
            {
                FixupKind = fixupKind;
                TypeDesc = typeDesc;
            }

            public bool Equals(TypeFixupKey other)
            {
                return FixupKind == other.FixupKind && TypeDesc == other.TypeDesc;
            }

            public override bool Equals(object obj)
            {
                return obj is TypeFixupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return FixupKind.GetHashCode() ^ (31 * TypeDesc.GetHashCode());
            }
        }

        private NodeCache<TypeFixupKey, TypeFixupSignature> _typeSignatures;

        public TypeFixupSignature TypeSignature(ReadyToRunFixupKind fixupKind, TypeDesc typeDesc)
        {
            TypeFixupKey fixupKey = new TypeFixupKey(fixupKind, typeDesc);
            return _typeSignatures.GetOrAdd(fixupKey);
        }

        private struct VirtualResolutionFixupSignatureFixupKey : IEquatable<VirtualResolutionFixupSignatureFixupKey>
        {
            public readonly ReadyToRunFixupKind FixupKind;
            public readonly MethodWithToken DeclMethod;
            public readonly TypeDesc ImplType;
            public readonly MethodWithToken ImplMethod;

            public VirtualResolutionFixupSignatureFixupKey(ReadyToRunFixupKind fixupKind, MethodWithToken declMethod, TypeDesc implType, MethodWithToken implMethod)
            {
                FixupKind = fixupKind;
                DeclMethod = declMethod;
                ImplType = implType;
                ImplMethod = implMethod;
            }

            public bool Equals(VirtualResolutionFixupSignatureFixupKey other)
            {
                return FixupKind == other.FixupKind && DeclMethod.Equals(other.DeclMethod) && ImplType == other.ImplType &&
                    ((ImplMethod == null && other.ImplMethod == null) || (ImplMethod != null && ImplMethod.Equals(other.ImplMethod)));
            }

            public override bool Equals(object obj)
            {
                return obj is VirtualResolutionFixupSignatureFixupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                if (ImplMethod != null)
                    return HashCode.Combine(FixupKind, DeclMethod, ImplType, ImplMethod);
                else
                    return HashCode.Combine(FixupKind, DeclMethod, ImplType);
            }

            public override string ToString() => $"'{FixupKind}' '{DeclMethod}' on '{ImplType}' results in '{(ImplMethod != null ? ImplMethod.ToString() : "null")}'";
        }

        private NodeCache<VirtualResolutionFixupSignatureFixupKey, VirtualResolutionFixupSignature> _virtualResolutionSignatures;

        public VirtualResolutionFixupSignature VirtualResolutionFixupSignature(ReadyToRunFixupKind fixupKind, MethodWithToken declMethod, TypeDesc implType, MethodWithToken implMethod)
        {
            return _virtualResolutionSignatures.GetOrAdd(new VirtualResolutionFixupSignatureFixupKey(fixupKind, declMethod, implType, implMethod));
        }

        private struct ILBodyFixupSignatureFixupKey : IEquatable<ILBodyFixupSignatureFixupKey>
        {
            public readonly ReadyToRunFixupKind FixupKind;
            public readonly EcmaMethod Method;

            public ILBodyFixupSignatureFixupKey(ReadyToRunFixupKind fixupKind, EcmaMethod method)
            {
                FixupKind = fixupKind;
                Method = method;
            }
            public bool Equals(ILBodyFixupSignatureFixupKey other) => FixupKind == other.FixupKind && Method.Equals(other.Method);
            public override bool Equals(object obj) => obj is ILBodyFixupSignatureFixupKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(FixupKind, Method);
            public override string ToString() => $"'{FixupKind}' '{Method}'";
        }

        private NodeCache<ILBodyFixupSignatureFixupKey, ILBodyFixupSignature> _ilBodySignatures =
            new NodeCache<ILBodyFixupSignatureFixupKey, ILBodyFixupSignature>((key) => new ILBodyFixupSignature(key.FixupKind, key.Method));

        public ILBodyFixupSignature ILBodyFixupSignature(ReadyToRunFixupKind fixupKind, EcmaMethod method)
        {
            return _ilBodySignatures.GetOrAdd(new ILBodyFixupSignatureFixupKey(fixupKind, method));
        }

        private struct ImportThunkKey : IEquatable<ImportThunkKey>
        {
            public readonly ReadyToRunHelper Helper;
            public readonly ImportSectionNode ContainingImportSection;
            public readonly bool UseVirtualCall;
            public readonly bool UseJumpableStub;

            public ImportThunkKey(ReadyToRunHelper helper, ImportSectionNode containingImportSection, bool useVirtualCall, bool useJumpableStub)
            {
                Helper = helper;
                ContainingImportSection = containingImportSection;
                UseVirtualCall = useVirtualCall;
                UseJumpableStub = useJumpableStub;
            }

            public bool Equals(ImportThunkKey other)
            {
                return Helper == other.Helper &&
                    ContainingImportSection == other.ContainingImportSection &&
                    UseVirtualCall == other.UseVirtualCall &&
                    UseJumpableStub == other.UseJumpableStub;
            }

            public override bool Equals(object obj)
            {
                return obj is ImportThunkKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return unchecked(31 * Helper.GetHashCode() +
                    31 * ContainingImportSection.GetHashCode() +
                    31 * UseVirtualCall.GetHashCode() +
                    31 * UseJumpableStub.GetHashCode());
            }
        }

        private NodeCache<ImportThunkKey, ImportThunk> _importThunks;

        public ImportThunk ImportThunk(ReadyToRunHelper helper, ImportSectionNode containingImportSection, bool useVirtualCall, bool useJumpableStub)
        {
            ImportThunkKey thunkKey = new ImportThunkKey(helper, containingImportSection, useVirtualCall, useJumpableStub);
            return _importThunks.GetOrAdd(thunkKey);
        }

        public void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph, ILProvider ilProvider)
        {
            graph.ComputingDependencyPhaseChange += Graph_ComputingDependencyPhaseChange;

            var compilerIdentifierNode = new CompilerIdentifierNode(Target);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.CompilerIdentifier, compilerIdentifierNode, compilerIdentifierNode);

            RuntimeFunctionsTable = new RuntimeFunctionsTableNode(this);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.RuntimeFunctions, RuntimeFunctionsTable, RuntimeFunctionsTable);

            RuntimeFunctionsGCInfo = new RuntimeFunctionsGCInfoNode();
            graph.AddRoot(RuntimeFunctionsGCInfo, "GC info is always generated");

            DelayLoadMethodCallThunks = new DelayLoadMethodCallThunkNodeRange();
            Header.Add(Internal.Runtime.ReadyToRunSectionType.DelayLoadMethodCallThunks, DelayLoadMethodCallThunks, DelayLoadMethodCallThunks);

            ExceptionInfoLookupTableNode exceptionInfoLookupTableNode = new ExceptionInfoLookupTableNode(this);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.ExceptionInfo, exceptionInfoLookupTableNode, exceptionInfoLookupTableNode);
            graph.AddRoot(exceptionInfoLookupTableNode, "ExceptionInfoLookupTable is always generated");

            ManifestMetadataTable = new ManifestMetadataTableNode(this);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.ManifestMetadata, ManifestMetadataTable, ManifestMetadataTable);
            Resolver.SetModuleIndexLookup(ManifestMetadataTable.ModuleToIndex);
            ((ReadyToRunILProvider)ilProvider).InitManifestMutableModule(ManifestMetadataTable._mutableModule);
            Resolver.InitManifestMutableModule(ManifestMetadataTable._mutableModule);

            ManifestAssemblyMvidHeaderNode mvidTableNode = new ManifestAssemblyMvidHeaderNode(ManifestMetadataTable);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.ManifestAssemblyMvids, mvidTableNode, mvidTableNode);

            AssemblyTableNode assemblyTable = null;

            if (CompilationModuleGroup.IsCompositeBuildMode)
            {
                assemblyTable = new AssemblyTableNode(Target);
                Header.Add(Internal.Runtime.ReadyToRunSectionType.ComponentAssemblies, assemblyTable, assemblyTable);
            }

            // Generate per assembly header tables
            int assemblyIndex = -1;
            foreach (EcmaModule inputModule in CompilationModuleGroup.CompilationModuleSet)
            {
                assemblyIndex++;
                HeaderNode tableHeader = Header;
                if (assemblyTable != null)
                {
                    AssemblyHeaderNode perAssemblyHeader = new AssemblyHeaderNode(Target, ReadyToRunFlags.READYTORUN_FLAG_Component, assemblyIndex);
                    assemblyTable.Add(perAssemblyHeader);
                    tableHeader = perAssemblyHeader;
                }

                MethodEntryPointTableNode methodEntryPointTable = new MethodEntryPointTableNode(inputModule, Target);
                tableHeader.Add(Internal.Runtime.ReadyToRunSectionType.MethodDefEntryPoints, methodEntryPointTable, methodEntryPointTable);

                TypesTableNode typesTable = new TypesTableNode(Target, inputModule);
                tableHeader.Add(Internal.Runtime.ReadyToRunSectionType.AvailableTypes, typesTable, typesTable);

                if (CompilationModuleGroup.IsCompositeBuildMode)
                {
                    InliningInfoNode inliningInfoTable = new InliningInfoNode(Target, inputModule, InliningInfoNode.InfoType.InliningInfo2);
                    tableHeader.Add(Internal.Runtime.ReadyToRunSectionType.InliningInfo2, inliningInfoTable, inliningInfoTable);
                }

                // Core library attributes are checked FAR more often than other dlls
                // attributes, so produce a highly efficient table for determining if they are
                // present. Other assemblies *MAY* benefit from this feature, but it doesn't show
                // as useful at this time.
                if (inputModule == TypeSystemContext.SystemModule)
                {
                    AttributePresenceFilterNode attributePresenceTable = new AttributePresenceFilterNode(inputModule);
                    tableHeader.Add(Internal.Runtime.ReadyToRunSectionType.AttributePresence, attributePresenceTable, attributePresenceTable);
                }
            }

            InliningInfoNode crossModuleInliningInfoTable = new InliningInfoNode(Target, null, 
                CompilationModuleGroup.IsCompositeBuildMode ? InliningInfoNode.InfoType.CrossModuleInliningForCrossModuleDataOnly : InliningInfoNode.InfoType.CrossModuleAllMethods);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.CrossModuleInlineInfo, crossModuleInliningInfoTable, crossModuleInliningInfoTable);
            this.CrossModuleInlningInfo = crossModuleInliningInfoTable;

            InstanceEntryPointTable = new InstanceEntryPointTableNode(this);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.InstanceMethodEntryPoints, InstanceEntryPointTable, InstanceEntryPointTable);

            ImportSectionsTable = new ImportSectionsTableNode(this);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.ImportSections, ImportSectionsTable, ImportSectionsTable.StartSymbol);

            DebugInfoTable = new DebugInfoTableNode(Target);
            Header.Add(Internal.Runtime.ReadyToRunSectionType.DebugInfo, DebugInfoTable, DebugInfoTable);

            EagerImports = new ImportSectionNode(
                "EagerImports",
                ReadyToRunImportSectionType.Unknown,
                ReadyToRunImportSectionFlags.Eager,
                (byte)Target.PointerSize,
                emitPrecode: false,
                emitGCRefMap: false);
            ImportSectionsTable.AddEmbeddedObject(EagerImports);

            // All ready-to-run images have a module import helper which gets patched by the runtime on image load
            ModuleImport = new Import(EagerImports, new ReadyToRunHelperSignature(
                ReadyToRunHelper.Module));
            graph.AddRoot(ModuleImport, "Module import is required by the R2R format spec");

            if (Target.Architecture != TargetArchitecture.X86)
            {
                Import personalityRoutineImport = new Import(EagerImports, new ReadyToRunHelperSignature(
                    ReadyToRunHelper.PersonalityRoutine));
                PersonalityRoutine = new ImportThunk(this,
                    ReadyToRunHelper.PersonalityRoutine, EagerImports, useVirtualCall: false, useJumpableStub: false);
                graph.AddRoot(PersonalityRoutine, "Personality routine is faster to root early rather than referencing it from each unwind info");

                Import filterFuncletPersonalityRoutineImport = new Import(EagerImports, new ReadyToRunHelperSignature(
                    ReadyToRunHelper.PersonalityRoutineFilterFunclet));
                FilterFuncletPersonalityRoutine = new ImportThunk(this,
                    ReadyToRunHelper.PersonalityRoutineFilterFunclet, EagerImports, useVirtualCall: false, useJumpableStub: false);
                graph.AddRoot(FilterFuncletPersonalityRoutine, "Filter funclet personality routine is faster to root early rather than referencing it from each unwind info");
            }

            if ((ProfileDataManager != null) && (ProfileDataManager.EmbedPgoDataInR2RImage))
            {
                // Profile instrumentation data attaches here
                HashSet<MethodDesc> methodsToInsertInstrumentationDataFor = new HashSet<MethodDesc>();
                foreach (EcmaModule inputModule in CompilationModuleGroup.CompilationModuleSet)
                {
                    foreach (MethodDesc method in ProfileDataManager.GetMethodsForModuleDesc(inputModule))
                    {
                        if (ProfileDataManager[method].SchemaData != null)
                        {
                            methodsToInsertInstrumentationDataFor.Add(method);
                        }
                    }
                }
                if (methodsToInsertInstrumentationDataFor.Count != 0)
                {
                    MethodDesc[] methodsToInsert = methodsToInsertInstrumentationDataFor.ToArray();
                    methodsToInsert.MergeSort(TypeSystemComparer.Instance.Compare);
                    InstrumentationDataTable = new InstrumentationDataTableNode(this, methodsToInsert, ProfileDataManager);
                    Header.Add(Internal.Runtime.ReadyToRunSectionType.PgoInstrumentationData, InstrumentationDataTable, InstrumentationDataTable);
                }
            }

            ILBodyPrecodeImports = new ImportSectionNode(
                "A_ILBodyPrecodeImports",
                ReadyToRunImportSectionType.Unknown,
                ReadyToRunImportSectionFlags.None,
                (byte)Target.PointerSize,
                emitPrecode: true,
                emitGCRefMap: false);
            ImportSectionsTable.AddEmbeddedObject(ILBodyPrecodeImports);

            MethodImports = new ImportSectionNode(
                "MethodImports",
                ReadyToRunImportSectionType.StubDispatch,
                ReadyToRunImportSectionFlags.PCode,
                (byte)Target.PointerSize,
                emitPrecode: false,
                emitGCRefMap: true);
            ImportSectionsTable.AddEmbeddedObject(MethodImports);

            DispatchImports = new ImportSectionNode(
                "DispatchImports",
                ReadyToRunImportSectionType.StubDispatch,
                ReadyToRunImportSectionFlags.PCode,
                (byte)Target.PointerSize,
                emitPrecode: false,
                emitGCRefMap: true);
            ImportSectionsTable.AddEmbeddedObject(DispatchImports);

            HelperImports = new ImportSectionNode(
                "HelperImports",
                ReadyToRunImportSectionType.Unknown,
                ReadyToRunImportSectionFlags.PCode,
                (byte)Target.PointerSize,
                emitPrecode: false,
                emitGCRefMap: false);
            ImportSectionsTable.AddEmbeddedObject(HelperImports);

            PrecodeImports = new ImportSectionNode(
                "PrecodeImports",
                ReadyToRunImportSectionType.Unknown,
                ReadyToRunImportSectionFlags.PCode,
                (byte)Target.PointerSize,
                emitPrecode: true,
                emitGCRefMap: false);
            ImportSectionsTable.AddEmbeddedObject(PrecodeImports);

            StringImports = new ImportSectionNode(
                "StringImports",
                ReadyToRunImportSectionType.StringHandle,
                ReadyToRunImportSectionFlags.None,
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
            graph.AddRoot(ILBodyPrecodeImports, "IL body precode imports are always generated");
            graph.AddRoot(StringImports, "String imports are always generated");
            graph.AddRoot(Header, "ReadyToRunHeader is always generated");
            graph.AddRoot(CopiedCorHeaderNode, "MSIL COR header is always generated for R2R files");
            graph.AddRoot(DebugDirectoryNode, "Debug Directory will always contain at least one entry");

            if (Win32ResourcesNode != null)
                graph.AddRoot(Win32ResourcesNode, "Win32 Resources are placed if not empty");

            MetadataManager.AttachToDependencyGraph(graph, this);
        }

        private void Graph_ComputingDependencyPhaseChange(int newPhase)
        {
            CompilationCurrentPhase = newPhase;
        }

        private ReadyToRunHelper GetGenericStaticHelper(ReadyToRunHelperId helperId)
        {
            ReadyToRunHelper r2rHelper;

            switch (helperId)
            {
                case ReadyToRunHelperId.GetGCStaticBase:
                    r2rHelper = ReadyToRunHelper.GenericGcStaticBase;
                    break;

                case ReadyToRunHelperId.GetNonGCStaticBase:
                    r2rHelper = ReadyToRunHelper.GenericNonGcStaticBase;
                    break;

                case ReadyToRunHelperId.GetThreadStaticBase:
                    r2rHelper = ReadyToRunHelper.GenericGcTlsBase;
                    break;

                case ReadyToRunHelperId.GetThreadNonGcStaticBase:
                    r2rHelper = ReadyToRunHelper.GenericNonGcTlsBase;
                    break;

                default:
                    throw new NotImplementedException();
            }

            return r2rHelper;
        }

        struct DynamicHelperCellKey : IEquatable<DynamicHelperCellKey>
        {
            public readonly MethodWithToken Method;
            public readonly bool IsUnboxingStub;
            public readonly bool IsInstantiatingStub;

            public DynamicHelperCellKey(MethodWithToken method, bool isUnboxingStub, bool isInstantiatingStub)
            {
                Method = method;
                IsUnboxingStub = isUnboxingStub;
                IsInstantiatingStub = isInstantiatingStub;
            }

            public bool Equals(DynamicHelperCellKey other)
            {
                return Method.Equals(other.Method)
                    && IsUnboxingStub == other.IsUnboxingStub
                    && IsInstantiatingStub == other.IsInstantiatingStub;
            }

            public override bool Equals(object obj)
            {
                return obj is DynamicHelperCellKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Method.GetHashCode()
                     ^ (IsUnboxingStub ? -0x80000000 : 0)
                     ^ (IsInstantiatingStub ? -0x40000000 : 0);
            }
        }

        private NodeCache<DynamicHelperCellKey, ISymbolNode> _dynamicHelperCellCache;

        public ISymbolNode DynamicHelperCell(MethodWithToken methodWithToken, bool isInstantiatingStub)
        {
            DynamicHelperCellKey key = new DynamicHelperCellKey(methodWithToken, isUnboxingStub: false, isInstantiatingStub);
            return _dynamicHelperCellCache.GetOrAdd(key);
        }

        private NodeCache<EcmaModule, CopiedCorHeaderNode> _copiedCorHeaders;

        public CopiedCorHeaderNode CopiedCorHeader(EcmaModule module)
        {
            return _copiedCorHeaders.GetOrAdd(module);
        }

        private NodeCache<ModuleAndIntValueKey, DebugDirectoryEntryNode> _debugDirectoryEntries;

        public DebugDirectoryEntryNode DebugDirectoryEntry(EcmaModule module, int debugDirEntryIndex)
        {
            return _debugDirectoryEntries.GetOrAdd(new ModuleAndIntValueKey(debugDirEntryIndex, module));
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

        private NodeCache<ModuleAndIntValueKey, CopiedFieldRvaNode> _copiedFieldRvas;

        public CopiedFieldRvaNode CopiedFieldRva(FieldDesc field)
        {
            Debug.Assert(field.HasRva);
            EcmaField ecmaField = (EcmaField)field.GetTypicalFieldDefinition();

            if (!CompilationModuleGroup.ContainsType(ecmaField.OwningType))
            {
                // TODO: cross-bubble RVA field
                throw new NotSupportedException($"{ecmaField} ... {ecmaField.Module.Assembly}");
            }

            return _copiedFieldRvas.GetOrAdd(new ModuleAndIntValueKey(ecmaField.GetFieldRvaValue(), ecmaField.Module));
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
    }
}

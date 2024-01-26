// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using ILCompiler.DependencyAnalysisFramework;

using Internal.IL;
using Internal.Runtime;
using Internal.Text;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    public abstract partial class NodeFactory
    {
        private TargetDetails _target;
        private CompilerTypeSystemContext _context;
        private CompilationModuleGroup _compilationModuleGroup;
        private VTableSliceProvider _vtableSliceProvider;
        private DictionaryLayoutProvider _dictionaryLayoutProvider;
        private InlinedThreadStatics _inlinedThreadStatics;
        protected readonly ImportedNodeProvider _importedNodeProvider;
        private bool _markingComplete;

        public NodeFactory(
            CompilerTypeSystemContext context,
            CompilationModuleGroup compilationModuleGroup,
            MetadataManager metadataManager,
            InteropStubManager interoptStubManager,
            NameMangler nameMangler,
            LazyGenericsPolicy lazyGenericsPolicy,
            VTableSliceProvider vtableSliceProvider,
            DictionaryLayoutProvider dictionaryLayoutProvider,
            InlinedThreadStatics inlinedThreadStatics,
            ImportedNodeProvider importedNodeProvider,
            PreinitializationManager preinitializationManager)
        {
            _target = context.Target;
            _context = context;
            _compilationModuleGroup = compilationModuleGroup;
            _vtableSliceProvider = vtableSliceProvider;
            _dictionaryLayoutProvider = dictionaryLayoutProvider;
            _inlinedThreadStatics = inlinedThreadStatics;
            NameMangler = nameMangler;
            InteropStubManager = interoptStubManager;
            CreateNodeCaches();
            MetadataManager = metadataManager;
            LazyGenericsPolicy = lazyGenericsPolicy;
            _importedNodeProvider = importedNodeProvider;
            PreinitializationManager = preinitializationManager;
        }

        public void SetMarkingComplete()
        {
            _markingComplete = true;
        }

        public bool MarkingComplete => _markingComplete;

        public TargetDetails Target
        {
            get
            {
                return _target;
            }
        }

        public LazyGenericsPolicy LazyGenericsPolicy { get; }
        public CompilationModuleGroup CompilationModuleGroup
        {
            get
            {
                return _compilationModuleGroup;
            }
        }

        public CompilerTypeSystemContext TypeSystemContext
        {
            get
            {
                return _context;
            }
        }

        public MetadataManager MetadataManager
        {
            get;
        }

        public NameMangler NameMangler
        {
            get;
        }

        public PreinitializationManager PreinitializationManager
        {
            get;
        }

        public InteropStubManager InteropStubManager
        {
            get;
        }

        /// <summary>
        /// Return true if the type is not permitted by the rules of the runtime to have an MethodTable.
        /// The implementation here is not intended to be complete, but represents many conditions
        /// which make a type ineligible to be an MethodTable. (This function is intended for use in assertions only)
        /// </summary>
        private static bool TypeCannotHaveEEType(TypeDesc type)
        {
            if (type.GetTypeDefinition() is INonEmittableType)
                return true;

            if (type.IsRuntimeDeterminedSubtype)
                return true;

            if (type.IsSignatureVariable)
                return true;

            if (type.IsGenericParameter)
                return true;

            return false;
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

        private void CreateNodeCaches()
        {
            _typeSymbols = new NecessaryTypeSymbolHashtable(this);

            _constructedTypeSymbols = new ConstructedTypeSymbolHashtable(this);

            _importedTypeSymbols = new NodeCache<TypeDesc, IEETypeNode>((TypeDesc type) =>
            {
                Debug.Assert(_compilationModuleGroup.ShouldReferenceThroughImportTable(type));
                return _importedNodeProvider.ImportedEETypeNode(this, type);
            });

            _nonGCStatics = new NodeCache<MetadataType, ISortableSymbolNode>((MetadataType type) =>
            {
                if (_compilationModuleGroup.ContainsType(type) && !_compilationModuleGroup.ShouldReferenceThroughImportTable(type))
                {
                    return new NonGCStaticsNode(type, PreinitializationManager);
                }
                else
                {
                    return _importedNodeProvider.ImportedNonGCStaticNode(this, type);
                }
            });

            _GCStatics = new NodeCache<MetadataType, ISortableSymbolNode>((MetadataType type) =>
            {
                if (_compilationModuleGroup.ContainsType(type) && !_compilationModuleGroup.ShouldReferenceThroughImportTable(type))
                {
                    return new GCStaticsNode(type, PreinitializationManager);
                }
                else
                {
                    return _importedNodeProvider.ImportedGCStaticNode(this, type);
                }
            });

            _GCStaticIndirectionNodes = new NodeCache<MetadataType, EmbeddedObjectNode>((MetadataType type) =>
            {
                ISymbolNode gcStaticsNode = TypeGCStaticsSymbol(type);
                Debug.Assert(gcStaticsNode is GCStaticsNode);
                return GCStaticsRegion.NewNode((GCStaticsNode)gcStaticsNode);
            });

            _threadStatics = new NodeCache<MetadataType, ISymbolDefinitionNode>(CreateThreadStaticsNode);

            if (_inlinedThreadStatics.IsComputed())
            {
                _inlinedThreadStatiscNode = new ThreadStaticsNode(_inlinedThreadStatics, this);
            }

            _typeThreadStaticIndices = new NodeCache<MetadataType, TypeThreadStaticIndexNode>(type =>
            {
                if (_inlinedThreadStatics.IsComputed() &&
                    _inlinedThreadStatics.GetOffsets().ContainsKey(type))
                {
                    return new TypeThreadStaticIndexNode(type, _inlinedThreadStatiscNode);
                }

                return new TypeThreadStaticIndexNode(type, null);
            });

            _GCStaticEETypes = new NodeCache<GCPointerMap, GCStaticEETypeNode>((GCPointerMap gcMap) =>
            {
                return new GCStaticEETypeNode(Target, gcMap);
            });

            _readOnlyDataBlobs = new NodeCache<ReadOnlyDataBlobKey, BlobNode>(key =>
            {
                return new BlobNode(key.Name, ObjectNodeSection.ReadOnlyDataSection, key.Data, key.Alignment);
            });

            _fieldRvaDataBlobs = new NodeCache<Internal.TypeSystem.Ecma.EcmaField, FieldRvaDataNode>(key =>
            {
                return new FieldRvaDataNode(key);
            });

            _externSymbols = new NodeCache<string, ExternSymbolNode>((string name) =>
            {
                return new ExternSymbolNode(name);
            });
            _externIndirectSymbols = new NodeCache<string, ExternSymbolNode>((string name) =>
            {
                return new ExternSymbolNode(name, isIndirection: true);
            });

            _pInvokeModuleFixups = new NodeCache<PInvokeModuleData, PInvokeModuleFixupNode>((PInvokeModuleData moduleData) =>
            {
                return new PInvokeModuleFixupNode(moduleData);
            });

            _pInvokeMethodFixups = new NodeCache<PInvokeMethodData, PInvokeMethodFixupNode>((PInvokeMethodData methodData) =>
            {
                return new PInvokeMethodFixupNode(methodData);
            });

            _methodEntrypoints = new MethodEntrypointHashtable(this);

            _tentativeMethodEntrypoints = new NodeCache<MethodDesc, IMethodNode>((MethodDesc method) =>
            {
                IMethodNode entrypoint = MethodEntrypoint(method, unboxingStub: false);
                return new TentativeMethodNode(entrypoint is TentativeMethodNode tentative ?
                    tentative.RealBody : (IMethodBodyNode)entrypoint);
            });

            _tentativeMethods = new NodeCache<MethodDesc, TentativeInstanceMethodNode>(method =>
            {
                return new TentativeInstanceMethodNode((IMethodBodyNode)MethodEntrypoint(method));
            });

            _unboxingStubs = new NodeCache<MethodDesc, IMethodNode>(CreateUnboxingStubNode);

            _methodAssociatedData = new NodeCache<IMethodNode, MethodAssociatedDataNode>(methodNode =>
            {
                return new MethodAssociatedDataNode(methodNode);
            });

            _fatFunctionPointers = new NodeCache<MethodKey, FatFunctionPointerNode>(method =>
            {
                return new FatFunctionPointerNode(method.Method, method.IsUnboxingStub);
            });

            _gvmDependenciesNode = new NodeCache<MethodDesc, GVMDependenciesNode>(method =>
            {
                return new GVMDependenciesNode(method);
            });

            _gvmImpls = new NodeCache<MethodDesc, GenericVirtualMethodImplNode>(method =>
            {
                return new GenericVirtualMethodImplNode(method);
            });

            _genericMethodEntries = new NodeCache<MethodDesc, GenericMethodsHashtableEntryNode>(method =>
            {
                return new GenericMethodsHashtableEntryNode(method);
            });

            _gvmTableEntries = new NodeCache<TypeDesc, TypeGVMEntriesNode>(type =>
            {
                return new TypeGVMEntriesNode(type);
            });

            _delegateTargetMethods = new NodeCache<MethodDesc, DelegateTargetVirtualMethodNode>(method =>
            {
                return new DelegateTargetVirtualMethodNode(method);
            });

            _reflectedDelegates = new NodeCache<TypeDesc, ReflectedDelegateNode>(type =>
            {
                return new ReflectedDelegateNode(type);
            });

            _reflectedMethods = new NodeCache<MethodDesc, ReflectedMethodNode>(method =>
            {
                return new ReflectedMethodNode(method);
            });

            _reflectedFields = new NodeCache<FieldDesc, ReflectedFieldNode>(field =>
            {
                return new ReflectedFieldNode(field);
            });

            _reflectedTypes = new NodeCache<TypeDesc, ReflectedTypeNode>(type =>
            {
                TypeSystemContext.EnsureLoadableType(type);
                return new ReflectedTypeNode(type);
            });

            _notReadOnlyFields = new NodeCache<FieldDesc, NotReadOnlyFieldNode>(field =>
            {
                return new NotReadOnlyFieldNode(field);
            });

            _genericStaticBaseInfos = new NodeCache<MetadataType, GenericStaticBaseInfoNode>(type =>
            {
                return new GenericStaticBaseInfoNode(type);
            });

            _objectGetTypeFlowDependencies = new NodeCache<MetadataType, ObjectGetTypeFlowDependenciesNode>(type =>
            {
                return new ObjectGetTypeFlowDependenciesNode(type);
            });

            _shadowConcreteMethods = new ShadowConcreteMethodHashtable(this);

            _shadowConcreteUnboxingMethods = new NodeCache<MethodDesc, ShadowConcreteUnboxingThunkNode>(method =>
            {
                MethodDesc canonMethod = method.GetCanonMethodTarget(CanonicalFormKind.Specific);
                return new ShadowConcreteUnboxingThunkNode(method, MethodEntrypoint(canonMethod, true));
            });

            _virtMethods = new VirtualMethodUseHashtable(this);

            _variantMethods = new NodeCache<MethodDesc, VariantInterfaceMethodUseNode>((MethodDesc method) =>
            {
                // We don't need to track virtual method uses for types that have a vtable with a known layout.
                // It's a waste of CPU time and memory.
                Debug.Assert(!VTable(method.OwningType).HasFixedSlots);

                return new VariantInterfaceMethodUseNode(method);
            });

            _readyToRunHelpers = new NodeCache<ReadyToRunHelperKey, ISymbolNode>(CreateReadyToRunHelperNode);

            _genericReadyToRunHelpersFromDict = new NodeCache<ReadyToRunGenericHelperKey, ISymbolNode>(CreateGenericLookupFromDictionaryNode);
            _genericReadyToRunHelpersFromType = new NodeCache<ReadyToRunGenericHelperKey, ISymbolNode>(CreateGenericLookupFromTypeNode);

            _frozenStringNodes = new NodeCache<string, FrozenStringNode>((string data) =>
            {
                return new FrozenStringNode(data, TypeSystemContext);
            });

            _frozenObjectNodes = new NodeCache<SerializedFrozenObjectKey, SerializedFrozenObjectNode>(key =>
            {
                return new SerializedFrozenObjectNode(key.OwnerType, key.AllocationSiteId, key.SerializableObject);
            });

            _frozenConstructedRuntimeTypeNodes = new NodeCache<TypeDesc, FrozenRuntimeTypeNode>(key =>
            {
                return new FrozenRuntimeTypeNode(key, constructed: true);
            });

            _frozenNecessaryRuntimeTypeNodes = new NodeCache<TypeDesc, FrozenRuntimeTypeNode>(key =>
            {
                return new FrozenRuntimeTypeNode(key, constructed: false);
            });

            _interfaceDispatchCells = new NodeCache<DispatchCellKey, InterfaceDispatchCellNode>(callSiteCell =>
            {
                return new InterfaceDispatchCellNode(callSiteCell.Target, callSiteCell.CallsiteId);
            });

            _interfaceDispatchMaps = new NodeCache<TypeDesc, InterfaceDispatchMapNode>((TypeDesc type) =>
            {
                return new InterfaceDispatchMapNode(this, type);
            });

            _sealedVtableNodes = new NodeCache<TypeDesc, SealedVTableNode>((TypeDesc type) =>
            {
                return new SealedVTableNode(type);
            });

            _runtimeMethodHandles = new NodeCache<MethodDesc, RuntimeMethodHandleNode>((MethodDesc method) =>
            {
                return new RuntimeMethodHandleNode(method);
            });

            _runtimeFieldHandles = new NodeCache<FieldDesc, RuntimeFieldHandleNode>((FieldDesc field) =>
            {
                return new RuntimeFieldHandleNode(field);
            });

            _dataflowAnalyzedMethods = new NodeCache<MethodILKey, DataflowAnalyzedMethodNode>((MethodILKey il) =>
            {
                return new DataflowAnalyzedMethodNode(il.MethodIL);
            });

            _dataflowAnalyzedTypeDefinitions = new NodeCache<TypeDesc, DataflowAnalyzedTypeDefinitionNode>((TypeDesc type) =>
            {
                return new DataflowAnalyzedTypeDefinitionNode(type);
            });

            _dynamicDependencyAttributesOnEntities = new NodeCache<TypeSystemEntity, DynamicDependencyAttributesOnEntityNode>((TypeSystemEntity entity) =>
            {
                return new DynamicDependencyAttributesOnEntityNode(entity);
            });

            _embeddedTrimmingDescriptors = new NodeCache<EcmaModule, EmbeddedTrimmingDescriptorNode>((module) =>
            {
                return new EmbeddedTrimmingDescriptorNode(module);
            });

            _genericCompositions = new NodeCache<Instantiation, GenericCompositionNode>((Instantiation details) =>
            {
                return new GenericCompositionNode(details);
            });

            _genericVariances = new NodeCache<GenericVarianceDetails, GenericVarianceNode>((GenericVarianceDetails details) =>
            {
                return new GenericVarianceNode(details);
            });

            _eagerCctorIndirectionNodes = new NodeCache<MethodDesc, EmbeddedObjectNode>((MethodDesc method) =>
            {
                Debug.Assert(method.IsStaticConstructor);
                Debug.Assert(PreinitializationManager.HasEagerStaticConstructor((MetadataType)method.OwningType));
                return EagerCctorTable.NewNode(MethodEntrypoint(method));
            });

            _delegateMarshalingDataNodes = new NodeCache<DefType, DelegateMarshallingDataNode>(type =>
            {
                return new DelegateMarshallingDataNode(type);
            });

            _structMarshalingDataNodes = new NodeCache<DefType, StructMarshallingDataNode>(type =>
            {
                return new StructMarshallingDataNode(type);
            });

            _vTableNodes = new VTableSliceHashtable(this);

            _methodGenericDictionaries = new NodeCache<MethodDesc, ISortableSymbolNode>(method =>
            {
                if (CompilationModuleGroup.ContainsMethodDictionary(method))
                {
                    return new MethodGenericDictionaryNode(method, this);
                }
                else
                {
                    return _importedNodeProvider.ImportedMethodDictionaryNode(this, method);
                }
            });

            _typeGenericDictionaries = new NodeCache<TypeDesc, TypeGenericDictionaryNode>(type =>
            {
                Debug.Assert(CompilationModuleGroup.ContainsTypeDictionary(type));
                Debug.Assert(!this.LazyGenericsPolicy.UsesLazyGenerics(type));
                return new TypeGenericDictionaryNode(type, this);
            });

            _typesWithMetadata = new NodeCache<MetadataType, TypeMetadataNode>(type =>
            {
                return new TypeMetadataNode(type);
            });

            _methodsWithMetadata = new NodeCache<MethodDesc, MethodMetadataNode>(method =>
            {
                return new MethodMetadataNode(method);
            });

            _fieldsWithMetadata = new NodeCache<FieldDesc, FieldMetadataNode>(field =>
            {
                return new FieldMetadataNode(field);
            });

            _modulesWithMetadata = new NodeCache<ModuleDesc, ModuleMetadataNode>(module =>
            {
                return new ModuleMetadataNode(module);
            });

            _inlineableStringResources = new NodeCache<EcmaModule, InlineableStringsResourceNode>(module =>
            {
                return new InlineableStringsResourceNode(module);
            });

            _customAttributesWithMetadata = new NodeCache<ReflectableCustomAttribute, CustomAttributeMetadataNode>(ca =>
            {
                return new CustomAttributeMetadataNode(ca);
            });

            _genericDictionaryLayouts = new NodeCache<TypeSystemEntity, DictionaryLayoutNode>(_dictionaryLayoutProvider.GetLayout);

            _stringAllocators = new NodeCache<MethodDesc, IMethodNode>(constructor =>
            {
                return new StringAllocatorMethodNode(constructor);
            });

            NativeLayout = new NativeLayoutHelper(this);
        }

        protected virtual ISymbolNode CreateGenericLookupFromDictionaryNode(ReadyToRunGenericHelperKey helperKey)
        {
            return new ReadyToRunGenericLookupFromDictionaryNode(this, helperKey.HelperId, helperKey.Target, helperKey.DictionaryOwner);
        }

        protected virtual ISymbolNode CreateGenericLookupFromTypeNode(ReadyToRunGenericHelperKey helperKey)
        {
            return new ReadyToRunGenericLookupFromTypeNode(this, helperKey.HelperId, helperKey.Target, helperKey.DictionaryOwner);
        }

        private IEETypeNode CreateNecessaryTypeNode(TypeDesc type)
        {
            Debug.Assert(!_compilationModuleGroup.ShouldReferenceThroughImportTable(type));
            if (_compilationModuleGroup.ContainsType(type))
            {
                if (type.IsGenericDefinition)
                {
                    return new GenericDefinitionEETypeNode(this, type);
                }
                else if (type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
                {
                    return new CanonicalDefinitionEETypeNode(this, type);
                }
                else if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
                {
                    return new NecessaryCanonicalEETypeNode(this, type);
                }
                else
                {
                    return new EETypeNode(this, type);
                }
            }
            else
            {
                return new ExternEETypeSymbolNode(this, type);
            }
        }

        private IEETypeNode CreateConstructedTypeNode(TypeDesc type)
        {
            // Canonical definition types are *not* constructed types (call NecessaryTypeSymbol to get them)
            Debug.Assert(!type.IsCanonicalDefinitionType(CanonicalFormKind.Any));
            Debug.Assert(!_compilationModuleGroup.ShouldReferenceThroughImportTable(type));

            if (_compilationModuleGroup.ContainsType(type))
            {
                if (type.IsCanonicalSubtype(CanonicalFormKind.Any))
                {
                    return new CanonicalEETypeNode(this, type);
                }
                else
                {
                    return new ConstructedEETypeNode(this, type);
                }
            }
            else
            {
                return new ExternEETypeSymbolNode(this, type);
            }
        }

        protected abstract IMethodNode CreateMethodEntrypointNode(MethodDesc method);

        protected abstract IMethodNode CreateUnboxingStubNode(MethodDesc method);

        protected abstract ISymbolNode CreateReadyToRunHelperNode(ReadyToRunHelperKey helperCall);

        protected virtual ISymbolDefinitionNode CreateThreadStaticsNode(MetadataType type)
        {
            return new ThreadStaticsNode(type, this);
        }

        private abstract class TypeSymbolHashtable : LockFreeReaderHashtable<TypeDesc, IEETypeNode>
        {
            protected readonly NodeFactory _factory;
            public TypeSymbolHashtable(NodeFactory factory) => _factory = factory;
            protected override bool CompareKeyToValue(TypeDesc key, IEETypeNode value) => key == value.Type;
            protected override bool CompareValueToValue(IEETypeNode value1, IEETypeNode value2) => value1.Type == value2.Type;
            protected override int GetKeyHashCode(TypeDesc key) => key.GetHashCode();
            protected override int GetValueHashCode(IEETypeNode value) => value.Type.GetHashCode();
        }

        private sealed class NecessaryTypeSymbolHashtable : TypeSymbolHashtable
        {
            public NecessaryTypeSymbolHashtable(NodeFactory factory) : base(factory) { }
            protected override IEETypeNode CreateValueFromKey(TypeDesc key) => _factory.CreateNecessaryTypeNode(key);
        }

        private NecessaryTypeSymbolHashtable _typeSymbols;

        public IEETypeNode NecessaryTypeSymbol(TypeDesc type)
        {
            if (_compilationModuleGroup.ShouldReferenceThroughImportTable(type))
            {
                return ImportedEETypeSymbol(type);
            }

            if (_compilationModuleGroup.ShouldPromoteToFullType(type))
            {
                return ConstructedTypeSymbol(type);
            }

            Debug.Assert(!TypeCannotHaveEEType(type));

            return _typeSymbols.GetOrCreateValue(type);
        }

        private sealed class ConstructedTypeSymbolHashtable : TypeSymbolHashtable
        {
            public ConstructedTypeSymbolHashtable(NodeFactory factory) : base(factory) { }
            protected override IEETypeNode CreateValueFromKey(TypeDesc key) => _factory.CreateConstructedTypeNode(key);
        }

        private ConstructedTypeSymbolHashtable _constructedTypeSymbols;

        public IEETypeNode ConstructedTypeSymbol(TypeDesc type)
        {
            if (_compilationModuleGroup.ShouldReferenceThroughImportTable(type))
            {
                return ImportedEETypeSymbol(type);
            }

            Debug.Assert(!TypeCannotHaveEEType(type));

            return _constructedTypeSymbols.GetOrCreateValue(type);
        }

        public IEETypeNode MaximallyConstructableType(TypeDesc type)
        {
            if (ConstructedEETypeNode.CreationAllowed(type))
                return ConstructedTypeSymbol(type);
            else
                return NecessaryTypeSymbol(type);
        }

        private NodeCache<TypeDesc, IEETypeNode> _importedTypeSymbols;

        private IEETypeNode ImportedEETypeSymbol(TypeDesc type)
        {
            Debug.Assert(_compilationModuleGroup.ShouldReferenceThroughImportTable(type));
            return _importedTypeSymbols.GetOrAdd(type);
        }

        private NodeCache<MetadataType, ISortableSymbolNode> _nonGCStatics;

        public ISortableSymbolNode TypeNonGCStaticsSymbol(MetadataType type)
        {
            Debug.Assert(!TypeCannotHaveEEType(type));
            return _nonGCStatics.GetOrAdd(type);
        }

        private NodeCache<MetadataType, ISortableSymbolNode> _GCStatics;

        public ISortableSymbolNode TypeGCStaticsSymbol(MetadataType type)
        {
            Debug.Assert(!TypeCannotHaveEEType(type));
            return _GCStatics.GetOrAdd(type);
        }

        private NodeCache<MetadataType, EmbeddedObjectNode> _GCStaticIndirectionNodes;

        public EmbeddedObjectNode GCStaticIndirection(MetadataType type)
        {
            return _GCStaticIndirectionNodes.GetOrAdd(type);
        }

        private NodeCache<MetadataType, ISymbolDefinitionNode> _threadStatics;
        private ThreadStaticsNode _inlinedThreadStatiscNode;

        public ISymbolDefinitionNode TypeThreadStaticsSymbol(MetadataType type)
        {
            // This node is always used in the context of its index within the region.
            // We should never ask for this if the current compilation doesn't contain the
            // associated type.
            Debug.Assert(_compilationModuleGroup.ContainsType(type));
            return _threadStatics.GetOrAdd(type);
        }

        private NodeCache<MetadataType, TypeThreadStaticIndexNode> _typeThreadStaticIndices;

        public ISortableSymbolNode TypeThreadStaticIndex(MetadataType type)
        {
            if (_compilationModuleGroup.ContainsType(type))
            {
                return _typeThreadStaticIndices.GetOrAdd(type);
            }
            else
            {
                return ExternSymbol(NameMangler.NodeMangler.ThreadStaticsIndex(type));
            }
        }

        private NodeCache<DispatchCellKey, InterfaceDispatchCellNode> _interfaceDispatchCells;

        public InterfaceDispatchCellNode InterfaceDispatchCell(MethodDesc method, ISortableSymbolNode callSite = null)
        {
            return _interfaceDispatchCells.GetOrAdd(new DispatchCellKey(method, callSite));
        }

        private NodeCache<MethodDesc, RuntimeMethodHandleNode> _runtimeMethodHandles;

        public RuntimeMethodHandleNode RuntimeMethodHandle(MethodDesc method)
        {
            return _runtimeMethodHandles.GetOrAdd(method);
        }

        private NodeCache<FieldDesc, RuntimeFieldHandleNode> _runtimeFieldHandles;

        public RuntimeFieldHandleNode RuntimeFieldHandle(FieldDesc field)
        {
            return _runtimeFieldHandles.GetOrAdd(field);
        }

        private NodeCache<MethodILKey, DataflowAnalyzedMethodNode> _dataflowAnalyzedMethods;

        public DataflowAnalyzedMethodNode DataflowAnalyzedMethod(MethodIL methodIL)
        {
            return _dataflowAnalyzedMethods.GetOrAdd(new MethodILKey(methodIL));
        }

        private NodeCache<TypeDesc, DataflowAnalyzedTypeDefinitionNode> _dataflowAnalyzedTypeDefinitions;

        public DataflowAnalyzedTypeDefinitionNode DataflowAnalyzedTypeDefinition(TypeDesc type)
        {
            return _dataflowAnalyzedTypeDefinitions.GetOrAdd(type);
        }

        private NodeCache<TypeSystemEntity, DynamicDependencyAttributesOnEntityNode> _dynamicDependencyAttributesOnEntities;

        public DynamicDependencyAttributesOnEntityNode DynamicDependencyAttributesOnEntity(TypeSystemEntity entity)
        {
            return _dynamicDependencyAttributesOnEntities.GetOrAdd(entity);
        }

        private NodeCache<EcmaModule, EmbeddedTrimmingDescriptorNode> _embeddedTrimmingDescriptors;

        public EmbeddedTrimmingDescriptorNode EmbeddedTrimmingDescriptor(EcmaModule module)
        {
            return _embeddedTrimmingDescriptors.GetOrAdd(module);
        }

        private NodeCache<GCPointerMap, GCStaticEETypeNode> _GCStaticEETypes;

        public ISymbolNode GCStaticEEType(GCPointerMap gcMap)
        {
            return _GCStaticEETypes.GetOrAdd(gcMap);
        }

        private NodeCache<ReadOnlyDataBlobKey, BlobNode> _readOnlyDataBlobs;

        public BlobNode ReadOnlyDataBlob(Utf8String name, byte[] blobData, int alignment)
        {
            return _readOnlyDataBlobs.GetOrAdd(new ReadOnlyDataBlobKey(name, blobData, alignment));
        }

        private NodeCache<Internal.TypeSystem.Ecma.EcmaField, FieldRvaDataNode> _fieldRvaDataBlobs;

        public ISymbolNode FieldRvaData(Internal.TypeSystem.Ecma.EcmaField field)
        {
            return _fieldRvaDataBlobs.GetOrAdd(field);
        }

        private NodeCache<TypeDesc, SealedVTableNode> _sealedVtableNodes;

        internal SealedVTableNode SealedVTable(TypeDesc type)
        {
            return _sealedVtableNodes.GetOrAdd(type);
        }

        private NodeCache<TypeDesc, InterfaceDispatchMapNode> _interfaceDispatchMaps;

        internal InterfaceDispatchMapNode InterfaceDispatchMap(TypeDesc type)
        {
            return _interfaceDispatchMaps.GetOrAdd(type);
        }

        private NodeCache<Instantiation, GenericCompositionNode> _genericCompositions;

        internal ISymbolNode GenericComposition(Instantiation details)
        {
            return _genericCompositions.GetOrAdd(details);
        }

        private NodeCache<GenericVarianceDetails, GenericVarianceNode> _genericVariances;

        internal ISymbolNode GenericVariance(GenericVarianceDetails details)
        {
            return _genericVariances.GetOrAdd(details);
        }

        private NodeCache<string, ExternSymbolNode> _externSymbols;

        public ISortableSymbolNode ExternSymbol(string name)
        {
            return _externSymbols.GetOrAdd(name);
        }

        private NodeCache<string, ExternSymbolNode> _externIndirectSymbols;

        public ISortableSymbolNode ExternIndirectSymbol(string name)
        {
            return _externIndirectSymbols.GetOrAdd(name);
        }

        private NodeCache<PInvokeModuleData, PInvokeModuleFixupNode> _pInvokeModuleFixups;

        public ISymbolNode PInvokeModuleFixup(PInvokeModuleData moduleData)
        {
            return _pInvokeModuleFixups.GetOrAdd(moduleData);
        }

        private NodeCache<PInvokeMethodData, PInvokeMethodFixupNode> _pInvokeMethodFixups;

        public PInvokeMethodFixupNode PInvokeMethodFixup(PInvokeMethodData methodData)
        {
            return _pInvokeMethodFixups.GetOrAdd(methodData);
        }

        private sealed class VTableSliceHashtable : LockFreeReaderHashtable<TypeDesc, VTableSliceNode>
        {
            private readonly NodeFactory _factory;
            public VTableSliceHashtable(NodeFactory factory) => _factory = factory;
            protected override bool CompareKeyToValue(TypeDesc key, VTableSliceNode value) => key == value.Type;
            protected override bool CompareValueToValue(VTableSliceNode value1, VTableSliceNode value2) => value1.Type == value2.Type;
            protected override VTableSliceNode CreateValueFromKey(TypeDesc key)
            {
                if (_factory.CompilationModuleGroup.ShouldProduceFullVTable(key))
                    return new EagerlyBuiltVTableSliceNode(key);
                else
                    return _factory._vtableSliceProvider.GetSlice(key);
            }
            protected override int GetKeyHashCode(TypeDesc key) => key.GetHashCode();
            protected override int GetValueHashCode(VTableSliceNode value) => value.Type.GetHashCode();
        }

        private VTableSliceHashtable _vTableNodes;

        public VTableSliceNode VTable(TypeDesc type)
        {
            return _vTableNodes.GetOrCreateValue(type);
        }

        private NodeCache<MethodDesc, ISortableSymbolNode> _methodGenericDictionaries;
        public ISortableSymbolNode MethodGenericDictionary(MethodDesc method)
        {
            return _methodGenericDictionaries.GetOrAdd(method);
        }

        private NodeCache<TypeDesc, TypeGenericDictionaryNode> _typeGenericDictionaries;
        public TypeGenericDictionaryNode TypeGenericDictionary(TypeDesc type)
        {
            return _typeGenericDictionaries.GetOrAdd(type);
        }

        private NodeCache<TypeSystemEntity, DictionaryLayoutNode> _genericDictionaryLayouts;
        public DictionaryLayoutNode GenericDictionaryLayout(TypeSystemEntity methodOrType)
        {
            return _genericDictionaryLayouts.GetOrAdd(methodOrType);
        }

        private NodeCache<MethodDesc, IMethodNode> _stringAllocators;
        public IMethodNode StringAllocator(MethodDesc stringConstructor)
        {
            return _stringAllocators.GetOrAdd(stringConstructor);
        }

        public uint ThreadStaticBaseOffset(MetadataType type)
        {
            if (_inlinedThreadStatics.IsComputed() &&
                _inlinedThreadStatics.GetOffsets().TryGetValue(type, out var offset))
            {
                return (uint)offset;
            }

            return 0;
        }

        private sealed class MethodEntrypointHashtable : LockFreeReaderHashtable<MethodDesc, IMethodNode>
        {
            private readonly NodeFactory _factory;
            public MethodEntrypointHashtable(NodeFactory factory) => _factory = factory;
            protected override bool CompareKeyToValue(MethodDesc key, IMethodNode value) => key == value.Method;
            protected override bool CompareValueToValue(IMethodNode value1, IMethodNode value2) => value1.Method == value2.Method;
            protected override IMethodNode CreateValueFromKey(MethodDesc key) => _factory.CreateMethodEntrypointNode(key);
            protected override int GetKeyHashCode(MethodDesc key) => key.GetHashCode();
            protected override int GetValueHashCode(IMethodNode value) => value.Method.GetHashCode();
        }

        private MethodEntrypointHashtable _methodEntrypoints;
        private NodeCache<MethodDesc, IMethodNode> _unboxingStubs;
        private NodeCache<IMethodNode, MethodAssociatedDataNode> _methodAssociatedData;

        public IMethodNode MethodEntrypoint(MethodDesc method, bool unboxingStub = false)
        {
            if (unboxingStub)
            {
                return _unboxingStubs.GetOrAdd(method);
            }

            return _methodEntrypoints.GetOrCreateValue(method);
        }

        protected NodeCache<MethodDesc, IMethodNode> _tentativeMethodEntrypoints;

        public IMethodNode TentativeMethodEntrypoint(MethodDesc method, bool unboxingStub = false)
        {
            // Didn't implement unboxing stubs for now. Would need to pass down the flag.
            Debug.Assert(!unboxingStub);
            return _tentativeMethodEntrypoints.GetOrAdd(method);
        }

        private NodeCache<MethodDesc, TentativeInstanceMethodNode> _tentativeMethods;
        public IMethodNode MethodEntrypointOrTentativeMethod(MethodDesc method, bool unboxingStub = false)
        {
            // We might be able to optimize the method body away if the owning type was never seen as allocated.
            if (method.NotCallableWithoutOwningEEType() && CompilationModuleGroup.AllowInstanceMethodOptimization(method))
            {
                Debug.Assert(!unboxingStub);
                return _tentativeMethods.GetOrAdd(method);
            }

            return MethodEntrypoint(method, unboxingStub);
        }

        public MethodAssociatedDataNode MethodAssociatedData(IMethodNode methodNode)
        {
            return _methodAssociatedData.GetOrAdd(methodNode);
        }

        private NodeCache<MethodKey, FatFunctionPointerNode> _fatFunctionPointers;

        public IMethodNode FatFunctionPointer(MethodDesc method, bool isUnboxingStub = false)
        {
            return _fatFunctionPointers.GetOrAdd(new MethodKey(method, isUnboxingStub));
        }

        public IMethodNode ExactCallableAddress(MethodDesc method, bool isUnboxingStub = false)
        {
            MethodDesc canonMethod = method.GetCanonMethodTarget(CanonicalFormKind.Specific);
            if (method != canonMethod)
                return FatFunctionPointer(method, isUnboxingStub);
            else
                return MethodEntrypoint(method, isUnboxingStub);
        }

        public IMethodNode CanonicalEntrypoint(MethodDesc method, bool isUnboxingStub = false)
        {
            MethodDesc canonMethod = method.GetCanonMethodTarget(CanonicalFormKind.Specific);
            if (method != canonMethod)
                return ShadowConcreteMethod(method, isUnboxingStub);
            else
                return MethodEntrypoint(method, isUnboxingStub);
        }

        private NodeCache<MethodDesc, GVMDependenciesNode> _gvmDependenciesNode;
        public GVMDependenciesNode GVMDependencies(MethodDesc method)
        {
            return _gvmDependenciesNode.GetOrAdd(method);
        }

        private NodeCache<MethodDesc, GenericVirtualMethodImplNode> _gvmImpls;
        public GenericVirtualMethodImplNode GenericVirtualMethodImpl(MethodDesc method)
        {
            return _gvmImpls.GetOrAdd(method);
        }

        private NodeCache<MethodDesc, GenericMethodsHashtableEntryNode> _genericMethodEntries;
        public GenericMethodsHashtableEntryNode GenericMethodsHashtableEntry(MethodDesc method)
        {
            return _genericMethodEntries.GetOrAdd(method);
        }

        private NodeCache<TypeDesc, TypeGVMEntriesNode> _gvmTableEntries;
        internal TypeGVMEntriesNode TypeGVMEntries(TypeDesc type)
        {
            return _gvmTableEntries.GetOrAdd(type);
        }

        private NodeCache<MethodDesc, DelegateTargetVirtualMethodNode> _delegateTargetMethods;
        public DelegateTargetVirtualMethodNode DelegateTargetVirtualMethod(MethodDesc method)
        {
            return _delegateTargetMethods.GetOrAdd(method);
        }

        private ReflectedDelegateNode _unknownReflectedDelegate = new ReflectedDelegateNode(null);
        private NodeCache<TypeDesc, ReflectedDelegateNode> _reflectedDelegates;
        public ReflectedDelegateNode ReflectedDelegate(TypeDesc type)
        {
            if (type == null)
                return _unknownReflectedDelegate;

            return _reflectedDelegates.GetOrAdd(type);
        }

        private NodeCache<MethodDesc, ReflectedMethodNode> _reflectedMethods;
        public ReflectedMethodNode ReflectedMethod(MethodDesc method)
        {
            return _reflectedMethods.GetOrAdd(method);
        }

        private NodeCache<FieldDesc, ReflectedFieldNode> _reflectedFields;
        public ReflectedFieldNode ReflectedField(FieldDesc field)
        {
            return _reflectedFields.GetOrAdd(field);
        }

        private NodeCache<TypeDesc, ReflectedTypeNode> _reflectedTypes;
        public ReflectedTypeNode ReflectedType(TypeDesc type)
        {
            return _reflectedTypes.GetOrAdd(type);
        }

        private NodeCache<FieldDesc, NotReadOnlyFieldNode> _notReadOnlyFields;
        public NotReadOnlyFieldNode NotReadOnlyField(FieldDesc field)
        {
            return _notReadOnlyFields.GetOrAdd(field);
        }

        private NodeCache<MetadataType, GenericStaticBaseInfoNode> _genericStaticBaseInfos;
        internal GenericStaticBaseInfoNode GenericStaticBaseInfo(MetadataType type)
        {
            return _genericStaticBaseInfos.GetOrAdd(type);
        }

        private NodeCache<MetadataType, ObjectGetTypeFlowDependenciesNode> _objectGetTypeFlowDependencies;
        internal ObjectGetTypeFlowDependenciesNode ObjectGetTypeFlowDependencies(MetadataType type)
        {
            return _objectGetTypeFlowDependencies.GetOrAdd(type);
        }

        private sealed class ShadowConcreteMethodHashtable : LockFreeReaderHashtable<MethodDesc, ShadowConcreteMethodNode>
        {
            private readonly NodeFactory _factory;
            public ShadowConcreteMethodHashtable(NodeFactory factory) => _factory = factory;
            protected override bool CompareKeyToValue(MethodDesc key, ShadowConcreteMethodNode value) => key == value.Method;
            protected override bool CompareValueToValue(ShadowConcreteMethodNode value1, ShadowConcreteMethodNode value2) => value1.Method == value2.Method;
            protected override ShadowConcreteMethodNode CreateValueFromKey(MethodDesc key) =>
                new ShadowConcreteMethodNode(key, _factory.MethodEntrypoint(key.GetCanonMethodTarget(CanonicalFormKind.Specific)));
            protected override int GetKeyHashCode(MethodDesc key) => key.GetHashCode();
            protected override int GetValueHashCode(ShadowConcreteMethodNode value) => value.Method.GetHashCode();
        }

        private ShadowConcreteMethodHashtable _shadowConcreteMethods;
        private NodeCache<MethodDesc, ShadowConcreteUnboxingThunkNode> _shadowConcreteUnboxingMethods;
        public IMethodNode ShadowConcreteMethod(MethodDesc method, bool isUnboxingStub = false)
        {
            if (isUnboxingStub)
                return _shadowConcreteUnboxingMethods.GetOrAdd(method);
            else
                return _shadowConcreteMethods.GetOrCreateValue(method);
        }

        private static readonly string[][] s_helperEntrypointNames = new string[][] {
            new string[] { "System.Runtime.CompilerServices", "ClassConstructorRunner", "CheckStaticClassConstructionReturnGCStaticBase" },
            new string[] { "System.Runtime.CompilerServices", "ClassConstructorRunner", "CheckStaticClassConstructionReturnNonGCStaticBase" },
            new string[] { "System.Runtime.CompilerServices", "ClassConstructorRunner", "CheckStaticClassConstructionReturnThreadStaticBase" },
            new string[] { "Internal.Runtime", "ThreadStatics", "GetThreadStaticBaseForType" },
            new string[] { "Internal.Runtime", "ThreadStatics", "GetInlinedThreadStaticBaseSlow" },
        };

        private ISymbolNode[] _helperEntrypointSymbols;

        public ISymbolNode HelperEntrypoint(HelperEntrypoint entrypoint)
        {
            _helperEntrypointSymbols ??= new ISymbolNode[s_helperEntrypointNames.Length];

            int index = (int)entrypoint;

            ISymbolNode symbol = _helperEntrypointSymbols[index];
            if (symbol == null)
            {
                var entry = s_helperEntrypointNames[index];

                var type = _context.SystemModule.GetKnownType(entry[0], entry[1]);
                var method = type.GetKnownMethod(entry[2], null);

                symbol = MethodEntrypoint(method);

                _helperEntrypointSymbols[index] = symbol;
            }
            return symbol;
        }

        private MetadataType _systemArrayOfTClass;
        public MetadataType ArrayOfTClass
        {
            get
            {
                return _systemArrayOfTClass ??= _context.SystemModule.GetKnownType("System", "Array`1");
            }
        }

        private TypeDesc _systemArrayOfTEnumeratorType;
        public TypeDesc ArrayOfTEnumeratorType
        {
            get
            {
                // This type is optional, but it's fine for this cache to be ineffective if that happens.
                // Those scenarios are rare and typically deal with small compilations.
                return _systemArrayOfTEnumeratorType ??= _context.SystemModule.GetType("System", "SZGenericArrayEnumerator`1", throwIfNotFound: false);
            }
        }

        private MethodDesc _instanceMethodRemovedHelper;
        public MethodDesc InstanceMethodRemovedHelper
        {
            get
            {
                // This helper is optional, but it's fine for this cache to be ineffective if that happens.
                // Those scenarios are rare and typically deal with small compilations.
                return _instanceMethodRemovedHelper ??= TypeSystemContext.GetOptionalHelperEntryPoint("ThrowHelpers", "ThrowInstanceBodyRemoved");
            }
        }

        private sealed class VirtualMethodUseHashtable : LockFreeReaderHashtable<MethodDesc, VirtualMethodUseNode>
        {
            private readonly NodeFactory _factory;
            public VirtualMethodUseHashtable(NodeFactory factory) => _factory = factory;
            protected override bool CompareKeyToValue(MethodDesc key, VirtualMethodUseNode value) => key == value.Method;
            protected override bool CompareValueToValue(VirtualMethodUseNode value1, VirtualMethodUseNode value2) => value1.Method == value2.Method;
            protected override VirtualMethodUseNode CreateValueFromKey(MethodDesc key)
            {
                // We don't need to track virtual method uses for types that have a vtable with a known layout.
                // It's a waste of CPU time and memory.
                Debug.Assert(!_factory.VTable(key.OwningType).HasFixedSlots);
                return new VirtualMethodUseNode(key);
            }
            protected override int GetKeyHashCode(MethodDesc key) => key.GetHashCode();
            protected override int GetValueHashCode(VirtualMethodUseNode value) => value.Method.GetHashCode();
        }

        private VirtualMethodUseHashtable _virtMethods;

        public DependencyNodeCore<NodeFactory> VirtualMethodUse(MethodDesc decl)
        {
            return _virtMethods.GetOrCreateValue(decl);
        }

        private NodeCache<MethodDesc, VariantInterfaceMethodUseNode> _variantMethods;

        public DependencyNodeCore<NodeFactory> VariantInterfaceMethodUse(MethodDesc decl)
        {
            return _variantMethods.GetOrAdd(decl);
        }

        private NodeCache<ReadyToRunHelperKey, ISymbolNode> _readyToRunHelpers;

        public ISymbolNode ReadyToRunHelper(ReadyToRunHelperId id, object target)
        {
            return _readyToRunHelpers.GetOrAdd(new ReadyToRunHelperKey(id, target));
        }

        private NodeCache<ReadyToRunGenericHelperKey, ISymbolNode> _genericReadyToRunHelpersFromDict;

        public ISymbolNode ReadyToRunHelperFromDictionaryLookup(ReadyToRunHelperId id, object target, TypeSystemEntity dictionaryOwner)
        {
            return _genericReadyToRunHelpersFromDict.GetOrAdd(new ReadyToRunGenericHelperKey(id, target, dictionaryOwner));
        }

        private NodeCache<ReadyToRunGenericHelperKey, ISymbolNode> _genericReadyToRunHelpersFromType;

        public ISymbolNode ReadyToRunHelperFromTypeLookup(ReadyToRunHelperId id, object target, TypeSystemEntity dictionaryOwner)
        {
            return _genericReadyToRunHelpersFromType.GetOrAdd(new ReadyToRunGenericHelperKey(id, target, dictionaryOwner));
        }

        private NodeCache<MetadataType, TypeMetadataNode> _typesWithMetadata;

        internal TypeMetadataNode TypeMetadata(MetadataType type)
        {
            // These are only meaningful for UsageBasedMetadataManager. We should not have them
            // in the dependency graph otherwise.
            Debug.Assert(MetadataManager is UsageBasedMetadataManager);
            return _typesWithMetadata.GetOrAdd(type);
        }

        private NodeCache<MethodDesc, MethodMetadataNode> _methodsWithMetadata;

        internal MethodMetadataNode MethodMetadata(MethodDesc method)
        {
            // These are only meaningful for UsageBasedMetadataManager. We should not have them
            // in the dependency graph otherwise.
            Debug.Assert(MetadataManager is UsageBasedMetadataManager);
            return _methodsWithMetadata.GetOrAdd(method);
        }

        private NodeCache<FieldDesc, FieldMetadataNode> _fieldsWithMetadata;

        internal FieldMetadataNode FieldMetadata(FieldDesc field)
        {
            // These are only meaningful for UsageBasedMetadataManager. We should not have them
            // in the dependency graph otherwise.
            Debug.Assert(MetadataManager is UsageBasedMetadataManager);
            return _fieldsWithMetadata.GetOrAdd(field);
        }

        private NodeCache<ModuleDesc, ModuleMetadataNode> _modulesWithMetadata;

        internal ModuleMetadataNode ModuleMetadata(ModuleDesc module)
        {
            // These are only meaningful for UsageBasedMetadataManager. We should not have them
            // in the dependency graph otherwise.
            Debug.Assert(MetadataManager is UsageBasedMetadataManager);
            return _modulesWithMetadata.GetOrAdd(module);
        }

        private NodeCache<EcmaModule, InlineableStringsResourceNode> _inlineableStringResources;
        internal InlineableStringsResourceNode InlineableStringResource(EcmaModule module)
        {
            return _inlineableStringResources.GetOrAdd(module);
        }

        private NodeCache<ReflectableCustomAttribute, CustomAttributeMetadataNode> _customAttributesWithMetadata;

        internal CustomAttributeMetadataNode CustomAttributeMetadata(ReflectableCustomAttribute ca)
        {
            // These are only meaningful for UsageBasedMetadataManager. We should not have them
            // in the dependency graph otherwise.
            Debug.Assert(MetadataManager is UsageBasedMetadataManager);
            return _customAttributesWithMetadata.GetOrAdd(ca);
        }

        private NodeCache<string, FrozenStringNode> _frozenStringNodes;

        public FrozenStringNode SerializedStringObject(string data)
        {
            return _frozenStringNodes.GetOrAdd(data);
        }

        private NodeCache<SerializedFrozenObjectKey, SerializedFrozenObjectNode> _frozenObjectNodes;

        public SerializedFrozenObjectNode SerializedFrozenObject(MetadataType owningType, int allocationSiteId, TypePreinit.ISerializableReference data)
        {
            return _frozenObjectNodes.GetOrAdd(new SerializedFrozenObjectKey(owningType, allocationSiteId, data));
        }

        public FrozenRuntimeTypeNode SerializedMaximallyConstructableRuntimeTypeObject(TypeDesc type)
        {
            if (ConstructedEETypeNode.CreationAllowed(type))
                return SerializedConstructedRuntimeTypeObject(type);
            return SerializedNecessaryRuntimeTypeObject(type);
        }

        private NodeCache<TypeDesc, FrozenRuntimeTypeNode> _frozenConstructedRuntimeTypeNodes;

        public FrozenRuntimeTypeNode SerializedConstructedRuntimeTypeObject(TypeDesc type)
        {
            return _frozenConstructedRuntimeTypeNodes.GetOrAdd(type);
        }

        private NodeCache<TypeDesc, FrozenRuntimeTypeNode> _frozenNecessaryRuntimeTypeNodes;

        public FrozenRuntimeTypeNode SerializedNecessaryRuntimeTypeObject(TypeDesc type)
        {
            return _frozenNecessaryRuntimeTypeNodes.GetOrAdd(type);
        }

        private NodeCache<MethodDesc, EmbeddedObjectNode> _eagerCctorIndirectionNodes;

        public EmbeddedObjectNode EagerCctorIndirection(MethodDesc cctorMethod)
        {
            return _eagerCctorIndirectionNodes.GetOrAdd(cctorMethod);
        }

        public ISymbolNode ConstantUtf8String(string str)
        {
            int stringBytesCount = Encoding.UTF8.GetByteCount(str);
            byte[] stringBytes = new byte[stringBytesCount + 1];
            Encoding.UTF8.GetBytes(str, 0, str.Length, stringBytes, 0);

            string symbolName = "__utf8str_" + NameMangler.GetMangledStringName(str);

            return ReadOnlyDataBlob(symbolName, stringBytes, 1);
        }

        private NodeCache<DefType, DelegateMarshallingDataNode> _delegateMarshalingDataNodes;

        public DelegateMarshallingDataNode DelegateMarshallingData(DefType type)
        {
            return _delegateMarshalingDataNodes.GetOrAdd(type);
        }

        private NodeCache<DefType, StructMarshallingDataNode> _structMarshalingDataNodes;

        public StructMarshallingDataNode StructMarshallingData(DefType type)
        {
            return _structMarshalingDataNodes.GetOrAdd(type);
        }

        /// <summary>
        /// Returns alternative symbol name that object writer should produce for given symbols
        /// in addition to the regular one.
        /// </summary>
        public string GetSymbolAlternateName(ISymbolNode node)
        {
            string value;
            if (!NodeAliases.TryGetValue(node, out value))
                return null;
            return value;
        }

        public ArrayOfEmbeddedPointersNode<GCStaticsNode> GCStaticsRegion = new ArrayOfEmbeddedPointersNode<GCStaticsNode>(
            "__GCStaticRegion",
            new SortableDependencyNode.ObjectNodeComparer(CompilerComparer.Instance));

        public ArrayOfEmbeddedDataNode<ThreadStaticsNode> ThreadStaticsRegion = new ArrayOfEmbeddedDataNode<ThreadStaticsNode>(
            "__ThreadStaticRegion",
            new SortableDependencyNode.EmbeddedObjectNodeComparer(CompilerComparer.Instance));

        public ArrayOfEmbeddedPointersNode<IMethodNode> EagerCctorTable = new ArrayOfEmbeddedPointersNode<IMethodNode>(
            "__EagerCctor",
            null);

        public ArrayOfFrozenObjectsNode FrozenSegmentRegion = new ArrayOfFrozenObjectsNode();

        internal ModuleInitializerListNode ModuleInitializerList = new ModuleInitializerListNode();

        public InterfaceDispatchCellSectionNode InterfaceDispatchCellSection = new InterfaceDispatchCellSectionNode();

        public ReadyToRunHeaderNode ReadyToRunHeader;

        public Dictionary<ISymbolNode, string> NodeAliases = new Dictionary<ISymbolNode, string>();

        protected internal TypeManagerIndirectionNode TypeManagerIndirection = new TypeManagerIndirectionNode();

        public TlsRootNode TlsRoot = new TlsRootNode();

        public virtual void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph)
        {
            ReadyToRunHeader = new ReadyToRunHeaderNode();

            graph.AddRoot(ReadyToRunHeader, "ReadyToRunHeader is always generated");
            graph.AddRoot(new ModulesSectionNode(), "ModulesSection is always generated");

            graph.AddRoot(GCStaticsRegion, "GC StaticsRegion is always generated");
            graph.AddRoot(ThreadStaticsRegion, "ThreadStaticsRegion is always generated");
            graph.AddRoot(EagerCctorTable, "EagerCctorTable is always generated");
            graph.AddRoot(TypeManagerIndirection, "TypeManagerIndirection is always generated");
            graph.AddRoot(FrozenSegmentRegion, "FrozenSegmentRegion is always generated");
            graph.AddRoot(InterfaceDispatchCellSection, "Interface dispatch cell section is always generated");
            graph.AddRoot(ModuleInitializerList, "Module initializer list is always generated");

            if (_inlinedThreadStatics.IsComputed())
            {
                graph.AddRoot(_inlinedThreadStatiscNode, "Inlined threadstatics are used if present");
                graph.AddRoot(TlsRoot, "Inlined threadstatics are used if present");
            }

            ReadyToRunHeader.Add(ReadyToRunSectionType.GCStaticRegion, GCStaticsRegion);
            ReadyToRunHeader.Add(ReadyToRunSectionType.ThreadStaticRegion, ThreadStaticsRegion);
            ReadyToRunHeader.Add(ReadyToRunSectionType.EagerCctor, EagerCctorTable);
            ReadyToRunHeader.Add(ReadyToRunSectionType.TypeManagerIndirection, TypeManagerIndirection);
            ReadyToRunHeader.Add(ReadyToRunSectionType.FrozenObjectRegion, FrozenSegmentRegion);
            ReadyToRunHeader.Add(ReadyToRunSectionType.ModuleInitializerList, ModuleInitializerList);

            var commonFixupsTableNode = new ExternalReferencesTableNode("CommonFixupsTable", this);
            InteropStubManager.AddToReadyToRunHeader(ReadyToRunHeader, this, commonFixupsTableNode);
            MetadataManager.AddToReadyToRunHeader(ReadyToRunHeader, this, commonFixupsTableNode);
            MetadataManager.AttachToDependencyGraph(graph);
            ReadyToRunHeader.Add(MetadataManager.BlobIdToReadyToRunSection(ReflectionMapBlob.CommonFixupsTable), commonFixupsTableNode);
        }

        protected struct MethodKey : IEquatable<MethodKey>
        {
            public readonly MethodDesc Method;
            public readonly bool IsUnboxingStub;

            public MethodKey(MethodDesc method, bool isUnboxingStub)
            {
                Method = method;
                IsUnboxingStub = isUnboxingStub;
            }

            public bool Equals(MethodKey other) => Method == other.Method && IsUnboxingStub == other.IsUnboxingStub;
            public override bool Equals(object obj) => obj is MethodKey && Equals((MethodKey)obj);
            public override int GetHashCode() => Method.GetHashCode();
        }

        protected struct ReadyToRunHelperKey : IEquatable<ReadyToRunHelperKey>
        {
            public readonly object Target;
            public readonly ReadyToRunHelperId HelperId;

            public ReadyToRunHelperKey(ReadyToRunHelperId helperId, object target)
            {
                HelperId = helperId;
                Target = target;
            }

            public bool Equals(ReadyToRunHelperKey other) => HelperId == other.HelperId && Target.Equals(other.Target);
            public override bool Equals(object obj) => obj is ReadyToRunHelperKey && Equals((ReadyToRunHelperKey)obj);
            public override int GetHashCode()
            {
                int hashCode = (int)HelperId * 0x5498341 + 0x832424;
                hashCode = hashCode * 23 + Target.GetHashCode();
                return hashCode;
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

        protected struct DispatchCellKey : IEquatable<DispatchCellKey>
        {
            public readonly MethodDesc Target;
            public readonly ISortableSymbolNode CallsiteId;

            public DispatchCellKey(MethodDesc target, ISortableSymbolNode callsiteId)
            {
                Target = target;
                CallsiteId = callsiteId;
            }

            public bool Equals(DispatchCellKey other) => Target == other.Target && CallsiteId == other.CallsiteId;
            public override bool Equals(object obj) => obj is DispatchCellKey && Equals((DispatchCellKey)obj);
            public override int GetHashCode()
            {
                int hashCode = Target.GetHashCode();
                if (CallsiteId != null)
                    hashCode = hashCode * 23 + CallsiteId.GetHashCode();
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

        protected struct SerializedFrozenObjectKey : IEquatable<SerializedFrozenObjectKey>
        {
            public readonly MetadataType OwnerType;
            public readonly int AllocationSiteId;
            public readonly TypePreinit.ISerializableReference SerializableObject;

            public SerializedFrozenObjectKey(MetadataType ownerType, int allocationSiteId, TypePreinit.ISerializableReference obj)
            {
                Debug.Assert(ownerType.HasStaticConstructor);
                OwnerType = ownerType;
                AllocationSiteId = allocationSiteId;
                SerializableObject = obj;
            }

            public override bool Equals(object obj) => obj is SerializedFrozenObjectKey && Equals((SerializedFrozenObjectKey)obj);
            public bool Equals(SerializedFrozenObjectKey other) => OwnerType == other.OwnerType && AllocationSiteId == other.AllocationSiteId;
            public override int GetHashCode() => HashCode.Combine(OwnerType.GetHashCode(), AllocationSiteId);
        }

        private struct MethodILKey : IEquatable<MethodILKey>
        {
            public readonly MethodIL MethodIL;

            public MethodILKey(MethodIL methodIL) => MethodIL = methodIL;
            public override bool Equals(object obj) => obj is MethodILKey other && Equals(other);
            public bool Equals(MethodILKey other) => other.MethodIL.OwningMethod == this.MethodIL.OwningMethod;
            public override int GetHashCode() => MethodIL.OwningMethod.GetHashCode();

        }
    }
}

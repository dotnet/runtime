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

namespace ILCompiler.DependencyAnalysis
{
    public abstract partial class NodeFactory
    {
        private TargetDetails _target;
        private CompilerTypeSystemContext _context;
        private CompilationModuleGroup _compilationModuleGroup;
        private VTableSliceProvider _vtableSliceProvider;
        private DictionaryLayoutProvider _dictionaryLayoutProvider;
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
            ImportedNodeProvider importedNodeProvider,
            PreinitializationManager preinitializationManager)
        {
            _target = context.Target;
            _context = context;
            _compilationModuleGroup = compilationModuleGroup;
            _vtableSliceProvider = vtableSliceProvider;
            _dictionaryLayoutProvider = dictionaryLayoutProvider;
            NameMangler = nameMangler;
            InteropStubManager = interoptStubManager;
            CreateNodeCaches();
            MetadataManager = metadataManager;
            LazyGenericsPolicy = lazyGenericsPolicy;
            _importedNodeProvider = importedNodeProvider;
            InterfaceDispatchCellSection = new InterfaceDispatchCellSectionNode(this);
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

        // Temporary workaround that is used to disable certain features from lighting up
        // in CppCodegen because they're not fully implemented yet.
        public virtual bool IsCppCodegenTemporaryWorkaround
        {
            get { return false; }
        }

        /// <summary>
        /// Return true if the type is not permitted by the rules of the runtime to have an MethodTable.
        /// The implementation here is not intended to be complete, but represents many conditions
        /// which make a type ineligible to be an MethodTable. (This function is intended for use in assertions only)
        /// </summary>
        private bool TypeCannotHaveEEType(TypeDesc type)
        {
            if (!IsCppCodegenTemporaryWorkaround && type.GetTypeDefinition() is INonEmittableType)
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
            _typeSymbols = new NodeCache<TypeDesc, IEETypeNode>(CreateNecessaryTypeNode);

            _constructedTypeSymbols = new NodeCache<TypeDesc, IEETypeNode>(CreateConstructedTypeNode);

            _clonedTypeSymbols = new NodeCache<TypeDesc, IEETypeNode>((TypeDesc type) =>
            {
                // Only types that reside in other binaries should be cloned
                Debug.Assert(_compilationModuleGroup.ShouldReferenceThroughImportTable(type));
                return new ClonedConstructedEETypeNode(this, type);
            });

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

            _GCStaticsPreInitDataNodes = new NodeCache<MetadataType, GCStaticsPreInitDataNode>((MetadataType type) =>
            {
                ISymbolNode gcStaticsNode = TypeGCStaticsSymbol(type);
                Debug.Assert(gcStaticsNode is GCStaticsNode);               
                return ((GCStaticsNode)gcStaticsNode).NewPreInitDataNode();
            });

            _GCStaticIndirectionNodes = new NodeCache<MetadataType, EmbeddedObjectNode>((MetadataType type) =>
            {
                ISymbolNode gcStaticsNode = TypeGCStaticsSymbol(type);
                Debug.Assert(gcStaticsNode is GCStaticsNode);
                return GCStaticsRegion.NewNode((GCStaticsNode)gcStaticsNode);
            });

            _threadStatics = new NodeCache<MetadataType, ISymbolDefinitionNode>(CreateThreadStaticsNode);

            _typeThreadStaticIndices = new NodeCache<MetadataType, TypeThreadStaticIndexNode>(type =>
            {
                return new TypeThreadStaticIndexNode(type);
            });

            _GCStaticEETypes = new NodeCache<GCPointerMap, GCStaticEETypeNode>((GCPointerMap gcMap) =>
            {
                return new GCStaticEETypeNode(Target, gcMap);
            });

            _readOnlyDataBlobs = new NodeCache<ReadOnlyDataBlobKey, BlobNode>(key =>
            {
                return new BlobNode(key.Name, ObjectNodeSection.ReadOnlyDataSection, key.Data, key.Alignment);
            });

            _uninitializedWritableDataBlobs = new NodeCache<UninitializedWritableDataBlobKey, BlobNode>(key =>
            {
                return new BlobNode(key.Name, ObjectNodeSection.BssSection, new byte[key.Size], key.Alignment);
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

            _methodEntrypoints = new NodeCache<MethodDesc, IMethodNode>(CreateMethodEntrypointNode);

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

            _gvmTableEntries = new NodeCache<TypeDesc, TypeGVMEntriesNode>(type =>
            {
                return new TypeGVMEntriesNode(type);
            });

            _dynamicInvokeTemplates = new NodeCache<MethodDesc, DynamicInvokeTemplateNode>(method =>
            {
                return new DynamicInvokeTemplateNode(method);
            });

            _reflectableMethods = new NodeCache<MethodDesc, ReflectableMethodNode>(method =>
            {
                return new ReflectableMethodNode(method);
            });

            _reflectableFields = new NodeCache<FieldDesc, ReflectableFieldNode>(field =>
            {
                return new ReflectableFieldNode(field);
            });

            _objectGetTypeFlowDependencies = new NodeCache<MetadataType, ObjectGetTypeFlowDependenciesNode>(type =>
            {
                return new ObjectGetTypeFlowDependenciesNode(type);
            });

            _shadowConcreteMethods = new NodeCache<MethodKey, IMethodNode>(methodKey =>
            {
                MethodDesc canonMethod = methodKey.Method.GetCanonMethodTarget(CanonicalFormKind.Specific);

                if (methodKey.IsUnboxingStub)
                {
                    return new ShadowConcreteUnboxingThunkNode(methodKey.Method, MethodEntrypoint(canonMethod, true));
                }
                else
                {
                    return new ShadowConcreteMethodNode(methodKey.Method, MethodEntrypoint(canonMethod));
                }
            });

            _virtMethods = new NodeCache<MethodDesc, VirtualMethodUseNode>((MethodDesc method) =>
            {
                // We don't need to track virtual method uses for types that have a vtable with a known layout.
                // It's a waste of CPU time and memory.
                Debug.Assert(!VTable(method.OwningType).HasFixedSlots);

                return new VirtualMethodUseNode(method);
            });

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
                return new FrozenStringNode(data, Target);
            });

            _frozenObjectNodes = new NodeCache<SerializedFrozenObjectKey, FrozenObjectNode>(key =>
            {
                return new FrozenObjectNode(key.Owner, key.SerializableObject);
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

            _interfaceDispatchMapIndirectionNodes = new NodeCache<TypeDesc, EmbeddedObjectNode>((TypeDesc type) =>
            {
                return DispatchMapTable.NewNodeWithSymbol(InterfaceDispatchMap(type));
            });

            _genericCompositions = new NodeCache<GenericCompositionDetails, GenericCompositionNode>((GenericCompositionDetails details) =>
            {
                return new GenericCompositionNode(details);
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

            _vTableNodes = new NodeCache<TypeDesc, VTableSliceNode>((TypeDesc type ) =>
            {
                if (CompilationModuleGroup.ShouldProduceFullVTable(type))
                    return new EagerlyBuiltVTableSliceNode(type);
                else
                    return _vtableSliceProvider.GetSlice(type);
            });

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

            _typeGenericDictionaries = new NodeCache<TypeDesc, ISortableSymbolNode>(type =>
            {
                if (CompilationModuleGroup.ContainsTypeDictionary(type))
                {
                    Debug.Assert(!this.LazyGenericsPolicy.UsesLazyGenerics(type));
                    return new TypeGenericDictionaryNode(type, this);
                }
                else
                {
                    return _importedNodeProvider.ImportedTypeDictionaryNode(this, type);
                }
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

        protected virtual IEETypeNode CreateNecessaryTypeNode(TypeDesc type)
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

        protected virtual IEETypeNode CreateConstructedTypeNode(TypeDesc type)
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

        private NodeCache<TypeDesc, IEETypeNode> _typeSymbols;

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

            return _typeSymbols.GetOrAdd(type);
        }

        private NodeCache<TypeDesc, IEETypeNode> _constructedTypeSymbols;

        public IEETypeNode ConstructedTypeSymbol(TypeDesc type)
        {
            if (_compilationModuleGroup.ShouldReferenceThroughImportTable(type))
            {
                return ImportedEETypeSymbol(type);
            }

            Debug.Assert(!TypeCannotHaveEEType(type));

            return _constructedTypeSymbols.GetOrAdd(type);
        }

        private NodeCache<TypeDesc, IEETypeNode> _clonedTypeSymbols;

        public IEETypeNode MaximallyConstructableType(TypeDesc type)
        {
            if (ConstructedEETypeNode.CreationAllowed(type))
                return ConstructedTypeSymbol(type);
            else
                return NecessaryTypeSymbol(type);
        }

        public IEETypeNode ConstructedClonedTypeSymbol(TypeDesc type)
        {
            Debug.Assert(!TypeCannotHaveEEType(type));
            return _clonedTypeSymbols.GetOrAdd(type);
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

        private NodeCache<MetadataType, GCStaticsPreInitDataNode> _GCStaticsPreInitDataNodes;

        public GCStaticsPreInitDataNode GCStaticsPreInitDataNode(MetadataType type)
        {
            return _GCStaticsPreInitDataNodes.GetOrAdd(type);
        }

        private NodeCache<MetadataType, EmbeddedObjectNode> _GCStaticIndirectionNodes;

        public EmbeddedObjectNode GCStaticIndirection(MetadataType type)
        {
            return _GCStaticIndirectionNodes.GetOrAdd(type);
        }

        private NodeCache<MetadataType, ISymbolDefinitionNode> _threadStatics;

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

        public InterfaceDispatchCellNode InterfaceDispatchCell(MethodDesc method, string callSite = null)
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

        private NodeCache<GCPointerMap, GCStaticEETypeNode> _GCStaticEETypes;

        public ISymbolNode GCStaticEEType(GCPointerMap gcMap)
        {
            return _GCStaticEETypes.GetOrAdd(gcMap);
        }

        private NodeCache<UninitializedWritableDataBlobKey, BlobNode> _uninitializedWritableDataBlobs;

        public BlobNode UninitializedWritableDataBlob(Utf8String name, int size, int alignment)
        {
            return _uninitializedWritableDataBlobs.GetOrAdd(new UninitializedWritableDataBlobKey(name, size, alignment));
        }

        private NodeCache<ReadOnlyDataBlobKey, BlobNode> _readOnlyDataBlobs;

        public BlobNode ReadOnlyDataBlob(Utf8String name, byte[] blobData, int alignment)
        {
            return _readOnlyDataBlobs.GetOrAdd(new ReadOnlyDataBlobKey(name, blobData, alignment));
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

        private NodeCache<TypeDesc, EmbeddedObjectNode> _interfaceDispatchMapIndirectionNodes;

        public EmbeddedObjectNode InterfaceDispatchMapIndirection(TypeDesc type)
        {
            return _interfaceDispatchMapIndirectionNodes.GetOrAdd(type);
        }

        private NodeCache<GenericCompositionDetails, GenericCompositionNode> _genericCompositions;

        internal ISymbolNode GenericComposition(GenericCompositionDetails details)
        {
            return _genericCompositions.GetOrAdd(details);
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

        private NodeCache<TypeDesc, VTableSliceNode> _vTableNodes;

        public VTableSliceNode VTable(TypeDesc type)
        {
            return _vTableNodes.GetOrAdd(type);
        }

        private NodeCache<MethodDesc, ISortableSymbolNode> _methodGenericDictionaries;
        public ISortableSymbolNode MethodGenericDictionary(MethodDesc method)
        {
            return _methodGenericDictionaries.GetOrAdd(method);
        }

        private NodeCache<TypeDesc, ISortableSymbolNode> _typeGenericDictionaries;
        public ISortableSymbolNode TypeGenericDictionary(TypeDesc type)
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

        protected NodeCache<MethodDesc, IMethodNode> _methodEntrypoints;
        private NodeCache<MethodDesc, IMethodNode> _unboxingStubs;
        private NodeCache<IMethodNode, MethodAssociatedDataNode> _methodAssociatedData;

        public IMethodNode MethodEntrypoint(MethodDesc method, bool unboxingStub = false)
        {
            if (unboxingStub)
            {
                return _unboxingStubs.GetOrAdd(method);
            }

            return _methodEntrypoints.GetOrAdd(method);
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

        private NodeCache<TypeDesc, TypeGVMEntriesNode> _gvmTableEntries;
        internal TypeGVMEntriesNode TypeGVMEntries(TypeDesc type)
        {
            return _gvmTableEntries.GetOrAdd(type);
        }

        private NodeCache<MethodDesc, ReflectableMethodNode> _reflectableMethods;
        public ReflectableMethodNode ReflectableMethod(MethodDesc method)
        {
            return _reflectableMethods.GetOrAdd(method);
        }

        private NodeCache<FieldDesc, ReflectableFieldNode> _reflectableFields;
        public ReflectableFieldNode ReflectableField(FieldDesc field)
        {
            return _reflectableFields.GetOrAdd(field);
        }

        private NodeCache<MetadataType, ObjectGetTypeFlowDependenciesNode> _objectGetTypeFlowDependencies;
        internal ObjectGetTypeFlowDependenciesNode ObjectGetTypeFlowDependencies(MetadataType type)
        {
            return _objectGetTypeFlowDependencies.GetOrAdd(type);
        }

        private NodeCache<MethodDesc, DynamicInvokeTemplateNode> _dynamicInvokeTemplates;
        internal DynamicInvokeTemplateNode DynamicInvokeTemplate(MethodDesc method)
        {
            return _dynamicInvokeTemplates.GetOrAdd(method);
        }

        private NodeCache<MethodKey, IMethodNode> _shadowConcreteMethods;
        public IMethodNode ShadowConcreteMethod(MethodDesc method, bool isUnboxingStub = false)
        {
            return _shadowConcreteMethods.GetOrAdd(new MethodKey(method, isUnboxingStub));
        }

        private static readonly string[][] s_helperEntrypointNames = new string[][] {
            new string[] { "System.Runtime.CompilerServices", "ClassConstructorRunner", "CheckStaticClassConstructionReturnGCStaticBase" },
            new string[] { "System.Runtime.CompilerServices", "ClassConstructorRunner", "CheckStaticClassConstructionReturnNonGCStaticBase" },
            new string[] { "System.Runtime.CompilerServices", "ClassConstructorRunner", "CheckStaticClassConstructionReturnThreadStaticBase" },
            new string[] { "Internal.Runtime", "ThreadStatics", "GetThreadStaticBaseForType" }
        };

        private ISymbolNode[] _helperEntrypointSymbols;

        public ISymbolNode HelperEntrypoint(HelperEntrypoint entrypoint)
        {
            if (_helperEntrypointSymbols == null)
                _helperEntrypointSymbols = new ISymbolNode[s_helperEntrypointNames.Length];

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
                if (_systemArrayOfTClass == null)
                {
                    _systemArrayOfTClass = _context.SystemModule.GetKnownType("System", "Array`1");
                }
                return _systemArrayOfTClass;
            }
        }

        private TypeDesc _systemArrayOfTEnumeratorType;
        public TypeDesc ArrayOfTEnumeratorType
        {
            get
            {
                if (_systemArrayOfTEnumeratorType == null)
                {
                    _systemArrayOfTEnumeratorType = ArrayOfTClass.GetNestedType("ArrayEnumerator");
                }
                return _systemArrayOfTEnumeratorType;
            }
        }

        private MethodDesc _instanceMethodRemovedHelper;
        public MethodDesc InstanceMethodRemovedHelper
        {
            get
            {
                if (_instanceMethodRemovedHelper == null)
                {
                    // This helper is optional, but it's fine for this cache to be ineffective if that happens.
                    // Those scenarios are rare and typically deal with small compilations.
                    _instanceMethodRemovedHelper = TypeSystemContext.GetOptionalHelperEntryPoint("ThrowHelpers", "ThrowInstanceBodyRemoved");
                }

                return _instanceMethodRemovedHelper;
            }
        }

        private NodeCache<MethodDesc, VirtualMethodUseNode> _virtMethods;

        public DependencyNodeCore<NodeFactory> VirtualMethodUse(MethodDesc decl)
        {
            return _virtMethods.GetOrAdd(decl);
        }

        private NodeCache<MethodDesc, VariantInterfaceMethodUseNode> _variantMethods;

        public DependencyNodeCore<NodeFactory> VariantInterfaceMethodUse(MethodDesc decl)
        {
            return _variantMethods.GetOrAdd(decl);
        }

        private NodeCache<ReadyToRunHelperKey, ISymbolNode> _readyToRunHelpers;

        public ISymbolNode ReadyToRunHelper(ReadyToRunHelperId id, Object target)
        {
            return _readyToRunHelpers.GetOrAdd(new ReadyToRunHelperKey(id, target));
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

        private NodeCache<SerializedFrozenObjectKey, FrozenObjectNode> _frozenObjectNodes;

        public FrozenObjectNode SerializedFrozenObject(FieldDesc owningField, TypePreinit.ISerializableReference data)
        {
            return _frozenObjectNodes.GetOrAdd(new SerializedFrozenObjectKey(owningField, data));
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
            "__GCStaticRegionStart", 
            "__GCStaticRegionEnd",
            new SortableDependencyNode.ObjectNodeComparer(CompilerComparer.Instance));

        public ArrayOfEmbeddedDataNode<ThreadStaticsNode> ThreadStaticsRegion = new ArrayOfEmbeddedDataNode<ThreadStaticsNode>(
            "__ThreadStaticRegionStart",
            "__ThreadStaticRegionEnd",
            new SortableDependencyNode.EmbeddedObjectNodeComparer(CompilerComparer.Instance));

        public ArrayOfEmbeddedPointersNode<IMethodNode> EagerCctorTable = new ArrayOfEmbeddedPointersNode<IMethodNode>(
            "__EagerCctorStart",
            "__EagerCctorEnd",
            null);

        public ArrayOfEmbeddedPointersNode<InterfaceDispatchMapNode> DispatchMapTable = new ArrayOfEmbeddedPointersNode<InterfaceDispatchMapNode>(
            "__DispatchMapTableStart",
            "__DispatchMapTableEnd",
            new SortableDependencyNode.ObjectNodeComparer(CompilerComparer.Instance));

        public ArrayOfEmbeddedDataNode<EmbeddedObjectNode> FrozenSegmentRegion = new ArrayOfFrozenObjectsNode<EmbeddedObjectNode>(
            "__FrozenSegmentRegionStart",
            "__FrozenSegmentRegionEnd",
            new SortableDependencyNode.EmbeddedObjectNodeComparer(CompilerComparer.Instance));

        internal ModuleInitializerListNode ModuleInitializerList = new ModuleInitializerListNode();

        public InterfaceDispatchCellSectionNode InterfaceDispatchCellSection { get; }

        public ReadyToRunHeaderNode ReadyToRunHeader;

        public Dictionary<ISymbolNode, string> NodeAliases = new Dictionary<ISymbolNode, string>();

        protected internal TypeManagerIndirectionNode TypeManagerIndirection = new TypeManagerIndirectionNode();

        public virtual void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph)
        {
            ReadyToRunHeader = new ReadyToRunHeaderNode(Target);

            graph.AddRoot(ReadyToRunHeader, "ReadyToRunHeader is always generated");
            graph.AddRoot(new ModulesSectionNode(Target), "ModulesSection is always generated");

            graph.AddRoot(GCStaticsRegion, "GC StaticsRegion is always generated");
            graph.AddRoot(ThreadStaticsRegion, "ThreadStaticsRegion is always generated");
            graph.AddRoot(EagerCctorTable, "EagerCctorTable is always generated");
            graph.AddRoot(TypeManagerIndirection, "TypeManagerIndirection is always generated");
            graph.AddRoot(DispatchMapTable, "DispatchMapTable is always generated");
            graph.AddRoot(FrozenSegmentRegion, "FrozenSegmentRegion is always generated");
            graph.AddRoot(InterfaceDispatchCellSection, "Interface dispatch cell section is always generated");
            graph.AddRoot(ModuleInitializerList, "Module initializer list is always generated");

            ReadyToRunHeader.Add(ReadyToRunSectionType.GCStaticRegion, GCStaticsRegion, GCStaticsRegion.StartSymbol, GCStaticsRegion.EndSymbol);
            ReadyToRunHeader.Add(ReadyToRunSectionType.ThreadStaticRegion, ThreadStaticsRegion, ThreadStaticsRegion.StartSymbol, ThreadStaticsRegion.EndSymbol);
            ReadyToRunHeader.Add(ReadyToRunSectionType.EagerCctor, EagerCctorTable, EagerCctorTable.StartSymbol, EagerCctorTable.EndSymbol);
            ReadyToRunHeader.Add(ReadyToRunSectionType.TypeManagerIndirection, TypeManagerIndirection, TypeManagerIndirection);
            ReadyToRunHeader.Add(ReadyToRunSectionType.InterfaceDispatchTable, DispatchMapTable, DispatchMapTable.StartSymbol);
            ReadyToRunHeader.Add(ReadyToRunSectionType.FrozenObjectRegion, FrozenSegmentRegion, FrozenSegmentRegion.StartSymbol, FrozenSegmentRegion.EndSymbol);
            ReadyToRunHeader.Add(ReadyToRunSectionType.ModuleInitializerList, ModuleInitializerList, ModuleInitializerList, ModuleInitializerList.EndSymbol);

            var commonFixupsTableNode = new ExternalReferencesTableNode("CommonFixupsTable", this);
            InteropStubManager.AddToReadyToRunHeader(ReadyToRunHeader, this, commonFixupsTableNode);
            MetadataManager.AddToReadyToRunHeader(ReadyToRunHeader, this, commonFixupsTableNode);
            MetadataManager.AttachToDependencyGraph(graph);
            ReadyToRunHeader.Add(MetadataManager.BlobIdToReadyToRunSection(ReflectionMapBlob.CommonFixupsTable), commonFixupsTableNode, commonFixupsTableNode, commonFixupsTableNode.EndSymbol);
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
            public readonly string CallsiteId;

            public DispatchCellKey(MethodDesc target, string callsiteId)
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

        protected struct UninitializedWritableDataBlobKey : IEquatable<UninitializedWritableDataBlobKey>
        {
            public readonly Utf8String Name;
            public readonly int Size;
            public readonly int Alignment;

            public UninitializedWritableDataBlobKey(Utf8String name, int size, int alignment)
            {
                Name = name;
                Size = size;
                Alignment = alignment;
            }

            // The assumption here is that the name of the blob is unique.
            // We can't emit two blobs with the same name and different contents.
            // The name is part of the symbolic name and we don't do any mangling on it.
            public bool Equals(UninitializedWritableDataBlobKey other) => Name.Equals(other.Name);
            public override bool Equals(object obj) => obj is UninitializedWritableDataBlobKey && Equals((UninitializedWritableDataBlobKey)obj);
            public override int GetHashCode() => Name.GetHashCode();
        }

        protected struct SerializedFrozenObjectKey : IEquatable<SerializedFrozenObjectKey>
        {
            public readonly FieldDesc Owner;
            public readonly TypePreinit.ISerializableReference SerializableObject;

            public SerializedFrozenObjectKey(FieldDesc owner, TypePreinit.ISerializableReference obj)
            {
                Owner = owner;
                SerializableObject = obj;
            }

            public override bool Equals(object obj) => obj is SerializedFrozenObjectKey && Equals((SerializedFrozenObjectKey)obj);
            public bool Equals(SerializedFrozenObjectKey other) => Owner == other.Owner;
            public override int GetHashCode() => Owner.GetHashCode();
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Metadata;

using Debug = System.Diagnostics.Debug;
using ReadyToRunSectionType = Internal.Runtime.ReadyToRunSectionType;
using ReflectionMapBlob = Internal.Runtime.ReflectionMapBlob;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using CombinedDependencyList = System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;
using CombinedDependencyListEntry = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry;
using MethodIL = Internal.IL.MethodIL;
using CustomAttributeValue = System.Reflection.Metadata.CustomAttributeValue<Internal.TypeSystem.TypeDesc>;
using MethodSignature = Internal.TypeSystem.MethodSignature;

using MetadataRecord = Internal.Metadata.NativeFormat.Writer.MetadataRecord;
using MetadataWriter = Internal.Metadata.NativeFormat.Writer.MetadataWriter;
using TypeReference = Internal.Metadata.NativeFormat.Writer.TypeReference;
using Field = Internal.Metadata.NativeFormat.Writer.Field;
using TypeSpecification = Internal.Metadata.NativeFormat.Writer.TypeSpecification;
using ConstantStringValue = Internal.Metadata.NativeFormat.Writer.ConstantStringValue;
using TypeInstantiationSignature = Internal.Metadata.NativeFormat.Writer.TypeInstantiationSignature;
using ConstantStringArray = Internal.Metadata.NativeFormat.Writer.ConstantStringArray;

namespace ILCompiler
{
    /// <summary>
    /// This class is responsible for managing native metadata to be emitted into the compiled
    /// module. It also helps facilitate mappings between generated runtime structures or code,
    /// and the native metadata.
    /// </summary>
    public abstract class MetadataManager : ICompilationRootProvider
    {
        internal const int MetadataOffsetMask = 0xFFFFFF;

        protected readonly MetadataManagerOptions _options;

        private byte[] _metadataBlob;
        private List<MetadataMapping<MetadataType>> _typeMappings;
        private List<MetadataMapping<FieldDesc>> _fieldMappings;
        private List<MetadataMapping<MethodDesc>> _methodMappings;
        private List<StackTraceMapping> _stackTraceMappings;
        protected readonly string _metadataLogFile;
        protected readonly StackTraceEmissionPolicy _stackTraceEmissionPolicy;

        protected readonly CompilerTypeSystemContext _typeSystemContext;
        protected readonly MetadataBlockingPolicy _blockingPolicy;
        protected readonly ManifestResourceBlockingPolicy _resourceBlockingPolicy;
        protected readonly DynamicInvokeThunkGenerationPolicy _dynamicInvokeThunkGenerationPolicy;

        private readonly List<InterfaceDispatchCellNode> _interfaceDispatchCells = new List<InterfaceDispatchCellNode>();
        private readonly SortedSet<NonGCStaticsNode> _cctorContextsGenerated = new SortedSet<NonGCStaticsNode>(CompilerComparer.Instance);
        private readonly SortedSet<MetadataType> _typesWithGCStaticsGenerated = new SortedSet<MetadataType>(CompilerComparer.Instance);
        private readonly SortedSet<MetadataType> _typesWithNonGCStaticsGenerated = new SortedSet<MetadataType>(CompilerComparer.Instance);
        private readonly SortedSet<MetadataType> _typesWithThreadStaticsGenerated = new SortedSet<MetadataType>(CompilerComparer.Instance);
        private readonly SortedSet<TypeDesc> _typesWithEETypesGenerated = new SortedSet<TypeDesc>(TypeSystemComparer.Instance);
        private readonly SortedSet<TypeDesc> _typesWithConstructedEETypesGenerated = new SortedSet<TypeDesc>(TypeSystemComparer.Instance);
        private readonly SortedSet<MethodDesc> _methodsGenerated = new SortedSet<MethodDesc>(TypeSystemComparer.Instance);
        private readonly SortedSet<MethodDesc> _reflectableMethods = new SortedSet<MethodDesc>(TypeSystemComparer.Instance);
        private readonly SortedSet<GenericDictionaryNode> _genericDictionariesGenerated = new SortedSet<GenericDictionaryNode>(CompilerComparer.Instance);
        private readonly SortedSet<IMethodBodyNode> _methodBodiesGenerated = new SortedSet<IMethodBodyNode>(CompilerComparer.Instance);
        private readonly SortedSet<FrozenObjectNode> _frozenObjects = new SortedSet<FrozenObjectNode>(CompilerComparer.Instance);
        private readonly SortedSet<TypeGVMEntriesNode> _typeGVMEntries
            = new SortedSet<TypeGVMEntriesNode>(Comparer<TypeGVMEntriesNode>.Create((a, b) => TypeSystemComparer.Instance.Compare(a.AssociatedType, b.AssociatedType)));
        private readonly SortedSet<DefType> _typesWithDelegateMarshalling = new SortedSet<DefType>(TypeSystemComparer.Instance);
        private readonly SortedSet<DefType> _typesWithStructMarshalling = new SortedSet<DefType>(TypeSystemComparer.Instance);
        private HashSet<NativeLayoutTemplateMethodSignatureVertexNode> _templateMethodEntries = new HashSet<NativeLayoutTemplateMethodSignatureVertexNode>();
        private readonly SortedSet<TypeDesc> _typeTemplates = new SortedSet<TypeDesc>(TypeSystemComparer.Instance);
        private readonly SortedSet<MetadataType> _typesWithGenericStaticBaseInfo = new SortedSet<MetadataType>(TypeSystemComparer.Instance);
        private readonly SortedSet<MethodDesc> _genericMethodHashtableEntries = new SortedSet<MethodDesc>(TypeSystemComparer.Instance);

        private List<(DehydratableObjectNode Node, ObjectNode.ObjectData Data)> _dehydratableData = new List<(DehydratableObjectNode Node, ObjectNode.ObjectData data)>();

        internal NativeLayoutInfoNode NativeLayoutInfo { get; private set; }

        public MetadataManager(CompilerTypeSystemContext typeSystemContext, MetadataBlockingPolicy blockingPolicy,
            ManifestResourceBlockingPolicy resourceBlockingPolicy, string logFile, StackTraceEmissionPolicy stackTracePolicy,
            DynamicInvokeThunkGenerationPolicy dynamicInvokeThunkGenerationPolicy,
            MetadataManagerOptions options)
        {
            _typeSystemContext = typeSystemContext;
            _blockingPolicy = blockingPolicy;
            _resourceBlockingPolicy = resourceBlockingPolicy;
            _dynamicInvokeThunkGenerationPolicy = dynamicInvokeThunkGenerationPolicy;
            _options = options;
            _metadataLogFile = logFile;
            _stackTraceEmissionPolicy = stackTracePolicy;
        }

        public bool IsDataDehydrated => (_options & MetadataManagerOptions.DehydrateData) != 0;

        internal ObjectNode.ObjectData PrepareForDehydration(DehydratableObjectNode node, ObjectNode.ObjectData hydratedData)
        {
            _dehydratableData.Add((node, hydratedData));

            return new ObjectNode.ObjectData(new byte[hydratedData.Data.Length],
                Array.Empty<Relocation>(),
                hydratedData.Alignment,
                hydratedData.DefinedSymbols);
        }

        public IEnumerable<ObjectNode.ObjectData> GetDehydratableData()
        {
#if DEBUG
            // We're making an assumption that PrepareForDehydration was called in the emission order.
            // Double check that here.
            var comparer = new CompilerComparer();
            for (int i = 1; i < _dehydratableData.Count; i++)
                Debug.Assert(comparer.Compare(_dehydratableData[i - 1].Node, _dehydratableData[i].Node) < 0);
#endif

            foreach (var entry in _dehydratableData)
                yield return entry.Data;
        }

        public void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph)
        {
            graph.NewMarkedNode += Graph_NewMarkedNode;
        }

        internal static ReadyToRunSectionType BlobIdToReadyToRunSection(ReflectionMapBlob blobId)
        {
            var result = (ReadyToRunSectionType)((int)blobId + (int)ReadyToRunSectionType.ReadonlyBlobRegionStart);
            Debug.Assert(result <= ReadyToRunSectionType.ReadonlyBlobRegionEnd);
            return result;
        }

        public virtual void AddToReadyToRunHeader(ReadyToRunHeaderNode header, NodeFactory nodeFactory, ExternalReferencesTableNode commonFixupsTableNode)
        {
            var metadataNode = new MetadataNode();
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.EmbeddedMetadata), metadataNode);

            var nativeReferencesTableNode = new ExternalReferencesTableNode("NativeReferences", nodeFactory);
            var nativeStaticsTableNode = new ExternalReferencesTableNode("NativeStatics", nodeFactory);

            var resourceDataNode = new ResourceDataNode();
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.BlobIdResourceData), resourceDataNode);

            var resourceIndexNode = new ResourceIndexNode(resourceDataNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.BlobIdResourceIndex), resourceIndexNode);

            var typeMapNode = new TypeMetadataMapNode(commonFixupsTableNode);

            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.TypeMap), typeMapNode);

            var cctorContextMapNode = new ClassConstructorContextMap(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.CCtorContextMap), cctorContextMapNode);

            var invokeMapNode = new ReflectionInvokeMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.InvokeMap), invokeMapNode);

            var arrayMapNode = new ArrayMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.ArrayMap), arrayMapNode);

            var byRefMapNode = new ByRefTypeMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.ByRefTypeMap), byRefMapNode);

            var pointerMapNode = new PointerTypeMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.PointerTypeMap), pointerMapNode);

            var functionPointerMapNode = new FunctionPointerMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.FunctionPointerTypeMap), functionPointerMapNode);

            var fieldMapNode = new ReflectionFieldMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.FieldAccessMap), fieldMapNode);

            NativeLayoutInfo = new NativeLayoutInfoNode(nativeReferencesTableNode, nativeStaticsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.NativeLayoutInfo), NativeLayoutInfo);

            var exactMethodInstantiations = new ExactMethodInstantiationsNode(nativeReferencesTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.ExactMethodInstantiationsHashtable), exactMethodInstantiations);

            var genericsTypesHashtableNode = new GenericTypesHashtableNode(nativeReferencesTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.GenericsHashtable), genericsTypesHashtableNode);

            var genericMethodsHashtableNode = new GenericMethodsHashtableNode(nativeReferencesTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.GenericMethodsHashtable), genericMethodsHashtableNode);

            var genericVirtualMethodTableNode = new GenericVirtualMethodTableNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.GenericVirtualMethodTable), genericVirtualMethodTableNode);

            var interfaceGenericVirtualMethodTableNode = new InterfaceGenericVirtualMethodTableNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.InterfaceGenericVirtualMethodTable), interfaceGenericVirtualMethodTableNode);

            var genericMethodsTemplatesMapNode = new GenericMethodsTemplateMap(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.GenericMethodsTemplateMap), genericMethodsTemplatesMapNode);

            var genericTypesTemplatesMapNode = new GenericTypesTemplateMap(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.TypeTemplateMap), genericTypesTemplatesMapNode);

            var staticsInfoHashtableNode = new StaticsInfoHashtableNode(nativeReferencesTableNode, nativeStaticsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.StaticsInfoHashtable), staticsInfoHashtableNode);

            var virtualInvokeMapNode = new ReflectionVirtualInvokeMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.VirtualInvokeMap), virtualInvokeMapNode);

            var stackTraceMethodMappingNode = new StackTraceMethodMappingNode();
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.BlobIdStackTraceMethodRvaToTokenMapping), stackTraceMethodMappingNode);

            // The external references tables should go last
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.NativeReferences), nativeReferencesTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.NativeStatics), nativeStaticsTableNode);

            if (IsDataDehydrated)
            {
                var dehydratedDataNode = new DehydratedDataNode();
                header.Add(ReadyToRunSectionType.DehydratedData, dehydratedDataNode);
            }
        }

        protected virtual void Graph_NewMarkedNode(DependencyNodeCore<NodeFactory> obj)
        {
            var eetypeNode = obj as EETypeNode;
            if (eetypeNode != null)
            {
                _typesWithEETypesGenerated.Add(eetypeNode.Type);

                if (eetypeNode is ConstructedEETypeNode || eetypeNode is CanonicalEETypeNode)
                {
                    _typesWithConstructedEETypesGenerated.Add(eetypeNode.Type);
                }

                return;
            }

            IMethodBodyNode methodBodyNode = obj as IMethodBodyNode;
            if (methodBodyNode != null)
            {
                _methodBodiesGenerated.Add(methodBodyNode);
            }

            IMethodNode methodNode = methodBodyNode;
            if (methodNode != null)
            {
                if (AllMethodsCanBeReflectable)
                    _reflectableMethods.Add(methodNode.Method);
            }

            methodNode ??= obj as ShadowConcreteMethodNode;

            if (methodNode != null)
            {
                _methodsGenerated.Add(methodNode.Method);
                return;
            }

            var reflectedMethodNode = obj as ReflectedMethodNode;
            if (reflectedMethodNode != null)
            {
                _reflectableMethods.Add(reflectedMethodNode.Method);
            }

            if (obj is NonGCStaticsNode nonGcStaticSectionNode)
            {
                if (nonGcStaticSectionNode.HasLazyStaticConstructor)
                    _cctorContextsGenerated.Add(nonGcStaticSectionNode);

                _typesWithNonGCStaticsGenerated.Add(nonGcStaticSectionNode.Type);
            }

            if (obj is GCStaticsNode gcStaticsNode)
            {
                _typesWithGCStaticsGenerated.Add(gcStaticsNode.Type);
            }

            if (obj is TypeThreadStaticIndexNode threadStaticsNode)
            {
                _typesWithThreadStaticsGenerated.Add(threadStaticsNode.Type);
            }

            var gvmEntryNode = obj as TypeGVMEntriesNode;
            if (gvmEntryNode != null)
            {
                _typeGVMEntries.Add(gvmEntryNode);
            }

            var dictionaryNode = obj as GenericDictionaryNode;
            if (dictionaryNode != null)
            {
                _genericDictionariesGenerated.Add(dictionaryNode);
            }

            if (obj is InterfaceDispatchCellNode dispatchCell)
            {
                _interfaceDispatchCells.Add(dispatchCell);
            }

            if (obj is StructMarshallingDataNode structMarshallingDataNode)
            {
                _typesWithStructMarshalling.Add(structMarshallingDataNode.Type);
            }

            if (obj is DelegateMarshallingDataNode delegateMarshallingDataNode)
            {
                _typesWithDelegateMarshalling.Add(delegateMarshallingDataNode.Type);
            }

            if (obj is NativeLayoutTemplateMethodSignatureVertexNode templateMethodEntry)
            {
                _templateMethodEntries.Add(templateMethodEntry);
            }

            if (obj is NativeLayoutTemplateTypeLayoutVertexNode typeTemplate)
            {
                _typeTemplates.Add(typeTemplate.CanonType);
            }

            if (obj is FrozenObjectNode frozenObj)
            {
                _frozenObjects.Add(frozenObj);
            }

            if (obj is GenericStaticBaseInfoNode genericStaticBaseInfo)
            {
                _typesWithGenericStaticBaseInfo.Add(genericStaticBaseInfo.Type);
            }

            if (obj is GenericMethodsHashtableEntryNode genericMethodsHashtableEntryNode)
            {
                _genericMethodHashtableEntries.Add(genericMethodsHashtableEntryNode.Method);
            }
        }

        protected virtual bool AllMethodsCanBeReflectable => false;

        /// <summary>
        /// Is a method that is reflectable a method which should be placed into the invoke map as invokable?
        /// </summary>
        public virtual bool IsReflectionInvokable(MethodDesc method)
        {
            return IsMethodSupportedInReflectionInvoke(method);
        }

        public static bool IsMethodSupportedInReflectionInvoke(MethodDesc method)
        {
            TypeDesc owningType = method.OwningType;

            // Methods on nullable are special cased in the runtime reflection
            if (owningType.IsNullable)
                return false;

            // Methods on arrays are special cased in the runtime reflection
            if (owningType.IsArray)
                return false;

            // Finalizers are not reflection invokable
            if (method.IsFinalizer)
                return false;

            // Static constructors are not reflection invokable
            if (method.IsStaticConstructor)
                return false;

            if (method.IsConstructor)
            {
                // Delegate construction is only allowed through specific IL sequences
                if (owningType.IsDelegate)
                    return false;

                // String constructors are intrinsic and special cased in runtime reflection
                if (owningType.IsString)
                    return false;
            }

            // TODO: Reflection invoking static virtual methods
            if (method.IsVirtual && method.Signature.IsStatic)
                return false;

            // Everything else can go in the mapping table.
            return true;
        }

        /// <summary>
        /// Is there a reflection invoke stub for a method that is invokable?
        /// </summary>
        public bool HasReflectionInvokeStub(MethodDesc method)
        {
            if (!IsReflectionInvokable(method))
                return false;

            return HasReflectionInvokeStubForInvokableMethod(method);
        }

        /// <summary>
        /// Is there a reflection invoke stub for a method that is invokable?
        /// </summary>
        public bool ShouldMethodBeInInvokeMap(MethodDesc method)
        {
            // The current format requires us to have an MethodTable for the owning type. We might want to lift this.
            if (!TypeGeneratesEEType(method.OwningType))
                return false;

            // We have a method body, we have a metadata token, but we can't get an invoke stub. Bail.
            if (!IsReflectionInvokable(method))
                return false;

            return true;
        }

        public void GetDependenciesDueToGenericDictionary(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            MetadataCategory category = GetMetadataCategory(method.GetCanonMethodTarget(CanonicalFormKind.Specific));

            if ((category & MetadataCategory.RuntimeMapping) != 0)
            {
                // If the method is visible from reflection, we need to keep track of this statically generated
                // dictionary to make sure MakeGenericMethod works even without a type loader template
                dependencies ??= new DependencyList();
                dependencies.Add(factory.GenericMethodsHashtableEntry(method), "Reflection visible dictionary");
            }

            if (method.Signature.IsStatic && method.IsSynchronized)
            {
                dependencies ??= new DependencyList();
                dependencies.Add(factory.GenericMethodsHashtableEntry(method), "Will need to look up owning type from dictionary");
            }
        }

        public IEnumerable<CombinedDependencyListEntry> GetConditionalDependenciesDueToGenericDictionary(NodeFactory factory, MethodDesc method)
        {
            // If there's a template for this method, we need to keep track of the dictionary so that we
            // don't accidentally create a new dictionary for the same method at runtime.
            yield return new CombinedDependencyListEntry(
                factory.GenericMethodsHashtableEntry(method),
                factory.NativeLayout.TemplateMethodEntry(method.GetCanonMethodTarget(CanonicalFormKind.Specific)),
                "Runtime-constructable dictionary");
        }

        /// <summary>
        /// This method is an extension point that can provide additional metadata-based dependencies to compiled method bodies.
        /// </summary>
        public void GetDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            MetadataCategory category = GetMetadataCategory(method);

            if ((category & MetadataCategory.Description) != 0)
            {
                GetMetadataDependenciesDueToReflectability(ref dependencies, factory, method);
            }

            if ((category & MetadataCategory.RuntimeMapping) != 0)
            {
                if (IsReflectionInvokable(method))
                {
                    // We're going to generate a mapping table entry for this. Collect dependencies.
                    ReflectionInvokeMapNode.AddDependenciesDueToReflectability(ref dependencies, factory, method);

                    ReflectionInvokeSupportDependencyAlgorithm.GetDependenciesFromParamsArray(ref dependencies, factory, method);
                }

                GenericMethodsTemplateMap.GetTemplateMethodDependencies(ref dependencies, factory, method);
                GenericTypesTemplateMap.GetTemplateTypeDependencies(ref dependencies, factory, method.OwningType);
            }
        }

        /// <summary>
        /// This method is an extension point that can provide additional metadata-based dependencies to generated fields.
        /// </summary>
        public void GetDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, FieldDesc field)
        {
            MetadataCategory category = GetMetadataCategory(field);

            if ((category & MetadataCategory.Description) != 0)
            {
                GetMetadataDependenciesDueToReflectability(ref dependencies, factory, field);
            }

            if ((category & MetadataCategory.RuntimeMapping) != 0)
            {
                TypeDesc owningCanonicalType = field.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific);
                GenericTypesTemplateMap.GetTemplateTypeDependencies(ref dependencies, factory, owningCanonicalType);
            }
        }

        /// <summary>
        /// This method is an extension point that can provide additional metadata-based dependencies on a virtual method.
        /// </summary>
        public virtual void GetDependenciesDueToVirtualMethodReflectability(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
        }

        protected virtual void GetMetadataDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the emission of metadata
            // (E.g. dependencies caused by the method having custom attributes applied to it: making sure we compile the attribute constructor
            // and property setters)
        }

        protected virtual void GetMetadataDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, FieldDesc field)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the emission of metadata
            // (E.g. dependencies caused by the field having custom attributes applied to it: making sure we compile the attribute constructor
            // and property setters)
        }

        /// <summary>
        /// This method is an extension point that can provide additional metadata-based dependencies to generated EETypes.
        /// </summary>
        public virtual void GetDependenciesDueToEETypePresence(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            MetadataCategory category = GetMetadataCategory(type);

            if ((category & MetadataCategory.Description) != 0)
            {
                GetMetadataDependenciesDueToReflectability(ref dependencies, factory, type);
            }
        }

        internal virtual void GetDependenciesDueToModuleUse(ref DependencyList dependencies, NodeFactory factory, ModuleDesc module)
        {
            // MetadataManagers can override this to provide additional dependencies caused by using a module
        }

        protected virtual void GetMetadataDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the emission of metadata
            // (E.g. dependencies caused by the type having custom attributes applied to it: making sure we compile the attribute constructor
            // and property setters)
        }

        public virtual void GetConditionalDependenciesDueToEETypePresence(ref CombinedDependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the presence of
            // an MethodTable.
        }

        public virtual bool HasConditionalDependenciesDueToEETypePresence(TypeDesc type)
        {
            return false;
        }

        /// <summary>
        /// This method is an extension point that can provide additional metadata-based dependencies to generated RuntimeMethodHandles.
        /// </summary>
        public virtual void GetDependenciesDueToLdToken(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the presence of a
            // RuntimeMethodHandle data structure.
        }

        /// <summary>
        /// This method is an extension point that can provide additional metadata-based dependencies to generated RuntimeFieldHandles.
        /// </summary>
        public virtual void GetDependenciesDueToLdToken(ref DependencyList dependencies, NodeFactory factory, FieldDesc field)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the presence of a
            // RuntimeFieldHandle data structure.
        }

        /// <summary>
        /// This method is an extension point that can provide additional metadata-based dependencies to delegate targets.
        /// </summary>
        public virtual void GetDependenciesDueToDelegateCreation(ref CombinedDependencyList dependencies, NodeFactory factory, TypeDesc delegateType, MethodDesc target)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the construction
            // of a delegate to a method.
        }

        /// <summary>
        /// This method is an extension point that can provide additional dependencies for overriden methods on constructed types.
        /// </summary>
        public virtual void GetDependenciesForOverridingMethod(ref CombinedDependencyList dependencies, NodeFactory factory, MethodDesc decl, MethodDesc impl)
        {
        }

        /// <summary>
        /// Gets a list of fields that got "compiled" and are eligible for a runtime mapping.
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<FieldDesc> GetFieldsWithRuntimeMapping();

        /// <summary>
        /// This method is an extension point that can provide additional metadata-based dependencies to generated method bodies.
        /// </summary>
        public void GetDependenciesDueToMethodCodePresence(ref DependencyList dependencies, NodeFactory factory, MethodDesc method, MethodIL methodIL)
        {
            if (method.HasInstantiation)
            {
                ExactMethodInstantiationsNode.GetExactMethodInstantiationDependenciesForMethod(ref dependencies, factory, method);
            }

            InlineableStringsResourceNode.AddDependenciesDueToResourceStringUse(ref dependencies, factory, method);

            GetDependenciesDueToMethodCodePresenceInternal(ref dependencies, factory, method, methodIL);
        }

        public virtual void GetConditionalDependenciesDueToMethodCodePresence(ref CombinedDependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the presence of
            // method code.
        }

        protected virtual void GetDependenciesDueToMethodCodePresenceInternal(ref DependencyList dependencies, NodeFactory factory, MethodDesc method, MethodIL methodIL)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the presence of a
            // compiled method body.
        }

        /// <summary>
        /// Given that a method is invokable, does there exist a reflection invoke stub?
        /// </summary>
        public bool HasReflectionInvokeStubForInvokableMethod(MethodDesc method)
        {
            Debug.Assert(IsReflectionInvokable(method));
            return _dynamicInvokeThunkGenerationPolicy.HasStaticInvokeThunk(method);
        }

        /// <summary>
        /// Given that a method is invokable, if it is inserted into the reflection invoke table
        /// will it use a method token to be referenced, or not?
        /// </summary>
        public bool WillUseMetadataTokenToReferenceMethod(MethodDesc method)
        {
            return (GetMetadataCategory(method) & MetadataCategory.Description) != 0;
        }

        /// <summary>
        /// Given that a method is invokable, if it is inserted into the reflection invoke table
        /// will it use a field token to be referenced, or not?
        /// </summary>
        public bool WillUseMetadataTokenToReferenceField(FieldDesc field)
        {
            return (GetMetadataCategory(field) & MetadataCategory.Description) != 0;
        }

        /// <summary>
        /// Gets a stub that can be used to reflection-invoke a method with a given signature.
        /// </summary>
        public MethodDesc GetReflectionInvokeStub(MethodDesc method)
        {
            return _typeSystemContext.GetDynamicInvokeThunk(method.Signature,
                !method.Signature.IsStatic && method.OwningType.IsValueType);
        }

        protected void EnsureMetadataGenerated(NodeFactory factory)
        {
            if (_metadataBlob != null)
                return;

            ComputeMetadata(factory, out _metadataBlob, out _typeMappings, out _methodMappings, out _fieldMappings, out _stackTraceMappings);
        }

        void ICompilationRootProvider.AddCompilationRoots(IRootingServiceProvider rootProvider)
        {
            // MetadataManagers can override this to provide metadata compilation roots that need to be added to the graph ahead of time.
            // (E.g. reflection roots computed by IL analyzers, or non-compilation-based roots)
        }

        protected abstract void ComputeMetadata(NodeFactory factory,
                                                out byte[] metadataBlob,
                                                out List<MetadataMapping<MetadataType>> typeMappings,
                                                out List<MetadataMapping<MethodDesc>> methodMappings,
                                                out List<MetadataMapping<FieldDesc>> fieldMappings,
                                                out List<StackTraceMapping> stackTraceMapping);

        protected void ComputeMetadata<TPolicy>(
            TPolicy policy,
            NodeFactory factory,
            out byte[] metadataBlob,
            out List<MetadataMapping<MetadataType>> typeMappings,
            out List<MetadataMapping<MethodDesc>> methodMappings,
            out List<MetadataMapping<FieldDesc>> fieldMappings,
            out List<StackTraceMapping> stackTraceMapping) where TPolicy : struct, IMetadataPolicy
        {
            var transformed = MetadataTransform.Run(policy, GetCompilationModulesWithMetadata());
            MetadataTransform transform = transformed.Transform;

            // Generate metadata blob
            var writer = new MetadataWriter();
            writer.ScopeDefinitions.AddRange(transformed.Scopes);

            // Generate entries in the blob for methods that will be necessary for stack trace purposes.
            var stackTraceRecords = new List<StackTraceRecordData>();
            foreach (var methodBody in GetCompiledMethodBodies())
            {
                MethodDesc method = methodBody.Method;

                MethodDesc typicalMethod = method.GetTypicalMethodDefinition();

                // Methods that will end up in the reflection invoke table should not have an entry in stack trace table
                // We'll try looking them up in reflection data at runtime.
                if (transformed.GetTransformedMethodDefinition(typicalMethod) != null &&
                    ShouldMethodBeInInvokeMap(method) &&
                    (GetMetadataCategory(method) & MetadataCategory.RuntimeMapping) != 0)
                    continue;

                MethodStackTraceVisibilityFlags stackVisibility = _stackTraceEmissionPolicy.GetMethodVisibility(method);
                bool isHidden = (stackVisibility & MethodStackTraceVisibilityFlags.IsHidden) != 0;

                if ((stackVisibility & MethodStackTraceVisibilityFlags.HasMetadata) != 0)
                {
                    StackTraceRecordData record = CreateStackTraceRecord(transform, method, isHidden);

                    stackTraceRecords.Add(record);

                    writer.AdditionalRootRecords.Add(record.OwningType);
                    writer.AdditionalRootRecords.Add(record.MethodName);
                    writer.AdditionalRootRecords.Add(record.MethodSignature);
                    writer.AdditionalRootRecords.Add(record.MethodInstantiationArgumentCollection);
                }
                else if (isHidden)
                {
                    stackTraceRecords.Add(new StackTraceRecordData(method, null, null, null, null, isHidden));
                }
            }

            var ms = new MemoryStream();

            // .NET metadata is UTF-16 and UTF-16 contains code points that don't translate to UTF-8.
            var noThrowUtf8Encoding = new UTF8Encoding(false, false);

            using (var logWriter = _metadataLogFile != null ? new StreamWriter(File.Open(_metadataLogFile, FileMode.Create, FileAccess.Write, FileShare.Read), noThrowUtf8Encoding) : null)
            {
                writer.LogWriter = logWriter;
                writer.Write(ms);
            }

            metadataBlob = ms.ToArray();

            const int MaxAllowedMetadataOffset = 0xFFFFFF;
            if (metadataBlob.Length > MaxAllowedMetadataOffset)
            {
                // Offset portion of metadata handles is limited to 16 MB.
                throw new InvalidOperationException($"Metadata blob exceeded the addressing range (allowed: {MaxAllowedMetadataOffset}, actual: {metadataBlob.Length})");
            }

            typeMappings = new List<MetadataMapping<MetadataType>>();
            methodMappings = new List<MetadataMapping<MethodDesc>>();
            fieldMappings = new List<MetadataMapping<FieldDesc>>();
            stackTraceMapping = new List<StackTraceMapping>();

            // Generate type definition mappings
            foreach (var type in factory.MetadataManager.GetTypesWithEETypes())
            {
                MetadataType definition = type.IsTypeDefinition ? type as MetadataType : null;
                if (definition == null)
                    continue;

                MetadataRecord record = transformed.GetTransformedTypeDefinition(definition);

                // Reflection requires that we maintain type identity. Even if we only generated a TypeReference record,
                // if there is an MethodTable for it, we also need a mapping table entry for it.
                record ??= transformed.GetTransformedTypeReference(definition);

                if (record != null)
                    typeMappings.Add(new MetadataMapping<MetadataType>(definition, writer.GetRecordHandle(record)));
            }

            foreach (var method in GetReflectableMethods())
            {
                if (method.IsGenericMethodDefinition || method.OwningType.IsGenericDefinition)
                {
                    // Generic definitions don't have runtime artifacts we would need to map to.
                    continue;
                }

                if (method.GetCanonMethodTarget(CanonicalFormKind.Specific) != method)
                {
                    // Methods that are not in their canonical form are not interesting
                    continue;
                }

                if (IsReflectionBlocked(method.Instantiation) || IsReflectionBlocked(method.OwningType.Instantiation))
                    continue;

                if ((GetMetadataCategory(method) & MetadataCategory.RuntimeMapping) == 0)
                    continue;

                MetadataRecord record = transformed.GetTransformedMethodDefinition(method.GetTypicalMethodDefinition());

                if (record != null)
                    methodMappings.Add(new MetadataMapping<MethodDesc>(method, writer.GetRecordHandle(record)));
            }

            HashSet<FieldDesc> canonicalFields = new HashSet<FieldDesc>();
            foreach (var field in GetFieldsWithRuntimeMapping())
            {
                FieldDesc fieldToAdd = field;
                TypeDesc canonOwningType = field.OwningType.ConvertToCanonForm(CanonicalFormKind.Specific);
                if (canonOwningType.IsCanonicalSubtype(CanonicalFormKind.Any))
                {
                    FieldDesc canonField = _typeSystemContext.GetFieldForInstantiatedType(field.GetTypicalFieldDefinition(), (InstantiatedType)canonOwningType);

                    // If we already added a canonically equivalent field, skip this one.
                    if (!canonicalFields.Add(canonField))
                        continue;

                    fieldToAdd = canonField;
                }

                Field record = transformed.GetTransformedFieldDefinition(fieldToAdd.GetTypicalFieldDefinition());
                if (record != null)
                    fieldMappings.Add(new MetadataMapping<FieldDesc>(fieldToAdd, writer.GetRecordHandle(record)));
            }

            // Generate stack trace metadata mapping
            foreach (var stackTraceRecord in stackTraceRecords)
            {
                if (stackTraceRecord.OwningType != null)
                {
                    StackTraceMapping mapping = new StackTraceMapping(
                        stackTraceRecord.Method,
                        writer.GetRecordHandle(stackTraceRecord.OwningType),
                        writer.GetRecordHandle(stackTraceRecord.MethodSignature),
                        writer.GetRecordHandle(stackTraceRecord.MethodName),
                        stackTraceRecord.MethodInstantiationArgumentCollection != null ? writer.GetRecordHandle(stackTraceRecord.MethodInstantiationArgumentCollection) : 0,
                        stackTraceRecord.IsHidden);
                    stackTraceMapping.Add(mapping);
                }
                else
                {
                    Debug.Assert(stackTraceRecord.IsHidden);
                    stackTraceMapping.Add(new StackTraceMapping(stackTraceRecord.Method, 0, 0, 0, 0, stackTraceRecord.IsHidden));
                }
            }
        }

        protected StackTraceRecordData CreateStackTraceRecord(Metadata.MetadataTransform transform, MethodDesc method, bool isHidden)
        {
            // In the metadata, we only represent the generic definition
            MethodDesc methodToGenerateMetadataFor = method.GetTypicalMethodDefinition();

            ConstantStringValue name = (ConstantStringValue)methodToGenerateMetadataFor.Name;
            MetadataRecord signature = transform.HandleMethodSignature(methodToGenerateMetadataFor.Signature);
            MetadataRecord owningType = transform.HandleType(methodToGenerateMetadataFor.OwningType);

            // If we're generating record for a method on a generic type, the owning type
            // should appear as if instantiated over its formals
            TypeDesc owningTypeToGenerateMetadataFor = methodToGenerateMetadataFor.OwningType;
            if (owningTypeToGenerateMetadataFor.HasInstantiation
                && owningType is TypeReference)
            {
                List<MetadataRecord> genericArgs = new List<MetadataRecord>();
                foreach (Internal.TypeSystem.Ecma.EcmaGenericParameter genericParam in owningTypeToGenerateMetadataFor.Instantiation)
                {
                    genericArgs.Add(new TypeReference
                    {
                        TypeName = (ConstantStringValue)genericParam.Name,
                    });
                }

                owningType = new TypeSpecification
                {
                    Signature = new TypeInstantiationSignature
                    {
                        GenericType = owningType,
                        GenericTypeArguments = genericArgs,
                    }
                };
            }

            // Generate metadata for the method instantiation arguments
            ConstantStringArray methodInst;
            if (methodToGenerateMetadataFor.HasInstantiation)
            {
                methodInst = new ConstantStringArray();
                foreach (Internal.TypeSystem.Ecma.EcmaGenericParameter typeArgument in methodToGenerateMetadataFor.Instantiation)
                {
                    methodInst.Value.Add((ConstantStringValue)typeArgument.Name);
                }
            }
            else
            {
                methodInst = null;
            }

            return new StackTraceRecordData(method, owningType, signature, name, methodInst, isHidden);
        }

        /// <summary>
        /// Returns a set of modules that will get some metadata emitted into the output module
        /// </summary>
        public abstract IEnumerable<ModuleDesc> GetCompilationModulesWithMetadata();

        public byte[] GetMetadataBlob(NodeFactory factory)
        {
            EnsureMetadataGenerated(factory);
            return _metadataBlob;
        }

        public IEnumerable<MetadataMapping<MetadataType>> GetTypeDefinitionMapping(NodeFactory factory)
        {
            EnsureMetadataGenerated(factory);
            return _typeMappings;
        }

        public IEnumerable<MetadataMapping<MethodDesc>> GetMethodMapping(NodeFactory factory)
        {
            EnsureMetadataGenerated(factory);
            return _methodMappings;
        }

        public IEnumerable<MetadataMapping<FieldDesc>> GetFieldMapping(NodeFactory factory)
        {
            EnsureMetadataGenerated(factory);
            return _fieldMappings;
        }

        public IEnumerable<StackTraceMapping> GetStackTraceMapping(NodeFactory factory)
        {
            EnsureMetadataGenerated(factory);
            return _stackTraceMappings;
        }

        internal IEnumerable<InterfaceDispatchCellNode> GetInterfaceDispatchCells()
        {
            return _interfaceDispatchCells;
        }

        internal IEnumerable<NonGCStaticsNode> GetCctorContextMapping()
        {
            return _cctorContextsGenerated;
        }

        internal IEnumerable<MetadataType> GetTypesWithStaticBases()
        {
            var allTypes = new SortedSet<MetadataType>(CompilerComparer.Instance);
            allTypes.UnionWith(_typesWithNonGCStaticsGenerated);
            allTypes.UnionWith(_typesWithGCStaticsGenerated);
            allTypes.UnionWith(_typesWithThreadStaticsGenerated);
            return allTypes;
        }

        internal bool HasNonGcStaticBase(MetadataType type) => _typesWithNonGCStaticsGenerated.Contains(type);
        internal bool HasGcStaticBase(MetadataType type) => _typesWithGCStaticsGenerated.Contains(type);
        internal bool HasThreadStaticBase(MetadataType type) => _typesWithThreadStaticsGenerated.Contains(type);
        internal bool HasConstructedEEType(TypeDesc type) => _typesWithConstructedEETypesGenerated.Contains(type);

        internal IEnumerable<TypeGVMEntriesNode> GetTypeGVMEntries()
        {
            return _typeGVMEntries;
        }

        internal IReadOnlyCollection<GenericDictionaryNode> GetCompiledGenericDictionaries()
        {
            return _genericDictionariesGenerated;
        }

        internal IEnumerable<DefType> GetTypesWithStructMarshalling()
        {
            return _typesWithStructMarshalling;
        }

        internal IEnumerable<DefType> GetTypesWithDelegateMarshalling()
        {
            return _typesWithDelegateMarshalling;
        }

        public IEnumerable<MethodDesc> GetCompiledMethods()
        {
            return _methodsGenerated;
        }

        public IEnumerable<MethodDesc> GetReflectableMethods()
        {
            return _reflectableMethods;
        }

        public IEnumerable<TypeDesc> GetTypeTemplates()
        {
            return _typeTemplates;
        }

        public IEnumerable<FrozenObjectNode> GetFrozenObjects()
        {
            return _frozenObjects;
        }

        public IEnumerable<MetadataType> GetTypesWithGenericStaticBaseInfos()
        {
            return _typesWithGenericStaticBaseInfo;
        }

        public IEnumerable<MethodDesc> GetGenericMethodHashtableEntries()
        {
            return _genericMethodHashtableEntries;
        }

        internal IEnumerable<IMethodBodyNode> GetCompiledMethodBodies()
        {
            return _methodBodiesGenerated;
        }

        internal bool TypeGeneratesEEType(TypeDesc type)
        {
            return _typesWithEETypesGenerated.Contains(type);
        }

        internal IEnumerable<TypeDesc> GetTypesWithEETypes()
        {
            return _typesWithEETypesGenerated;
        }

        internal IEnumerable<TypeDesc> GetTypesWithConstructedEETypes()
        {
            return _typesWithConstructedEETypesGenerated;
        }

        internal IEnumerable<NativeLayoutTemplateMethodSignatureVertexNode> GetTemplateMethodEntries()
        {
            return _templateMethodEntries;
        }

        public bool IsReflectionBlocked(TypeDesc type)
        {
            switch (type.Category)
            {
                case TypeFlags.SzArray:
                case TypeFlags.Array:
                case TypeFlags.Pointer:
                case TypeFlags.ByRef:
                    return IsReflectionBlocked(((ParameterizedType)type).ParameterType);

                case TypeFlags.FunctionPointer:
                    MethodSignature pointerSignature = ((FunctionPointerType)type).Signature;

                    for (int i = 0; i < pointerSignature.Length; i++)
                        if (IsReflectionBlocked(pointerSignature[i]))
                            return true;

                    return IsReflectionBlocked(pointerSignature.ReturnType);

                default:
                    Debug.Assert(type.IsDefType);

                    TypeDesc typeDefinition = type.GetTypeDefinition();
                    if (type != typeDefinition)
                    {
                        if (_blockingPolicy.IsBlocked((MetadataType)typeDefinition))
                            return true;

                        if (IsReflectionBlocked(type.Instantiation))
                            return true;

                        return false;
                    }

                    return _blockingPolicy.IsBlocked((MetadataType)type);
            }
        }

        protected bool IsReflectionBlocked(Instantiation instantiation)
        {
            foreach (TypeDesc type in instantiation)
            {
                if (IsReflectionBlocked(type) && !type.IsCanonicalDefinitionType(CanonicalFormKind.Any))
                    return true;
            }
            return false;
        }

        public bool IsReflectionBlocked(FieldDesc field)
        {
            FieldDesc typicalFieldDefinition = field.GetTypicalFieldDefinition();
            if (typicalFieldDefinition != field && IsReflectionBlocked(field.OwningType.Instantiation))
            {
                return true;
            }

            return _blockingPolicy.IsBlocked(typicalFieldDefinition);
        }

        public bool IsReflectionBlocked(MethodDesc method)
        {
            MethodDesc methodDefinition = method.GetMethodDefinition();
            if (method != methodDefinition && IsReflectionBlocked(method.Instantiation))
            {
                return true;
            }

            MethodDesc typicalMethodDefinition = methodDefinition.GetTypicalMethodDefinition();
            if (typicalMethodDefinition != methodDefinition && IsReflectionBlocked(method.OwningType.Instantiation))
            {
                return true;
            }

            return _blockingPolicy.IsBlocked(typicalMethodDefinition);
        }

        public bool IsManifestResourceBlocked(NodeFactory factory, Internal.TypeSystem.Ecma.EcmaModule module, string resourceName)
        {
            if (_resourceBlockingPolicy.IsManifestResourceBlocked(module, resourceName))
                return true;

            // If this is a resource strings resource but we don't actually need it, block it.
            if (InlineableStringsResourceNode.IsInlineableStringsResource(module, resourceName)
                && !factory.InlineableStringResource(module).Marked)
                return true;

            return false;
        }

        public bool CanGenerateMetadata(MetadataType type)
        {
            return (GetMetadataCategory(type) & MetadataCategory.Description) != 0;
        }

        public bool CanGenerateMetadata(MethodDesc method)
        {
            Debug.Assert(method.IsTypicalMethodDefinition);
            return (GetMetadataCategory(method) & MetadataCategory.Description) != 0;
        }

        public bool CanGenerateMetadata(FieldDesc field)
        {
            Debug.Assert(field.IsTypicalFieldDefinition);
            return (GetMetadataCategory(field) & MetadataCategory.Description) != 0;
        }

        /// <summary>
        /// Gets the metadata category for a compiled method body in the current compilation.
        /// The method will only get called with '<paramref name="method"/>' that has a compiled method body
        /// in this compilation.
        /// Note that if this method doesn't return <see cref="MetadataCategory.Description"/>, it doesn't mean
        /// that the method never has metadata. The metadata might just be generated in a different compilation.
        /// </summary>
        protected abstract MetadataCategory GetMetadataCategory(MethodDesc method);

        /// <summary>
        /// Gets the metadata category for a generated type in the current compilation.
        /// The method can assume it will only get called with '<paramref name="type"/>' that has an MethodTable generated
        /// in the current compilation.
        /// Note that if this method doesn't return <see cref="MetadataCategory.Description"/>, it doesn't mean
        /// that the method never has metadata. The metadata might just be generated in a different compilation.
        /// </summary>
        protected abstract MetadataCategory GetMetadataCategory(TypeDesc type);
        protected abstract MetadataCategory GetMetadataCategory(FieldDesc field);

        public virtual void GetDependenciesDueToAccess(ref DependencyList dependencies, NodeFactory factory, MethodIL methodIL, MethodDesc calledMethod)
        {
        }

        public virtual void GetDependenciesDueToAccess(ref DependencyList dependencies, NodeFactory factory, MethodIL methodIL, FieldDesc writtenField)
        {
        }

        public virtual DependencyList GetDependenciesForCustomAttribute(NodeFactory factory, MethodDesc attributeCtor, CustomAttributeValue decodedValue, TypeSystemEntity parent)
        {
            return null;
        }

        public virtual void NoteOverridingMethod(MethodDesc baseMethod, MethodDesc overridingMethod)
        {
        }
    }

    public readonly struct MetadataMapping<TEntity>
    {
        public readonly TEntity Entity;
        public readonly int MetadataHandle;

        public MetadataMapping(TEntity entity, int metadataHandle)
            => (Entity, MetadataHandle) = (entity, metadataHandle);
    }

    public readonly struct StackTraceMapping
    {
        public readonly MethodDesc Method;
        public readonly int OwningTypeHandle;
        public readonly int MethodSignatureHandle;
        public readonly int MethodNameHandle;
        public readonly int MethodInstantiationArgumentCollectionHandle;
        public readonly bool IsHidden;

        public StackTraceMapping(MethodDesc method, int owningTypeHandle, int methodSignatureHandle, int methodNameHandle, int methodInstantiationArgumentCollectionHandle, bool isHidden)
            => (Method, OwningTypeHandle, MethodSignatureHandle, MethodNameHandle, MethodInstantiationArgumentCollectionHandle, IsHidden)
            = (method, owningTypeHandle, methodSignatureHandle, methodNameHandle, methodInstantiationArgumentCollectionHandle, isHidden);
    }

    public readonly struct StackTraceRecordData
    {
        public readonly MethodDesc Method;
        public readonly MetadataRecord OwningType;
        public readonly MetadataRecord MethodSignature;
        public readonly MetadataRecord MethodName;
        public readonly MetadataRecord MethodInstantiationArgumentCollection;
        public readonly bool IsHidden;

        public StackTraceRecordData(MethodDesc method, MetadataRecord owningType, MetadataRecord methodSignature, MetadataRecord methodName, MetadataRecord methodInstantiationArgumentCollection, bool isHidden)
            => (Method, OwningType, MethodSignature, MethodName, MethodInstantiationArgumentCollection, IsHidden)
            = (method, owningType, methodSignature, methodName, methodInstantiationArgumentCollection, isHidden);
    }

    [Flags]
    public enum MetadataCategory
    {
        None = 0x00,
        Description = 0x01,
        RuntimeMapping = 0x02,
    }

    [Flags]
    public enum MetadataManagerOptions
    {
        DehydrateData = 0x01,
    }
}

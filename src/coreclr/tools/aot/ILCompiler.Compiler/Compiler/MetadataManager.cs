// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using Debug = System.Diagnostics.Debug;
using ReadyToRunSectionType = Internal.Runtime.ReadyToRunSectionType;
using ReflectionMapBlob = Internal.Runtime.ReflectionMapBlob;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using CombinedDependencyList = System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;
using MethodIL = Internal.IL.MethodIL;
using CustomAttributeValue = System.Reflection.Metadata.CustomAttributeValue<Internal.TypeSystem.TypeDesc>;

using MetadataRecord = Internal.Metadata.NativeFormat.Writer.MetadataRecord;
using MemberReference = Internal.Metadata.NativeFormat.Writer.MemberReference;
using TypeReference = Internal.Metadata.NativeFormat.Writer.TypeReference;
using TypeSpecification = Internal.Metadata.NativeFormat.Writer.TypeSpecification;
using ConstantStringValue = Internal.Metadata.NativeFormat.Writer.ConstantStringValue;
using TypeInstantiationSignature = Internal.Metadata.NativeFormat.Writer.TypeInstantiationSignature;
using MethodInstantiation = Internal.Metadata.NativeFormat.Writer.MethodInstantiation;

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

        private byte[] _metadataBlob;
        private List<MetadataMapping<MetadataType>> _typeMappings;
        private List<MetadataMapping<FieldDesc>> _fieldMappings;
        private List<MetadataMapping<MethodDesc>> _methodMappings;
        private List<MetadataMapping<MethodDesc>> _stackTraceMappings;

        protected readonly CompilerTypeSystemContext _typeSystemContext;
        protected readonly MetadataBlockingPolicy _blockingPolicy;
        protected readonly ManifestResourceBlockingPolicy _resourceBlockingPolicy;
        protected readonly DynamicInvokeThunkGenerationPolicy _dynamicInvokeThunkGenerationPolicy;

        private readonly SortedSet<NonGCStaticsNode> _cctorContextsGenerated = new SortedSet<NonGCStaticsNode>(CompilerComparer.Instance);
        private readonly SortedSet<TypeDesc> _typesWithEETypesGenerated = new SortedSet<TypeDesc>(TypeSystemComparer.Instance);
        private readonly SortedSet<TypeDesc> _typesWithConstructedEETypesGenerated = new SortedSet<TypeDesc>(TypeSystemComparer.Instance);
        private readonly SortedSet<MethodDesc> _methodsGenerated = new SortedSet<MethodDesc>(TypeSystemComparer.Instance);
        private readonly SortedSet<MethodDesc> _reflectableMethods = new SortedSet<MethodDesc>(TypeSystemComparer.Instance);
        private readonly SortedSet<GenericDictionaryNode> _genericDictionariesGenerated = new SortedSet<GenericDictionaryNode>(CompilerComparer.Instance);
        private readonly SortedSet<IMethodBodyNode> _methodBodiesGenerated = new SortedSet<IMethodBodyNode>(CompilerComparer.Instance);
        private readonly SortedSet<TypeGVMEntriesNode> _typeGVMEntries
            = new SortedSet<TypeGVMEntriesNode>(Comparer<TypeGVMEntriesNode>.Create((a, b) => TypeSystemComparer.Instance.Compare(a.AssociatedType, b.AssociatedType)));
        private readonly SortedSet<DefType> _typesWithDelegateMarshalling = new SortedSet<DefType>(TypeSystemComparer.Instance);
        private readonly SortedSet<DefType> _typesWithStructMarshalling = new SortedSet<DefType>(TypeSystemComparer.Instance);
        private readonly SortedSet<MethodDesc> _dynamicInvokeTemplates = new SortedSet<MethodDesc>(TypeSystemComparer.Instance);
        private HashSet<NativeLayoutTemplateMethodSignatureVertexNode> _templateMethodEntries = new HashSet<NativeLayoutTemplateMethodSignatureVertexNode>();

        internal NativeLayoutInfoNode NativeLayoutInfo { get; private set; }
        internal DynamicInvokeTemplateDataNode DynamicInvokeTemplateData { get; private set; }

        public MetadataManager(CompilerTypeSystemContext typeSystemContext, MetadataBlockingPolicy blockingPolicy,
            ManifestResourceBlockingPolicy resourceBlockingPolicy, DynamicInvokeThunkGenerationPolicy dynamicInvokeThunkGenerationPolicy)
        {
            _typeSystemContext = typeSystemContext;
            _blockingPolicy = blockingPolicy;
            _resourceBlockingPolicy = resourceBlockingPolicy;
            _dynamicInvokeThunkGenerationPolicy = dynamicInvokeThunkGenerationPolicy;
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
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.EmbeddedMetadata), metadataNode, metadataNode, metadataNode.EndSymbol);

            var nativeReferencesTableNode = new ExternalReferencesTableNode("NativeReferences", nodeFactory);
            var nativeStaticsTableNode = new ExternalReferencesTableNode("NativeStatics", nodeFactory);

            var resourceDataNode = new ResourceDataNode();
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.BlobIdResourceData), resourceDataNode, resourceDataNode, resourceDataNode.EndSymbol);

            var resourceIndexNode = new ResourceIndexNode(resourceDataNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.BlobIdResourceIndex), resourceIndexNode, resourceIndexNode, resourceIndexNode.EndSymbol);

            var typeMapNode = new TypeMetadataMapNode(commonFixupsTableNode);

            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.TypeMap), typeMapNode, typeMapNode, typeMapNode.EndSymbol);

            var cctorContextMapNode = new ClassConstructorContextMap(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.CCtorContextMap), cctorContextMapNode, cctorContextMapNode, cctorContextMapNode.EndSymbol);

            DynamicInvokeTemplateData = new DynamicInvokeTemplateDataNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.DynamicInvokeTemplateData), DynamicInvokeTemplateData, DynamicInvokeTemplateData, DynamicInvokeTemplateData.EndSymbol);

            var invokeMapNode = new ReflectionInvokeMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.InvokeMap), invokeMapNode, invokeMapNode, invokeMapNode.EndSymbol);

            var arrayMapNode = new ArrayMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.ArrayMap), arrayMapNode, arrayMapNode, arrayMapNode.EndSymbol);

            var fieldMapNode = new ReflectionFieldMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.FieldAccessMap), fieldMapNode, fieldMapNode, fieldMapNode.EndSymbol);

            NativeLayoutInfo = new NativeLayoutInfoNode(nativeReferencesTableNode, nativeStaticsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.NativeLayoutInfo), NativeLayoutInfo, NativeLayoutInfo, NativeLayoutInfo.EndSymbol);

            var exactMethodInstantiations = new ExactMethodInstantiationsNode(nativeReferencesTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.ExactMethodInstantiationsHashtable), exactMethodInstantiations, exactMethodInstantiations, exactMethodInstantiations.EndSymbol);

            var genericsTypesHashtableNode = new GenericTypesHashtableNode(nativeReferencesTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.GenericsHashtable), genericsTypesHashtableNode, genericsTypesHashtableNode, genericsTypesHashtableNode.EndSymbol);

            var genericMethodsHashtableNode = new GenericMethodsHashtableNode(nativeReferencesTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.GenericMethodsHashtable), genericMethodsHashtableNode, genericMethodsHashtableNode, genericMethodsHashtableNode.EndSymbol);

            var genericVirtualMethodTableNode = new GenericVirtualMethodTableNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.GenericVirtualMethodTable), genericVirtualMethodTableNode, genericVirtualMethodTableNode, genericVirtualMethodTableNode.EndSymbol);

            var interfaceGenericVirtualMethodTableNode = new InterfaceGenericVirtualMethodTableNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.InterfaceGenericVirtualMethodTable), interfaceGenericVirtualMethodTableNode, interfaceGenericVirtualMethodTableNode, interfaceGenericVirtualMethodTableNode.EndSymbol);

            var genericMethodsTemplatesMapNode = new GenericMethodsTemplateMap(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.GenericMethodsTemplateMap), genericMethodsTemplatesMapNode, genericMethodsTemplatesMapNode, genericMethodsTemplatesMapNode.EndSymbol);

            var genericTypesTemplatesMapNode = new GenericTypesTemplateMap(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.TypeTemplateMap), genericTypesTemplatesMapNode, genericTypesTemplatesMapNode, genericTypesTemplatesMapNode.EndSymbol);

            var blockReflectionTypeMapNode = new BlockReflectionTypeMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.BlockReflectionTypeMap), blockReflectionTypeMapNode, blockReflectionTypeMapNode, blockReflectionTypeMapNode.EndSymbol);

            var staticsInfoHashtableNode = new StaticsInfoHashtableNode(nativeReferencesTableNode, nativeStaticsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.StaticsInfoHashtable), staticsInfoHashtableNode, staticsInfoHashtableNode, staticsInfoHashtableNode.EndSymbol);

            var virtualInvokeMapNode = new ReflectionVirtualInvokeMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.VirtualInvokeMap), virtualInvokeMapNode, virtualInvokeMapNode, virtualInvokeMapNode.EndSymbol);

            var defaultConstructorMapNode = new DefaultConstructorMapNode(commonFixupsTableNode);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.DefaultConstructorMap), defaultConstructorMapNode, defaultConstructorMapNode, defaultConstructorMapNode.EndSymbol);

            var stackTraceMethodMappingNode = new StackTraceMethodMappingNode();
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.BlobIdStackTraceMethodRvaToTokenMapping), stackTraceMethodMappingNode, stackTraceMethodMappingNode, stackTraceMethodMappingNode.EndSymbol);

            // The external references tables should go last
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.NativeReferences), nativeReferencesTableNode, nativeReferencesTableNode, nativeReferencesTableNode.EndSymbol);
            header.Add(BlobIdToReadyToRunSection(ReflectionMapBlob.NativeStatics), nativeStaticsTableNode, nativeStaticsTableNode, nativeStaticsTableNode.EndSymbol);
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
            if (methodNode == null)
                methodNode = obj as ShadowConcreteMethodNode;

            if (methodNode != null)
            {
                _methodsGenerated.Add(methodNode.Method);

                if (AllMethodsCanBeReflectable)
                    _reflectableMethods.Add(methodNode.Method);

                return;
            }

            var reflectableMethodNode = obj as ReflectableMethodNode;
            if (reflectableMethodNode != null)
            {
                _reflectableMethods.Add(reflectableMethodNode.Method);
            }

            var nonGcStaticSectionNode = obj as NonGCStaticsNode;
            if (nonGcStaticSectionNode != null && nonGcStaticSectionNode.HasCCtorContext)
            {
                _cctorContextsGenerated.Add(nonGcStaticSectionNode);
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

            if (obj is StructMarshallingDataNode structMarshallingDataNode)
            {
                _typesWithStructMarshalling.Add(structMarshallingDataNode.Type);
            }

            if (obj is DelegateMarshallingDataNode delegateMarshallingDataNode)
            {
                _typesWithDelegateMarshalling.Add(delegateMarshallingDataNode.Type);
            }

            if (obj is DynamicInvokeTemplateNode dynamicInvokeTemplate)
            {
                _dynamicInvokeTemplates.Add(dynamicInvokeTemplate.Method);
            }

            if (obj is NativeLayoutTemplateMethodSignatureVertexNode templateMethodEntry)
            {
                _templateMethodEntries.Add(templateMethodEntry);
            }
        }

        protected virtual bool AllMethodsCanBeReflectable => false;

        /// <summary>
        /// Is a method that is reflectable a method which should be placed into the invoke map as invokable?
        /// </summary>
        public virtual bool IsReflectionInvokable(MethodDesc method)
        {
            return Internal.IL.Stubs.DynamicInvokeMethodThunk.SupportsSignature(method.Signature)
                && IsMethodSupportedInReflectionInvoke(method);
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

        /// <summary>
        /// This method is an extension point that can provide additional metadata-based dependencies to generated EETypes.
        /// </summary>
        public void GetDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            MetadataCategory category = GetMetadataCategory(type);

            if ((category & MetadataCategory.Description) != 0)
            {
                GetMetadataDependenciesDueToReflectability(ref dependencies, factory, type);
            }

            if ((category & MetadataCategory.RuntimeMapping) != 0)
            {
                // We're going to generate a mapping table entry for this. Collect dependencies.

                // Nothing special is needed for the mapping table (we only emit the MethodTable and we already
                // have one, since we got this callback). But check if a child wants to do something extra.
                GetRuntimeMappingDependenciesDueToReflectability(ref dependencies, factory, type);
            }

            GetDependenciesDueToEETypePresence(ref dependencies, factory, type);
        }

        protected virtual void GetMetadataDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the emission of metadata
            // (E.g. dependencies caused by the type having custom attributes applied to it: making sure we compile the attribute constructor
            // and property setters)
        }

        protected virtual void GetRuntimeMappingDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the emission of a runtime
            // mapping for a type.
        }

        protected virtual void GetDependenciesDueToEETypePresence(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the emission of an MethodTable.
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
        /// This method is an extension point that can provide additional metadata-based dependencies to generated method bodies.
        /// </summary>
        public void GetDependenciesDueToMethodCodePresence(ref DependencyList dependencies, NodeFactory factory, MethodDesc method, MethodIL methodIL)
        {
            if (method.HasInstantiation)
            {
                ExactMethodInstantiationsNode.GetExactMethodInstantiationDependenciesForMethod(ref dependencies, factory, method);
            }

            GetDependenciesDueToTemplateTypeLoader(ref dependencies, factory, method);
            GetDependenciesDueToMethodCodePresenceInternal(ref dependencies, factory, method, methodIL);
        }

        public virtual void GetConditionalDependenciesDueToMethodCodePresence(ref CombinedDependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            // MetadataManagers can override this to provide additional dependencies caused by the presence of
            // method code.
        }

        private void GetDependenciesDueToTemplateTypeLoader(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            // TODO-SIZE: this is overly generous in the templates we create
            if (_blockingPolicy is FullyBlockedMetadataBlockingPolicy)
                return;

            if (method.HasInstantiation)
            {
                GenericMethodsTemplateMap.GetTemplateMethodDependencies(ref dependencies, factory, method);
            }
            else
            {
                TypeDesc owningTemplateType = method.OwningType;

                // Unboxing and Instantiating stubs use a different type as their template
                if (factory.TypeSystemContext.IsSpecialUnboxingThunk(method))
                    owningTemplateType = factory.TypeSystemContext.GetTargetOfSpecialUnboxingThunk(method).OwningType;

                GenericTypesTemplateMap.GetTemplateTypeDependencies(ref dependencies, factory, owningTemplateType);
            }
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
        public abstract bool WillUseMetadataTokenToReferenceMethod(MethodDesc method);

        /// <summary>
        /// Given that a method is invokable, if it is inserted into the reflection invoke table
        /// will it use a field token to be referenced, or not?
        /// </summary>
        public abstract bool WillUseMetadataTokenToReferenceField(FieldDesc field);

        /// <summary>
        /// Gets a stub that can be used to reflection-invoke a method with a given signature.
        /// </summary>
        public abstract MethodDesc GetCanonicalReflectionInvokeStub(MethodDesc method);

        /// <summary>
        /// Compute the canonical instantiation of a dynamic invoke thunk needed to invoke a method
        /// This algorithm is shared with the runtime, so if a thunk requires generic instantiation
        /// to be used, it must match this algorithm, and cannot be different with different MetadataManagers
        /// NOTE: This function may return null in cases where an exact instantiation does not exist. (Universal Generics)
        /// </summary>
        protected MethodDesc InstantiateCanonicalDynamicInvokeMethodForMethod(MethodDesc thunk, MethodDesc method)
        {
            if (thunk.Instantiation.Length == 0)
            {
                // nothing to instantiate
                return thunk;
            }

            MethodSignature sig = method.Signature;
            TypeSystemContext context = method.Context;

            //
            // Instantiate the generic thunk over the parameters and the return type of the target method
            //

            TypeDesc[] instantiation = Internal.IL.Stubs.DynamicInvokeMethodThunk.GetThunkInstantiationForMethod(method);
            Debug.Assert(thunk.Instantiation.Length == instantiation.Length);

            // Check if at least one of the instantiation arguments is a universal canonical type, and if so, we 
            // won't create a dynamic invoker instantiation. The arguments will be interpreted at runtime by the
            // calling convention converter during the dynamic invocation
            foreach (TypeDesc type in instantiation)
            {
                if (type.IsCanonicalSubtype(CanonicalFormKind.Universal))
                    return null;
            }

            // If the thunk ends up being shared code, return the canonical method body.
            // The concrete dictionary for the thunk will be built at runtime and is not interesting for the compiler.
            Instantiation canonInstantiation = context.ConvertInstantiationToCanonForm(new Instantiation(instantiation), CanonicalFormKind.Specific);

            MethodDesc instantiatedDynamicInvokeMethod = thunk.Context.GetInstantiatedMethod(thunk, canonInstantiation);
            return instantiatedDynamicInvokeMethod;
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
                                                out List<MetadataMapping<MethodDesc>> stackTraceMapping);

        protected MetadataRecord CreateStackTraceRecord(Metadata.MetadataTransform transform, MethodDesc method)
        {
            // In the metadata, we only represent the generic definition
            MethodDesc methodToGenerateMetadataFor = method.GetTypicalMethodDefinition();
            MetadataRecord record = transform.HandleQualifiedMethod(methodToGenerateMetadataFor);

            // If we're generating a MemberReference to a method on a generic type, the owning type
            // should appear as if instantiated over its formals
            TypeDesc owningTypeToGenerateMetadataFor = methodToGenerateMetadataFor.OwningType;
            if (owningTypeToGenerateMetadataFor.HasInstantiation
                && record is MemberReference memberRefRecord
                && memberRefRecord.Parent is TypeReference)
            {
                List<MetadataRecord> genericArgs = new List<MetadataRecord>();
                foreach (Internal.TypeSystem.Ecma.EcmaGenericParameter genericParam in owningTypeToGenerateMetadataFor.Instantiation)
                {
                    genericArgs.Add(new TypeReference
                    {
                        TypeName = (ConstantStringValue)genericParam.Name,
                    });
                }

                memberRefRecord.Parent = new TypeSpecification
                {
                    Signature = new TypeInstantiationSignature
                    {
                        GenericType = memberRefRecord.Parent,
                        GenericTypeArguments = genericArgs,
                    }
                };
            }

            // As a twist, instantiated generic methods appear as if instantiated over their formals.
            if (methodToGenerateMetadataFor.HasInstantiation)
            {
                var methodInst = new MethodInstantiation
                {
                    Method = record,
                };
                methodInst.GenericTypeArguments.Capacity = methodToGenerateMetadataFor.Instantiation.Length;
                foreach (Internal.TypeSystem.Ecma.EcmaGenericParameter typeArgument in methodToGenerateMetadataFor.Instantiation)
                {
                    var genericParam = new TypeReference
                    {
                        TypeName = (ConstantStringValue)typeArgument.Name,
                    };
                    methodInst.GenericTypeArguments.Add(genericParam);
                }
                record = methodInst;
            }

            return record;
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

        public IEnumerable<MetadataMapping<MethodDesc>> GetStackTraceMapping(NodeFactory factory)
        {
            EnsureMetadataGenerated(factory);
            return _stackTraceMappings;
        }

        internal IEnumerable<NonGCStaticsNode> GetCctorContextMapping()
        {
            return _cctorContextsGenerated;
        }

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

        internal IEnumerable<MethodDesc> GetDynamicInvokeTemplateMethods()
        {
            return _dynamicInvokeTemplates;
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

        public bool IsManifestResourceBlocked(ModuleDesc module, string resourceName)
        {
            return _resourceBlockingPolicy.IsManifestResourceBlocked(module, resourceName);
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

        public virtual DependencyList GetDependenciesForCustomAttribute(NodeFactory factory, MethodDesc attributeCtor, CustomAttributeValue decodedValue)
        {
            return null;
        }

        public virtual void GetDependenciesForGenericDictionary(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
        }

        public virtual void GetDependenciesForGenericDictionary(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
        }

        public virtual void NoteOverridingMethod(MethodDesc baseMethod, MethodDesc overridingMethod)
        {
        }
    }

    public struct MetadataMapping<TEntity>
    {
        public readonly TEntity Entity;
        public readonly int MetadataHandle;

        public MetadataMapping(TEntity entity, int metadataHandle)
        {
            Entity = entity;
            MetadataHandle = metadataHandle;
        }
    }

    [Flags]
    public enum MetadataCategory
    {
        None = 0x00,
        Description = 0x01,
        RuntimeMapping = 0x02,
    }
}

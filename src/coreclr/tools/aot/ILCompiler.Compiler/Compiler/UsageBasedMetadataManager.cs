// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using System.Reflection.Metadata;
using System.IO;
using System.Xml;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using ILCompiler.Metadata;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;

using FlowAnnotations = ILCompiler.Dataflow.FlowAnnotations;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using CombinedDependencyList = System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;
using Debug = System.Diagnostics.Debug;
using CustomAttributeValue = System.Reflection.Metadata.CustomAttributeValue<Internal.TypeSystem.TypeDesc>;
using EcmaModule = Internal.TypeSystem.Ecma.EcmaModule;
using EcmaType = Internal.TypeSystem.Ecma.EcmaType;
using CustomAttributeHandle = System.Reflection.Metadata.CustomAttributeHandle;
using CustomAttributeTypeProvider = Internal.TypeSystem.Ecma.CustomAttributeTypeProvider;
using MetadataExtensions = Internal.TypeSystem.Ecma.MetadataExtensions;

namespace ILCompiler
{
    /// <summary>
    /// This class is responsible for managing native metadata to be emitted into the compiled
    /// module. It applies a policy that every type/method that is statically used shall be reflectable.
    /// </summary>
    public sealed class UsageBasedMetadataManager : GeneratingMetadataManager
    {
        private readonly CompilationModuleGroup _compilationModuleGroup;

        internal readonly UsageBasedMetadataGenerationOptions _generationOptions;
        private readonly bool _hasPreciseFieldUsageInformation;

        private readonly FeatureSwitchHashtable _featureSwitchHashtable;

        private readonly List<ModuleDesc> _modulesWithMetadata = new List<ModuleDesc>();
        private readonly List<FieldDesc> _fieldsWithMetadata = new List<FieldDesc>();
        private readonly List<MethodDesc> _methodsWithMetadata = new List<MethodDesc>();
        private readonly List<MetadataType> _typesWithMetadata = new List<MetadataType>();
        private readonly List<ReflectableCustomAttribute> _customAttributesWithMetadata = new List<ReflectableCustomAttribute>();

        private readonly HashSet<ModuleDesc> _rootEntireAssembliesExaminedModules = new HashSet<ModuleDesc>();

        private readonly HashSet<string> _rootEntireAssembliesModules;
        private readonly HashSet<string> _trimmedAssemblies;

        internal FlowAnnotations FlowAnnotations { get; }

        internal Logger Logger { get; }

        public UsageBasedMetadataManager(
            CompilationModuleGroup group,
            CompilerTypeSystemContext typeSystemContext,
            MetadataBlockingPolicy blockingPolicy,
            ManifestResourceBlockingPolicy resourceBlockingPolicy,
            string logFile,
            StackTraceEmissionPolicy stackTracePolicy,
            DynamicInvokeThunkGenerationPolicy invokeThunkGenerationPolicy,
            FlowAnnotations flowAnnotations,
            UsageBasedMetadataGenerationOptions generationOptions,
            Logger logger,
            IEnumerable<KeyValuePair<string, bool>> featureSwitchValues,
            IEnumerable<string> rootEntireAssembliesModules,
            IEnumerable<string> trimmedAssemblies)
            : base(typeSystemContext, blockingPolicy, resourceBlockingPolicy, logFile, stackTracePolicy, invokeThunkGenerationPolicy)
        {
            // We use this to mark places that would behave differently if we tracked exact fields used. 
            _hasPreciseFieldUsageInformation = false;
            _compilationModuleGroup = group;
            _generationOptions = generationOptions;

            FlowAnnotations = flowAnnotations;
            Logger = logger;

            _featureSwitchHashtable = new FeatureSwitchHashtable(new Dictionary<string, bool>(featureSwitchValues));

            _rootEntireAssembliesModules = new HashSet<string>(rootEntireAssembliesModules);
            _trimmedAssemblies = new HashSet<string>(trimmedAssemblies);
        }

        protected override void Graph_NewMarkedNode(DependencyNodeCore<NodeFactory> obj)
        {
            base.Graph_NewMarkedNode(obj);

            var moduleMetadataNode = obj as ModuleMetadataNode;
            if (moduleMetadataNode != null)
            {
                _modulesWithMetadata.Add(moduleMetadataNode.Module);
            }

            var fieldMetadataNode = obj as FieldMetadataNode;
            if (fieldMetadataNode != null)
            {
                _fieldsWithMetadata.Add(fieldMetadataNode.Field);
            }

            var methodMetadataNode = obj as MethodMetadataNode;
            if (methodMetadataNode != null)
            {
                _methodsWithMetadata.Add(methodMetadataNode.Method);
            }

            var typeMetadataNode = obj as TypeMetadataNode;
            if (typeMetadataNode != null)
            {
                _typesWithMetadata.Add(typeMetadataNode.Type);
            }

            var customAttributeMetadataNode = obj as CustomAttributeMetadataNode;
            if (customAttributeMetadataNode != null)
            {
                _customAttributesWithMetadata.Add(customAttributeMetadataNode.CustomAttribute);
            }
        }

        protected override MetadataCategory GetMetadataCategory(FieldDesc field)
        {
            MetadataCategory category = 0;

            if (!IsReflectionBlocked(field))
            {
                // Can't do mapping for uninstantiated things
                if (!field.OwningType.IsGenericDefinition)
                    category = MetadataCategory.RuntimeMapping;

                if (_compilationModuleGroup.ContainsType(field.GetTypicalFieldDefinition().OwningType))
                    category |= MetadataCategory.Description;
            }

            return category;
        }

        protected override MetadataCategory GetMetadataCategory(MethodDesc method)
        {
            MetadataCategory category = 0;

            if (!IsReflectionBlocked(method))
            {
                // Can't do mapping for uninstantiated things
                if (!method.IsGenericMethodDefinition && !method.OwningType.IsGenericDefinition)
                    category = MetadataCategory.RuntimeMapping;

                if (_compilationModuleGroup.ContainsType(method.GetTypicalMethodDefinition().OwningType))
                    category |= MetadataCategory.Description;
            }

            return category;
        }

        protected override MetadataCategory GetMetadataCategory(TypeDesc type)
        {
            MetadataCategory category = 0;

            if (!IsReflectionBlocked(type))
            {
                category = MetadataCategory.RuntimeMapping;

                if (_compilationModuleGroup.ContainsType(type.GetTypeDefinition()))
                    category |= MetadataCategory.Description;
            }

            return category;
        }

        protected override bool AllMethodsCanBeReflectable => (_generationOptions & UsageBasedMetadataGenerationOptions.ReflectedMembersOnly) == 0;

        protected override void ComputeMetadata(NodeFactory factory,
            out byte[] metadataBlob,
            out List<MetadataMapping<MetadataType>> typeMappings,
            out List<MetadataMapping<MethodDesc>> methodMappings,
            out List<MetadataMapping<FieldDesc>> fieldMappings,
            out List<MetadataMapping<MethodDesc>> stackTraceMapping)
        {
            ComputeMetadata(new GeneratedTypesAndCodeMetadataPolicy(_blockingPolicy, factory),
                factory, out metadataBlob, out typeMappings, out methodMappings, out fieldMappings, out stackTraceMapping);
        }

        protected override void GetMetadataDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            dependencies = dependencies ?? new DependencyList();
            dependencies.Add(factory.MethodMetadata(method.GetTypicalMethodDefinition()), "Reflectable method");
        }

        protected override void GetMetadataDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            TypeMetadataNode.GetMetadataDependencies(ref dependencies, factory, type, "Reflectable type");

            // If we don't have precise field usage information, apply policy that all fields that
            // are eligible to have metadata get metadata.
            if (!_hasPreciseFieldUsageInformation)
            {
                TypeDesc typeDefinition = type.GetTypeDefinition();

                foreach (FieldDesc field in typeDefinition.GetFields())
                {
                    if ((GetMetadataCategory(field) & MetadataCategory.Description) != 0)
                    {
                        dependencies = dependencies ?? new DependencyList();
                        dependencies.Add(factory.FieldMetadata(field), "Field of a reflectable type");
                    }
                }
            }

            MetadataType mdType = type as MetadataType;

            // If anonymous type heuristic is turned on and this is an anonymous type, make sure we have
            // method bodies for all properties. It's common to have anonymous types used with reflection
            // and it's hard to specify them in RD.XML.
            if ((_generationOptions & UsageBasedMetadataGenerationOptions.AnonymousTypeHeuristic) != 0)
            {
                if (mdType != null &&
                    mdType.HasInstantiation &&
                    !mdType.IsGenericDefinition &&
                    mdType.HasCustomAttribute("System.Runtime.CompilerServices", "CompilerGeneratedAttribute") &&
                    mdType.Name.Contains("AnonymousType"))
                {
                    foreach (MethodDesc method in type.GetMethods())
                    {
                        if (!method.Signature.IsStatic && method.IsSpecialName)
                        {
                            dependencies = dependencies ?? new DependencyList();
                            dependencies.Add(factory.CanonicalEntrypoint(method), "Anonymous type accessor");
                        }
                    }
                }
            }

            ModuleDesc module = mdType?.Module;
            if (module != null && !_rootEntireAssembliesExaminedModules.Contains(module))
            {
                // If the owning assembly needs to be fully compiled, do that.
                _rootEntireAssembliesExaminedModules.Add(module);

                string assemblyName = module.Assembly.GetName().Name;

                bool fullyRoot;
                string reason;

                if (_rootEntireAssembliesModules.Contains(assemblyName))
                {
                    // If the assembly was specified as a root on the command line, root it
                    fullyRoot = true;
                    reason = "Rooted from command line";
                }
                else if (_trimmedAssemblies.Contains(assemblyName) || IsTrimmableAssembly(module))
                {
                    // If the assembly was specified as trimmed on the command line, do not root
                    // If the assembly is marked trimmable via an attribute, do not root
                    fullyRoot = false;
                    reason = null;
                }
                else
                {
                    // If rooting default assemblies was requested, root
                    fullyRoot = (_generationOptions & UsageBasedMetadataGenerationOptions.RootDefaultAssemblies) != 0;
                    reason = "Assemblies rooted from command line";
                }

                if (fullyRoot)
                {
                    dependencies = dependencies ?? new DependencyList();
                    var rootProvider = new RootingServiceProvider(factory, dependencies.Add);
                    foreach (TypeDesc t in mdType.Module.GetAllTypes())
                    {
                        RootingHelpers.TryRootType(rootProvider, t, reason);
                    }
                }
            }

            // Event sources need their special nested types
            if (mdType != null && mdType.HasCustomAttribute("System.Diagnostics.Tracing", "EventSourceAttribute"))
            {
                AddEventSourceSpecialTypeDependencies(ref dependencies, factory, mdType.GetNestedType("Keywords"));
                AddEventSourceSpecialTypeDependencies(ref dependencies, factory, mdType.GetNestedType("Tasks"));
                AddEventSourceSpecialTypeDependencies(ref dependencies, factory, mdType.GetNestedType("Opcodes"));

                void AddEventSourceSpecialTypeDependencies(ref DependencyList dependencies, NodeFactory factory, MetadataType type)
                {
                    if (type != null)
                    {
                        const string reason = "Event source";
                        dependencies = dependencies ?? new DependencyList();
                        dependencies.Add(factory.TypeMetadata(type), reason);
                        foreach (FieldDesc field in type.GetFields())
                        {
                            if (field.IsLiteral)
                                dependencies.Add(factory.FieldMetadata(field), reason);
                        }
                    }
                }
            }
        }

        private static bool IsTrimmableAssembly(ModuleDesc assembly)
        {
            if (assembly is EcmaAssembly ecmaAssembly)
            {
                foreach (var attribute in ecmaAssembly.GetDecodedCustomAttributes("System.Reflection", "AssemblyMetadataAttribute"))
                {
                    if (attribute.FixedArguments.Length != 2)
                        continue;

                    if (!attribute.FixedArguments[0].Type.IsString
                        || ((string)(attribute.FixedArguments[0].Value)).Equals("IsTrimmable", StringComparison.Ordinal))
                        continue;

                    if (!attribute.FixedArguments[1].Type.IsString)
                        continue;

                    string value = (string)attribute.FixedArguments[1].Value;

                    if (value.Equals("True", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override void GetRuntimeMappingDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            // If we precisely track field usage, we don't need the logic below.
            if (_hasPreciseFieldUsageInformation)
                return;

            const string reason = "Reflection";

            // This logic is applying policy: if a type is reflectable (has a runtime mapping), all of it's fields
            // are reflectable (with a runtime mapping) as well.
            // This is potentially overly broad (we don't know if any of the fields will actually be eligile
            // for metadata - e.g. they could all be reflection blocked). This is fine since lack of
            // precise field usage information is already not ideal from a size on disk perspective.
            // The more precise way to do this would be to go over each field, check that it's eligible for RuntimeMapping
            // according to the policy (e.g. it's not blocked), and only then root the base of the field.
            if (type is MetadataType metadataType && !type.IsGenericDefinition)
            {
                Debug.Assert(!type.IsCanonicalSubtype(CanonicalFormKind.Any));

                if (metadataType.GCStaticFieldSize.AsInt > 0)
                {
                    dependencies.Add(factory.TypeGCStaticsSymbol(metadataType), reason);
                }

                if (metadataType.NonGCStaticFieldSize.AsInt > 0 || factory.PreinitializationManager.HasLazyStaticConstructor(metadataType))
                {
                    dependencies.Add(factory.TypeNonGCStaticsSymbol(metadataType), reason);
                }

                if (metadataType.ThreadGcStaticFieldSize.AsInt > 0)
                {
                    dependencies.Add(factory.TypeThreadStaticIndex(metadataType), reason);
                }

                Debug.Assert(metadataType.ThreadNonGcStaticFieldSize.AsInt == 0);
            }
        }

        public override bool HasConditionalDependenciesDueToEETypePresence(TypeDesc type)
        {
            // Note: duplicated with the check in GetConditionalDependenciesDueToEETypePresence
            return type.IsDefType && !type.IsInterface && FlowAnnotations.GetTypeAnnotation(type) != default;
        }

        public override void GetConditionalDependenciesDueToEETypePresence(ref CombinedDependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            // Check to see if we have any dataflow annotations on the type.
            // The check below also covers flow annotations inherited through base classes and implemented interfaces.
            if (type.IsDefType
                && !type.IsInterface /* "IFoo x; x.GetType();" -> this doesn't actually return an interface type */
                && FlowAnnotations.GetTypeAnnotation(type) != default)
            {
                // We have some flow annotations on this type.
                //
                // The flow annotations are supposed to ensure that should we call object.GetType on a location
                // typed as one of the annotated subclasses of this type, this type is going to have the specified
                // members kept. We don't keep them right away, but condition them on the object.GetType being called.
                //
                // Now we figure out where the annotations are coming from:

                DefType baseType = type.BaseType;
                if (baseType != null && FlowAnnotations.GetTypeAnnotation(baseType) != default)
                {
                    // There's an annotation on the base type. If object.GetType was called on something
                    // statically typed as the base type, we might actually be calling it on this type.
                    // Ensure we have the flow dependencies.
                    dependencies ??= new CombinedDependencyList();
                    dependencies.Add(new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                        factory.ObjectGetTypeFlowDependencies((MetadataType)type),
                        factory.ObjectGetTypeFlowDependencies((MetadataType)baseType),
                        "GetType called on the base type"));

                    // We don't have to follow all the bases since the base MethodTable will bubble this up
                }

                foreach (DefType interfaceType in type.RuntimeInterfaces)
                {
                    if (FlowAnnotations.GetTypeAnnotation(interfaceType) != default)
                    {
                        // There's an annotation on the interface type. If object.GetType was called on something
                        // statically typed as the interface type, we might actually be calling it on this type.
                        // Ensure we have the flow dependencies.
                        dependencies ??= new CombinedDependencyList();
                        dependencies.Add(new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                            factory.ObjectGetTypeFlowDependencies((MetadataType)type),
                            factory.ObjectGetTypeFlowDependencies((MetadataType)interfaceType),
                            "GetType called on the interface"));
                    }

                    // We don't have to recurse into the interface because we're inspecting runtime interfaces
                    // and this list is already flattened.
                }

                // Note we don't add any conditional dependencies if this type itself was annotated and none
                // of the bases/interfaces are annotated.
                // ObjectGetTypeFlowDependencies don't need to be conditional in that case. They'll be added as needed.
            }
        }

        public override void GetDependenciesDueToLdToken(ref DependencyList dependencies, NodeFactory factory, FieldDesc field)
        {
            // In order for the RuntimeFieldHandle data structure to be usable at runtime, ensure the field
            // is generating metadata.
            if ((GetMetadataCategory(field) & MetadataCategory.Description) == MetadataCategory.Description)
            {
                dependencies = dependencies ?? new DependencyList();
                dependencies.Add(factory.FieldMetadata(field.GetTypicalFieldDefinition()), "LDTOKEN field");
            }
        }

        public override void GetDependenciesDueToLdToken(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            dependencies = dependencies ?? new DependencyList();

            if (!IsReflectionBlocked(method))
                dependencies.Add(factory.ReflectableMethod(method), "LDTOKEN method");
        }

        protected override void GetDependenciesDueToMethodCodePresenceInternal(ref DependencyList dependencies, NodeFactory factory, MethodDesc method, MethodIL methodIL)
        {
            bool scanReflection = (_generationOptions & UsageBasedMetadataGenerationOptions.ReflectionILScanning) != 0;

            Debug.Assert(methodIL != null || method.IsAbstract || method.IsPInvoke || method.IsInternalCall);

            if (methodIL != null && scanReflection)
            {
                if (FlowAnnotations.RequiresDataflowAnalysis(method))
                {
                    dependencies = dependencies ?? new DependencyList();
                    dependencies.Add(factory.DataflowAnalyzedMethod(methodIL.GetMethodILDefinition()), "Method has annotated parameters");
                }

                if ((method.HasInstantiation && !method.IsCanonicalMethod(CanonicalFormKind.Any)))
                {
                    MethodDesc typicalMethod = method.GetTypicalMethodDefinition();
                    Debug.Assert(typicalMethod != method);

                    GetFlowDependenciesForInstantiation(ref dependencies, factory, method.Instantiation, typicalMethod.Instantiation, method);
                }

                TypeDesc owningType = method.OwningType;
                if (owningType.HasInstantiation && !owningType.IsCanonicalSubtype(CanonicalFormKind.Any))
                {
                    TypeDesc owningTypeDefinition = owningType.GetTypeDefinition();
                    Debug.Assert(owningType != owningTypeDefinition);

                    GetFlowDependenciesForInstantiation(ref dependencies, factory, owningType.Instantiation, owningTypeDefinition.Instantiation, owningType);
                }
            }

            if (method.GetTypicalMethodDefinition() is Internal.TypeSystem.Ecma.EcmaMethod ecmaMethod)
            {
                DynamicDependencyAttributeAlgorithm.AddDependenciesDueToDynamicDependencyAttribute(ref dependencies, factory, ecmaMethod);
            }

            // Presence of code might trigger the reflectability dependencies.
            if ((_generationOptions & UsageBasedMetadataGenerationOptions.ReflectedMembersOnly) == 0)
            {
                GetDependenciesDueToReflectability(ref dependencies, factory, method);
            }
        }

        public override void GetConditionalDependenciesDueToMethodCodePresence(ref CombinedDependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            MethodDesc typicalMethod = method.GetTypicalMethodDefinition();

            // Ensure methods with genericness have the same reflectability by injecting a conditional dependency.
            if ((_generationOptions & UsageBasedMetadataGenerationOptions.ReflectedMembersOnly) != 0
                && method != typicalMethod)
            {
                dependencies ??= new CombinedDependencyList();
                dependencies.Add(new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                    factory.ReflectableMethod(method), factory.ReflectableMethod(typicalMethod), "Reflectability of methods is same across genericness"));
            }
        }

        public override void GetDependenciesDueToVirtualMethodReflectability(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            if ((_generationOptions & UsageBasedMetadataGenerationOptions.ReflectedMembersOnly) == 0)
            {
                // If we have a use of an abstract method, GetDependenciesDueToReflectability is not going to see the method
                // as being used since there's no body. We inject a dependency on a new node that serves as a logical method body
                // for the metadata manager. Metadata manager treats that node the same as a body.
                if (method.IsAbstract && GetMetadataCategory(method) != 0)
                {
                    dependencies = dependencies ?? new DependencyList();
                    dependencies.Add(factory.ReflectableMethod(method), "Abstract reflectable method");
                }
            }
        }

        protected override IEnumerable<FieldDesc> GetFieldsWithRuntimeMapping()
        {
            if (_hasPreciseFieldUsageInformation)
            {
                // TODO
            }
            else
            {
                // This applies a policy that fields inherit runtime mapping from their owning type,
                // unless they are blocked.
                foreach (var type in GetTypesWithRuntimeMapping())
                {
                    if (type.IsGenericDefinition)
                        continue;

                    foreach (var field in type.GetFields())
                    {
                        if ((GetMetadataCategory(field) & MetadataCategory.RuntimeMapping) != 0)
                            yield return field;
                    }
                }
            }
        }

        public override IEnumerable<ModuleDesc> GetCompilationModulesWithMetadata()
        {
            return _modulesWithMetadata;
        }

        private IEnumerable<TypeDesc> GetTypesWithRuntimeMapping()
        {
            // All constructed types that are not blocked get runtime mapping
            foreach (var constructedType in GetTypesWithConstructedEETypes())
            {
                if (!IsReflectionBlocked(constructedType))
                    yield return constructedType;
            }

            // All necessary types for which this is the highest load level that are not blocked
            // get runtime mapping.
            foreach (var necessaryType in GetTypesWithEETypes())
            {
                if (!ConstructedEETypeNode.CreationAllowed(necessaryType) &&
                    !IsReflectionBlocked(necessaryType))
                    yield return necessaryType;
            }
        }

        public override void GetDependenciesDueToAccess(ref DependencyList dependencies, NodeFactory factory, MethodIL methodIL, FieldDesc writtenField)
        {
            bool scanReflection = (_generationOptions & UsageBasedMetadataGenerationOptions.ReflectionILScanning) != 0;
            if (scanReflection && Dataflow.ReflectionMethodBodyScanner.RequiresReflectionMethodBodyScannerForAccess(FlowAnnotations, writtenField))
            {
                dependencies = dependencies ?? new DependencyList();
                dependencies.Add(factory.DataflowAnalyzedMethod(methodIL.GetMethodILDefinition()), "Access to interesting field");
            }
        }

        public override void GetDependenciesDueToAccess(ref DependencyList dependencies, NodeFactory factory, MethodIL methodIL, MethodDesc calledMethod)
        {
            bool scanReflection = (_generationOptions & UsageBasedMetadataGenerationOptions.ReflectionILScanning) != 0;
            if (scanReflection && Dataflow.ReflectionMethodBodyScanner.RequiresReflectionMethodBodyScannerForCallSite(FlowAnnotations, calledMethod))
            {
                dependencies = dependencies ?? new DependencyList();
                dependencies.Add(factory.DataflowAnalyzedMethod(methodIL.GetMethodILDefinition()), "Call to interesting method");
            }
        }

        public override DependencyList GetDependenciesForCustomAttribute(NodeFactory factory, MethodDesc attributeCtor, CustomAttributeValue decodedValue)
        {
            bool scanReflection = (_generationOptions & UsageBasedMetadataGenerationOptions.ReflectionILScanning) != 0;
            if (scanReflection)
            {
                return Dataflow.ReflectionMethodBodyScanner.ProcessAttributeDataflow(factory, FlowAnnotations, Logger, attributeCtor, decodedValue);
            }

            return null;
        }

        private void GetFlowDependenciesForInstantiation(ref DependencyList dependencies, NodeFactory factory, Instantiation instantiation, Instantiation typicalInstantiation, TypeSystemEntity source)
        {
            for (int i = 0; i < instantiation.Length; i++)
            {
                var genericParameter = (GenericParameterDesc)typicalInstantiation[i];
                if (FlowAnnotations.GetGenericParameterAnnotation(genericParameter) != default)
                {
                    var deps = ILCompiler.Dataflow.ReflectionMethodBodyScanner.ProcessGenericArgumentDataFlow(factory, FlowAnnotations, Logger, genericParameter, instantiation[i], source);
                    if (deps.Count > 0)
                    {
                        if (dependencies == null)
                            dependencies = deps;
                        else
                            dependencies.AddRange(deps);
                    }
                }
            }
        }

        public override void GetDependenciesForGenericDictionary(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            TypeDesc owningType = method.OwningType;

            if (FlowAnnotations.HasAnyAnnotations(owningType))
            {
                MethodDesc typicalMethod = method.GetTypicalMethodDefinition();
                Debug.Assert(typicalMethod != method);

                GetFlowDependenciesForInstantiation(ref dependencies, factory, method.Instantiation, typicalMethod.Instantiation, method);

                if (owningType.HasInstantiation)
                {
                    // Since this also introduces a new type instantiation into the system, collect the dependencies for that too.
                    // We might not see the instantiated type elsewhere.
                    GetFlowDependenciesForInstantiation(ref dependencies, factory, owningType.Instantiation, owningType.GetTypeDefinition().Instantiation, method);
                }
            }
        }

        public override void GetDependenciesForGenericDictionary(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            if (FlowAnnotations.HasAnyAnnotations(type))
            {
                TypeDesc typeDefinition = type.GetTypeDefinition();
                Debug.Assert(type != typeDefinition);
                GetFlowDependenciesForInstantiation(ref dependencies, factory, type.Instantiation, typeDefinition.Instantiation, type);
            }
        }

        public bool GeneratesAttributeMetadata(TypeDesc attributeType)
        {
            var ecmaType = attributeType.GetTypeDefinition() as EcmaType;
            if (ecmaType != null)
            {
                var moduleInfo = _featureSwitchHashtable.GetOrCreateValue(ecmaType.EcmaModule);
                return !moduleInfo.RemovedAttributes.Contains(ecmaType);
            }

            return true;
        }

        public override void NoteOverridingMethod(MethodDesc baseMethod, MethodDesc overridingMethod)
        {
            // We validate that the various dataflow/Requires* annotations are consistent across virtual method overrides

            bool baseMethodRequiresUnreferencedCode = baseMethod.HasCustomAttribute("System.Diagnostics.CodeAnalysis", "RequiresUnreferencedCodeAttribute");
            bool overridingMethodRequiresUnreferencedCode = overridingMethod.HasCustomAttribute("System.Diagnostics.CodeAnalysis", "RequiresUnreferencedCodeAttribute");

            bool baseMethodRequiresDynamicCode = baseMethod.HasCustomAttribute("System.Diagnostics.CodeAnalysis", "RequiresDynamicCodeAttribute");
            bool overridingMethodRequiresDynamicCode = overridingMethod.HasCustomAttribute("System.Diagnostics.CodeAnalysis", "RequiresDynamicCodeAttribute");

            bool baseMethodRequiresDataflow = FlowAnnotations.RequiresDataflowAnalysis(baseMethod);
            bool overridingMethodRequiresDataflow = FlowAnnotations.RequiresDataflowAnalysis(overridingMethod);

            if (baseMethodRequiresUnreferencedCode != overridingMethodRequiresUnreferencedCode)
            {
                Logger.LogWarning(
                    $"Presence of 'RequiresUnreferencedCodeAttribute' on method '{overridingMethod.GetDisplayName()}' doesn't match overridden method '{baseMethod.GetDisplayName()}'. " +
                    $"All overridden methods must have 'RequiresUnreferencedCodeAttribute'.", 2046, overridingMethod, MessageSubCategory.TrimAnalysis);
            }

            if (baseMethodRequiresDynamicCode != overridingMethodRequiresDynamicCode)
            {
                Logger.LogWarning(
                    $"Presence of 'RequiresDynamicCodeAttribute' on method '{overridingMethod.GetDisplayName()}' doesn't match overridden method '{baseMethod.GetDisplayName()}'. " +
                    $"All overridden methods must have 'RequiresDynamicCodeAttribute'.", 2046, overridingMethod, MessageSubCategory.AotAnalysis);
            }

            if (baseMethodRequiresDataflow || overridingMethodRequiresDataflow)
            {
                FlowAnnotations.ValidateMethodAnnotationsAreSame(overridingMethod, baseMethod);
            }
        }

        public MetadataManager ToAnalysisBasedMetadataManager()
        {
            var reflectableTypes = ReflectableEntityBuilder<TypeDesc>.Create();

            // Collect the list of types that are generating reflection metadata
            foreach (var typeWithMetadata in _typesWithMetadata)
            {
                reflectableTypes[typeWithMetadata] = MetadataCategory.Description;
            }

            foreach (var constructedType in GetTypesWithRuntimeMapping())
            {
                reflectableTypes[constructedType] |= MetadataCategory.RuntimeMapping;

                // Also set the description bit if the definition is getting metadata.
                TypeDesc constructedTypeDefinition = constructedType.GetTypeDefinition();
                if (constructedType != constructedTypeDefinition &&
                    (reflectableTypes[constructedTypeDefinition] & MetadataCategory.Description) != 0)
                {
                    reflectableTypes[constructedType] |= MetadataCategory.Description;
                }
            }

            var reflectableMethods = ReflectableEntityBuilder<MethodDesc>.Create();
            foreach (var methodWithMetadata in _methodsWithMetadata)
            {
                reflectableMethods[methodWithMetadata] = MetadataCategory.Description;
            }

            foreach (var method in GetReflectableMethods())
            {
                if (method.IsGenericMethodDefinition || method.OwningType.IsGenericDefinition)
                    continue;

                if (!IsReflectionBlocked(method))
                {
                    if ((reflectableTypes[method.OwningType] & MetadataCategory.RuntimeMapping) != 0)
                        reflectableMethods[method] |= MetadataCategory.RuntimeMapping;

                    // Also set the description bit if the definition is getting metadata.
                    MethodDesc typicalMethod = method.GetTypicalMethodDefinition();
                    if (method != typicalMethod &&
                        (reflectableMethods[typicalMethod] & MetadataCategory.Description) != 0)
                    {
                        reflectableMethods[method] |= MetadataCategory.Description;
                        reflectableTypes[method.OwningType] |= MetadataCategory.Description;
                    }
                }
            }

            var reflectableFields = ReflectableEntityBuilder<FieldDesc>.Create();
            foreach (var fieldWithMetadata in _fieldsWithMetadata)
            {
                reflectableFields[fieldWithMetadata] = MetadataCategory.Description;
            }

            if (_hasPreciseFieldUsageInformation)
            {
                // TODO
            }
            else
            {
                // If we don't have precise field usage information we apply a policy that
                // says the fields inherit the setting from the type, potentially restricted by blocking.
                // (I.e. if a type has RuntimeMapping metadata, the field has RuntimeMapping too, unless blocked.)
                foreach (var reflectableType in reflectableTypes.ToEnumerable())
                {
                    if (reflectableType.Entity.IsGenericDefinition)
                        continue;

                    if (reflectableType.Entity.IsCanonicalSubtype(CanonicalFormKind.Specific))
                        continue;

                    if ((reflectableType.Category & MetadataCategory.RuntimeMapping) == 0)
                        continue;

                    foreach (var field in reflectableType.Entity.GetFields())
                    {
                        if (!IsReflectionBlocked(field))
                        {
                            reflectableFields[field] |= MetadataCategory.RuntimeMapping;

                            // Also set the description bit if the definition is getting metadata.
                            FieldDesc typicalField = field.GetTypicalFieldDefinition();
                            if (field != typicalField &&
                                (reflectableFields[typicalField] & MetadataCategory.Description) != 0)
                            {
                                reflectableFields[field] |= MetadataCategory.Description;
                            }
                        }
                    }
                }
            }

            return new AnalysisBasedMetadataManager(
                _typeSystemContext, _blockingPolicy, _resourceBlockingPolicy, _metadataLogFile, _stackTraceEmissionPolicy, _dynamicInvokeThunkGenerationPolicy,
                _modulesWithMetadata, reflectableTypes.ToEnumerable(), reflectableMethods.ToEnumerable(),
                reflectableFields.ToEnumerable(), _customAttributesWithMetadata);
        }

        private struct ReflectableEntityBuilder<T>
        {
            private Dictionary<T, MetadataCategory> _dictionary;

            public static ReflectableEntityBuilder<T> Create()
            {
                return new ReflectableEntityBuilder<T>
                {
                    _dictionary = new Dictionary<T, MetadataCategory>(),
                };
            }

            public MetadataCategory this[T key]
            {
                get
                {
                    if (_dictionary.TryGetValue(key, out MetadataCategory category))
                        return category;
                    return 0;
                }
                set
                {
                    _dictionary[key] = value;
                }
            }

            public IEnumerable<ReflectableEntity<T>> ToEnumerable()
            {
                foreach (var entry in _dictionary)
                {
                    yield return new ReflectableEntity<T>(entry.Key, entry.Value);
                }
            }
        }

        private struct GeneratedTypesAndCodeMetadataPolicy : IMetadataPolicy
        {
            private readonly MetadataBlockingPolicy _blockingPolicy;
            private readonly NodeFactory _factory;

            public GeneratedTypesAndCodeMetadataPolicy(MetadataBlockingPolicy blockingPolicy, NodeFactory factory)
            {
                _blockingPolicy = blockingPolicy;
                _factory = factory;
            }

            public bool GeneratesMetadata(FieldDesc fieldDef)
            {
                return _factory.FieldMetadata(fieldDef).Marked;
            }

            public bool GeneratesMetadata(MethodDesc methodDef)
            {
                return _factory.MethodMetadata(methodDef).Marked;
            }

            public bool GeneratesMetadata(MetadataType typeDef)
            {
                return _factory.TypeMetadata(typeDef).Marked;
            }

            public bool GeneratesMetadata(EcmaModule module, CustomAttributeHandle caHandle)
            {
                return _factory.CustomAttributeMetadata(new ReflectableCustomAttribute(module, caHandle)).Marked;
            }

            public bool GeneratesMetadata(EcmaModule module, ExportedTypeHandle exportedTypeHandle)
            {
                try
                {
                    // Generate the forwarder only if we generated the target type.
                    // If the target type is in a different compilation group, assume we generated it there.
                    var targetType = (MetadataType)module.GetObject(exportedTypeHandle);
                    return GeneratesMetadata(targetType) || !_factory.CompilationModuleGroup.ContainsType(targetType);
                }
                catch (TypeSystemException)
                {
                    // No harm in generating a forwarder that didn't resolve.
                    // We'll get matching behavior at runtime.
                    return true;
                }
            }

            public bool IsBlocked(MetadataType typeDef)
            {
                return _blockingPolicy.IsBlocked(typeDef);
            }

            public bool IsBlocked(MethodDesc methodDef)
            {
                return _blockingPolicy.IsBlocked(methodDef);
            }
        }

        private class FeatureSwitchHashtable : LockFreeReaderHashtable<EcmaModule, AssemblyFeatureInfo>
        {
            private readonly Dictionary<string, bool> _switchValues;

            public FeatureSwitchHashtable(Dictionary<string, bool> switchValues)
            {
                _switchValues = switchValues;
            }

            protected override bool CompareKeyToValue(EcmaModule key, AssemblyFeatureInfo value) => key == value.Module;
            protected override bool CompareValueToValue(AssemblyFeatureInfo value1, AssemblyFeatureInfo value2) => value1.Module == value2.Module;
            protected override int GetKeyHashCode(EcmaModule key) => key.GetHashCode();
            protected override int GetValueHashCode(AssemblyFeatureInfo value) => value.Module.GetHashCode();

            protected override AssemblyFeatureInfo CreateValueFromKey(EcmaModule key)
            {
                return new AssemblyFeatureInfo(key, _switchValues);
            }
        }

        private class AssemblyFeatureInfo
        {
            public EcmaModule Module { get; }

            public HashSet<TypeDesc> RemovedAttributes { get; }

            public AssemblyFeatureInfo(EcmaModule module, IReadOnlyDictionary<string, bool> featureSwitchValues)
            {
                Module = module;
                RemovedAttributes = new HashSet<TypeDesc>();

                PEMemoryBlock resourceDirectory = module.PEReader.GetSectionData(module.PEReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);

                foreach (var resourceHandle in module.MetadataReader.ManifestResources)
                {
                    ManifestResource resource = module.MetadataReader.GetManifestResource(resourceHandle);

                    // Don't try to process linked resources or resources in other assemblies
                    if (!resource.Implementation.IsNil)
                    {
                        continue;
                    }

                    string resourceName = module.MetadataReader.GetString(resource.Name);
                    if (resourceName == "ILLink.LinkAttributes.xml")
                    {
                        BlobReader reader = resourceDirectory.GetReader((int)resource.Offset, resourceDirectory.Length - (int)resource.Offset);
                        int length = (int)reader.ReadUInt32();

                        UnmanagedMemoryStream ms;
                        unsafe
                        {
                            ms = new UnmanagedMemoryStream(reader.CurrentPointer, length);
                        }

                        RemovedAttributes = LinkAttributesReader.GetRemovedAttributes(module.Context, XmlReader.Create(ms), module, featureSwitchValues);
                    }
                }
            }
        }

        private class LinkAttributesReader : ProcessLinkerXmlBase
        {
            private readonly HashSet<TypeDesc> _removedAttributes;

            private LinkAttributesReader(TypeSystemContext context, XmlReader reader, ModuleDesc module, IReadOnlyDictionary<string, bool> featureSwitchValues)
                : base(context, reader, module, featureSwitchValues)
            {
                _removedAttributes = new HashSet<TypeDesc>();
            }

            protected override void ProcessAttribute(TypeDesc type)
            {
                string internalValue = GetAttribute("internal");
                if (internalValue == "RemoveAttributeInstances" && IsEmpty())
                {
                    _removedAttributes.Add(type);
                }
            }

            public static HashSet<TypeDesc> GetRemovedAttributes(TypeSystemContext context, XmlReader reader, ModuleDesc module, IReadOnlyDictionary<string, bool> featureSwitchValues)
            {
                var rdr = new LinkAttributesReader(context, reader, module, featureSwitchValues);
                rdr.ProcessXml();
                return rdr._removedAttributes;
            }
        }
    }

    [Flags]
    public enum UsageBasedMetadataGenerationOptions
    {
        None = 0,

        /// <summary>
        /// Specifies that complete metadata should be generated for types.
        /// </summary>
        /// <remarks>
        /// If this option is set, generated metadata will no longer be pay for play,
        /// and a certain class of bugs will disappear (APIs returning "member doesn't
        /// exist" at runtime, even though the member exists and we just didn't generate the metadata).
        /// Reflection blocking still applies.
        /// </remarks>
        CompleteTypesOnly = 1,

        /// <summary>
        /// Specifies that heuristic that makes anonymous types work should be applied.
        /// </summary>
        /// <remarks>
        /// Generates method bodies for properties on anonymous types even if they're not
        /// statically used.
        /// </remarks>
        AnonymousTypeHeuristic = 2,

        /// <summary>
        /// Scan IL for common reflection patterns to find additional compilation roots.
        /// </summary>
        ReflectionILScanning = 4,

        /// <summary>
        /// Only members that were seen as reflected on will be reflectable.
        /// </summary>
        ReflectedMembersOnly = 8,

        /// <summary>
        /// Fully root used assemblies that are not marked IsTrimmable in metadata.
        /// </summary>
        RootDefaultAssemblies = 16,
    }
}

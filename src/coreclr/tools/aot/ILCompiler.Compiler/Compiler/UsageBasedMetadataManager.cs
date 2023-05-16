// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Xml.XPath;

using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Logging;
using ILCompiler.Metadata;
using ILLink.Shared;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using CombinedDependencyList = System.Collections.Generic.List<ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.CombinedDependencyListEntry>;
using CustomAttributeHandle = System.Reflection.Metadata.CustomAttributeHandle;
using CustomAttributeValue = System.Reflection.Metadata.CustomAttributeValue<Internal.TypeSystem.TypeDesc>;
using Debug = System.Diagnostics.Debug;
using DependencyList = ILCompiler.DependencyAnalysisFramework.DependencyNodeCore<ILCompiler.DependencyAnalysis.NodeFactory>.DependencyList;
using EcmaModule = Internal.TypeSystem.Ecma.EcmaModule;
using EcmaType = Internal.TypeSystem.Ecma.EcmaType;
using FlowAnnotations = ILLink.Shared.TrimAnalysis.FlowAnnotations;

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

        private readonly LinkAttributesHashTable _linkAttributesHashTable;

        private static (string AttributeName, DiagnosticId Id)[] _requiresAttributeMismatchNameAndId = new[]
            {
                (DiagnosticUtilities.RequiresUnreferencedCodeAttribute, DiagnosticId.RequiresUnreferencedCodeAttributeMismatch),
                (DiagnosticUtilities.RequiresDynamicCodeAttribute, DiagnosticId.RequiresDynamicCodeAttributeMismatch),
                (DiagnosticUtilities.RequiresAssemblyFilesAttribute, DiagnosticId.RequiresAssemblyFilesAttributeMismatch)
            };

        private readonly List<TypeDesc> _typesWithForcedEEType = new List<TypeDesc>();
        private readonly List<ModuleDesc> _modulesWithMetadata = new List<ModuleDesc>();
        private readonly List<FieldDesc> _fieldsWithMetadata = new List<FieldDesc>();
        private readonly List<MethodDesc> _methodsWithMetadata = new List<MethodDesc>();
        private readonly List<MetadataType> _typesWithMetadata = new List<MetadataType>();
        private readonly List<FieldDesc> _fieldsWithRuntimeMapping = new List<FieldDesc>();
        private readonly List<ReflectableCustomAttribute> _customAttributesWithMetadata = new List<ReflectableCustomAttribute>();

        internal IReadOnlyDictionary<string, bool> FeatureSwitches { get; }

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
            MetadataManagerOptions options,
            Logger logger,
            IEnumerable<KeyValuePair<string, bool>> featureSwitchValues,
            IEnumerable<string> rootEntireAssembliesModules,
            IEnumerable<string> additionalRootedAssemblies,
            IEnumerable<string> trimmedAssemblies)
            : base(typeSystemContext, blockingPolicy, resourceBlockingPolicy, logFile, stackTracePolicy, invokeThunkGenerationPolicy, options)
        {
            _compilationModuleGroup = group;
            _generationOptions = generationOptions;

            FlowAnnotations = flowAnnotations;
            Logger = logger;

            _linkAttributesHashTable = new LinkAttributesHashTable(Logger, new Dictionary<string, bool>(featureSwitchValues));
            FeatureSwitches = new Dictionary<string, bool>(featureSwitchValues);

            _rootEntireAssembliesModules = new HashSet<string>(rootEntireAssembliesModules);
            _rootEntireAssembliesModules.UnionWith(additionalRootedAssemblies);
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

            var reflectedFieldNode = obj as ReflectedFieldNode;
            if (reflectedFieldNode != null)
            {
                FieldDesc field = reflectedFieldNode.Field;

                // Filter out to those that make sense to have in the mapping tables
                if (!field.OwningType.IsGenericDefinition && !field.IsLiteral)
                {
                    Debug.Assert((GetMetadataCategory(field) & MetadataCategory.RuntimeMapping) != 0);
                    _fieldsWithRuntimeMapping.Add(field);
                }
            }

            if (obj is ReflectedTypeNode reflectableType)
            {
                _typesWithForcedEEType.Add(reflectableType.Type);
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

        protected override bool AllMethodsCanBeReflectable => (_generationOptions & UsageBasedMetadataGenerationOptions.CreateReflectableArtifacts) != 0;

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
            dependencies ??= new DependencyList();
            dependencies.Add(factory.MethodMetadata(method.GetTypicalMethodDefinition()), "Reflectable method");
        }

        protected override void GetMetadataDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, FieldDesc field)
        {
            dependencies ??= new DependencyList();
            dependencies.Add(factory.FieldMetadata(field.GetTypicalFieldDefinition()), "Reflectable field");
        }

        internal override void GetDependenciesDueToModuleUse(ref DependencyList dependencies, NodeFactory factory, ModuleDesc module)
        {
            dependencies ??= new DependencyList();
            if (module.GetGlobalModuleType().GetStaticConstructor() is MethodDesc moduleCctor)
            {
                dependencies.Add(factory.MethodEntrypoint(moduleCctor), "Module with a static constructor");
            }
            if (module is EcmaModule ecmaModule)
            {
                foreach (var resourceHandle in ecmaModule.MetadataReader.ManifestResources)
                {
                    ManifestResource resource = ecmaModule.MetadataReader.GetManifestResource(resourceHandle);

                    // Don't try to process linked resources or resources in other assemblies
                    if (!resource.Implementation.IsNil)
                    {
                        continue;
                    }

                    string resourceName = ecmaModule.MetadataReader.GetString(resource.Name);
                    if (resourceName == "ILLink.Descriptors.xml")
                    {
                        dependencies.Add(factory.EmbeddedTrimmingDescriptor(ecmaModule), "Embedded descriptor file");
                    }
                }
            }
        }

        protected override void GetMetadataDependenciesDueToReflectability(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            TypeMetadataNode.GetMetadataDependencies(ref dependencies, factory, type, "Reflectable type");

            if (type.IsDelegate)
            {
                // We've decided as a policy that delegate Invoke methods will be generated in full.
                // The libraries (e.g. System.Linq.Expressions) have trimming warning suppressions
                // in places where they assume IL-level trimming (where the method cannot be removed).
                // We ask for a full reflectable method with its method body instead of just the
                // metadata.
                MethodDesc invokeMethod = type.GetMethod("Invoke", null);
                if (!IsReflectionBlocked(invokeMethod))
                {
                    dependencies ??= new DependencyList();
                    dependencies.Add(factory.ReflectedMethod(invokeMethod), "Delegate invoke method is always reflectable");
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
                            dependencies ??= new DependencyList();
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

                // https://github.com/dotnet/runtime/issues/78752
                // Compat with https://github.com/dotnet/linker/issues/1541 IL Linker bug:
                // Asking to root an assembly with entrypoint will not actually root things in the assembly.
                // We need to emulate this because the SDK injects a root for the entrypoint assembly right now
                // because of IL Linker's implementation details (IL Linker won't root Main() by itself).
                // TODO: We should technically reflection-root Main() here but hopefully the above issue
                // will be fixed before it comes to that being necessary.
                bool isEntrypointAssembly = module is EcmaModule ecmaModule && ecmaModule.PEReader.PEHeaders.IsExe;

                if (!isEntrypointAssembly && _rootEntireAssembliesModules.Contains(assemblyName))
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
                    dependencies ??= new DependencyList();
                    var rootProvider = new RootingServiceProvider(factory, dependencies.Add);
                    foreach (TypeDesc t in mdType.Module.GetAllTypes())
                    {
                        RootingHelpers.TryRootType(rootProvider, t, reason);
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
                        || !((string)(attribute.FixedArguments[0].Value)).Equals("IsTrimmable", StringComparison.Ordinal))
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

        public override void GetDependenciesDueToEETypePresence(ref DependencyList dependencies, NodeFactory factory, TypeDesc type)
        {
            base.GetDependenciesDueToEETypePresence(ref dependencies, factory, type);

            DataflowAnalyzedTypeDefinitionNode.GetDependencies(ref dependencies, factory, FlowAnnotations, type);
        }

        public override bool HasConditionalDependenciesDueToEETypePresence(TypeDesc type)
        {
            // Note: these are duplicated with the checks in GetConditionalDependenciesDueToEETypePresence

            // If there's dataflow annotations on the type, we have conditional dependencies
            if (type.IsDefType && !type.IsInterface && FlowAnnotations.GetTypeAnnotation(type) != default)
                return true;

            // If we need to ensure fields are consistently reflectable on various generic instances
            if (type.HasInstantiation && !type.IsGenericDefinition && !IsReflectionBlocked(type))
                return true;

            return false;
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

            if (type.HasInstantiation && !type.IsTypeDefinition && !IsReflectionBlocked(type))
            {
                // Ensure fields can be consistently reflection set & get.
                foreach (FieldDesc field in type.GetFields())
                {
                    // Tiny optimization: no get/set for literal fields since they only exist in metadata
                    if (field.IsLiteral)
                        continue;

                    if (IsReflectionBlocked(field))
                        continue;

                    dependencies ??= new CombinedDependencyList();
                    dependencies.Add(new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                        factory.ReflectedField(field),
                        factory.ReflectedField(field.GetTypicalFieldDefinition()),
                        "Fields have same reflectability"));
                }

                // Ensure methods can be consistently reflection-accessed
                foreach (MethodDesc method in type.GetMethods())
                {
                    if (IsReflectionBlocked(method))
                        continue;

                    // Generic methods need to be instantiated over something.
                    if (method.HasInstantiation)
                        continue;

                    dependencies ??= new CombinedDependencyList();
                    dependencies.Add(new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                        factory.ReflectedMethod(method),
                        factory.ReflectedMethod(method.GetTypicalMethodDefinition()),
                        "Methods have same reflectability"));
                }
            }
        }

        public override void GetDependenciesDueToLdToken(ref DependencyList dependencies, NodeFactory factory, FieldDesc field)
        {
            if (!IsReflectionBlocked(field))
            {
                dependencies ??= new DependencyList();
                dependencies.Add(factory.ReflectedField(field), "LDTOKEN field");
            }
        }

        public override void GetDependenciesDueToLdToken(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            dependencies ??= new DependencyList();

            if (!IsReflectionBlocked(method))
                dependencies.Add(factory.ReflectedMethod(method), "LDTOKEN method");
        }

        public override void GetDependenciesDueToDelegateCreation(ref DependencyList dependencies, NodeFactory factory, MethodDesc target)
        {
            if (!IsReflectionBlocked(target))
            {
                dependencies ??= new DependencyList();
                dependencies.Add(factory.ReflectedMethod(target), "Target of a delegate");
            }
        }

        public override void GetDependenciesForOverridingMethod(ref CombinedDependencyList dependencies, NodeFactory factory, MethodDesc decl, MethodDesc impl)
        {
            Debug.Assert(decl.IsVirtual && MetadataVirtualMethodAlgorithm.FindSlotDefiningMethodForVirtualMethod(decl) == decl);

            // If a virtual method slot is reflection visible, all implementations become reflection visible.
            //
            // We could technically come up with a weaker position on this because the code below just needs to
            // to ensure that delegates to virtual methods can have their GetMethodInfo() called.
            // Delegate construction introduces a ReflectableMethod for the slot defining method; it doesn't need to.
            // We could have a specialized node type to track that specific thing and introduce a conditional dependency
            // on that.
            //
            // class Base { abstract Boo(); }
            // class Derived1 : Base { override Boo() { } }
            // class Derived2 : Base { override Boo() { } }
            //
            // typeof(Derived2).GetMethods(...)
            //
            // In the above case, we don't really need Derived1.Boo to become reflection visible
            // but the below code will do that because ReflectedMethodNode tracks all reflectable methods,
            // without keeping information about subtleities like "reflectable delegate".
            if (!IsReflectionBlocked(decl) && !IsReflectionBlocked(impl))
            {
                dependencies ??= new CombinedDependencyList();
                dependencies.Add(new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                    factory.ReflectedMethod(impl.GetCanonMethodTarget(CanonicalFormKind.Specific)),
                    factory.ReflectedMethod(decl.GetCanonMethodTarget(CanonicalFormKind.Specific)),
                    "Virtual method declaration is reflectable"));
            }
        }

        protected override void GetDependenciesDueToMethodCodePresenceInternal(ref DependencyList dependencies, NodeFactory factory, MethodDesc method, MethodIL methodIL)
        {
            bool scanReflection = (_generationOptions & UsageBasedMetadataGenerationOptions.ReflectionILScanning) != 0;

            Debug.Assert(methodIL != null || method.IsAbstract || method.IsPInvoke || method.IsInternalCall);

            if (scanReflection)
            {
                if (methodIL != null && Dataflow.ReflectionMethodBodyScanner.RequiresReflectionMethodBodyScannerForMethodBody(FlowAnnotations, method))
                {
                    AddDataflowDependency(ref dependencies, factory, methodIL, "Method has annotated parameters");
                }

                if (method.IsStaticConstructor)
                {
                    if (DiagnosticUtilities.TryGetRequiresAttribute(method, DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out _))
                        Logger.LogWarning(new MessageOrigin(method), DiagnosticId.RequiresUnreferencedCodeOnStaticConstructor, method.GetDisplayName());

                    if (DiagnosticUtilities.TryGetRequiresAttribute(method, DiagnosticUtilities.RequiresDynamicCodeAttribute, out _))
                        Logger.LogWarning(new MessageOrigin(method), DiagnosticId.RequiresDynamicCodeOnStaticConstructor, method.GetDisplayName());

                    if (DiagnosticUtilities.TryGetRequiresAttribute(method, DiagnosticUtilities.RequiresAssemblyFilesAttribute, out _))
                        Logger.LogWarning(new MessageOrigin(method), DiagnosticId.RequiresAssemblyFilesOnStaticConstructor, method.GetDisplayName());
                }
            }

            if (method.GetTypicalMethodDefinition() is Internal.TypeSystem.Ecma.EcmaMethod ecmaMethod)
            {
                DynamicDependencyAttributesOnEntityNode.AddDependenciesDueToDynamicDependencyAttribute(ref dependencies, factory, ecmaMethod);
            }

            // Presence of code might trigger the reflectability dependencies.
            if ((_generationOptions & UsageBasedMetadataGenerationOptions.CreateReflectableArtifacts) != 0)
            {
                GetDependenciesDueToReflectability(ref dependencies, factory, method);
            }
        }

        public override void GetConditionalDependenciesDueToMethodCodePresence(ref CombinedDependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            MethodDesc typicalMethod = method.GetTypicalMethodDefinition();

            // Ensure methods with genericness have the same reflectability by injecting a conditional dependency.
            if ((_generationOptions & UsageBasedMetadataGenerationOptions.CreateReflectableArtifacts) == 0
                && method != typicalMethod)
            {
                dependencies ??= new CombinedDependencyList();
                dependencies.Add(new DependencyNodeCore<NodeFactory>.CombinedDependencyListEntry(
                    factory.ReflectedMethod(method), factory.ReflectedMethod(typicalMethod), "Reflectability of methods is same across genericness"));
            }
        }

        public override void GetDependenciesDueToVirtualMethodReflectability(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            if ((_generationOptions & UsageBasedMetadataGenerationOptions.CreateReflectableArtifacts) != 0)
            {
                // If we have a use of an abstract method, GetDependenciesDueToReflectability is not going to see the method
                // as being used since there's no body. We inject a dependency on a new node that serves as a logical method body
                // for the metadata manager. Metadata manager treats that node the same as a body.
                if (method.IsAbstract && GetMetadataCategory(method) != 0)
                {
                    dependencies ??= new DependencyList();
                    dependencies.Add(factory.ReflectedMethod(method), "Abstract reflectable method");
                }
            }
        }

        protected override IEnumerable<FieldDesc> GetFieldsWithRuntimeMapping()
        {
            return _fieldsWithRuntimeMapping;
        }

        public override IEnumerable<ModuleDesc> GetCompilationModulesWithMetadata()
        {
            return _modulesWithMetadata;
        }

        private IEnumerable<TypeDesc> GetTypesWithRuntimeMapping()
        {
            // All constructed types that are not blocked get runtime mapping
            foreach (var constructedType in GetTypesWithEETypes())
            {
                if (!IsReflectionBlocked(constructedType))
                    yield return constructedType;
            }
        }

        public override void GetDependenciesDueToAccess(ref DependencyList dependencies, NodeFactory factory, MethodIL methodIL, FieldDesc writtenField)
        {
            bool scanReflection = (_generationOptions & UsageBasedMetadataGenerationOptions.ReflectionILScanning) != 0;
            if (scanReflection && Dataflow.ReflectionMethodBodyScanner.RequiresReflectionMethodBodyScannerForAccess(FlowAnnotations, writtenField))
            {
                AddDataflowDependency(ref dependencies, factory, methodIL, "Access to interesting field");
            }

            if ((_generationOptions & UsageBasedMetadataGenerationOptions.CreateReflectableArtifacts) != 0
                && !IsReflectionBlocked(writtenField))
            {
                FieldDesc fieldToReport = writtenField;

                // The field could be on something odd like Foo<__Canon, object>. Normalize to Foo<__Canon, __Canon>.
                DefType fieldOwningType = writtenField.OwningType;
                if (fieldOwningType.IsCanonicalSubtype(CanonicalFormKind.Specific))
                {
                    TypeDesc fieldOwningTypeNormalized = fieldOwningType.NormalizeInstantiation();
                    if (fieldOwningType != fieldOwningTypeNormalized)
                    {
                        fieldToReport = factory.TypeSystemContext.GetFieldForInstantiatedType(
                            writtenField.GetTypicalFieldDefinition(),
                            (InstantiatedType)fieldOwningTypeNormalized);
                    }
                }

                dependencies ??= new DependencyList();
                dependencies.Add(factory.ReflectedField(fieldToReport), "Use of a field");
            }

            if (writtenField.GetTypicalFieldDefinition() is EcmaField ecmaField)
            {
                DynamicDependencyAttributesOnEntityNode.AddDependenciesDueToDynamicDependencyAttribute(ref dependencies, factory, ecmaField);
            }
        }

        public override void GetDependenciesDueToAccess(ref DependencyList dependencies, NodeFactory factory, MethodIL methodIL, MethodDesc calledMethod)
        {
            bool scanReflection = (_generationOptions & UsageBasedMetadataGenerationOptions.ReflectionILScanning) != 0;
            if (scanReflection && Dataflow.ReflectionMethodBodyScanner.RequiresReflectionMethodBodyScannerForCallSite(FlowAnnotations, calledMethod))
            {
                AddDataflowDependency(ref dependencies, factory, methodIL, "Call to interesting method");
            }
        }

        public override DependencyList GetDependenciesForCustomAttribute(NodeFactory factory, MethodDesc attributeCtor, CustomAttributeValue decodedValue, TypeSystemEntity parent)
        {
            bool scanReflection = (_generationOptions & UsageBasedMetadataGenerationOptions.ReflectionILScanning) != 0;
            if (scanReflection)
            {
                return (new AttributeDataFlow(Logger, factory, FlowAnnotations, new Logging.MessageOrigin(parent))).ProcessAttributeDataflow(attributeCtor, decodedValue);
            }

            return null;
        }

        public bool GeneratesAttributeMetadata(TypeDesc attributeType)
        {
            var ecmaType = attributeType.GetTypeDefinition() as EcmaType;
            if (ecmaType != null)
            {
                var moduleInfo = _linkAttributesHashTable.GetOrCreateValue(ecmaType.EcmaModule);
                return !moduleInfo.RemovedAttributes.Contains(ecmaType);
            }

            return true;
        }

        public override void NoteOverridingMethod(MethodDesc baseMethod, MethodDesc overridingMethod)
        {
            baseMethod = baseMethod.GetTypicalMethodDefinition();
            overridingMethod = overridingMethod.GetTypicalMethodDefinition();

            if (baseMethod == overridingMethod)
                return;

            bool baseMethodTypeIsInterface = baseMethod.OwningType.IsInterface;
            foreach (var requiresAttribute in _requiresAttributeMismatchNameAndId)
            {
                // We validate that the various dataflow/Requires* annotations are consistent across virtual method overrides
                if (HasMismatchingAttributes(baseMethod, overridingMethod, requiresAttribute.AttributeName))
                {
                    string overridingMethodName = overridingMethod.GetDisplayName();
                    string baseMethodName = baseMethod.GetDisplayName();
                    string message = MessageFormat.FormatRequiresAttributeMismatch(overridingMethod.DoesMethodRequire(requiresAttribute.AttributeName, out _),
                        baseMethodTypeIsInterface, requiresAttribute.AttributeName, overridingMethodName, baseMethodName);

                    Logger.LogWarning(overridingMethod, requiresAttribute.Id, message);
                }
            }

            bool baseMethodRequiresDataflow = FlowAnnotations.RequiresVirtualMethodDataflowAnalysis(baseMethod);
            bool overridingMethodRequiresDataflow = FlowAnnotations.RequiresVirtualMethodDataflowAnalysis(overridingMethod);
            if (baseMethodRequiresDataflow || overridingMethodRequiresDataflow)
            {
                FlowAnnotations.ValidateMethodAnnotationsAreSame(overridingMethod, baseMethod);
            }
        }

        public static bool HasMismatchingAttributes(MethodDesc baseMethod, MethodDesc overridingMethod, string requiresAttributeName)
        {
            bool baseMethodCreatesRequirement = baseMethod.DoesMethodRequire(requiresAttributeName, out _);
            bool overridingMethodCreatesRequirement = overridingMethod.DoesMethodRequire(requiresAttributeName, out _);
            bool baseMethodFulfillsRequirement = baseMethod.IsInRequiresScope(requiresAttributeName);
            bool overridingMethodFulfillsRequirement = overridingMethod.IsInRequiresScope(requiresAttributeName);
            return (baseMethodCreatesRequirement && !overridingMethodFulfillsRequirement) || (overridingMethodCreatesRequirement && !baseMethodFulfillsRequirement);
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

            foreach (var fieldWithRuntimeMapping in _fieldsWithRuntimeMapping)
            {
                reflectableFields[fieldWithRuntimeMapping] |= MetadataCategory.RuntimeMapping;

                // Also set the description bit if the definition is getting metadata.
                FieldDesc typicalField = fieldWithRuntimeMapping.GetTypicalFieldDefinition();
                if (fieldWithRuntimeMapping != typicalField &&
                    (reflectableFields[typicalField] & MetadataCategory.Description) != 0)
                {
                    reflectableFields[fieldWithRuntimeMapping] |= MetadataCategory.Description;
                }
            }

            var rootedCctorContexts = new List<MetadataType>();
            foreach (NonGCStaticsNode cctorContext in GetCctorContextMapping())
            {
                // If we generated a static constructor and the owning type, this might be something
                // that gets fed to RuntimeHelpers.RunClassConstructor. RunClassConstructor
                // also works on reflection blocked types and there is a possibility that we
                // wouldn't have generated the cctor otherwise.
                //
                // This is a heuristic and we'll possibly root more cctor contexts than
                // strictly necessary, but it's not worth introducing a new node type
                // in the compiler just so we can propagate this knowledge from dataflow analysis
                // (that detects RunClassConstructor usage) and this spot.
                if (!TypeGeneratesEEType(cctorContext.Type))
                    continue;

                rootedCctorContexts.Add(cctorContext.Type);
            }

            return new AnalysisBasedMetadataManager(
                _typeSystemContext, _blockingPolicy, _resourceBlockingPolicy, _metadataLogFile, _stackTraceEmissionPolicy, _dynamicInvokeThunkGenerationPolicy,
                _modulesWithMetadata, _typesWithForcedEEType, reflectableTypes.ToEnumerable(), reflectableMethods.ToEnumerable(),
                reflectableFields.ToEnumerable(), _customAttributesWithMetadata, rootedCctorContexts, _options);
        }

        private void AddDataflowDependency(ref DependencyList dependencies, NodeFactory factory, MethodIL methodIL, string reason)
        {
            if (ShouldSkipDataflowForMethod(methodIL))
                return;

            MethodIL methodILDefinition = methodIL.GetMethodILDefinition();
            if (FlowAnnotations.CompilerGeneratedState.TryGetUserMethodForCompilerGeneratedMember(methodILDefinition.OwningMethod, out var userMethod))
            {
                Debug.Assert(userMethod != methodILDefinition.OwningMethod);

                // It is possible that this will try to add the DatadlowAnalyzedMethod node multiple times for the same method
                // but that's OK since the node factory will only add actually one node.
                methodILDefinition = FlowAnnotations.ILProvider.GetMethodIL(userMethod);
                if (ShouldSkipDataflowForMethod(methodILDefinition))
                    return;
            }

            // Data-flow (reflection scanning) for compiler-generated methods will happen as part of the
            // data-flow scan of the user-defined method which uses this compiler-generated method.
            if (CompilerGeneratedState.IsNestedFunctionOrStateMachineMember(methodILDefinition.OwningMethod))
                return;

            // Currently we add data flow for reasons which are not directly related to data flow
            // or don't need full data flow processing.
            // The prime example is that we run data flow for any method which contains an access
            // to a member on a generic type with annotated generic parameters. These are relatively common
            // as there are quite a few types which use the new/struct/unmanaged constraints (which are all treated as annotations).
            // Technically we don't need to run data flow only because of this, since the result of analysis
            // will not depend on stack modeling and of the other data flow functionality.
            // See https://github.com/dotnet/runtime/issues/82603 for more details and some ideas.

            dependencies ??= new DependencyList();
            dependencies.Add(factory.DataflowAnalyzedMethod(methodILDefinition), reason);

            // Some MethodIL implementations can't/don't provide the method definition version of the IL
            // for example if the method is a stub generated by the compiler, the IL may look very different
            // between each instantiation (and definition version may not even make sense).
            // We will skip data flow for such methods for now, since currently dataflow can't handle instantiated
            // generics.
            static bool ShouldSkipDataflowForMethod(MethodIL method)
                => method.GetMethodILDefinition() == method &&
                method.OwningMethod.GetTypicalMethodDefinition() != method.OwningMethod;
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
                // Generate the forwarder only if we generated the target type.
                // If the target type is in a different compilation group, assume we generated it there.
                var targetType = (MetadataType)module.GetObject(exportedTypeHandle, NotFoundBehavior.ReturnNull);
                if (targetType == null)
                {
                    // No harm in generating a forwarder that didn't resolve.
                    // We'll get matching behavior at runtime.
                    return true;
                }
                return GeneratesMetadata(targetType) || !_factory.CompilationModuleGroup.ContainsType(targetType);
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

        private sealed class LinkAttributesHashTable : LockFreeReaderHashtable<EcmaModule, AssemblyFeatureInfo>
        {
            private readonly Dictionary<string, bool> _switchValues;
            private readonly Logger _logger;

            public LinkAttributesHashTable(Logger logger, Dictionary<string, bool> switchValues)
            {
                _logger = logger;
                _switchValues = switchValues;
            }

            protected override bool CompareKeyToValue(EcmaModule key, AssemblyFeatureInfo value) => key == value.Module;
            protected override bool CompareValueToValue(AssemblyFeatureInfo value1, AssemblyFeatureInfo value2) => value1.Module == value2.Module;
            protected override int GetKeyHashCode(EcmaModule key) => key.GetHashCode();
            protected override int GetValueHashCode(AssemblyFeatureInfo value) => value.Module.GetHashCode();

            protected override AssemblyFeatureInfo CreateValueFromKey(EcmaModule key)
            {
                return new AssemblyFeatureInfo(key, _logger, _switchValues);
            }
        }

        private sealed class AssemblyFeatureInfo
        {
            public EcmaModule Module { get; }

            public HashSet<TypeDesc> RemovedAttributes { get; }

            public AssemblyFeatureInfo(EcmaModule module, Logger logger, IReadOnlyDictionary<string, bool> featureSwitchValues)
            {
                Module = module;
                RemovedAttributes = new HashSet<TypeDesc>();

                // System.Private.CorLib has a special functionality that could delete an attribute in all modules.
                // In order to get the set of attributes that need to be removed the modules need collect both the
                // set of attributes in it's embedded XML file and the set inside System.Private.CorLib embedded
                // XML file
                ParseLinkAttributesXml(module, logger, featureSwitchValues, globalAttributeRemoval: false);
                ParseLinkAttributesXml(module, logger, featureSwitchValues, globalAttributeRemoval: true);
            }

            public void ParseLinkAttributesXml(EcmaModule module, Logger logger, IReadOnlyDictionary<string, bool> featureSwitchValues, bool globalAttributeRemoval)
            {
                EcmaModule xmlModule = globalAttributeRemoval ? (EcmaModule)module.Context.SystemModule : module;
                PEMemoryBlock resourceDirectory = xmlModule.PEReader.GetSectionData(xmlModule.PEReader.PEHeaders.CorHeader.ResourcesDirectory.RelativeVirtualAddress);

                foreach (var resourceHandle in xmlModule.MetadataReader.ManifestResources)
                {
                    ManifestResource resource = xmlModule.MetadataReader.GetManifestResource(resourceHandle);

                    // Don't try to process linked resources or resources in other assemblies
                    if (!resource.Implementation.IsNil)
                    {
                        continue;
                    }

                    string resourceName = xmlModule.MetadataReader.GetString(resource.Name);
                    if (resourceName == "ILLink.LinkAttributes.xml")
                    {
                        BlobReader reader = resourceDirectory.GetReader((int)resource.Offset, resourceDirectory.Length - (int)resource.Offset);
                        int length = (int)reader.ReadUInt32();

                        UnmanagedMemoryStream ms;
                        unsafe
                        {
                            ms = new UnmanagedMemoryStream(reader.CurrentPointer, length);
                        }

                        RemovedAttributes.UnionWith(LinkAttributesReader.GetRemovedAttributes(logger, xmlModule.Context, ms, resource, module, "resource " + resourceName + " in " + module.ToString(), featureSwitchValues, globalAttributeRemoval));
                    }
                }
            }
        }

        internal sealed class LinkAttributesReader : ProcessLinkerXmlBase
        {
            private readonly HashSet<TypeDesc> _removedAttributes = new();

            public LinkAttributesReader(Logger logger, TypeSystemContext context, Stream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues, bool globalAttributeRemoval)
                : base(logger, context, documentStream, resource, resourceAssembly, xmlDocumentLocation, featureSwitchValues, globalAttributeRemoval)
            {
            }

            private static bool IsRemoveAttributeInstances(string attributeName) => attributeName == "RemoveAttributeInstances" || attributeName == "RemoveAttributeInstancesAttribute";

            private void ProcessAttribute(TypeDesc type, XPathNavigator nav)
            {
                string internalValue = GetAttribute(nav, "internal");
                if (!string.IsNullOrEmpty(internalValue))
                {
                    if (!IsRemoveAttributeInstances(internalValue) || !nav.IsEmptyElement)
                    {
                        LogWarning(nav, DiagnosticId.UnrecognizedInternalAttribute, internalValue);
                    }
                    if (IsRemoveAttributeInstances(internalValue) && nav.IsEmptyElement)
                    {
                        _removedAttributes.Add(type);
                    }
                }
            }

            public static HashSet<TypeDesc> GetRemovedAttributes(Logger logger, TypeSystemContext context, Stream documentStream, ManifestResource resource, ModuleDesc resourceAssembly, string xmlDocumentLocation, IReadOnlyDictionary<string, bool> featureSwitchValues, bool globalAttributeRemoval)
            {
                var rdr = new LinkAttributesReader(logger, context, documentStream, resource, resourceAssembly, xmlDocumentLocation, featureSwitchValues, globalAttributeRemoval);
                rdr.ProcessXml(false);
                return rdr._removedAttributes;
            }

            protected override AllowedAssemblies AllowedAssemblySelector
            {
                get
                {
                    if (_owningModule?.Assembly == null)
                        return AllowedAssemblies.AllAssemblies;

                    // Corelib XML may contain assembly wildcard to support compiler-injected attribute types
                    if (_owningModule?.Assembly == _context.SystemModule)
                        return AllowedAssemblies.AllAssemblies;

                    return AllowedAssemblies.ContainingAssembly;
                }
            }

            protected override void ProcessAssembly(ModuleDesc assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
            {
                ProcessTypes(assembly, nav, warnOnUnresolvedTypes);
            }

            protected override void ProcessType(TypeDesc type, XPathNavigator nav)
            {
                foreach (XPathNavigator child in nav.SelectChildren("attribute", ""))
                {
                    ProcessAttribute(type, child);
                }
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
        /// Consider all native artifacts (native method bodies, etc) visible from reflection.
        /// </summary>
        CreateReflectableArtifacts = 8,

        /// <summary>
        /// Fully root used assemblies that are not marked IsTrimmable in metadata.
        /// </summary>
        RootDefaultAssemblies = 16,
    }
}

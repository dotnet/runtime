// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Metadata;

using Internal.TypeSystem.Ecma;

using ILCompiler.DependencyAnalysisFramework;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Class that aids in interning nodes of the dependency graph.
    /// </summary>
    public sealed class NodeFactory
    {
        IReadOnlySet<string> _trimAssemblies { get; }
        public TrimmerSettings Settings { get; }

        public NodeFactory(IEnumerable<string> trimAssemblies, TrimmerSettings settings)
        {
            _trimAssemblies = new HashSet<string>(trimAssemblies);
            Settings = settings;
        }

        /// <summary>
        /// Given a module-qualified token, get the dependency graph node that represent the token.
        /// </summary>
        public TokenBasedNode GetNodeForToken(EcmaModule module, EntityHandle handle)
        {
            switch (handle.Kind)
            {
                case HandleKind.TypeReference:
                    return TypeReference(module, (TypeReferenceHandle)handle);
                case HandleKind.TypeDefinition:
                    return TypeDefinition(module, (TypeDefinitionHandle)handle);
                case HandleKind.FieldDefinition:
                    return FieldDefinition(module, (FieldDefinitionHandle)handle);
                case HandleKind.MethodDefinition:
                    return MethodDefinition(module, (MethodDefinitionHandle)handle);
                case HandleKind.Parameter:
                    return Parameter(module, (ParameterHandle)handle);
                case HandleKind.InterfaceImplementation:
                    throw new NotImplementedException();
                case HandleKind.MemberReference:
                    return MemberReference(module, (MemberReferenceHandle)handle);
                case HandleKind.Constant:
                    return Constant(module, (ConstantHandle)handle);
                case HandleKind.CustomAttribute:
                    return CustomAttribute(module, (CustomAttributeHandle)handle);
                case HandleKind.DeclarativeSecurityAttribute:
                    throw new NotImplementedException();
                case HandleKind.StandaloneSignature:
                    return StandaloneSignature(module, (StandaloneSignatureHandle)handle);
                case HandleKind.EventDefinition:
                    return EventDefinition(module, (EventDefinitionHandle)handle);
                case HandleKind.PropertyDefinition:
                    return PropertyDefinition(module, (PropertyDefinitionHandle)handle);
                case HandleKind.MethodImplementation:
                    return MethodImplementation(module, (MethodImplementationHandle)handle);
                case HandleKind.ModuleReference:
                    return ModuleReference(module, (ModuleReferenceHandle)handle);
                case HandleKind.TypeSpecification:
                    return TypeSpecification(module, (TypeSpecificationHandle)handle);
                case HandleKind.AssemblyReference:
                    throw new InvalidOperationException("Assembly references are not represented by token-based nodes.");
                case HandleKind.AssemblyFile:
                    throw new NotImplementedException();
                case HandleKind.ExportedType:
                    throw new NotImplementedException();
                case HandleKind.ManifestResource:
                    return ManifestResource(module, (ManifestResourceHandle)handle);
                case HandleKind.GenericParameter:
                    return GenericParameter(module, (GenericParameterHandle)handle);
                case HandleKind.MethodSpecification:
                    return MethodSpecification(module, (MethodSpecificationHandle)handle);
                case HandleKind.GenericParameterConstraint:
                    return GenericParameterConstraint(module, (GenericParameterConstraintHandle)handle);
                default:
                    throw new NotImplementedException();
            }
        }

        NodeCache<EcmaType, ConstructedTypeNode> _constructedTypes = new NodeCache<EcmaType, ConstructedTypeNode>(key
            => new ConstructedTypeNode(key));
        public ConstructedTypeNode ConstructedType(EcmaType type)
        {
            return _constructedTypes.GetOrAdd(type);
        }

        NodeCache<EcmaMethod, VirtualMethodUseNode> _virtualMethodUses = new NodeCache<EcmaMethod, VirtualMethodUseNode>(key
            => new VirtualMethodUseNode(key));
        public VirtualMethodUseNode VirtualMethodUse(EcmaMethod method)
        {
            return _virtualMethodUses.GetOrAdd(method);
        }

        NodeCache<EcmaType, InterfaceUseNode> _interfaceUses = new NodeCache<EcmaType, InterfaceUseNode>(key
            => new InterfaceUseNode(key));
        public InterfaceUseNode InterfaceUse(EcmaType type)
        {
            return _interfaceUses.GetOrAdd(type);
        }

        NodeCache<HandleKey<TypeReferenceHandle>, TypeReferenceNode> _typeReferences
            = new NodeCache<HandleKey<TypeReferenceHandle>, TypeReferenceNode>(key
                => new TypeReferenceNode(key.Module, key.Handle));
        public TypeReferenceNode TypeReference(EcmaModule module, TypeReferenceHandle handle)
        {
            return _typeReferences.GetOrAdd(new HandleKey<TypeReferenceHandle>(module, handle));
        }

        NodeCache<HandleKey<TypeDefinitionHandle>, TypeDefinitionNode> _typeDefinitions
            = new NodeCache<HandleKey<TypeDefinitionHandle>, TypeDefinitionNode>(key
                => new TypeDefinitionNode(key.Module, key.Handle));
        public TypeDefinitionNode TypeDefinition(EcmaModule module, TypeDefinitionHandle handle)
        {
            return _typeDefinitions.GetOrAdd(new HandleKey<TypeDefinitionHandle>(module, handle));
        }

        NodeCache<HandleKey<MethodImplementationHandle>, MethodImplementationNode> _methodImplementations
            = new NodeCache<HandleKey<MethodImplementationHandle>, MethodImplementationNode>(key
                => new MethodImplementationNode(key.Module, key.Handle));
        public MethodImplementationNode MethodImplementation(EcmaModule module, MethodImplementationHandle handle)
        {
            return _methodImplementations.GetOrAdd(new HandleKey<MethodImplementationHandle>(module, handle));
        }

        NodeCache<HandleKey<FieldDefinitionHandle>, FieldDefinitionNode> _fieldDefinitions
            = new NodeCache<HandleKey<FieldDefinitionHandle>, FieldDefinitionNode>(key
                => new FieldDefinitionNode(key.Module, key.Handle));
        public FieldDefinitionNode FieldDefinition(EcmaModule module, FieldDefinitionHandle handle)
        {
            return _fieldDefinitions.GetOrAdd(new HandleKey<FieldDefinitionHandle>(module, handle));
        }

        NodeCache<HandleKey<MethodDefinitionHandle>, MethodDefinitionNode> _methodDefinitions
            = new NodeCache<HandleKey<MethodDefinitionHandle>, MethodDefinitionNode>(key
                => new MethodDefinitionNode(key.Module, key.Handle));
        public MethodDefinitionNode MethodDefinition(EcmaModule module, MethodDefinitionHandle handle)
        {
            return _methodDefinitions.GetOrAdd(new HandleKey<MethodDefinitionHandle>(module, handle));
        }

        NodeCache<HandleKey<MethodDefinitionHandle>, MethodBodyNode> _methodBodies
            = new NodeCache<HandleKey<MethodDefinitionHandle>, MethodBodyNode>(key
                => new MethodBodyNode(key.Module, key.Handle));
        public MethodBodyNode MethodBody(EcmaModule module, MethodDefinitionHandle handle)
        {
            return _methodBodies.GetOrAdd(new HandleKey<MethodDefinitionHandle>(module, handle));
        }

        NodeCache<HandleKey<MemberReferenceHandle>, MemberReferenceNode> _memberReferences
            = new NodeCache<HandleKey<MemberReferenceHandle>, MemberReferenceNode>(key
                => new MemberReferenceNode(key.Module, key.Handle));
        public MemberReferenceNode MemberReference(EcmaModule module, MemberReferenceHandle handle)
        {
            return _memberReferences.GetOrAdd(new HandleKey<MemberReferenceHandle>(module, handle));
        }

        NodeCache<HandleKey<ParameterHandle>, ParameterNode> _parameters
           = new NodeCache<HandleKey<ParameterHandle>, ParameterNode>(key
               => new ParameterNode(key.Module, key.Handle));
        public ParameterNode Parameter(EcmaModule module, ParameterHandle handle)
        {
            return _parameters.GetOrAdd(new HandleKey<ParameterHandle>(module, handle));
        }

        NodeCache<HandleKey<ConstantHandle>, ConstantNode> _constants
            = new NodeCache<HandleKey<ConstantHandle>, ConstantNode>(key
                => new ConstantNode(key.Module, key.Handle));
        public ConstantNode Constant(EcmaModule module, ConstantHandle handle)
        {
            return _constants.GetOrAdd(new HandleKey<ConstantHandle>(module, handle));
        }

        NodeCache<HandleKey<CustomAttributeHandle>, CustomAttributeNode> _customAttributes
            = new NodeCache<HandleKey<CustomAttributeHandle>, CustomAttributeNode>(key
                => new CustomAttributeNode(key.Module, key.Handle));
        public CustomAttributeNode CustomAttribute(EcmaModule module, CustomAttributeHandle handle)
        {
            return _customAttributes.GetOrAdd(new HandleKey<CustomAttributeHandle>(module, handle));
        }

        NodeCache<HandleKey<StandaloneSignatureHandle>, StandaloneSignatureNode> _standaloneSignatures
            = new NodeCache<HandleKey<StandaloneSignatureHandle>, StandaloneSignatureNode>(key
                => new StandaloneSignatureNode(key.Module, key.Handle));
        public StandaloneSignatureNode StandaloneSignature(EcmaModule module, StandaloneSignatureHandle handle)
        {
            return _standaloneSignatures.GetOrAdd(new HandleKey<StandaloneSignatureHandle>(module, handle));
        }

        NodeCache<HandleKey<EventDefinitionHandle>, EventDefinitionNode> _eventDefinitions
            = new NodeCache<HandleKey<EventDefinitionHandle>, EventDefinitionNode>(key
                => new EventDefinitionNode(key.Module, key.Handle));
        public EventDefinitionNode EventDefinition(EcmaModule module, EventDefinitionHandle handle)
        {
            return _eventDefinitions.GetOrAdd(new HandleKey<EventDefinitionHandle>(module, handle));
        }

        NodeCache<HandleKey<PropertyDefinitionHandle>, PropertyDefinitionNode> _propertyDefinitions
                = new NodeCache<HandleKey<PropertyDefinitionHandle>, PropertyDefinitionNode>(key
                    => new PropertyDefinitionNode(key.Module, key.Handle));
        public PropertyDefinitionNode PropertyDefinition(EcmaModule module, PropertyDefinitionHandle handle)
        {
            return _propertyDefinitions.GetOrAdd(new HandleKey<PropertyDefinitionHandle>(module, handle));
        }

        NodeCache<EcmaModule, ModuleDefinitionNode> _moduleDefinitions
            = new NodeCache<EcmaModule, ModuleDefinitionNode>(
                key => new ModuleDefinitionNode(key));
        public ModuleDefinitionNode ModuleDefinition(EcmaModule module)
        {
            return _moduleDefinitions.GetOrAdd(module);
        }

        NodeCache<HandleKey<MethodSpecificationHandle>, MethodSpecificationNode> _methodSpecifications
            = new NodeCache<HandleKey<MethodSpecificationHandle>, MethodSpecificationNode>(key
                => new MethodSpecificationNode(key.Module, key.Handle));
        public MethodSpecificationNode MethodSpecification(EcmaModule module, MethodSpecificationHandle handle)
        {
            return _methodSpecifications.GetOrAdd(new HandleKey<MethodSpecificationHandle>(module, handle));
        }

        NodeCache<HandleKey<TypeSpecificationHandle>, TypeSpecificationNode> _typeSpecifications
            = new NodeCache<HandleKey<TypeSpecificationHandle>, TypeSpecificationNode>(key
                => new TypeSpecificationNode(key.Module, key.Handle));
        public TypeSpecificationNode TypeSpecification(EcmaModule module, TypeSpecificationHandle handle)
        {
            return _typeSpecifications.GetOrAdd(new HandleKey<TypeSpecificationHandle>(module, handle));
        }

        NodeCache<EcmaModule, AssemblyDefinitionNode> _assemblyDefinitions
            = new NodeCache<EcmaModule, AssemblyDefinitionNode>(
                key => new AssemblyDefinitionNode(key));
        public AssemblyDefinitionNode AssemblyDefinition(EcmaModule module)
        {
            return _assemblyDefinitions.GetOrAdd(module);
        }

        NodeCache<HandleKey<AssemblyReferenceValue>, AssemblyReferenceNode> _assemblyReferences
            = new NodeCache<HandleKey<AssemblyReferenceValue>, AssemblyReferenceNode>(key
                => new AssemblyReferenceNode(key.Module, key.Handle.Reference));
        public AssemblyReferenceNode AssemblyReference(EcmaModule module, EcmaAssembly reference)
        {
            return _assemblyReferences.GetOrAdd(new HandleKey<AssemblyReferenceValue>(module, new AssemblyReferenceValue(reference)));
        }

        NodeCache<HandleKey<ModuleReferenceHandle>, ModuleReferenceNode> _moduleReferences
            = new NodeCache<HandleKey<ModuleReferenceHandle>, ModuleReferenceNode>(key
                => new ModuleReferenceNode(key.Module, key.Handle));
        public ModuleReferenceNode ModuleReference(EcmaModule module, ModuleReferenceHandle handle)
        {
            return _moduleReferences.GetOrAdd(new HandleKey<ModuleReferenceHandle>(module, handle));
        }

        NodeCache<HandleKey<ManifestResourceHandle>, ManifestResourceNode> _manifestResources
           = new NodeCache<HandleKey<ManifestResourceHandle>, ManifestResourceNode>(key
               => new ManifestResourceNode(key.Module, key.Handle));
        public ManifestResourceNode ManifestResource(EcmaModule module, ManifestResourceHandle handle)
        {
            return _manifestResources.GetOrAdd(new HandleKey<ManifestResourceHandle>(module, handle));
        }

        NodeCache<HandleKey<GenericParameterHandle>, GenericParameterNode> _genericParameters
           = new NodeCache<HandleKey<GenericParameterHandle>, GenericParameterNode>(key
               => new GenericParameterNode(key.Module, key.Handle));
        public GenericParameterNode GenericParameter(EcmaModule module, GenericParameterHandle handle)
        {
            return _genericParameters.GetOrAdd(new HandleKey<GenericParameterHandle>(module, handle));
        }

        NodeCache<HandleKey<GenericParameterConstraintHandle>, GenericParameterConstraintNode> _genericParameterConstraints
           = new NodeCache<HandleKey<GenericParameterConstraintHandle>, GenericParameterConstraintNode>(key
               => new GenericParameterConstraintNode(key.Module, key.Handle));
        public GenericParameterConstraintNode GenericParameterConstraint(EcmaModule module, GenericParameterConstraintHandle handle)
        {
            return _genericParameterConstraints.GetOrAdd(new HandleKey<GenericParameterConstraintHandle>(module, handle));
        }

        public bool IsModuleTrimmed(EcmaModule module)
        {
            return _trimAssemblies.Contains(module.Assembly.GetName().Name);
        }

        public bool IsModuleTrimmedInLibraryMode()
        {
            return Settings.LibraryMode;
        }

        private struct HandleKey<T> : IEquatable<HandleKey<T>> where T : struct, IEquatable<T>
        {
            public readonly EcmaModule Module;
            public readonly T Handle;
            public HandleKey(EcmaModule module, T handle)
                => (Module, Handle) = (module, handle);

            public override bool Equals(object obj) => obj is HandleKey<T> key && Equals(key);
            public bool Equals(HandleKey<T> other)
            {
                return Handle.Equals(other.Handle)
                    && Module == other.Module;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Handle, Module.MetadataReader);
            }
        }

        private struct NodeCache<TKey, TValue>
        {
            private Func<TKey, TValue> _creator;
#if SINGLE_THREADED
            private Dictionary<TKey, TValue> _cache;
#else
            private ConcurrentDictionary<TKey, TValue> _cache;
#endif

            public NodeCache(Func<TKey, TValue> creator)
            {
                _creator = creator;
#if SINGLE_THREADED
                _cache = new Dictionary<TKey, TValue>();
#else
                _cache = new ConcurrentDictionary<TKey, TValue>();
#endif
            }

            public TValue GetOrAdd(TKey key)
            {
#if SINGLE_THREADED
                if (_cache.TryGetValue(key, out var value))
                    return value;

                _cache[key] = value = _creator(key);
                return value;
#else
                return _cache.GetOrAdd(key, _creator);
#endif
            }
        }
    }
}

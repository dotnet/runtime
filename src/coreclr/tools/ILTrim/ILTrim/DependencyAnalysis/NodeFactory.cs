// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Metadata;

using ILCompiler.DependencyAnalysisFramework;

namespace ILTrim.DependencyAnalysis
{
    /// <summary>
    /// Class that aids in interning nodes of the dependency graph.
    /// </summary>
    public sealed class NodeFactory
    {
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
                    throw new NotImplementedException();
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
                    throw new NotImplementedException();
                case HandleKind.PropertyDefinition:
                    throw new NotImplementedException();
                case HandleKind.MethodImplementation:
                    throw new NotImplementedException();
                case HandleKind.ModuleReference:
                    throw new NotImplementedException();
                case HandleKind.TypeSpecification:
                    return TypeSpecification(module, (TypeSpecificationHandle)handle);
                case HandleKind.AssemblyReference:
                    return AssemblyReference(module, (AssemblyReferenceHandle)handle);
                case HandleKind.AssemblyFile:
                    throw new NotImplementedException();
                case HandleKind.ExportedType:
                    throw new NotImplementedException();
                case HandleKind.ManifestResource:
                    throw new NotImplementedException();
                case HandleKind.GenericParameter:
                    throw new NotImplementedException();
                case HandleKind.MethodSpecification:
                    return MethodSpecification(module, (MethodSpecificationHandle)handle);
                case HandleKind.GenericParameterConstraint:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
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

        NodeCache<HandleKey<AssemblyReferenceHandle>, AssemblyReferenceNode> _assemblyReferences
            = new NodeCache<HandleKey<AssemblyReferenceHandle>, AssemblyReferenceNode>(key
                => new AssemblyReferenceNode(key.Module, key.Handle));
        public AssemblyReferenceNode AssemblyReference(EcmaModule module, AssemblyReferenceHandle handle)
        {
            return _assemblyReferences.GetOrAdd(new HandleKey<AssemblyReferenceHandle>(module, handle));
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
    }
}

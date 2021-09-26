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
                    throw new NotImplementedException();
                case HandleKind.TypeDefinition:
                    return TypeDefinition(module, (TypeDefinitionHandle)handle);
                case HandleKind.FieldDefinition:
                    throw new NotImplementedException();
                case HandleKind.MethodDefinition:
                    return MethodDefinition(module, (MethodDefinitionHandle)handle);
                case HandleKind.Parameter:
                    throw new NotImplementedException();
                case HandleKind.InterfaceImplementation:
                    throw new NotImplementedException();
                case HandleKind.MemberReference:
                    throw new NotImplementedException();
                case HandleKind.Constant:
                    throw new NotImplementedException();
                case HandleKind.CustomAttribute:
                    throw new NotImplementedException();
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
                    throw new NotImplementedException();
                case HandleKind.AssemblyReference:
                    throw new NotImplementedException();
                case HandleKind.AssemblyFile:
                    throw new NotImplementedException();
                case HandleKind.ExportedType:
                    throw new NotImplementedException();
                case HandleKind.ManifestResource:
                    throw new NotImplementedException();
                case HandleKind.GenericParameter:
                    throw new NotImplementedException();
                case HandleKind.MethodSpecification:
                    throw new NotImplementedException();
                case HandleKind.GenericParameterConstraint:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }

        NodeCache<HandleKey<TypeDefinitionHandle>, TypeDefinitionNode> _typeDefinitions
            = new NodeCache<HandleKey<TypeDefinitionHandle>, TypeDefinitionNode>(key
                => new TypeDefinitionNode(key.Module, key.Handle));
        public TypeDefinitionNode TypeDefinition(EcmaModule module, TypeDefinitionHandle handle)
        {
            return _typeDefinitions.GetOrAdd(new HandleKey<TypeDefinitionHandle>(module, handle));
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

        NodeCache<EcmaModule, AssemblyDefinitionNode> _assemblyDefinitions
            = new NodeCache<EcmaModule, AssemblyDefinitionNode>(
                key => new AssemblyDefinitionNode(key));
        public AssemblyDefinitionNode AssemblyDefinition(EcmaModule module)
        {
            return _assemblyDefinitions.GetOrAdd(module);
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

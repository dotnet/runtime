// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection.Metadata;
using ILCompiler.DependencyAnalysis;
using ILCompiler.DependencyAnalysisFramework;
using Internal.IL;
using Internal.IL.Stubs;
using Internal.TypeSystem;
using ReflectionMapBlob = Internal.Runtime.ReflectionMapBlob;

namespace ILCompiler
{

    /// <summary>
    /// This class is responsible for managing emitted data for type maps.
    /// </summary>
    public abstract class TypeMapManager : ICompilationRootProvider
    {
        protected internal sealed class TypeMapState
        {
            private readonly Dictionary<TypeDesc, TypeDesc> _associatedTypeMap = [];
            private readonly Dictionary<string, (TypeDesc type, TypeDesc trimmingTarget)> _externalTypeMap = [];
            private ThrowingMethodStub _externalTypeMapExceptionStub;
            private ThrowingMethodStub _associatedTypeMapExceptionStub;

            public void AddAssociatedTypeMapEntry(TypeDesc type, TypeDesc associatedType)
            {
                if (!_associatedTypeMap.TryAdd(type, associatedType))
                {
                    ThrowHelper.ThrowBadImageFormatException();
                }
            }
            public void AddExternalTypeMapEntry(string typeName, TypeDesc type, TypeDesc trimmingTarget)
            {
                if (!_externalTypeMap.TryAdd(typeName, (type, trimmingTarget)))
                {
                    ThrowHelper.ThrowBadImageFormatException();
                }
            }

            public void SetExternalTypeMapStub(ThrowingMethodStub stub)
            {
                if (_externalTypeMapExceptionStub?.Exception is TypeSystemException.FileNotFoundException)
                {
                    // FileNotFound exception takes precedence.
                    return;
                }
                _externalTypeMapExceptionStub ??= stub;
            }

            public void SetAssociatedTypeMapStub(ThrowingMethodStub stub)
            {
                if (_associatedTypeMapExceptionStub?.Exception is TypeSystemException.FileNotFoundException)
                {
                    // FileNotFound exception takes precedence.
                    return;
                }
                _associatedTypeMapExceptionStub ??= stub;
            }

            public IExternalTypeMapNode GetExternalTypeMapNode(TypeDesc group)
            {
                if (_externalTypeMapExceptionStub is not null)
                {
                    return new InvalidExternalTypeMapNode(group, _externalTypeMapExceptionStub);
                }
                return new ExternalTypeMapNode(group, _externalTypeMap);
            }

            public IProxyTypeMapNode GetProxyTypeMapNode(TypeDesc group)
            {
                if (_associatedTypeMapExceptionStub is not null)
                {
                    return new InvalidProxyTypeMapNode(group, _associatedTypeMapExceptionStub);
                }
                return new ProxyTypeMapNode(group, _associatedTypeMap);
            }
        }

        protected internal sealed class ThrowingMethodStub(TypeDesc owningType, TypeDesc typeMapGroup, bool externalTypeMap, TypeSystemException ex) : ILStubMethod
        {
            public TypeSystemException Exception => ex;
            public override string Name => $"InvalidTypeMapStub_{typeMapGroup}_{(externalTypeMap ? "External" : "Proxy")}";
            public override MethodIL EmitIL()
            {
                return TypeSystemThrowingILEmitter.EmitIL(this, Exception);
            }

            protected override int CompareToImpl(MethodDesc other, TypeSystemComparer comparer) => other is ThrowingMethodStub otherStub ? Name.CompareTo(otherStub.Name, StringComparison.Ordinal) : -1;

            public override bool IsPInvoke => false;

            public override string DiagnosticName => Name;

            protected override int ClassCode => 1744789196;

            public override TypeDesc OwningType => owningType;

            public override MethodSignature Signature => new MethodSignature(MethodSignatureFlags.Static, 0, Context.GetWellKnownType(WellKnownType.Void), []);

            public override TypeSystemContext Context => owningType.Context;
        }

        protected readonly TypeMapStates _typeMaps;

        protected TypeMapManager(TypeMapStates typeMapStates)
        {
            _typeMaps = typeMapStates;
        }

        public enum TypeMapAttributeKind
        {
            None,
            TypeMapAssemblyTarget,
            TypeMap,
            TypeMapAssociation
        }

        public static TypeMapAttributeKind LookupTypeMapType(TypeDesc attrType)
        {
            TypeDesc typeDef = attrType.GetTypeDefinition();
            return typeDef switch
            {
                MetadataType { Namespace: "System.Runtime.InteropServices", Name: "TypeMapAssemblyTargetAttribute`1", Instantiation.Length: 1 } => TypeMapAttributeKind.TypeMapAssemblyTarget,
                MetadataType { Namespace: "System.Runtime.InteropServices", Name: "TypeMapAttribute`1", Instantiation.Length: 1 } => TypeMapAttributeKind.TypeMap,
                MetadataType { Namespace: "System.Runtime.InteropServices", Name: "TypeMapAssociationAttribute`1", Instantiation.Length: 1 } => TypeMapAttributeKind.TypeMapAssociation,
                _ => TypeMapAttributeKind.None,
            };
        }

        public virtual void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph)
        {
        }

        internal abstract IEnumerable<IExternalTypeMapNode> GetExternalTypeMaps();

        internal abstract IEnumerable<IProxyTypeMapNode> GetProxyTypeMaps();

        public abstract void AddCompilationRoots(IRootingServiceProvider rootProvider);

        public void AddToReadyToRunHeader(ReadyToRunHeaderNode header, NodeFactory nodeFactory, ExternalReferencesTableNode commonFixupsTableNode)
        {
            if (_typeMaps.IsEmpty)
            {
                return; // No type maps to emit
            }

            header.Add(MetadataManager.BlobIdToReadyToRunSection(ReflectionMapBlob.ExternalTypeMap), new ExternalTypeMapObjectNode(commonFixupsTableNode));
            header.Add(MetadataManager.BlobIdToReadyToRunSection(ReflectionMapBlob.ProxyTypeMap), new ProxyTypeMapObjectNode(commonFixupsTableNode));
        }
    }
}

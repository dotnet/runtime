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
        public enum TypeMapAttributeKind
        {
            None,
            TypeMapAssemblyTarget,
            TypeMap,
            TypeMapAssociation
        }

        public static TypeMapAttributeKind LookupTypeMapType(TypeDesc attrType)
        {
            var typeDef = attrType.GetTypeDefinition() as MetadataType;
            if (typeDef != null && typeDef.Namespace.SequenceEqual("System.Runtime.InteropServices"u8))
            {
                if (typeDef.Name.SequenceEqual("TypeMapAssemblyTargetAttribute`1"u8))
                    return TypeMapAttributeKind.TypeMapAssemblyTarget;
                else if (typeDef.Name.SequenceEqual("TypeMapAttribute`1"u8))
                    return TypeMapAttributeKind.TypeMap;
                else if (typeDef.Name.SequenceEqual("TypeMapAssociationAttribute`1"u8))
                    return TypeMapAttributeKind.TypeMapAssociation;
            }
            return TypeMapAttributeKind.None;
        }

        public virtual void AttachToDependencyGraph(DependencyAnalyzerBase<NodeFactory> graph)
        {
        }

        internal abstract IEnumerable<IExternalTypeMapNode> GetExternalTypeMaps();

        internal abstract IEnumerable<IProxyTypeMapNode> GetProxyTypeMaps();

        public abstract void AddCompilationRoots(IRootingServiceProvider rootProvider);

        protected abstract bool IsEmpty { get; }

        public void AddToReadyToRunHeader(ReadyToRunHeaderNode header, NodeFactory nodeFactory, ExternalReferencesTableNode commonFixupsTableNode)
        {
            if (IsEmpty)
            {
                return; // No type maps to emit
            }

            header.Add(MetadataManager.BlobIdToReadyToRunSection(ReflectionMapBlob.ExternalTypeMap), new ExternalTypeMapObjectNode(commonFixupsTableNode));
            header.Add(MetadataManager.BlobIdToReadyToRunSection(ReflectionMapBlob.ProxyTypeMap), new ProxyTypeMapObjectNode(commonFixupsTableNode));
        }
    }
}

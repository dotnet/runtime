// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;

namespace ILCompiler
{
    public abstract class TypeMapManager : ICompilationRootProvider
    {
        public enum TypeMapAttributeKind
        {
            None,
            TypeMapAssemblyTarget,
            TypeMap,
            TypeMapAssociation
        }
        public abstract void AddCompilationRoots(IRootingServiceProvider rootProvider);
        public abstract void AddToReadyToRunHeader(ReadyToRunHeaderNode header, NodeFactory nodeFactory, ExternalReferencesTableNode commonFixupsTableNode);

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
    }
}

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
        public abstract TypeMapAttributeKind LookupTypeMapType(TypeDesc attrType);
    }
}

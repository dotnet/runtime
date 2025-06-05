// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.DependencyAnalysis;
using Internal.TypeSystem;

namespace ILCompiler
{
    public sealed class EmptyTypeMapManager : TypeMapManager
    {
        public override void AddCompilationRoots(IRootingServiceProvider rootProvider) { }
        public override void AddToReadyToRunHeader(ReadyToRunHeaderNode header, NodeFactory nodeFactory, ExternalReferencesTableNode commonFixupsTableNode) { }
        public override TypeMapAttributeKind LookupTypeMapType(TypeDesc attrType) => TypeMapAttributeKind.None;
    }
}

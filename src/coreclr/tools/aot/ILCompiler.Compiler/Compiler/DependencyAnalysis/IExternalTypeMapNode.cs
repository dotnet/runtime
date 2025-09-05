// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using ILCompiler.DependencyAnalysisFramework;
using Internal.NativeFormat;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    public interface IExternalTypeMapNode : IDependencyNode, ISortableNode
    {
        TypeDesc TypeMapGroup { get; }

        Vertex CreateTypeMap(NodeFactory factory, NativeWriter writer, Section section, ExternalReferencesTableNode externalReferences);

        IExternalTypeMapNode ToAnalysisBasedNode(NodeFactory factory);
    }
}

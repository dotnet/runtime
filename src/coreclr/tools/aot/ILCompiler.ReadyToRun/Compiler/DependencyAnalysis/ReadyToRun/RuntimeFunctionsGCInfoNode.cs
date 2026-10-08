// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class RuntimeFunctionsGCInfoNode : ArrayOfEmbeddedDataNode<MethodGCInfoNode>
    {
        public RuntimeFunctionsGCInfoNode()
            : base("RuntimeFunctionsGCInfo", new EmbeddedObjectNodeComparer(CompilerComparer.Instance))
        {
        }

        public HashSet<MethodGCInfoNode> Deduplicator;

        protected internal override int Phase => (int)ObjectNodePhase.Ordered;

        public override int ClassCode => (int)ObjectNodeOrder.RuntimeFunctionsGCInfoNode;

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            return ObjectNodeSection.ReadOnlyDataSection;
        }

        public override bool StaticDependenciesAreComputed => true;

        public override bool IsShareable => false;
    }
}

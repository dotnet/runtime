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

        public override int ClassCode => 316678892;

        public override ObjectNodeSection GetSection(NodeFactory factory)
        {
            // We may want to emit info into the XData section if we produce native
            // unwind info for another format. Don't put this into the XData section
            // unless we're producing PEs, where we will also emit the unwind info
            // into the PData section.
            return factory.Format switch
            {
                ReadyToRunContainerFormat.PE => ObjectNodeSection.XDataSection,
                _ => ObjectNodeSection.ReadOnlyDataSection
            };
        }

        public override bool StaticDependenciesAreComputed => true;

        public override bool IsShareable => false;
    }
}

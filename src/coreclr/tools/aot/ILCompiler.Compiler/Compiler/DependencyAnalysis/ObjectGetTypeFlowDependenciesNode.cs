// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents dataflow dependencies from a call to Object.GetType on an instance statically
    /// typed as the given type.
    /// </summary>
    internal class ObjectGetTypeFlowDependenciesNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MetadataType _type;

        public ObjectGetTypeFlowDependenciesNode(MetadataType type)
        {
            _type = type;
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"Object.GetType dependencies called on {_type}";
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            var mdManager = (UsageBasedMetadataManager)factory.MetadataManager;
            
            // We don't mark any members on interfaces - these nodes are only used as conditional dependencies
            // of other nodes. Calling `object.GetType()` on something typed as an interface will return
            // something that implements the interface, not the interface itself. We're not reflecting on the
            // interface.
            if (_type.IsInterface)
                return Array.Empty<DependencyListEntry>();

            return Dataflow.ReflectionMethodBodyScanner.ProcessTypeGetTypeDataflow(factory, mdManager.FlowAnnotations, mdManager.Logger, _type);
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;
        
    }
}

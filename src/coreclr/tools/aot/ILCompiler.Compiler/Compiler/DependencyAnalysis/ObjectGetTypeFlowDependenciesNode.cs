// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using ILLink.Shared.TrimAnalysis;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents dataflow dependencies from a call to Object.GetType on an instance statically
    /// typed as the given type.
    /// </summary>
    internal sealed class ObjectGetTypeFlowDependenciesNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MetadataType _type;

        public ObjectGetTypeFlowDependenciesNode(MetadataType type)
        {
            _type = type;
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"Object.GetType dependencies for {_type}";
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            var mdManager = (UsageBasedMetadataManager)factory.MetadataManager;
            FlowAnnotations flowAnnotations = mdManager.FlowAnnotations;

            DependencyList result = Dataflow.ReflectionMethodBodyScanner.ProcessTypeGetTypeDataflow(factory, mdManager.FlowAnnotations, mdManager.Logger, _type);

            MetadataType baseType = _type.BaseType;
            if (baseType != null && flowAnnotations.GetTypeAnnotation(baseType) != default)
            {
                result.Add(factory.ObjectGetTypeFlowDependencies(baseType), "Apply annotations to bases");
            }

            foreach (DefType interfaceType in _type.RuntimeInterfaces)
            {
                if (flowAnnotations.GetTypeAnnotation(interfaceType) != default)
                {
                    result.Add(factory.ObjectGetTypeFlowDependencies((MetadataType)interfaceType), "Apply annotations to interfaces");
                }
            }

            return result;
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory factory) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory factory) => null;

    }
}

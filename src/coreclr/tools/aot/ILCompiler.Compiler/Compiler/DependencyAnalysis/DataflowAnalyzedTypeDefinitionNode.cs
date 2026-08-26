// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;

using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Logging;

using ILLink.Shared;
using ILLink.Shared.TrimAnalysis;

namespace ILCompiler.DependencyAnalysis
{
    public class DataflowAnalyzedTypeDefinitionNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _typeDefinition;

        public DataflowAnalyzedTypeDefinitionNode(TypeDesc typeDefinition)
        {
            Debug.Assert(typeDefinition.IsTypeDefinition);
            _typeDefinition = typeDefinition;
        }

        public static void GetDependencies(ref DependencyList dependencies, NodeFactory factory, FlowAnnotations flowAnnotations, TypeDesc type)
        {
            bool needsDataflowAnalysis = false;

            type = type.GetTypeDefinition();

            try
            {
                if (type.HasBaseType)
                {
                    if (type.BaseType.DoesTypeRequire(DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out _) &&
                        !type.DoesTypeRequire(DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out _))
                        needsDataflowAnalysis = true;

                    needsDataflowAnalysis |= GenericArgumentDataFlow.RequiresGenericArgumentDataFlow(flowAnnotations, type.BaseType);
                }

                if (type is MetadataType metadataType)
                {
                    foreach (var interfaceType in metadataType.ExplicitlyImplementedInterfaces)
                    {
                        needsDataflowAnalysis |= GenericArgumentDataFlow.RequiresGenericArgumentDataFlow(flowAnnotations, interfaceType);
                    }
                }
            }
            catch (TypeSystemException)
            {
                // Wasn't able to do dataflow because of missing references or something like that.
                // This likely won't compile either, so we don't care about missing dependencies.
            }

            if (needsDataflowAnalysis)
            {
                dependencies ??= new DependencyList();
                dependencies.Add(factory.DataflowAnalyzedTypeDefinition(type), "Dataflow for type definition");
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            DependencyList dependencies = null;

            if (_typeDefinition is MetadataType metadataType)
            {
                // The generic instantiation in the interface list is only reachable through the members of
                // the type which implements the interface, which are all in the Requires scope of a
                // type-level Requires attribute, so the matching attribute silences these warnings. Note
                // that the data flow still needs to run to mark the members required by the instantiation.
                bool suppressTrimAnalysisWarnings = _typeDefinition.DoesTypeRequire(DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out _);
                bool suppressAotAnalysisWarnings = _typeDefinition.DoesTypeRequire(DiagnosticUtilities.RequiresDynamicCodeAttribute, out _);

                foreach (var interfaceType in metadataType.ExplicitlyImplementedInterfaces)
                {
                    GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(
                        ref dependencies,
                        factory,
                        new MessageOrigin(_typeDefinition),
                        interfaceType,
                        _typeDefinition,
                        suppressTrimAnalysisWarnings,
                        suppressAotAnalysisWarnings);
                }
            }

            return dependencies;
        }

        protected override string GetName(NodeFactory factory)
        {
            return "Dataflow analysis for type definition " + _typeDefinition.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
    }
}

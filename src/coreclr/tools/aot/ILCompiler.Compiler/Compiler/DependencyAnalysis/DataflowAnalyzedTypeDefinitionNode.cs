// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;

using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysisFramework;

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
            bool foundGenericParameterAnnotation = false;

            type = type.GetTypeDefinition();

            try
            {
                if (type.HasBaseType)
                {
                    foundGenericParameterAnnotation |= IsTypeWithGenericParameterAnnotations(flowAnnotations, type.BaseType);
                }

                if (type is MetadataType metadataType)
                {
                    foreach (var interfaceType in metadataType.ExplicitlyImplementedInterfaces)
                    {
                        foundGenericParameterAnnotation |= IsTypeWithGenericParameterAnnotations(flowAnnotations, interfaceType);
                    }
                }
            }
            catch (TypeSystemException)
            {
                // Wasn't able to do dataflow because of missing references or something like that.
                // This likely won't compile either, so we don't care about missing dependencies.
            }

            if (foundGenericParameterAnnotation)
            {
                dependencies ??= new DependencyList();
                dependencies.Add(factory.DataflowAnalyzedTypeDefinition(type), "Generic parameter dataflow");
            }

            static bool IsTypeWithGenericParameterAnnotations(FlowAnnotations flowAnnotations, TypeDesc type)
                => type.HasInstantiation && flowAnnotations.HasGenericParameterAnnotation(type);
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            var mdManager = (UsageBasedMetadataManager)factory.MetadataManager;

            DependencyList dependencies = null;

            if (_typeDefinition.HasBaseType)
            {
                GetDataFlowDependenciesForInstantiation(ref dependencies, mdManager.Logger, factory, mdManager.FlowAnnotations, _typeDefinition.BaseType, _typeDefinition);
            }

            if (_typeDefinition is MetadataType metadataType)
            {
                foreach (var interfaceType in metadataType.ExplicitlyImplementedInterfaces)
                {
                    GetDataFlowDependenciesForInstantiation(ref dependencies, mdManager.Logger, factory, mdManager.FlowAnnotations, interfaceType, _typeDefinition);
                }
            }

            return dependencies;
        }

        private static void GetDataFlowDependenciesForInstantiation(
            ref DependencyList dependencies,
            Logger logger,
            NodeFactory factory,
            FlowAnnotations flowAnnotations,
            TypeDesc type,
            TypeDesc contextType)
        {
            TypeDesc instantiatedType = type.InstantiateSignature(contextType.Instantiation, Instantiation.Empty);
            GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(ref dependencies, logger, factory, flowAnnotations, new Logging.MessageOrigin(contextType), instantiatedType);
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

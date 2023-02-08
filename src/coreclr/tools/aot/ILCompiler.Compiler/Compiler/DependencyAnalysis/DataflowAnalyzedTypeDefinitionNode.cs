// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;

using ILCompiler.Dataflow;
using ILCompiler.DependencyAnalysisFramework;

using ILLink.Shared.TrimAnalysis;
using ILCompiler.Logging;

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
            DependencyList dependencies = null;

            if (_typeDefinition.HasBaseType)
            {
                GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(ref dependencies, factory, new MessageOrigin(_typeDefinition), _typeDefinition.BaseType, _typeDefinition);
            }

            if (_typeDefinition is MetadataType metadataType)
            {
                foreach (var interfaceType in metadataType.ExplicitlyImplementedInterfaces)
                {
                    GenericArgumentDataFlow.ProcessGenericArgumentDataFlow(ref dependencies, factory, new MessageOrigin(_typeDefinition), interfaceType, _typeDefinition);
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

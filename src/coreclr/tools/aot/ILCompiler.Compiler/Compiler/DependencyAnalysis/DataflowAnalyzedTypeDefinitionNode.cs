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

            if (_typeDefinition.HasBaseType)
            {
                if (_typeDefinition.BaseType.DoesTypeRequire(DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out var requiresAttribute) &&
                    !_typeDefinition.DoesTypeRequire(DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out _))
                {
                    UsageBasedMetadataManager metadataManager = (UsageBasedMetadataManager)factory.MetadataManager;
                    string arg1 = MessageFormat.FormatRequiresAttributeMessageArg(DiagnosticUtilities.GetRequiresAttributeMessage(requiresAttribute.Value));
                    string arg2 = MessageFormat.FormatRequiresAttributeUrlArg(DiagnosticUtilities.GetRequiresAttributeUrl(requiresAttribute.Value));
                    metadataManager.Logger.LogWarning(new MessageOrigin(_typeDefinition), DiagnosticId.RequiresUnreferencedCodeOnBaseClass, _typeDefinition.GetDisplayName(), _typeDefinition.BaseType.GetDisplayName(), arg1, arg2);
                }

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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Internal.TypeSystem;

using ILCompiler.DependencyAnalysisFramework;
using ILCompiler.Dataflow;
using ILCompiler.Logging;

using ILLink.Shared;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Node that performs reflection analysis for static constructor
    /// </summary>
    public class StaticConstructorAnalysisNode : DependencyNodeCore<NodeFactory>
    {
        private readonly MethodDesc _staticConstructor;

        public StaticConstructorAnalysisNode(MethodDesc staticConstructor)
        {
            Debug.Assert(staticConstructor.IsStaticConstructor);
            Debug.Assert(staticConstructor.IsTypicalMethodDefinition);
            _staticConstructor = staticConstructor;
        }

        public static void GetDependencies(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            if (!method.IsStaticConstructor)
                return;

            if (DiagnosticUtilities.TryGetRequiresAttribute(method, DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out _) ||
                DiagnosticUtilities.TryGetRequiresAttribute(method, DiagnosticUtilities.RequiresDynamicCodeAttribute, out _) ||
                DiagnosticUtilities.TryGetRequiresAttribute(method, DiagnosticUtilities.RequiresAssemblyFilesAttribute, out _))
            {
                dependencies ??= new DependencyList();
                dependencies.Add(factory.StaticConstructorAnalysis(method), "Static constructor presence");
            }
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            var metadataManager = (UsageBasedMetadataManager)factory.MetadataManager;

            if (DiagnosticUtilities.TryGetRequiresAttribute(_staticConstructor, DiagnosticUtilities.RequiresUnreferencedCodeAttribute, out _))
                metadataManager.Logger.LogWarning(new MessageOrigin(_staticConstructor), DiagnosticId.RequiresUnreferencedCodeOnStaticConstructor, _staticConstructor.GetDisplayName());

            if (DiagnosticUtilities.TryGetRequiresAttribute(_staticConstructor, DiagnosticUtilities.RequiresDynamicCodeAttribute, out _))
                metadataManager.Logger.LogWarning(new MessageOrigin(_staticConstructor), DiagnosticId.RequiresDynamicCodeOnStaticConstructor, _staticConstructor.GetDisplayName());

            if (DiagnosticUtilities.TryGetRequiresAttribute(_staticConstructor, DiagnosticUtilities.RequiresAssemblyFilesAttribute, out _))
                metadataManager.Logger.LogWarning(new MessageOrigin(_staticConstructor), DiagnosticId.RequiresAssemblyFilesOnStaticConstructor, _staticConstructor.GetDisplayName());

            return Array.Empty<DependencyListEntry>();
        }

        protected override string GetName(NodeFactory factory)
        {
            return "Static constructor analysis for " + _staticConstructor.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a resource blob used by the SR class in the BCL.
    /// If this node is present in the graph, it means we were not able to optimize its use away
    /// and the blob has to be generated.
    /// </summary>
    internal sealed class InlineableStringsResourceNode : DependencyNodeCore<NodeFactory>
    {
        private readonly EcmaModule _module;

        public InlineableStringsResourceNode(EcmaModule module)
        {
            _module = module;
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public static bool IsInlineableStringsResource(EcmaModule module, string resourceName)
        {
            if (!resourceName.EndsWith(".resources"))
                return false;

            // TODO: we should grab this name from the SR class
            string resourceName1 = $"{module.Assembly.GetName().Name}.Strings.resources";
            string resourceName2 = $"FxResources.{module.Assembly.GetName().Name}.SR.resources";

            if (resourceName != resourceName1 && resourceName != resourceName2)
                return false;

            MetadataType srType = module.GetType("System", "SR", throwIfNotFound: false);
            if (srType == null)
                return false;

            return srType.GetMethod("GetResourceString", null) != null;
        }

        public static void AddDependenciesDueToResourceStringUse(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            if (method.Name == "GetResourceString" && method.OwningType is MetadataType mdType
                && mdType.Name == "SR" && mdType.Namespace == "System")
            {
                dependencies ??= new DependencyList();
                dependencies.Add(factory.InlineableStringResource((EcmaModule)mdType.Module), "Using the System.SR class");
            }
        }

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
        protected override string GetName(NodeFactory context)
            => $"String resources for {_module.Assembly.GetName().Name}";
    }
}

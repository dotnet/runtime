// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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

        public const string ResourceAccessorTypeName = "SR";
        public const string ResourceAccessorTypeNamespace = "System";
        public const string ResourceAccessorGetStringMethodName = "GetResourceString";

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
            if (!resourceName.EndsWith(".resources", StringComparison.Ordinal))
                return false;

            // Make a guess at the name of the resource Arcade tooling generated for the resource
            // strings.
            // https://github.com/dotnet/runtime/issues/81385 tracks not having to guess this.
            string simpleName = module.Assembly.GetName().Name;
            string resourceName1 = $"{simpleName}.Strings.resources";
            string resourceName2 = $"FxResources.{simpleName}.SR.resources";

            if (resourceName != resourceName1 && resourceName != resourceName2)
                return false;

            MetadataType srType = module.GetType(ResourceAccessorTypeNamespace, ResourceAccessorTypeName, throwIfNotFound: false);
            if (srType == null)
                return false;

            return srType.GetMethod(ResourceAccessorGetStringMethodName, null) != null;
        }

        public static void AddDependenciesDueToResourceStringUse(ref DependencyList dependencies, NodeFactory factory, MethodDesc method)
        {
            if (method.Name == ResourceAccessorGetStringMethodName && method.OwningType is MetadataType mdType
                && mdType.Name == ResourceAccessorTypeName && mdType.Namespace == ResourceAccessorTypeNamespace)
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

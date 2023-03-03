//Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    public class TrimmingDescriptorNode : DependencyNodeCore<NodeFactory>, ICompilationRootProvider
    {
        private readonly string _fileName;

        public TrimmingDescriptorNode(string fileName)
        {
            _fileName = fileName;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            using (Stream fs = File.OpenRead(_fileName))
            {
                var metadataManager = (UsageBasedMetadataManager)factory.MetadataManager;
                return DescriptorMarker.GetDependencies(factory, fs, default, default, _fileName, metadataManager.FeatureSwitches);
            }
        }

        protected override string GetName(NodeFactory factory)
        {
            return $"Descriptor in {_fileName}";
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
        void ICompilationRootProvider.AddCompilationRoots(IRootingServiceProvider rootProvider) => rootProvider.AddCompilationRoot(this, "Descriptor from command line");
    }
}

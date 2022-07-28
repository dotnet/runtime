// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Generic;

using Internal.TypeSystem;
using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Models dependencies of the dynamic invoke methods that aid in reflection invoking methods.
    /// Dynamic invoke methods are shared method bodies that perform calling convention conversion
    /// from object[] to the expected signature of the called method.
    /// </summary>
    internal class DynamicInvokeTemplateNode : DependencyNodeCore<NodeFactory>
    {
        public MethodDesc Method { get; }

        public DynamicInvokeTemplateNode(MethodDesc method)
        {
            Debug.Assert(method.IsSharedByGenericInstantiations);
            Method = method;
        }

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory factory)
        {
            return DynamicInvokeTemplateDataNode.GetDependenciesDueToInvokeTemplatePresence(factory, Method);
        }

        protected override string GetName(NodeFactory factory)
        {
            return "DynamicInvokeTemplate: " + factory.NameMangler.GetMangledMethodName(Method);
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;
using Internal.Text;
using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class AllMethodsOnTypeNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;

        public AllMethodsOnTypeNode(TypeDesc type)
        {
            _type = type;
        }

        public TypeDesc Type => _type;

        public override bool InterestingForDynamicDependencyAnalysis => false;

        public override bool HasDynamicDependencies => false;

        public override bool HasConditionalStaticDependencies => false;

        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context) => null;
        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context)
        {
            DependencyList dependencies = new DependencyList();

            foreach (MethodDesc method in Type.GetAllMethods())
            {
                if (!method.IsGenericMethodDefinition &&
                    context.CompilationModuleGroup.ContainsMethodBody(method, false))
                {
                    dependencies.Add(context.CompiledMethodNode(method), $"Method on type {Type.ToString()}");
                }
            }

            return dependencies;
        }

        protected override string GetName(NodeFactory factory) => $"All methods on type {Type.ToString()}";
    }
}

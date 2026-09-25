// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a property that has metadata generated in the current compilation.
    /// This corresponds to an ECMA-335 Property record. Unlike fields and methods,
    /// properties are not first-class entities in the compiler's type system (there is no
    /// interned PropertyDesc).
    /// </summary>
    /// <remarks>
    /// Only expected to be used during ILScanning when scanning for reflection.
    /// </remarks>
    internal sealed class PropertyMetadataNode : DependencyNodeCore<NodeFactory>
    {
        private readonly PropertyPseudoDesc _property;

        public PropertyMetadataNode(PropertyPseudoDesc property)
        {
            _property = property;
        }

        public PropertyPseudoDesc Property => _property;

        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory factory) { }

        public override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
            DependencySink<NodeFactory> dependencies = sink;
            CustomAttributeBasedDependencyAlgorithm.AddDependenciesDueToCustomAttributes(dependencies, factory, _property);
        }

        protected override string GetName(NodeFactory factory)
        {
            return "Property metadata: " + _property.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => true;
        public override bool StaticDependenciesAreComputed => true;
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory factory) { }
    }
}

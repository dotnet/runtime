// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a type that is forced to be visible from reflection.
    /// The system needs to implicitly assume that any allocated type could be visible from
    /// reflection due to <see cref="object.GetType" />, so presence of this node is not
    /// a necessary condition for a type to be reflection visible. However, the presence of this
    /// node indicates that a new reflectable type was forced into existence by e.g. dataflow
    /// analysis, and is not just a byproduct of allocating an instance of this type.
    /// </summary>
    public class ReflectedTypeNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;

        public ReflectedTypeNode(TypeDesc type)
        {
            Debug.Assert(!type.IsCanonicalSubtype(CanonicalFormKind.Any)
                || type.ConvertToCanonForm(CanonicalFormKind.Specific) == type);
            _type = type;
        }

        public TypeDesc Type => _type;

        public override void AddStaticDependencies(DependencySink<NodeFactory> sink, NodeFactory factory)
        {
            sink.Add(factory.MaximallyConstructableType(_type), "Reflection target");

            if (_type.IsCanonicalSubtype(CanonicalFormKind.Any))
            {
                GenericTypesTemplateMap.AddTemplateTypeDependencies(sink, factory, _type);
            }
        }
        protected override string GetName(NodeFactory factory)
        {
            return "Reflectable type: " + _type.ToString();
        }

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;
        public override void AddConditionalDependencies(DependencySink<NodeFactory> sink, NodeFactory factory) { }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink<NodeFactory> sink, NodeFactory factory) { }
    }
}

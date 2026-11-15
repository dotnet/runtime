// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents the condition "the type loader template for <see cref="Type"/> is present, and the generic arguments
    /// of <see cref="Type"/> before <see cref="ArgumentIndex"/> are present in the compilation or could be created
    /// by the type loader at runtime". Once the last argument is checked too, <see cref="TypeLoaderLoadableTypeNode"/>
    /// is marked.
    /// </summary>
    /// <remarks>
    /// A generic type that could be created by the type loader at runtime can only be created if all of its
    /// arguments exist at runtime. If some argument is not needed for any other reason, there is no
    /// need to eagerly create the type. Arguments that are arrays, or generic types that the type loader
    /// can create themselves, do not need to be in the compilation.
    ///
    /// A conditional dependency can only be conditioned on a single other node, so "all arguments are present" is
    /// expressed as a chain with one node per argument. The node for argument i is marked once the node for
    /// argument i - 1 is marked and argument i is present. Arguments become present at arbitrary points of
    /// the analysis, so this can't be checked at once by looking at all of them.
    /// </remarks>
    public sealed class TypeLoaderArgumentGateNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;
        private readonly int _argumentIndex;

        public TypeLoaderArgumentGateNode(TypeDesc type, int argumentIndex)
        {
            Debug.Assert(argumentIndex >= 0 && argumentIndex < type.Instantiation.Length);
            _type = type;
            _argumentIndex = argumentIndex;
        }

        public TypeDesc Type => _type;
        public int ArgumentIndex => _argumentIndex;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => true;
        public override bool StaticDependenciesAreComputed => true;

        public override void AddStaticDependencies(DependencySink sink, NodeFactory context) { }

        public override void AddConditionalDependencies(DependencySink sink, NodeFactory context)
        {
            // Arrays can be created by the type loader out of their element type.
            TypeDesc argument = _type.Instantiation[_argumentIndex];
            while (argument.IsParameterizedType)
                argument = ((ParameterizedType)argument).ParameterType;

            DependencyNodeCore<NodeFactory> next = _argumentIndex + 1 < _type.Instantiation.Length
                ? context.TypeLoaderArgumentGate(_type, _argumentIndex + 1)
                : context.TypeLoaderLoadableType(_type);

            sink.Add(new CombinedDependencyListEntry(
                next,
                context.NecessaryTypeSymbol(argument),
                "Generic argument of a type loader candidate is present"));

            // The argument is a generic type that could be created by the type loader too.
            RuntimeConstructableTypeDependencies.AddTypeLoaderDependencies(
                sink,
                context,
                argument,
                next,
                "Generic argument of a type loader candidate can be loaded at runtime");
        }

        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory context) { }

        protected override string GetName(NodeFactory context)
            => $"Type loader candidate gate: {_type} (argument {_argumentIndex})";
    }

    /// <summary>
    /// Represents the condition "<see cref="Type"/> can be created by the type loader at runtime": its type loader
    /// template is present and all of its generic arguments are present or can be created by the type loader too.
    /// </summary>
    public sealed class TypeLoaderLoadableTypeNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;

        public TypeLoaderLoadableTypeNode(TypeDesc type)
        {
            _type = type;
        }

        public TypeDesc Type => _type;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => false;
        public override bool StaticDependenciesAreComputed => true;

        public override void AddStaticDependencies(DependencySink sink, NodeFactory context) { }
        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory context) { }

        protected override string GetName(NodeFactory context)
            => $"Type loader can load: {_type}";
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents the condition "the type loader template for the type is present, and the generic arguments
    /// of the type before the argument at the given index are present in the compilation or could be created
    /// by the type loader at runtime". The node for the index past the last argument, see <see cref="GetLast"/>,
    /// means that the type can be created by the type loader at runtime.
    /// </summary>
    /// <remarks>
    /// A generic type that could be created by the type loader at runtime can only be created if all of its
    /// arguments exist at runtime. If some argument is not needed for any other reason, there is no
    /// need to eagerly create the type. Arguments that the type loader can create themselves, such as generic
    /// types and arrays that have a template, do not need to be in the compilation. The element type of an array
    /// is its single argument.
    ///
    /// A conditional dependency can only be conditioned on a single other node, so "all arguments are present" is
    /// expressed as a chain with one node per argument, plus the final node returned by <see cref="GetLast"/>. The node
    /// for argument i is marked once the node for argument i - 1 is marked and argument i is present. Arguments become present at arbitrary points of
    /// the analysis, so this can't be checked at once by looking at all of them.
    /// </remarks>
    public sealed class TypeLoaderArgumentGateNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;
        private readonly int _argumentIndex;

        public TypeLoaderArgumentGateNode(TypeDesc type, int argumentIndex)
        {
            Debug.Assert(argumentIndex >= 0 && argumentIndex <= GetArgumentCount(type));
            _type = type;
            _argumentIndex = argumentIndex;
        }

        /// <summary>
        /// Gets the last node of the chain, which is marked once all arguments have been checked.
        /// </summary>
        public TypeLoaderArgumentGateNode GetLast(NodeFactory factory) => factory.TypeLoaderArgumentGate(_type, GetArgumentCount(_type));

        // The element type of an array is its only argument, since the type loader can't create the array without it.
        private static int GetArgumentCount(TypeDesc type) => type.IsArray ? 1 : type.Instantiation.Length;

        private static TypeDesc GetArgument(TypeDesc type, int index) => type.IsArray ? ((ArrayType)type).ElementType : type.Instantiation[index];

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => _argumentIndex < GetArgumentCount(_type);
        public override bool StaticDependenciesAreComputed => true;

        public override IEnumerable<DependencyListEntry> GetStaticDependencies(NodeFactory context) => null;

        public override IEnumerable<CombinedDependencyListEntry> GetConditionalStaticDependencies(NodeFactory context)
        {
            // The type loader can create a parameterized type out of its parameter type, except for single-dimensional
            // arrays that also need a template (which is assumed to be present for arrays of reference types).
            // This is the type that has to be present or loadable for the argument to exist at runtime.
            TypeDesc argument = RuntimeConstructableTypeDependencies.GetEffectiveTrimTargetType(
                context, GetArgument(_type, _argumentIndex), conditionConstructed: false).Type;

            TypeLoaderArgumentGateNode next = context.TypeLoaderArgumentGate(_type, _argumentIndex + 1);

            var dependencies = new List<CombinedDependencyListEntry>
            {
                new CombinedDependencyListEntry(
                    next,
                    context.NecessaryTypeSymbol(argument),
                    "Generic argument of a type loader candidate is present")
            };

            // The argument is a generic type or an array that could be created by the type loader too.
            RuntimeConstructableTypeDependencies.AddTypeLoaderDependencies(
                dependencies,
                context,
                argument,
                next,
                "Generic argument of a type loader candidate can be loaded at runtime");

            return dependencies;
        }

        public override IEnumerable<CombinedDependencyListEntry> SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, NodeFactory context) => null;

        protected override string GetName(NodeFactory context)
            => _argumentIndex < GetArgumentCount(_type)
                ? $"Type loader candidate gate: {_type} (argument {_argumentIndex})"
                : $"Type loader candidate gate: {_type} (loadable)";
    }
}

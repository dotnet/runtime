// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using ILCompiler.DependencyAnalysisFramework;

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Checks that all generic arguments of <see cref="Type"/> (starting at <see cref="NextArgument"/>) are present
    /// in the compilation or could be created by the type loader at runtime. Once they are, this node marks
    /// <see cref="TypeToMark"/> (if any).
    /// </summary>
    /// <remarks>
    /// A generic type that could be created by the type loader at runtime can only be created if all of its
    /// arguments exist at runtime. If some argument is not needed for any other reason, there is no
    /// need to eagerly create the type. Arguments that are arrays, or generic types that the type loader
    /// can create themselves, do not need to be in the compilation.
    /// </remarks>
    public sealed class TypeLoaderArgumentGateNode : DependencyNodeCore<NodeFactory>
    {
        private readonly TypeDesc _type;
        private readonly int _nextArgument;
        private readonly IEETypeNode _typeToMark;

        public TypeLoaderArgumentGateNode(TypeDesc type, int nextArgument, IEETypeNode typeToMark)
        {
            _type = type;
            _nextArgument = nextArgument;
            _typeToMark = typeToMark;
        }

        public TypeDesc Type => _type;
        public int NextArgument => _nextArgument;
        public IEETypeNode TypeToMark => _typeToMark;

        private bool IsLast => _nextArgument >= _type.Instantiation.Length;

        public override bool InterestingForDynamicDependencyAnalysis => false;
        public override bool HasDynamicDependencies => false;
        public override bool HasConditionalStaticDependencies => !IsLast;
        public override bool StaticDependenciesAreComputed => true;

        public override void AddStaticDependencies(DependencySink sink, NodeFactory context)
        {
            if (IsLast && _typeToMark is not null)
                sink.Add(_typeToMark, "Type loader candidate with all generic arguments present");
        }

        public override void AddConditionalDependencies(DependencySink sink, NodeFactory context)
        {
            if (IsLast)
                return;

            // Arrays can be created by the type loader out of their element type.
            TypeDesc argument = _type.Instantiation[_nextArgument];
            while (argument.IsParameterizedType)
                argument = ((ParameterizedType)argument).ParameterType;

            TypeLoaderArgumentGateNode next = context.TypeLoaderArgumentGate(_type, _nextArgument + 1, _typeToMark);

            sink.Add(new CombinedDependencyListEntry(
                next,
                context.NecessaryTypeSymbol(argument),
                "Generic argument of a type loader candidate is present"));

            // The argument is a generic type that could be created by the type loader too.
            TypeDesc canonArgument = argument.ConvertToCanonForm(CanonicalFormKind.Specific);
            if (argument.HasInstantiation && canonArgument != argument && GenericTypesTemplateMap.IsEligibleToHaveATemplate(canonArgument))
            {
                sink.Add(new CombinedDependencyListEntry(
                    context.TypeLoaderArgumentGate(argument, 0, null),
                    context.NativeLayout.TemplateTypeLayout(canonArgument),
                    "Generic argument of a type loader candidate could be loaded at runtime"));

                sink.Add(new CombinedDependencyListEntry(
                    next,
                    context.TypeLoaderArgumentGate(argument, argument.Instantiation.Length, null),
                    "Generic argument of a type loader candidate can be loaded at runtime"));
            }
        }

        public override void SearchDynamicDependencies(List<DependencyNodeCore<NodeFactory>> markedNodes, int firstNode, DependencySink sink, NodeFactory context) { }

        protected override string GetName(NodeFactory context)
            => $"Type loader candidate gate: {_type} (argument {_nextArgument}{(_typeToMark is null ? ", unmarked" : "")})";
    }
}

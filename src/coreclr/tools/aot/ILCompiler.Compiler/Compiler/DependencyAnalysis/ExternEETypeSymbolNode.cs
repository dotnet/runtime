// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a symbol that is defined externally but modelled as a type in the
    /// DependencyAnalysis infrastructure during compilation.
    /// </summary>
    public sealed class ExternEETypeSymbolNode : ExternSymbolNode, IEETypeNode
    {
        private TypeDesc _type;

        public ExternEETypeSymbolNode(NodeFactory factory, TypeDesc type)
            : base(factory.NameMangler.NodeMangler.MethodTable(type))
        {
            _type = type;

            factory.TypeSystemContext.EnsureLoadableType(type);
        }

        public TypeDesc Type => _type;
    }
}

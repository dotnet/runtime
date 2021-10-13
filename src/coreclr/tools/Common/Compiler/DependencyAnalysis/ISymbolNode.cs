// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILCompiler.DependencyAnalysisFramework;
using Internal.Text;

namespace ILCompiler.DependencyAnalysis
{
    /// <summary>
    /// Represents a reference to a symbol. The reference can potentially be offset by a value.
    /// </summary>
    public interface ISymbolNode : IDependencyNode<NodeFactory>
    {
        /// <summary>
        /// Appends the mangled name of the symbol to the string builder provided.
        /// </summary>
        void AppendMangledName(NameMangler nameMangler, Utf8StringBuilder sb);

        /// <summary>
        /// Gets the offset (delta) from the symbol this <see cref="ISymbolNode"/> references.
        /// </summary>
        int Offset { get; }

        /// <summary>
        /// Set the return value of this property to true to indicate that this symbol
        /// is an indirection cell to data that is needed, not the actual data itself.
        /// Most commonly affects the code generation which accesses symbols such
        /// Types which may require an indirection to access or not.
        /// </summary>
        bool RepresentsIndirectionCell { get; }
    }

    /// <summary>
    /// Represents a symbol backed by a different symbol for object emission purposes.
    /// </summary>
    public interface ISymbolNodeWithLinkage : ISymbolNode
    {
        /// <summary>
        /// Return a node that is used for linkage
        /// </summary>
        ISymbolNode NodeForLinkage(NodeFactory factory);
    }

    public interface ISortableSymbolNode : ISymbolNode, ISortableNode
    {
    }


    /// <summary>
    /// Represents a definition of a symbol within an <see cref="ObjectNode"/>. The symbol will be defined
    /// at the specified offset from the beginning of the <see cref="ObjectNode"/> that reports this as one of
    /// the symbols it defines.
    /// </summary>
    public interface ISymbolDefinitionNode : ISymbolNode
    {
        /// <summary>
        /// Gets the offset (delta) from the beginning of the <see cref="ObjectNode"/> where this symbol should
        /// be defined. Note this is different from <see cref="ISymbolNode.Offset"/>, which is the offset to be
        /// used when referencing the symbol.
        /// </summary>
        /// <remarks>
        /// Most node types will want to implement both <see cref="ISymbolNode.Offset"/> and <see cref="Offset"/>
        /// to return 0. The name was chosen to make this convenient. If an object node implements this interface,
        /// it will pretty much always want to either implement both as returning 0, or *one of them* to return
        /// a non-zero value.
        /// Some examples: an MethodTable node defines the symbol of the MethodTable in the middle of the object node,
        /// since EETypes are prefixed by a GCDesc structure (that is not considered the beginning of the MethodTable).
        /// This means that <see cref="Offset"/> will return a non-zero value. When referencing the MethodTable by its
        /// symbol, the GCDesc was already accounted for when the symbol was defined, so we want the
        /// <see cref="ISymbolNode.Offset"/> to be zero.
        /// </remarks>
        new int Offset { get; }
    }

    public static class ISymbolNodeExtensions
    {
        [ThreadStatic]
        static Utf8StringBuilder s_cachedUtf8StringBuilder;

        public static string GetMangledName(this ISymbolNode symbolNode, NameMangler nameMangler)
        {
            Utf8StringBuilder sb = s_cachedUtf8StringBuilder;
            if (sb == null)
                sb = new Utf8StringBuilder();

            symbolNode.AppendMangledName(nameMangler, sb);
            string ret = sb.ToString();

            sb.Clear();
            s_cachedUtf8StringBuilder = sb;

            return ret;
        }
    }
}

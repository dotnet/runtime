using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Interop
{
    internal static class Comparers
    {
        /// <summary>
        /// Comparer for the set of all of the generated stubs and diagnostics generated for each of them.
        /// </summary>
        public static readonly IEqualityComparer<ImmutableArray<(string, ImmutableArray<Diagnostic>)>> GeneratedSourceSet = new ImmutableArraySequenceEqualComparer<(string, ImmutableArray<Diagnostic>)>(new CustomValueTupleElementComparer<string, ImmutableArray<Diagnostic>>(EqualityComparer<string>.Default, new ImmutableArraySequenceEqualComparer<Diagnostic>(EqualityComparer<Diagnostic>.Default)));
        
        /// <summary>
        /// Comparer for an individual generated stub source as a string and the generated diagnostics for the stub.
        /// </summary>
        public static readonly IEqualityComparer<(string, ImmutableArray<Diagnostic>)> GeneratedSource = new CustomValueTupleElementComparer<string, ImmutableArray<Diagnostic>>(EqualityComparer<string>.Default, new ImmutableArraySequenceEqualComparer<Diagnostic>(EqualityComparer<Diagnostic>.Default));
        
        /// <summary>
        /// Comparer for an individual generated stub source as a syntax tree and the generated diagnostics for the stub.
        /// </summary>
        public static readonly IEqualityComparer<(MemberDeclarationSyntax Syntax, ImmutableArray<Diagnostic> Diagnostics)> GeneratedSyntax = new CustomValueTupleElementComparer<MemberDeclarationSyntax, ImmutableArray<Diagnostic>>(new SyntaxEquivalentComparer(), new ImmutableArraySequenceEqualComparer<Diagnostic>(EqualityComparer<Diagnostic>.Default));

        /// <summary>
        /// Comparer for the context used to generate a stub and the original user-provided syntax that triggered stub creation.
        /// </summary>
        public static readonly IEqualityComparer<(MethodDeclarationSyntax Syntax, DllImportGenerator.IncrementalStubGenerationContext StubContext)> CalculatedContextWithSyntax = new CustomValueTupleElementComparer<MethodDeclarationSyntax, DllImportGenerator.IncrementalStubGenerationContext>(new SyntaxEquivalentComparer(), EqualityComparer<DllImportGenerator.IncrementalStubGenerationContext>.Default);
    }

    /// <summary>
    /// Generic comparer to compare two <see cref="ImmutableArray{T}"/> instances element by element.
    /// </summary>
    /// <typeparam name="T">The type of immutable array element.</typeparam>
    internal class ImmutableArraySequenceEqualComparer<T> : IEqualityComparer<ImmutableArray<T>>
    {
        private readonly IEqualityComparer<T> elementComparer;

        /// <summary>
        /// Creates an <see cref="ImmutableArraySequenceEqualComparer{T}"/> with a custom comparer for the elements of the collection.
        /// </summary>
        /// <param name="elementComparer">The comparer instance for the collection elements.</param>
        public ImmutableArraySequenceEqualComparer(IEqualityComparer<T> elementComparer)
        {
            this.elementComparer = elementComparer;
        }

        public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
        {
            return x.SequenceEqual(y, elementComparer);
        }

        public int GetHashCode(ImmutableArray<T> obj)
        {
            throw new UnreachableException();
        }
    }

    internal class SyntaxEquivalentComparer : IEqualityComparer<SyntaxNode>
    {
        public bool Equals(SyntaxNode x, SyntaxNode y)
        {
            return x.IsEquivalentTo(y);
        }

        public int GetHashCode(SyntaxNode obj)
        {
            throw new UnreachableException();
        }
    }

    internal class CustomValueTupleElementComparer<T, U> : IEqualityComparer<(T, U)>
    {
        private readonly IEqualityComparer<T> item1Comparer;
        private readonly IEqualityComparer<U> item2Comparer;

        public CustomValueTupleElementComparer(IEqualityComparer<T> item1Comparer, IEqualityComparer<U> item2Comparer)
        {
            this.item1Comparer = item1Comparer;
            this.item2Comparer = item2Comparer;
        }

        public bool Equals((T, U) x, (T, U) y)
        {
            return item1Comparer.Equals(x.Item1, y.Item1) && item2Comparer.Equals(x.Item2, y.Item2);
        }

        public int GetHashCode((T, U) obj)
        {
            throw new UnreachableException();
        }
    }
}

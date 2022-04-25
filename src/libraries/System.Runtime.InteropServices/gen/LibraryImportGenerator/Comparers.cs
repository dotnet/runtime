// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        public static readonly IEqualityComparer<(MemberDeclarationSyntax Syntax, ImmutableArray<Diagnostic> Diagnostics)> GeneratedSyntax = new CustomValueTupleElementComparer<MemberDeclarationSyntax, ImmutableArray<Diagnostic>>(SyntaxEquivalentComparer.Instance, new ImmutableArraySequenceEqualComparer<Diagnostic>(EqualityComparer<Diagnostic>.Default));

        /// <summary>
        /// Comparer for the context used to generate a stub and the original user-provided syntax that triggered stub creation.
        /// </summary>
        public static readonly IEqualityComparer<(MethodDeclarationSyntax Syntax, LibraryImportGenerator.IncrementalStubGenerationContext StubContext)> CalculatedContextWithSyntax = new CustomValueTupleElementComparer<MethodDeclarationSyntax, LibraryImportGenerator.IncrementalStubGenerationContext>(SyntaxEquivalentComparer.Instance, EqualityComparer<LibraryImportGenerator.IncrementalStubGenerationContext>.Default);
    }

    /// <summary>
    /// Generic comparer to compare two <see cref="ImmutableArray{T}"/> instances element by element.
    /// </summary>
    /// <typeparam name="T">The type of immutable array element.</typeparam>
    internal sealed class ImmutableArraySequenceEqualComparer<T> : IEqualityComparer<ImmutableArray<T>>
    {
        private readonly IEqualityComparer<T> _elementComparer;

        /// <summary>
        /// Creates an <see cref="ImmutableArraySequenceEqualComparer{T}"/> with a custom comparer for the elements of the collection.
        /// </summary>
        /// <param name="elementComparer">The comparer instance for the collection elements.</param>
        public ImmutableArraySequenceEqualComparer(IEqualityComparer<T> elementComparer)
        {
            _elementComparer = elementComparer;
        }

        public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
        {
            return x.SequenceEqual(y, _elementComparer);
        }

        public int GetHashCode(ImmutableArray<T> obj)
        {
            throw new UnreachableException();
        }
    }

    internal sealed class SyntaxEquivalentComparer : IEqualityComparer<SyntaxNode>
    {
        public static readonly SyntaxEquivalentComparer Instance = new();

        private SyntaxEquivalentComparer() { }

        public bool Equals(SyntaxNode x, SyntaxNode y)
        {
            return x.IsEquivalentTo(y);
        }

        public int GetHashCode(SyntaxNode obj)
        {
            throw new UnreachableException();
        }
    }

    internal sealed class CustomValueTupleElementComparer<T, U> : IEqualityComparer<(T, U)>
    {
        private readonly IEqualityComparer<T> _item1Comparer;
        private readonly IEqualityComparer<U> _item2Comparer;

        public CustomValueTupleElementComparer(IEqualityComparer<T> item1Comparer, IEqualityComparer<U> item2Comparer)
        {
            _item1Comparer = item1Comparer;
            _item2Comparer = item2Comparer;
        }

        public bool Equals((T, U) x, (T, U) y)
        {
            return _item1Comparer.Equals(x.Item1, y.Item1) && _item2Comparer.Equals(x.Item2, y.Item2);
        }

        public int GetHashCode((T, U) obj)
        {
            throw new UnreachableException();
        }
    }
}

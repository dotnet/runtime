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
        /// Comparer for an individual generated stub source as a syntax tree and the generated diagnostics for the stub.
        /// </summary>
        public static readonly IEqualityComparer<(MemberDeclarationSyntax Syntax, ImmutableArray<DiagnosticInfo> Diagnostics)> GeneratedSyntax = new CustomValueTupleElementComparer<MemberDeclarationSyntax, ImmutableArray<DiagnosticInfo>>(SyntaxEquivalentComparer.Instance, new ImmutableArraySequenceEqualComparer<DiagnosticInfo>(EqualityComparer<DiagnosticInfo>.Default));
        public static readonly IEqualityComparer<(MemberDeclarationSyntax, StatementSyntax, AttributeListSyntax, ImmutableArray<DiagnosticInfo>)> GeneratedSyntax4 =
            new CustomValueTupleElementComparer<MemberDeclarationSyntax, StatementSyntax, AttributeListSyntax, ImmutableArray<DiagnosticInfo>>(
                SyntaxEquivalentComparer.Instance, SyntaxEquivalentComparer.Instance, SyntaxEquivalentComparer.Instance,
                new ImmutableArraySequenceEqualComparer<DiagnosticInfo>(EqualityComparer<DiagnosticInfo>.Default));
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

    internal sealed class CustomValueTupleElementComparer<T, U, V, W> : IEqualityComparer<(T, U, V, W)>
    {
        private readonly IEqualityComparer<T> _item1Comparer;
        private readonly IEqualityComparer<U> _item2Comparer;
        private readonly IEqualityComparer<V> _item3Comparer;
        private readonly IEqualityComparer<W> _item4Comparer;

        public CustomValueTupleElementComparer(IEqualityComparer<T> item1Comparer, IEqualityComparer<U> item2Comparer, IEqualityComparer<V> item3Comparer, IEqualityComparer<W> item4Comparer)
        {
            _item1Comparer = item1Comparer;
            _item2Comparer = item2Comparer;
            _item3Comparer = item3Comparer;
            _item4Comparer = item4Comparer;
        }

        public bool Equals((T, U, V, W) x, (T, U, V, W) y)
        {
            return _item1Comparer.Equals(x.Item1, y.Item1)
                && _item2Comparer.Equals(x.Item2, y.Item2)
                && _item3Comparer.Equals(x.Item3, y.Item3)
                && _item4Comparer.Equals(x.Item4, y.Item4)
                ;
        }

        public int GetHashCode((T, U, V, W) obj)
        {
            throw new UnreachableException();
        }
    }
}

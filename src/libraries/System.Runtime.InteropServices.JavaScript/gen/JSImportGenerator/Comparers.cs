// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace Microsoft.Interop
{
    internal static class Comparers
    {
        /// <summary>
        /// Comparer for an individual generated stub source as a syntax tree and the registration statement and attribute.
        /// </summary>
        public static readonly IEqualityComparer<(MemberDeclarationSyntax, StatementSyntax, AttributeListSyntax)> GeneratedSyntax3 =
            new CustomValueTupleElementComparer<MemberDeclarationSyntax, StatementSyntax, AttributeListSyntax>(
                SyntaxEquivalentComparer.Instance, SyntaxEquivalentComparer.Instance, SyntaxEquivalentComparer.Instance);
    }

    internal sealed class CustomValueTupleElementComparer<T, U, V> : IEqualityComparer<(T, U, V)>
    {
        private readonly IEqualityComparer<T> _item1Comparer;
        private readonly IEqualityComparer<U> _item2Comparer;
        private readonly IEqualityComparer<V> _item3Comparer;

        public CustomValueTupleElementComparer(IEqualityComparer<T> item1Comparer, IEqualityComparer<U> item2Comparer, IEqualityComparer<V> item3Comparer)
        {
            _item1Comparer = item1Comparer;
            _item2Comparer = item2Comparer;
            _item3Comparer = item3Comparer;
        }

        public bool Equals((T, U, V) x, (T, U, V) y)
        {
            return _item1Comparer.Equals(x.Item1, y.Item1)
                && _item2Comparer.Equals(x.Item2, y.Item2)
                && _item3Comparer.Equals(x.Item3, y.Item3);
        }

        public int GetHashCode((T, U, V) obj)
        {
            throw new UnreachableException();
        }
    }
}

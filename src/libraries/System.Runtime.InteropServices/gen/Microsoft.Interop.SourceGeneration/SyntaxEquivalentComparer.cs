// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public sealed class SyntaxEquivalentComparer : IEqualityComparer<SyntaxNode>, IEqualityComparer<SyntaxToken>
    {
        public static readonly SyntaxEquivalentComparer Instance = new();

        private SyntaxEquivalentComparer() { }

        public bool Equals(SyntaxNode x, SyntaxNode y)
        {
            if ((x is null) != (y is null))
            {
                return false;
            }
            // Implies that y is also null.
            if (x is null)
            {
                return true;
            }
            return x.IsEquivalentTo(y);
        }

        public bool Equals(SyntaxToken x, SyntaxToken y)
        {
            return x.IsEquivalentTo(y);
        }

        public int GetHashCode(SyntaxNode obj)
        {
            throw new UnreachableException();
        }

        public int GetHashCode(SyntaxToken obj)
        {
            throw new UnreachableException();
        }
    }
}

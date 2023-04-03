// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public record struct SyntaxEquivalentNode<T>(T Node)
        where T : SyntaxNode
    {
        public bool Equals(SyntaxEquivalentNode<T> other)
        {
            return SyntaxEquivalentComparer.Instance.Equals(Node, other.Node);
        }

        public override int GetHashCode() => throw new UnreachableException();
    }
}

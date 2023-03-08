// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    internal static class IncrementalValuesProviderExtensions
    {
        public static IncrementalValuesProvider<(T Left, U Right)> Zip<T, U>(this IncrementalValuesProvider<T> left, IncrementalValuesProvider<U> right)
        {
            return left
                .Collect()
                .Combine(right.Collect())
                .SelectMany((data, ct) =>
                {
                    if (data.Left.Length != data.Right.Length)
                    {
                        throw new InvalidOperationException("The two value providers must provide the same number of values.");
                    }
                    ImmutableArray<(T, U)>.Builder builder = ImmutableArray.CreateBuilder<(T, U)>(data.Left.Length);
                    for (int i = 0; i < data.Left.Length; i++)
                    {
                        builder.Add((data.Left[i], data.Right[i]));
                    }
                    return builder.MoveToImmutable();
                });
        }

        public static IncrementalValuesProvider<TNode> SelectNormalized<TNode>(this IncrementalValuesProvider<TNode> provider)
            where TNode : SyntaxNode
        {
            return provider.Select((node, ct) => node.NormalizeWhitespace());
        }
    }
}

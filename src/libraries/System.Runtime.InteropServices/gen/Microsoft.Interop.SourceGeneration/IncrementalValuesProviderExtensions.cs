// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public static class IncrementalValuesProviderExtensions
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

        /// <summary>
        /// Format the syntax nodes in the given provider such that we will not re-normalize if the input nodes have not changed.
        /// </summary>
        /// <typeparam name="TNode">A syntax node kind.</typeparam>
        /// <param name="provider">The input nodes</param>
        /// <returns>A provider of the formatted syntax nodes.</returns>
        /// <remarks>
        /// Normalizing whitespace is very expensive, so if a generator will have cases where the input information into the step
        /// that creates <paramref name="provider"/> may change but the results of <paramref name="provider"/> will say the same,
        /// using this method to format the code in a separate step will reduce the amount of work the generator repeats when the
        /// output code will not change.
        /// </remarks>
        public static IncrementalValuesProvider<TNode> SelectNormalized<TNode>(this IncrementalValuesProvider<TNode> provider)
            where TNode : SyntaxNode
        {
            return provider.Select((node, ct) => node.NormalizeWhitespace());
        }

        public static (IncrementalValuesProvider<T>,  IncrementalValuesProvider<T2>) Split<T, T2>(this IncrementalValuesProvider<(T, T2)> provider)
        {
            return (provider.Select(static (data, ct) => data.Item1), provider.Select(static (data, ct) => data.Item2));
        }
    }
}

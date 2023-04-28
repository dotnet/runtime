// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

        public static IncrementalValuesProvider<(TGrouper, SequenceEqualImmutableArray<TGroupee>)> GroupTuples<TGrouper, TGroupee>(this IncrementalValuesProvider<(TGrouper Key, TGroupee Value)> values)
        {
            return values.Collect().SelectMany(static (values, ct) =>
            {
                var valueMap = new Dictionary<TGrouper, List<TGroupee>>();
                foreach (var value in values)
                {
                    if (!valueMap.TryGetValue(value.Key, out var list))
                    {
                        list = new();
                    }
                    list.Add(value.Value);
                    valueMap[value.Key] = list;
                }

                var builder = ImmutableArray.CreateBuilder<(TGrouper, SequenceEqualImmutableArray<TGroupee>)>(valueMap.Count);
                foreach (var kvp in valueMap)
                {
                    builder.Add((kvp.Key, kvp.Value.ToSequenceEqualImmutableArray()));
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
    }
}

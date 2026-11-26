// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public static class IncrementalValuesProviderExtensions
    {
        public static (IncrementalValuesProvider<T>, IncrementalValuesProvider<T2>) Split<T, T2>(this IncrementalValuesProvider<(T, T2)> provider)
        {
            return (provider.Select(static (data, ct) => data.Item1), provider.Select(static (data, ct) => data.Item2));
        }

        public static IncrementalValuesProvider<T> Concat<T>(this IncrementalValuesProvider<T> first, IncrementalValuesProvider<T> second)
        {
            return first.Collect().Combine(second.Collect()).SelectMany(static (data, ct) => data.Left.AddRange(data.Right));
        }
    }
}

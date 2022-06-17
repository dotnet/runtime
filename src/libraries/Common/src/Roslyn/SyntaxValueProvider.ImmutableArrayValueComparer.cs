﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DotnetRuntime.Extensions;

internal static partial class SyntaxValueProviderExtensions
{
    private sealed class ImmutableArrayValueComparer<T> : IEqualityComparer<ImmutableArray<T>>
    {
        public static readonly IEqualityComparer<ImmutableArray<T>> Instance = new ImmutableArrayValueComparer<T>();

        public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
        {
            if (x == y)
                return true;

            if (x.Length != y.Length)
                return false;

            for (int i = 0, n = x.Length; i < n; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(x[i], y[i]))
                    return false;
            }

            return true;
        }

        public int GetHashCode(ImmutableArray<T> obj)
        {
            var hashCode = 0;
            foreach (var value in obj)
                hashCode = Hash.Combine(hashCode, EqualityComparer<T>.Default.GetHashCode(value!));

            return hashCode;
        }
    }
}

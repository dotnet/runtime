// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace UnityEmbedHost.Generator;

static class ManagedUtils
{
    public static string FormatMethodParametersForMethodSignature(this IMethodSymbol methodSymbol) =>
        methodSymbol.Parameters.Select(p => $"{p.Type} {p.Name}")
            .AggregateWithCommaSpace();
}

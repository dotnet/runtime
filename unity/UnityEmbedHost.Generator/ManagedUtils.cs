// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Unity.CoreCLRHelpers;

namespace UnityEmbedHost.Generator;

static class ManagedUtils
{
    public static string FormatMethodParametersForMethodSignature(this IMethodSymbol methodSymbol) =>
        methodSymbol.Parameters.Select(p => $"{p.Type} {p.Name}")
            .AggregateWithCommaSpace();

    public static ManagedWrapperOptions ManagedWrapperOptions(this ISymbol symbol)
        => symbol.GetAttributes().ManagedWrapperOptions();

    public static ManagedWrapperOptions ManagedWrapperOptionsForReturnType(this IMethodSymbol symbol)
        => symbol.GetReturnTypeAttributes().ManagedWrapperOptions();

    public static ManagedWrapperOptions ManagedWrapperOptions(this ImmutableArray<AttributeData> attributes)
    {
        if (attributes.TryFirstAttributeValue(CoreCLRHostNativeWrappersGenerator.ManagedWrapperOptionsAttributeName, out int value))
            return (ManagedWrapperOptions)value;

        return Unity.CoreCLRHelpers.ManagedWrapperOptions.Default;
    }

    public static T? ManagedWrapperOptionsValue<T>(this ImmutableArray<AttributeData> attributes, int index)
    {
        if (attributes.TryFirstAttributeValue(CoreCLRHostNativeWrappersGenerator.ManagedWrapperOptionsAttributeName, out T? value, ctorParameterIndex: index))
        {
            return value;
        }

        return default;
    }
}

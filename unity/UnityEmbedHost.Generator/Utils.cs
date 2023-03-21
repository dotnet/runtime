// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace UnityEmbedHost.Generator;

static class Utils
{
    public static bool HasAttribute(this IMethodSymbol methodSymbol, string attributeName)
        => methodSymbol.GetAttributes().HasAttribute(attributeName);

    public static bool HasAttribute(this ImmutableArray<AttributeData> attributes, string attributeName)
        => attributes.FirstOrDefault(attr => attr.AttributeClass!.Name == attributeName) != null;

    public static bool TryReturnTypeFirstAttributeValue<T>(this IMethodSymbol methodSymbol, string attributeName, out T? value)
        => methodSymbol.GetReturnTypeAttributes().TryFirstAttributeValue(attributeName, out value);

    public static bool TryFirstAttributeValue<T>(this IMethodSymbol methodSymbol, string attributeName, out T? value)
        => methodSymbol.GetAttributes().TryFirstAttributeValue<T>(attributeName, out value);

    public static bool TryFirstAttributeValue<T>(this ImmutableArray<AttributeData> attributes, string attributeName, out T? value)
    {
        var explicitNativeTypeAttribute = attributes.FirstOrDefault(attr => attr.AttributeClass!.Name == attributeName);
        if (explicitNativeTypeAttribute != null)
        {
            value = (T)explicitNativeTypeAttribute.ConstructorArguments[0].Value!;
            return true;
        }

        value = default;
        return false;
    }

    public static string AggregateWithCommaSpace(this IEnumerable<string> elements)
        => elements.AggregateWith(", ", new StringBuilder());

    public static string AggregateWith(this IEnumerable<string> elements, string separator)
        => elements.AggregateWith(separator, new StringBuilder());

    public static string AggregateWithSpace(this IEnumerable<string> elements)
    {
        return elements.AggregateWithSpace(new StringBuilder());
    }

    public static string AggregateWithSpace(this IEnumerable<string> elements, StringBuilder builder)
    {
        return elements.AggregateWith(" ", builder);
    }

    public static string AggregateWith(this IEnumerable<string> elements, string separator, StringBuilder builder)
    {
        bool addSep = false;
        foreach (var item in elements)
        {
            if (addSep)
                builder.Append(separator);
            builder.Append(item);
            addSep = true;
        }

        return builder.ToString();
    }
}

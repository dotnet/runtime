// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace UnityEmbedHost.Generator;

static class NativeUtils
{
    public static string NativeWrapperName(this IMethodSymbol methodSymbol)
    {
        if (methodSymbol.TryFirstAttributeValue<string>(NativeGeneration.NativeWrapperNameAttributeName, out var value))
            return value!;
        return $"mono_{methodSymbol.Name}";
    }

    public static string NativeCallbackName(this IMethodSymbol methodSymbol)
        => methodSymbol.Name;

    public static string NativeCallbackType(this IParameterSymbol parameterSymbol)
        => NativeCallbackTypeFor(parameterSymbol.Type, parameterSymbol.GetAttributes());

    public static string NativeCallbackTypeForReturnType(this IMethodSymbol methodSymbol)
        => NativeCallbackTypeFor(methodSymbol.ReturnType, methodSymbol.GetReturnTypeAttributes());

    public static string NativeWrapperTypeFor(this IParameterSymbol parameterSymbol)
        => NativeWrapperTypeFor(parameterSymbol.Type, parameterSymbol.GetAttributes());

    public static string NativeWrapperTypeForReturnType(this IMethodSymbol methodSymbol)
        => NativeWrapperTypeFor(methodSymbol.ReturnType, methodSymbol.GetReturnTypeAttributes());

    public static string NativeWrapperTypeFor(this ITypeSymbol type, ImmutableArray<AttributeData> providerAttributes)
    {
        if (providerAttributes.TryFirstAttributeValue<string>(NativeGeneration.NativeWrapperTypeAttributeName, out var value))
            return value!;

        // If there is not a custom native wrapper type, fallback to the native callback type

        return type.NativeCallbackTypeFor(providerAttributes);
    }

    public static string NativeCallbackTypeFor(this ITypeSymbol type, ImmutableArray<AttributeData> providerAttributes)
    {
        if (providerAttributes.TryFirstAttributeValue<string>(NativeGeneration.NativeCallbackTypeAttributeName, out var value))
            return value!;

        return NativeTypeFor(type);
    }

    static string NativeTypeFor(ITypeSymbol type)
    {
        if (type is IPointerTypeSymbol pointerTypeSymbol)
        {
            return $"{NativeTypeFor(pointerTypeSymbol.PointedAtType)}*";
        }

        switch (type.Name)
        {
            case "IntPtr":
                return "intptr_t";
            case "UIntPtr":
                return "uintptr_t";
            case "Boolean":
                return "gboolean";
            case "Int32":
                return "int32_t";
            case "UInt32":
                return "guint32";
            case "Int64":
                return "int64_t";
            case "Byte":
                return "const char";
            case "UInt16":
                return "const guint16";
        }

        return $"UNHANDLED_MANAGED_WAS_{type.Name}";
    }
}

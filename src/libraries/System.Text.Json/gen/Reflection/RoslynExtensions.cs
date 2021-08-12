// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace System.Text.Json.Reflection
{
    internal static class RoslynExtensions
    {
        public static Type AsType(this ITypeSymbol typeSymbol, MetadataLoadContextInternal metadataLoadContext)
        {
            if (typeSymbol == null)
            {
                return null;
            }

            return new TypeWrapper(typeSymbol, metadataLoadContext);
        }

        public static MethodInfo AsMethodInfo(this IMethodSymbol methodSymbol, MetadataLoadContextInternal metadataLoadContext) => (methodSymbol == null ? null : new MethodInfoWrapper(methodSymbol, metadataLoadContext))!;

        public static IEnumerable<INamedTypeSymbol> BaseTypes(this INamedTypeSymbol typeSymbol)
        {
            var t = typeSymbol;
            while (t != null)
            {
                yield return t;
                t = t.BaseType;
            }
        }

        public static MethodAttributes GetMethodAttributes(this IMethodSymbol methodSymbol)
        {
            MethodAttributes attributes = default(MethodAttributes);

            if (methodSymbol.IsAbstract)
            {
                attributes |= MethodAttributes.Abstract;
            }

            if (methodSymbol.IsStatic)
            {
                attributes |= MethodAttributes.Static;
            }

            if (methodSymbol.IsVirtual || methodSymbol.IsOverride)
            {
                attributes |= MethodAttributes.Virtual;
            }

            switch (methodSymbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    attributes |= MethodAttributes.Public;
                    break;
                case Accessibility.Private:
                    attributes |= MethodAttributes.Private;
                    break;
                case Accessibility.Internal:
                    attributes |= MethodAttributes.Assembly;
                    break;
            }

            return attributes;
        }
    }
}

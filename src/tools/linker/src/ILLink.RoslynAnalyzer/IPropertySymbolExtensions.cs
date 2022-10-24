// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
    public static class IPropertySymbolExtensions
    {
        public static IMethodSymbol? GetGetMethod(this IPropertySymbol property)
        {
            IPropertySymbol? declaringProperty = property;
            IMethodSymbol? getMethod;
            while ((getMethod = declaringProperty.GetMethod) == null)
            {
                if ((declaringProperty = declaringProperty.OverriddenProperty) == null)
                    break;
            }
            return getMethod;
        }

        public static IMethodSymbol? GetSetMethod(this IPropertySymbol property)
        {
            IPropertySymbol? declaringProperty = property;
            IMethodSymbol? setMethod;
            while ((setMethod = declaringProperty.SetMethod) == null)
            {
                if ((declaringProperty = declaringProperty.OverriddenProperty) == null)
                    break;
            }
            return setMethod;
        }
    }
}

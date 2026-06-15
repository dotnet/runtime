// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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

        public static bool IsAutoProperty(this IPropertySymbol property)
        {
            if (property.IsAbstract)
                return false;

            return (property.GetMethod?.IsAutoAccessor() ?? false) || (property.SetMethod?.IsAutoAccessor() ?? false);
        }

        private static bool IsAutoAccessor(this IMethodSymbol method)
        {
            if (method == null || method.IsAbstract)
                return false;

            foreach (var decl in method.DeclaringSyntaxReferences)
            {
                var syntax = decl.GetSyntax();
                // Auto property accessors have no body in their syntax
                switch (syntax)
                {
                    case AccessorDeclarationSyntax a:
                        if (a.Body is not null)
                            return false;
                        if (a.ExpressionBody is not null)
                            return false;
                        return true;
                    default:
                        break;
                }
            }
            return false;
        }
    }
}

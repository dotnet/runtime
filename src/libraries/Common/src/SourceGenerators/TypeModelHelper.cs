// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace SourceGenerators
{
    internal static class TypeModelHelper
    {
        public static List<ITypeSymbol>? GetAllTypeArgumentsInScope(this INamedTypeSymbol type)
        {
            if (!type.IsGenericType)
            {
                return null;
            }

            List<ITypeSymbol>? args = null;
            TraverseContainingTypes(type);
            return args;

            void TraverseContainingTypes(INamedTypeSymbol current)
            {
                if (current.ContainingType is INamedTypeSymbol parent)
                {
                    TraverseContainingTypes(parent);
                }

                if (!current.TypeArguments.IsEmpty)
                {
                    (args ??= new()).AddRange(current.TypeArguments);
                }
            }
        }

        public static string GetFullyQualifiedName(this ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        /// <summary>
        /// Removes any type metadata that is erased at compile time, such as NRT annotations and tuple labels.
        /// </summary>
        public static ITypeSymbol EraseCompileTimeMetadata(this Compilation compilation, ITypeSymbol type)
        {
            if (type.NullableAnnotation is NullableAnnotation.Annotated)
            {
                type = type.WithNullableAnnotation(NullableAnnotation.None);
            }

            if (type is INamedTypeSymbol namedType)
            {
                if (namedType.IsTupleType)
                {
                    if (namedType.TupleElements.Length < 2)
                    {
                        return type;
                    }

                    ImmutableArray<ITypeSymbol> erasedElements = namedType.TupleElements
                        .Select(e => compilation.EraseCompileTimeMetadata(e.Type))
                        .ToImmutableArray();

                    type = compilation.CreateTupleTypeSymbol(erasedElements);
                }
                else if (namedType.IsGenericType)
                {
                    if (namedType.IsUnboundGenericType)
                    {
                        return namedType;
                    }

                    ImmutableArray<ITypeSymbol> typeArguments = namedType.TypeArguments;
                    INamedTypeSymbol? containingType = namedType.ContainingType;

                    if (containingType?.IsGenericType == true)
                    {
                        containingType = (INamedTypeSymbol)compilation.EraseCompileTimeMetadata(containingType);
                        type = namedType = containingType.GetTypeMembers().First(t => t.Name == namedType.Name && t.Arity == namedType.Arity);
                    }

                    if (typeArguments.Length > 0)
                    {
                        ITypeSymbol[] erasedTypeArgs = typeArguments
                            .Select(compilation.EraseCompileTimeMetadata)
                            .ToArray();

                        type = namedType.ConstructedFrom.Construct(erasedTypeArgs);
                    }
                }
            }

            return type;
        }
    }
}

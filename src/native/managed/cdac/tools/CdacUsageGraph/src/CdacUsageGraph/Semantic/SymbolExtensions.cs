// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CdacUsageGraph.Semantic;

/// <summary>Semantic relationship helpers for Roslyn symbols.</summary>
internal static class SymbolExtensions
{
    /// <summary>
    /// Enumerates every named type declared under a namespace, including nested types at every
    /// depth. Discovery passes use this shared traversal so they inspect the same compilation
    /// surface.
    /// </summary>
    public static IEnumerable<INamedTypeSymbol> EnumerateNamedTypes(this INamespaceSymbol @namespace)
    {
        foreach (INamedTypeSymbol type in @namespace.GetTypeMembers())
            foreach (INamedTypeSymbol nested in EnumerateTypeAndNested(type))
                yield return nested;

        foreach (INamespaceSymbol child in @namespace.GetNamespaceMembers())
            foreach (INamedTypeSymbol type in child.EnumerateNamedTypes())
                yield return type;
    }

    /// <summary>
    /// Returns whether C# permits an implicit conversion from <paramref name="source"/> to
    /// <paramref name="target"/>. Covers identity, class/interface inheritance, implemented
    /// interfaces, and applicable generic/variance conversions.
    /// </summary>
    public static bool IsAssignableTo(
        this CSharpCompilation compilation,
        ITypeSymbol source,
        ITypeSymbol target) =>
        compilation.ClassifyConversion(source, target).IsImplicit;

    private static IEnumerable<INamedTypeSymbol> EnumerateTypeAndNested(INamedTypeSymbol type)
    {
        yield return type;
        foreach (INamedTypeSymbol nested in type.GetTypeMembers())
            foreach (INamedTypeSymbol descendant in EnumerateTypeAndNested(nested))
                yield return descendant;
    }
}

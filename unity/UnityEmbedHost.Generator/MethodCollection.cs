// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace UnityEmbedHost.Generator;

static class MethodCollection
{
    private static INamedTypeSymbol FindTypeByName(INamespaceSymbol nsSymbol, string name)
    {
        foreach (var member in nsSymbol.GetMembers())
        {
            if (member == null)
                continue;

            if (member.IsNamespace)
                return FindTypeByName((INamespaceSymbol)member, name);

            var typeSymbol = (INamedTypeSymbol)member;
            if (typeSymbol.Name == name)
                return typeSymbol;
        }

        throw new ArgumentException($"Could not locate a type named {name}");
    }

    private static IEnumerable<IMethodSymbol> FindUnmanagedCallerMethods(INamespaceSymbol nsSymbol)
        => GetCallbackMethods(FindTypeByName(nsSymbol, "CoreCLRHost"));

    static IEnumerable<IMethodSymbol> GetCallbackMethods(INamedTypeSymbol typeSymbol) =>
        typeSymbol.GetMembers()
            .Where(member => member is IMethodSymbol methodSymbol && methodSymbol.DeclaredAccessibility == Accessibility.Public)
            .Cast<IMethodSymbol>();

    public static IEnumerable<IMethodSymbol> FindUnmanagedCallerMethods(GeneratorExecutionContext context)
        => FindUnmanagedCallerMethods(context.Compilation.GlobalNamespace);
}

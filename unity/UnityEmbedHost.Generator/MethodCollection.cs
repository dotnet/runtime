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
            {
                return FindTypeByName((INamespaceSymbol)member, name);
            }
            else
            {
                var typeSymbol = (INamedTypeSymbol)member;
                if (typeSymbol.Name == name)
                {
                    return typeSymbol;
                }
            }
        }

        throw new ArgumentException($"Could not locate a type named {name}");
    }

    private static IEnumerable<IMethodSymbol> FindUnmanagedCallerMethods(INamespaceSymbol nsSymbol)
    {
        var typeSymbol = FindTypeByName(nsSymbol, "CoreCLRHost");
        foreach (var method in GetCallbackMethods(typeSymbol))
            yield return method;
    }

    static IEnumerable<IMethodSymbol> GetCallbackMethods(INamedTypeSymbol typeSymbol)
    {
        foreach (var member in typeSymbol.GetMembers())
        {
            if (member is IMethodSymbol methodSymbol && methodSymbol.DeclaredAccessibility == Accessibility.Public)
            {
                yield return methodSymbol;
            }
        }
    }

    public static IEnumerable<IMethodSymbol> FindUnmanagedCallerMethods(GeneratorExecutionContext context)
    {
        return FindUnmanagedCallerMethods(context.Compilation.GlobalNamespace);
    }
}

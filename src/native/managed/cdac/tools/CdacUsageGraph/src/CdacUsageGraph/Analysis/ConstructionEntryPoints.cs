// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace CdacUsageGraph.Analysis;

internal static class ConstructionEntryPoints
{
    public static IReadOnlyCollection<ISymbol> Get(
        INamedTypeSymbol type,
        IMethodSymbol? constructor)
    {
        HashSet<ISymbol> entries = new(SymbolEqualityComparer.Default);
        if (constructor is not null)
            entries.Add(constructor.OriginalDefinition);

        for (INamedTypeSymbol? current = type;
            current is not null && SymbolEqualityComparer.Default.Equals(
                current.ContainingAssembly,
                type.ContainingAssembly);
            current = current.BaseType)
        {
            foreach (ISymbol member in current.OriginalDefinition.GetMembers())
            {
                if (member is IFieldSymbol { IsConst: false, IsImplicitlyDeclared: false } or
                    IPropertySymbol)
                    entries.Add(member);
            }
        }

        return entries;
    }
}

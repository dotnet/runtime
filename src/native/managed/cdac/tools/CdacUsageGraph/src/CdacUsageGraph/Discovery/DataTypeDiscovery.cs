// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CdacUsageGraph.Model;
using CdacUsageGraph.Semantic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CdacUsageGraph.Discovery;

/// <summary>Builds <see cref="DataDescriptorType"/> models from cDAC Data type symbols.</summary>
internal static class DataTypeDiscovery
{
    /// <summary>Builds the descriptor index by scanning every class in the generated Contracts compilation.</summary>
    public static DataTypeIndex BuildIndex(CSharpCompilation compilation)
    {
        CdacAttributeMatcher attributes = new(compilation);
        SymbolEqualityComparer comparer = SymbolEqualityComparer.Default;
        INamedTypeSymbol? iDataDefinition = compilation.GetTypeByMetadataName(
            CdacSymbols.IDataMetadataName);
        if (iDataDefinition is null)
            throw new InvalidOperationException($"Could not resolve {CdacSymbols.IDataMetadataName}.");

        Dictionary<INamedTypeSymbol, DataDescriptorType> typesBySymbol =
            new(comparer);

        foreach (INamedTypeSymbol candidate in compilation.Assembly.GlobalNamespace.EnumerateNamedTypes())
        {
            if (candidate.TypeKind != TypeKind.Class ||
                !compilation.IsAssignableTo(candidate, iDataDefinition.Construct(candidate)))
                continue;

            DataDescriptorType type = new(candidate, attributes.GetNames(candidate));
            typesBySymbol.Add(candidate, type);
        }

        return new DataTypeIndex(typesBySymbol);
    }

}

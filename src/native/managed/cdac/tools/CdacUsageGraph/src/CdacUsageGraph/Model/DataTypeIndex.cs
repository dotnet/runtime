// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace CdacUsageGraph.Model;

/// <summary>
/// Phase B (part 1): lookup index over the discovered <see cref="DataDescriptorType"/> objects.
/// Detection requires the real <c>IData&lt;TSelf&gt;</c> contract; cDAC names and descriptor
/// dependency metadata are owned by each <see cref="DataDescriptorType"/>.
/// </summary>
internal sealed class DataTypeIndex
{
    private readonly Dictionary<INamedTypeSymbol, DataDescriptorType> _typesBySymbol;
    private readonly Dictionary<string, DataDescriptorType> _typesByName;

    internal DataTypeIndex(Dictionary<INamedTypeSymbol, DataDescriptorType> typesBySymbol)
    {
        _typesBySymbol = typesBySymbol;
        _typesByName = new Dictionary<string, DataDescriptorType>(StringComparer.Ordinal);
        foreach (DataDescriptorType type in typesBySymbol.Values)
        {
            foreach (string name in type.Names)
                _typesByName[name] = type;
        }
    }

    public int Count => _typesBySymbol.Count;

    public IEnumerable<DataDescriptorType> Types => _typesBySymbol.Values;

    public bool TryGetDataType(ITypeSymbol? symbol, out DataDescriptorType info)
    {
        if (symbol is INamedTypeSymbol named)
            return _typesBySymbol.TryGetValue(named.OriginalDefinition, out info!);

        info = null!;
        return false;
    }

    public bool IsDataType(ITypeSymbol? symbol) => TryGetDataType(symbol, out _);

    /// <summary>Resolves a cDAC layout name from <c>GetTypeInfo(...)</c>.</summary>
    public bool TryGetType(string name, out DataDescriptorType info) =>
        _typesByName.TryGetValue(name, out info!);

    /// <summary>The discovered Data types that implement <paramref name="interfaceType"/>.</summary>
    public IEnumerable<DataDescriptorType> DataTypesImplementing(INamedTypeSymbol interfaceType) =>
        _typesBySymbol.Values.Where(info =>
            info.Symbol.AllInterfaces.Any(i =>
                SymbolEqualityComparer.Default.Equals(
                    i.OriginalDefinition, interfaceType.OriginalDefinition)));

}

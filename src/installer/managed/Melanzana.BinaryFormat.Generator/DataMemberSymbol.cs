using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace BinaryFormat
{
    /// <summary>
    /// Represents a member with no arguments, namely a field or property
    /// </summary>
    internal readonly struct DataMemberSymbol
    {
        private readonly ISymbol _symbol;
        public DataMemberSymbol(ISymbol symbol)
        {
            Debug.Assert(symbol is
                IFieldSymbol or
                IPropertySymbol { Parameters: { Length: 0 }});
            _symbol = symbol;
        }

        public ISymbol Symbol => _symbol;

        public ITypeSymbol Type => _symbol switch
        {
            IFieldSymbol f => f.Type,
            IPropertySymbol p => p.Type,
            _ => throw new InvalidOperationException()
        };

        public ImmutableArray<Location> Locations => _symbol.Locations;

        public string Name => _symbol.Name;
    }
}
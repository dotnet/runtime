// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public abstract record SimpleTypeSpec : TypeSpec
    {
        public SimpleTypeSpec(ITypeSymbol type) : base(type) { }
    }

    internal sealed record ConfigurationSectionSpec : SimpleTypeSpec
    {
        public ConfigurationSectionSpec(ITypeSymbol type) : base(type) { }
    }

    public sealed record ParsableFromStringSpec : SimpleTypeSpec
    {
        public ParsableFromStringSpec(ITypeSymbol type) : base(type) { }

        public required StringParsableTypeKind StringParsableTypeKind { get; init; }
    }

    public enum StringParsableTypeKind
    {
        None = 0,

        /// <summary>
        /// Declared types that can be assigned directly from IConfigurationSection.Value, i.e. string and typeof(object).
        /// </summary>
        AssignFromSectionValue = 1,
        Enum = 2,
        ByteArray = 3,
        Integer = 4,
        Float = 5,
        Parse = 6,
        ParseInvariant = 7,
        CultureInfo = 8,
        Uri = 9,
    }
}

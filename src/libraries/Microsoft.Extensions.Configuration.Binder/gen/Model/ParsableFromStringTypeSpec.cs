// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record ParsableFromStringTypeSpec : TypeSpec
    {
        public ParsableFromStringTypeSpec(ITypeSymbol type) : base(type) { }

        public override TypeSpecKind SpecKind => TypeSpecKind.ParsableFromString;

        public required StringParsableTypeKind StringParsableTypeKind { get; init; }

        private string? _parseMethodName;
        public string ParseMethodName
        {
            get
            {
                Debug.Assert(StringParsableTypeKind is not StringParsableTypeKind.ConfigValue);

                _parseMethodName ??= StringParsableTypeKind is StringParsableTypeKind.ByteArray
                    ? "ParseByteArray"
                    // MinimalDisplayString.Length is certainly > 2.
                    : $"Parse{(char.ToUpper(MinimalDisplayString[0]) + MinimalDisplayString.Substring(1)).Replace(".", "")}";

                return _parseMethodName;
            }
        }
    }

    internal enum StringParsableTypeKind
    {
        None = 0,
        ConfigValue = 1,
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record ParsableFromStringSpec : TypeSpec
    {
        public ParsableFromStringSpec(ITypeSymbol type) : base(type) { }

        public override TypeSpecKind SpecKind => TypeSpecKind.ParsableFromString;

        public required StringParsableTypeKind StringParsableTypeKind { get; init; }

        private string? _parseMethodName;
        public string ParseMethodName
        {
            get
            {
                Debug.Assert(StringParsableTypeKind is not StringParsableTypeKind.AssignFromSectionValue);

                _parseMethodName ??= StringParsableTypeKind is StringParsableTypeKind.ByteArray
                    ? "ParseByteArray"
                    // MinimalDisplayString.Length is certainly > 2.
                    : $"Parse{(char.ToUpper(DisplayString[0]) + DisplayString.Substring(1)).Replace(".", "")}";

                return _parseMethodName;
            }
        }
    }

    internal enum StringParsableTypeKind
    {
        None = 0,

        /// <summary>
        /// Declared types that can be assigned directly from IConfigurationSection.Value, i.e. string and tyepof(object).
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

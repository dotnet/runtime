// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record ParameterSpec
    {
        public ParameterSpec(IParameterSymbol parameter)
        {
            Name = parameter.Name;
            RefKind = parameter.RefKind;

            HasExplicitDefaultValue = parameter.HasExplicitDefaultValue;
            if (HasExplicitDefaultValue)
            {
                string formatted = SymbolDisplay.FormatPrimitive(parameter.ExplicitDefaultValue, quoteStrings: true, useHexadecimalNumbers: false);
                DefaultValue = formatted is "null" ? "default!" : formatted;
            }
        }

        public required TypeSpec Type { get; init; }

        public string Name { get; }

        public required string ConfigurationKeyName { get; init; }

        public RefKind RefKind { get; }

        public bool HasExplicitDefaultValue { get; init; }

        public string DefaultValue { get; } = "default!";
    }
}

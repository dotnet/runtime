// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record ParameterSpec
    {
        public required TypeSpec? Type { get; init; }
        public string Name { get; }
        public PropertySpec? MatchingProperty { get; set; }
        public RefKind RefKind { get; }
        public string DefaultValue { get; } = "default!";

        public ParameterSpec(IParameterSymbol parameter)
        {
            Name = parameter.Name;
            RefKind = parameter.RefKind;

            if (parameter.HasExplicitDefaultValue)
            {
                string formatted = SymbolDisplay.FormatPrimitive(parameter.ExplicitDefaultValue, quoteStrings: true, useHexadecimalNumbers: false);
                DefaultValue = formatted is "null" ? "default!" : formatted;
            }
        }

        public string GetExpressionForArgument(string argument) => RefKind switch
        {
            RefKind.None => argument,
            RefKind.Ref => $"ref {argument}",
            RefKind.Out => "out _",
            RefKind.In => $"in {argument}",
            _ => throw new InvalidOperationException("Unknown ref kind")
        };
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal sealed record ParameterSpec : MemberSpec
    {
        public ParameterSpec(IParameterSymbol parameter) : base(parameter)
        {
            RefKind = parameter.RefKind;

            if (parameter.HasExplicitDefaultValue)
            {
                string formatted = SymbolDisplay.FormatPrimitive(parameter.ExplicitDefaultValue, quoteStrings: true, useHexadecimalNumbers: false);
                DefaultValueExpr = formatted is "null" ? "default!" : formatted;
            }
            else
            {
                DefaultValueExpr = "default!";
                ErrorOnFailedBinding = true;
            }
        }

        public RefKind RefKind { get; }

        public override string DefaultValueExpr { get; }

        public override bool CanGet => false;

        public override bool CanSet => true;

        public override bool ErrorOnFailedBinding { get; }
    }
}

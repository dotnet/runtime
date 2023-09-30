// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed record ParameterSpec : MemberSpec
    {
        public ParameterSpec(IParameterSymbol parameter) : base(parameter)
        {
            RefKind = parameter.RefKind;

            if (parameter.HasExplicitDefaultValue)
            {
                string formatted = SymbolDisplay.FormatPrimitive(parameter.ExplicitDefaultValue!, quoteStrings: true, useHexadecimalNumbers: false);
                if (formatted is not "null")
                {
                    DefaultValueExpr = formatted;
                }
            }
            else
            {
                ErrorOnFailedBinding = true;
            }
        }

        public bool ErrorOnFailedBinding { get; private set; }

        public RefKind RefKind { get; }

        public override bool CanGet => false;

        public override bool CanSet => true;
    }
}

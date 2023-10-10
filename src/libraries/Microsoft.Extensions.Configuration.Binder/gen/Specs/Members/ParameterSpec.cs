// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed record ParameterSpec : MemberSpec
    {
        public ParameterSpec(IParameterSymbol parameter, TypeRef typeRef) : base(parameter, typeRef)
        {
            RefKind = parameter.RefKind;

            if (parameter.HasExplicitDefaultValue)
            {
                DefaultValueExpr = CSharpSyntaxUtilities.FormatLiteral(parameter.ExplicitDefaultValue, TypeRef);
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

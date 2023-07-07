// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    internal abstract record MemberSpec
    {
        public MemberSpec(ISymbol member)
        {
            Debug.Assert(member is IPropertySymbol or IParameterSymbol);
            Name = member.Name;
            DefaultValueExpr = "default";
        }

        public string Name { get; }
        public bool ErrorOnFailedBinding { get; protected set; }
        public string DefaultValueExpr { get; protected set; }

        public required TypeSpec Type { get; init; }
        public required string ConfigurationKeyName { get; init; }

        public abstract bool CanGet { get; }
        public abstract bool CanSet { get; }
    }
}

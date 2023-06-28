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
        }

        public required TypeSpec Type { get; init; }
        public string Name { get; }
        public required string ConfigurationKeyName { get; init; }
        public abstract string DefaultValueExpr { get; }
        public abstract bool CanGet { get; }
        public abstract bool CanSet { get; }
        public abstract bool ErrorOnFailedBinding { get; }
    }
}

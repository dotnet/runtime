// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using SourceGenerators;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public abstract record MemberSpec
    {
        public MemberSpec(ISymbol member, TypeRef typeRef)
        {
            Debug.Assert(member is IPropertySymbol or IParameterSymbol);
            Name = member.Name;
            DeclaringTypeFullName = member.ContainingType.GetFullName();
            DefaultValueExpr = "default";
            TypeRef = typeRef;
        }

        public string Name { get; }
        public string DeclaringTypeFullName { get; }
        public string DefaultValueExpr { get; protected set; }

        public TypeRef TypeRef { get; }
        public required string ConfigurationKeyName { get; init; }
        public TypeConverterSpec? TypeConverter { get; init; }
        public TypeConverterSpec? FallbackTypeConverter { get; init; }
        public bool HasUnresolvedTypeConverterFallback { get; init; }

        public abstract bool CanGet { get; }
        public abstract bool CanSet { get; }
    }

    public sealed record TypeConverterSpec(TypeRef ConverterType, TypeRef TargetType, string Identifier, bool PassTargetType);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private sealed record ClassAttributeValue(
        AttributeValue<TypeAttributes> Attribute,
        EntityRegistry.WellKnownBaseType? FallbackBase,
        bool RequireSealed);

    private sealed record ClassHeaderValue(
        bool IsValid,
        IToken? NameToken,
        string FullName,
        ImmutableArray<ClassAttributeValue> Attributes,
        ImmutableArray<GenericParameterDeclarationValue> GenericParameters,
        TypeSpecificationValue? BaseType,
        ImmutableArray<TypeSpecificationValue> Interfaces)
    {
        public static ClassHeaderValue Error { get; } = new(
            false,
            null,
            string.Empty,
            [],
            [],
            null,
            []);
    }

    private static ClassAttributeValue GetClassAttributeValue(object? value)
        => value as ClassAttributeValue ?? new(new(0, 0, true), null, false);

    private static ClassHeaderValue GetClassHeaderValue(object? value)
        => value as ClassHeaderValue ?? ClassHeaderValue.Error;

    private static ImmutableArray<TypeSpecificationValue> GetInterfaceTypes(object? value)
        => value is ImmutableArray<TypeSpecificationValue> interfaces ? interfaces : [];
}

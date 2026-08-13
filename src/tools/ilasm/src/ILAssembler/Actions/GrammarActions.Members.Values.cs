// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private sealed record FieldDeclarationValue(
        bool IsValid,
        FieldAttributes Attributes,
        TypeValue FieldType,
        string Name,
        MarshallingDescriptorValue Marshalling,
        string? DataDeclarationName,
        int? Offset,
        object? ConstantValue)
    {
        public static FieldDeclarationValue Error { get; } = new(
            false,
            0,
            TypeValue.Error,
            string.Empty,
            GetMarshallingDescriptorValue(null),
            null,
            null,
            NoConstantSentinel.Instance);
    }

    private sealed record PropertyHeaderValue(
        bool IsValid,
        PropertyAttributes Attributes,
        byte CallingConvention,
        TypeValue PropertyType,
        string Name,
        ImmutableArray<SignatureArgumentValue> Arguments,
        object? ConstantValue)
    {
        public static PropertyHeaderValue Error { get; } = new(
            false,
            0,
            0,
            TypeValue.Error,
            string.Empty,
            [],
            NoConstantSentinel.Instance);
    }

    private sealed record EventHeaderValue(
        bool IsValid,
        EventAttributes Attributes,
        TypeSpecificationValue? EventType,
        string Name)
    {
        public static EventHeaderValue Error { get; } = new(false, 0, null, string.Empty);
    }

    private static FieldDeclarationValue GetFieldDeclarationValue(object? value)
        => value as FieldDeclarationValue ?? FieldDeclarationValue.Error;

    private static PropertyHeaderValue GetPropertyHeaderValue(object? value)
        => value as PropertyHeaderValue ?? PropertyHeaderValue.Error;

    private static EventHeaderValue GetEventHeaderValue(object? value)
        => value as EventHeaderValue ?? EventHeaderValue.Error;
}

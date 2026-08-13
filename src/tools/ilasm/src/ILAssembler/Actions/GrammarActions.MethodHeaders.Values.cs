// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private sealed record AttributeValue<T>(T Value, T GroupMask, bool ShouldAppend)
        where T : struct, System.Enum;

    private sealed record PInvokeValue(
        string? ModuleName,
        string? EntryPointName,
        MethodImportAttributes Attributes);

    private sealed record GenericParameterDeclarationValue(
        GenericParameterAttributes Attributes,
        string Name,
        ImmutableArray<TypeSpecificationValue> Constraints);

    private sealed record MethodHeaderValue(
        bool IsValid,
        MethodAttributes Attributes,
        ImmutableArray<PInvokeValue> PInvokes,
        byte CallingConvention,
        int ReturnAttributes,
        TypeValue ReturnType,
        MarshallingDescriptorValue ReturnMarshalling,
        string Name,
        ImmutableArray<GenericParameterDeclarationValue> GenericParameters,
        ImmutableArray<SignatureArgumentValue> Arguments,
        MethodImplAttributes ImplementationAttributes)
    {
        public static MethodHeaderValue Error { get; } = new(
            false,
            0,
            [],
            0,
            0,
            TypeValue.Error,
            GetMarshallingDescriptorValue(null),
            string.Empty,
            [],
            [],
            0);
    }

    private static AttributeValue<T> GetAttributeValue<T>(object? value)
        where T : struct, System.Enum
        => value as AttributeValue<T> ?? new(default, default, true);

    private static T ApplyAttribute<T>(T current, AttributeValue<T> attribute)
        where T : struct, System.Enum
    {
        if (!attribute.ShouldAppend)
        {
            return attribute.Value;
        }

        int currentValue = System.Convert.ToInt32(current);
        int groupMask = System.Convert.ToInt32(attribute.GroupMask);
        int attributeValue = System.Convert.ToInt32(attribute.Value);
        return (T)System.Enum.ToObject(typeof(T), (currentValue & ~groupMask) | attributeValue);
    }

    private static ImmutableArray<GenericParameterDeclarationValue> GetGenericParameterDeclarations(object? value)
        => value is ImmutableArray<GenericParameterDeclarationValue> parameters ? parameters : [];

    private static GenericParameterDeclarationValue GetGenericParameterDeclaration(object? value)
        => value as GenericParameterDeclarationValue ?? new(0, string.Empty, []);

    private static MethodHeaderValue GetMethodHeaderValue(object? value)
        => value as MethodHeaderValue ?? MethodHeaderValue.Error;

    private static PInvokeValue GetPInvokeValue(object? value)
        => value as PInvokeValue ?? new(null, null, 0);
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection;
using Antlr4.Runtime;

namespace ILAssembler;

public partial class CILParser
{
    public sealed record AttributeValue<T>(T Value, T GroupMask, bool ShouldAppend)
        where T : struct, System.Enum
    {
        public static AttributeValue<T> Empty { get; } = new(default, default, true);
    }

    public sealed record PInvokeValue(
        string? ModuleName,
        string? EntryPointName,
        MethodImportAttributes Attributes);

    public sealed record GenericParameterDeclarationValue(
        GenericParameterAttributes Attributes,
        string Name,
        ImmutableArray<TypeSpecificationValue> Constraints)
    {
        public static GenericParameterDeclarationValue Error { get; } =
            new(0, string.Empty, []);
    }

    public sealed record MethodHeaderValue(
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
            MarshallingDescriptorValue.Empty,
            string.Empty,
            [],
            [],
            0);
    }

    public sealed record FieldDeclarationValue(
        bool IsValid,
        FieldAttributes Attributes,
        TypeValue FieldType,
        string Name,
        MarshallingDescriptorValue Marshalling,
        string? DataDeclarationName,
        int? Offset,
        FieldInitializerValue Initializer)
    {
        public static FieldDeclarationValue Error { get; } = new(
            false,
            0,
            TypeValue.Error,
            string.Empty,
            MarshallingDescriptorValue.Empty,
            null,
            null,
            FieldInitializerValue.Empty);
    }

    public sealed record PropertyHeaderValue(
        bool IsValid,
        PropertyAttributes Attributes,
        byte CallingConvention,
        TypeValue PropertyType,
        string Name,
        ImmutableArray<SignatureArgumentValue> Arguments,
        FieldInitializerValue Initializer)
    {
        public static PropertyHeaderValue Error { get; } = new(
            false,
            0,
            0,
            TypeValue.Error,
            string.Empty,
            [],
            FieldInitializerValue.Empty);
    }

    public sealed record EventHeaderValue(
        bool IsValid,
        EventAttributes Attributes,
        TypeSpecificationValue? EventType,
        string Name)
    {
        public static EventHeaderValue Error { get; } =
            new(false, 0, null, string.Empty);
    }

    public sealed class ClassAttributeValue
    {
        public static ClassAttributeValue Empty { get; } =
            new(AttributeValue<TypeAttributes>.Empty, null, false);

        internal ClassAttributeValue(
            AttributeValue<TypeAttributes> attribute,
            EntityRegistry.WellKnownBaseType? fallbackBase,
            bool requireSealed)
        {
            Attribute = attribute;
            FallbackBase = fallbackBase;
            RequireSealed = requireSealed;
        }

        internal AttributeValue<TypeAttributes> Attribute { get; }

        internal EntityRegistry.WellKnownBaseType? FallbackBase { get; }

        internal bool RequireSealed { get; }
    }

    public sealed record ClassHeaderValue(
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

    public sealed class MethodHeaderBuilder
    {
        internal MethodAttributes Attributes { get; set; }

        internal ImmutableArray<PInvokeValue>.Builder PInvokes { get; } =
            ImmutableArray.CreateBuilder<PInvokeValue>();

        internal MethodImplAttributes ImplementationAttributes { get; set; }
    }

    public sealed class PInvokeBuilder
    {
        internal string? ModuleName { get; set; }

        internal string? EntryPointName { get; set; }

        internal MethodImportAttributes Attributes { get; set; }
    }

    public sealed class FieldDeclarationBuilder
    {
        internal FieldAttributes Attributes { get; set; }

        internal MarshallingDescriptorValue Marshalling { get; set; } =
            MarshallingDescriptorValue.Empty;
    }

    public sealed class PropertyHeaderBuilder
    {
        internal PropertyAttributes Attributes { get; set; }
    }

    public sealed class EventHeaderBuilder
    {
        internal EventAttributes Attributes { get; set; }
    }

    public sealed class ClassHeaderBuilder
    {
        internal ImmutableArray<ClassAttributeValue>.Builder Attributes { get; } =
            ImmutableArray.CreateBuilder<ClassAttributeValue>();
    }

    public sealed class PropertyBodyValue
    {
        internal PropertyBodyValue(EntityRegistry.PropertyEntity? property)
        {
            Property = property;
        }

        internal EntityRegistry.PropertyEntity? Property { get; }
    }

    public sealed class EventBodyValue
    {
        internal EventBodyValue(EntityRegistry.EventEntity? @event)
        {
            Event = @event;
        }

        internal EntityRegistry.EventEntity? Event { get; }
    }

    public sealed class CustomAttributeOwnerValue
    {
        internal CustomAttributeOwnerValue(EntityRegistry.EntityBase? owner)
        {
            Owner = owner;
        }

        internal EntityRegistry.EntityBase? Owner { get; }
    }
}

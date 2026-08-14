// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal void AddPropertyAttribute(
        CILParser.PropertyHeaderBuilder builder,
        CILParser.AttributeValue<PropertyAttributes> value)
        => builder.Attributes = ApplyAttribute(builder.Attributes, value);

    internal PropertyHeaderValue CreatePropertyHeader(
        CILParser.PropHeadContext context,
        CILParser.PropertyHeaderBuilder builder,
        int initialSyntaxErrorCount,
        byte callingConvention,
        TypeValue propertyType,
        string name,
        System.Collections.Immutable.ImmutableArray<SignatureArgumentValue> arguments,
        FieldInitializerValue initializer)
    {
        if (HasSyntaxErrorsSince(initialSyntaxErrorCount) ||
            context.exception is not null)
        {
            return PropertyHeaderValue.Error;
        }

        return new PropertyHeaderValue(
            true,
            builder.Attributes,
            callingConvention,
            propertyType,
            name,
            arguments,
            initializer);
    }

    internal CILParser.AttributeValue<PropertyAttributes> CreatePropertyAttribute(IToken token)
        => token.Text switch
        {
            "specialname" => new CILParser.AttributeValue<PropertyAttributes>(
                PropertyAttributes.SpecialName,
                0,
                true),
            "rtspecialname" => new CILParser.AttributeValue<PropertyAttributes>(0, 0, true),
            _ => throw new UnreachableException(),
        };

    internal CILParser.PropertyBodyValue BeginProperty(PropertyHeaderValue value)
    {
        PrepareClassMember();
        EntityRegistry.PropertyEntity? property = null;
        if (value.IsValid && _currentTypeDefinition.PeekOrDefault() is { } currentType)
        {
            BlobBuilder signature = new();
            signature.WriteByte(
                (byte)(value.CallingConvention | (byte)SignatureKind.Property));
            signature.WriteCompressedInteger(value.Arguments.Length);
            MaterializeType(value.PropertyType).WriteContentTo(signature);
            foreach (SignatureArgumentValue argument in value.Arguments)
            {
                MaterializeSignatureArgument(argument).SignatureBlob.WriteContentTo(signature);
            }

            property = new EntityRegistry.PropertyEntity(value.Attributes, signature, value.Name);
            if (value.Initializer.HasValue)
            {
                property.ConstantValue = value.Initializer.ConstantValue;
                property.HasConstant = true;
                property.Attributes |= PropertyAttributes.HasDefault;
            }

            currentType.Properties.Add(property);
        }

        return new CILParser.PropertyBodyValue(property);
    }

    internal void AddPropertySetter(
        CILParser.PropertyBodyValue body,
        MethodReferenceValue value)
        => AddPropertyAccessor(body, MethodSemanticsAttributes.Setter, value);

    internal void AddPropertyGetter(
        CILParser.PropertyBodyValue body,
        MethodReferenceValue value)
        => AddPropertyAccessor(body, MethodSemanticsAttributes.Getter, value);

    internal void AddPropertyOther(
        CILParser.PropertyBodyValue body,
        MethodReferenceValue value)
        => AddPropertyAccessor(body, MethodSemanticsAttributes.Other, value);

    private void AddPropertyAccessor(
        CILParser.PropertyBodyValue body,
        MethodSemanticsAttributes semantics,
        MethodReferenceValue value)
    {
        if (body.Property is { } property)
        {
            property.Accessors.Add(
                (semantics, MaterializeMethodReference(value)));
        }
    }

    internal void AddPropertyCustomAttribute(
        CILParser.PropertyBodyValue body,
        CILParser.CustomAttrDeclContext attribute)
    {
        if (attribute.HasSyntaxError ||
            body.Property is not { } property)
        {
            return;
        }

        if (MaterializeCustomAttributeDeclaration(attribute) is { } customAttribute)
        {
            customAttribute.Owner = property;
        }
    }

    internal void ProcessPropertySourceDirective(
        CILParser.PropertyBodyValue body,
        CILParser.ExtSourceSpecContext context)
    {
        _ = body;
        _ = context;
    }

    internal void ProcessPropertyLanguageDirective(
        CILParser.PropertyBodyValue body,
        CILParser.LanguageDeclContext context)
    {
        _ = body;
        _ = context;
    }

    internal void AddEventAttribute(
        CILParser.EventHeaderBuilder builder,
        CILParser.AttributeValue<EventAttributes> value)
        => builder.Attributes = ApplyAttribute(builder.Attributes, value);

    internal EventHeaderValue CreateEventHeader(
        CILParser.EventHeadContext context,
        CILParser.EventHeaderBuilder builder,
        int initialSyntaxErrorCount,
        TypeSpecificationValue? eventType,
        string name)
    {
        if (HasSyntaxErrorsSince(initialSyntaxErrorCount) ||
            context.exception is not null)
        {
            return EventHeaderValue.Error;
        }

        return new EventHeaderValue(
            true,
            builder.Attributes,
            eventType,
            name);
    }

    internal CILParser.AttributeValue<EventAttributes> CreateEventAttribute(IToken token)
        => token.Text switch
        {
            "specialname" => new CILParser.AttributeValue<EventAttributes>(
                EventAttributes.SpecialName,
                0,
                true),
            "rtspecialname" => new CILParser.AttributeValue<EventAttributes>(0, 0, true),
            _ => throw new UnreachableException(),
        };

    internal CILParser.EventBodyValue BeginEvent(EventHeaderValue value)
    {
        PrepareClassMember();
        EntityRegistry.EventEntity? @event = null;
        if (value.IsValid && _currentTypeDefinition.PeekOrDefault() is { } currentType)
        {
            @event = new EntityRegistry.EventEntity(
                value.Attributes,
                value.EventType is null
                    ? null
                    : ResolveTypeSpecification(value.EventType),
                value.Name);
            currentType.Events.Add(@event);
        }

        return new CILParser.EventBodyValue(@event);
    }

    internal void AddEventAdder(CILParser.EventBodyValue body, MethodReferenceValue value)
        => AddEventAccessor(body, MethodSemanticsAttributes.Adder, value);

    internal void AddEventRemover(CILParser.EventBodyValue body, MethodReferenceValue value)
        => AddEventAccessor(body, MethodSemanticsAttributes.Remover, value);

    internal void AddEventRaiser(CILParser.EventBodyValue body, MethodReferenceValue value)
        => AddEventAccessor(body, MethodSemanticsAttributes.Raiser, value);

    internal void AddEventOther(CILParser.EventBodyValue body, MethodReferenceValue value)
        => AddEventAccessor(body, MethodSemanticsAttributes.Other, value);

    private void AddEventAccessor(
        CILParser.EventBodyValue body,
        MethodSemanticsAttributes semantics,
        MethodReferenceValue value)
    {
        if (body.Event is { } @event)
        {
            @event.Accessors.Add(
                (semantics, MaterializeMethodReference(value)));
        }
    }

    internal void AddEventCustomAttribute(
        CILParser.EventBodyValue body,
        CILParser.CustomAttrDeclContext attribute)
    {
        if (attribute.HasSyntaxError ||
            body.Event is not { } @event)
        {
            return;
        }

        if (MaterializeCustomAttributeDeclaration(attribute) is { } customAttribute)
        {
            customAttribute.Owner = @event;
        }
    }

    internal void ProcessEventSourceDirective(
        CILParser.EventBodyValue body,
        CILParser.ExtSourceSpecContext context)
    {
        _ = body;
        _ = context;
    }

    internal void ProcessEventLanguageDirective(
        CILParser.EventBodyValue body,
        CILParser.LanguageDeclContext context)
    {
        _ = body;
        _ = context;
    }
}

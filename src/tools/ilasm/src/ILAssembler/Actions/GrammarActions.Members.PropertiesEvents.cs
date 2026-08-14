// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly Stack<PropertyHeaderFrame> _propertyHeaderFrames = new();
    private readonly Stack<EventHeaderFrame> _eventHeaderFrames = new();
    private readonly Stack<PropertyBodyFrame> _propertyBodyFrames = new();
    private readonly Stack<EventBodyFrame> _eventBodyFrames = new();

    private sealed class PropertyHeaderFrame
    {
        public PropertyHeaderFrame(CILParser.PropHeadContext owner, int initialSyntaxErrorCount)
        {
            Owner = owner;
            InitialSyntaxErrorCount = initialSyntaxErrorCount;
        }

        public CILParser.PropHeadContext Owner { get; }

        public int InitialSyntaxErrorCount { get; }

        public PropertyAttributes Attributes { get; set; }
    }

    private sealed class EventHeaderFrame
    {
        public EventHeaderFrame(CILParser.EventHeadContext owner, int initialSyntaxErrorCount)
        {
            Owner = owner;
            InitialSyntaxErrorCount = initialSyntaxErrorCount;
        }

        public CILParser.EventHeadContext Owner { get; }

        public int InitialSyntaxErrorCount { get; }

        public EventAttributes Attributes { get; set; }
    }

    private sealed record PropertyBodyFrame(
        CILParser.ClassDeclContext Owner,
        EntityRegistry.PropertyEntity? Property);

    private sealed record EventBodyFrame(
        CILParser.ClassDeclContext Owner,
        EntityRegistry.EventEntity? Event);

    internal void BeginPropertyHeader(CILParser.PropHeadContext context)
        => _propertyHeaderFrames.Push(new(context, _syntaxErrorCount));

    internal void AddPropertyAttribute(CILParser.PropHeadContext context, object? value)
    {
        if (TryGetPropertyHeaderFrame(context) is { } frame)
        {
            frame.Attributes = ApplyAttribute(
                frame.Attributes,
                GetAttributeValue<PropertyAttributes>(value));
        }
    }
    internal object CreatePropertyHeader(
        CILParser.PropHeadContext context,
        byte callingConvention,
        object? propertyType,
        string name,
        object? arguments,
        object? initializer)
    {
        PropertyHeaderFrame? frame = TryGetPropertyHeaderFrame(context);
        if (frame is null ||
            frame.InitialSyntaxErrorCount != _syntaxErrorCount ||
            context.exception is not null)
        {
            return PropertyHeaderValue.Error;
        }

        return new PropertyHeaderValue(
            true,
            frame.Attributes,
            callingConvention,
            GetTypeValue(propertyType),
            name,
            GetSignatureArgumentsValue(arguments),
            initializer);
    }

    internal void EndPropertyHeader(CILParser.PropHeadContext context)
    {
        if (_propertyHeaderFrames.Count == 0)
        {
            return;
        }

        PropertyHeaderFrame frame = _propertyHeaderFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            _propertyHeaderFrames.Pop();
        }
    }

    internal object CreatePropertyAttribute(IToken token)
        => token.Text switch
        {
            "specialname" => new AttributeValue<PropertyAttributes>(
                PropertyAttributes.SpecialName,
                0,
                true),
            "rtspecialname" => new AttributeValue<PropertyAttributes>(0, 0, true),
            _ => throw new UnreachableException(),
        };

    internal void BeginProperty(CILParser.ClassDeclContext context, object? value)
    {
        PrepareClassMember();
        PropertyHeaderValue header = GetPropertyHeaderValue(value);
        EntityRegistry.PropertyEntity? property = null;
        if (header.IsValid && _currentTypeDefinition.PeekOrDefault() is { } currentType)
        {
            BlobBuilder signature = new();
            signature.WriteByte(
                (byte)(header.CallingConvention | (byte)SignatureKind.Property));
            signature.WriteCompressedInteger(header.Arguments.Length);
            MaterializeType(header.PropertyType).WriteContentTo(signature);
            foreach (SignatureArgumentValue argument in header.Arguments)
            {
                MaterializeSignatureArgument(argument).SignatureBlob.WriteContentTo(signature);
            }

            property = new EntityRegistry.PropertyEntity(header.Attributes, signature, header.Name);
            if (header.ConstantValue is not NoConstantSentinel)
            {
                property.ConstantValue = header.ConstantValue;
                property.HasConstant = true;
                property.Attributes |= PropertyAttributes.HasDefault;
            }

            currentType.Properties.Add(property);
        }

        _propertyBodyFrames.Push(new(context, property));
    }

    internal void AddPropertySetter(CILParser.PropDeclContext context, object? value)
        => AddPropertyAccessor(context, MethodSemanticsAttributes.Setter, value);

    internal void AddPropertyGetter(CILParser.PropDeclContext context, object? value)
        => AddPropertyAccessor(context, MethodSemanticsAttributes.Getter, value);

    internal void AddPropertyOther(CILParser.PropDeclContext context, object? value)
        => AddPropertyAccessor(context, MethodSemanticsAttributes.Other, value);

    private void AddPropertyAccessor(
        CILParser.PropDeclContext context,
        MethodSemanticsAttributes semantics,
        object? value)
    {
        if (TryGetPropertyBodyFrame(context) is { Property: { } property })
        {
            property.Accessors.Add(
                (semantics, MaterializeMethodReference(GetMethodReferenceValue(value))));
        }
    }

    internal void AddPropertyCustomAttribute(
        CILParser.PropDeclContext context,
        CILParser.CustomAttrDeclContext attribute)
    {
        if (attribute.HasSyntaxError ||
            TryGetPropertyBodyFrame(context) is not { Property: { } property })
        {
            return;
        }

        if (VisitCustomAttrDecl(attribute).Value is { } customAttribute)
        {
            customAttribute.Owner = property;
        }
    }

    internal void ProcessPropertySourceDirective(CILParser.ExtSourceSpecContext context)
        => _ = context;

    internal void ProcessPropertyLanguageDirective(CILParser.LanguageDeclContext context)
        => _ = context;

    internal void BeginEventHeader(CILParser.EventHeadContext context)
        => _eventHeaderFrames.Push(new(context, _syntaxErrorCount));

    internal void AddEventAttribute(CILParser.EventHeadContext context, object? value)
    {
        if (TryGetEventHeaderFrame(context) is { } frame)
        {
            frame.Attributes = ApplyAttribute(
                frame.Attributes,
                GetAttributeValue<EventAttributes>(value));
        }
    }

    internal object CreateEventHeader(
        CILParser.EventHeadContext context,
        object? eventType,
        string name)
    {
        EventHeaderFrame? frame = TryGetEventHeaderFrame(context);
        if (frame is null ||
            frame.InitialSyntaxErrorCount != _syntaxErrorCount ||
            context.exception is not null)
        {
            return EventHeaderValue.Error;
        }

        return new EventHeaderValue(
            true,
            frame.Attributes,
            eventType is null ? null : GetTypeSpecificationValue(eventType),
            name);
    }

    internal void EndEventHeader(CILParser.EventHeadContext context)
    {
        if (_eventHeaderFrames.Count == 0)
        {
            return;
        }

        EventHeaderFrame frame = _eventHeaderFrames.Peek();
        Debug.Assert(ReferenceEquals(frame.Owner, context));
        if (ReferenceEquals(frame.Owner, context))
        {
            _eventHeaderFrames.Pop();
        }
    }

    internal object CreateEventAttribute(IToken token)
        => token.Text switch
        {
            "specialname" => new AttributeValue<EventAttributes>(
                EventAttributes.SpecialName,
                0,
                true),
            "rtspecialname" => new AttributeValue<EventAttributes>(0, 0, true),
            _ => throw new UnreachableException(),
        };

    internal void BeginEvent(CILParser.ClassDeclContext context, object? value)
    {
        PrepareClassMember();
        EventHeaderValue header = GetEventHeaderValue(value);
        EntityRegistry.EventEntity? @event = null;
        if (header.IsValid && _currentTypeDefinition.PeekOrDefault() is { } currentType)
        {
            @event = new EntityRegistry.EventEntity(
                header.Attributes,
                header.EventType is null
                    ? null
                    : ResolveTypeSpecification(header.EventType),
                header.Name);
            currentType.Events.Add(@event);
        }

        _eventBodyFrames.Push(new(context, @event));
    }

    internal void AddEventAdder(CILParser.EventDeclContext context, object? value)
        => AddEventAccessor(context, MethodSemanticsAttributes.Adder, value);

    internal void AddEventRemover(CILParser.EventDeclContext context, object? value)
        => AddEventAccessor(context, MethodSemanticsAttributes.Remover, value);

    internal void AddEventRaiser(CILParser.EventDeclContext context, object? value)
        => AddEventAccessor(context, MethodSemanticsAttributes.Raiser, value);

    internal void AddEventOther(CILParser.EventDeclContext context, object? value)
        => AddEventAccessor(context, MethodSemanticsAttributes.Other, value);

    private void AddEventAccessor(
        CILParser.EventDeclContext context,
        MethodSemanticsAttributes semantics,
        object? value)
    {
        if (TryGetEventBodyFrame(context) is { Event: { } @event })
        {
            @event.Accessors.Add(
                (semantics, MaterializeMethodReference(GetMethodReferenceValue(value))));
        }
    }

    internal void AddEventCustomAttribute(
        CILParser.EventDeclContext context,
        CILParser.CustomAttrDeclContext attribute)
    {
        if (attribute.HasSyntaxError ||
            TryGetEventBodyFrame(context) is not { Event: { } @event })
        {
            return;
        }

        if (VisitCustomAttrDecl(attribute).Value is { } customAttribute)
        {
            customAttribute.Owner = @event;
        }
    }

    internal void ProcessEventSourceDirective(CILParser.ExtSourceSpecContext context)
        => _ = context;

    internal void ProcessEventLanguageDirective(CILParser.LanguageDeclContext context)
        => _ = context;

    private void EndPropertyAndEventBodies(CILParser.ClassDeclContext context)
    {
        if (_propertyBodyFrames.Count > 0 &&
            ReferenceEquals(_propertyBodyFrames.Peek().Owner, context))
        {
            _propertyBodyFrames.Pop();
        }

        if (_eventBodyFrames.Count > 0 &&
            ReferenceEquals(_eventBodyFrames.Peek().Owner, context))
        {
            _eventBodyFrames.Pop();
        }
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitEventAttr(CILParser.EventAttrContext context)
        => VisitEventAttr(context);

    public static GrammarResult.Flag<EventAttributes> VisitEventAttr(
        CILParser.EventAttrContext context)
    {
        AttributeValue<EventAttributes> attribute =
            GetAttributeValue<EventAttributes>(context.Value);
        return new(attribute.Value, attribute.ShouldAppend, attribute.GroupMask);
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitEventDecl(CILParser.EventDeclContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    GrammarResult ICILVisitor<GrammarResult>.VisitEventDecls(CILParser.EventDeclsContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    GrammarResult ICILVisitor<GrammarResult>.VisitEventHead(CILParser.EventHeadContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    GrammarResult ICILVisitor<GrammarResult>.VisitPropAttr(CILParser.PropAttrContext context)
        => VisitPropAttr(context);

    public static GrammarResult.Flag<PropertyAttributes> VisitPropAttr(
        CILParser.PropAttrContext context)
    {
        AttributeValue<PropertyAttributes> attribute =
            GetAttributeValue<PropertyAttributes>(context.Value);
        return new(attribute.Value, attribute.ShouldAppend, attribute.GroupMask);
    }

    GrammarResult ICILVisitor<GrammarResult>.VisitPropDecl(CILParser.PropDeclContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    GrammarResult ICILVisitor<GrammarResult>.VisitPropDecls(CILParser.PropDeclsContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    GrammarResult ICILVisitor<GrammarResult>.VisitPropHead(CILParser.PropHeadContext context)
        => throw new UnreachableException(StructuralNodeIsDrivenByParserActions);

    private PropertyHeaderFrame? TryGetPropertyHeaderFrame(CILParser.PropHeadContext context)
    {
        Debug.Assert(_propertyHeaderFrames.Count > 0);
        PropertyHeaderFrame? frame =
            _propertyHeaderFrames.Count == 0 ? null : _propertyHeaderFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    private EventHeaderFrame? TryGetEventHeaderFrame(CILParser.EventHeadContext context)
    {
        Debug.Assert(_eventHeaderFrames.Count > 0);
        EventHeaderFrame? frame =
            _eventHeaderFrames.Count == 0 ? null : _eventHeaderFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    private PropertyBodyFrame? TryGetPropertyBodyFrame(CILParser.PropDeclContext context)
    {
        PropertyBodyFrame? frame =
            _propertyBodyFrames.Count == 0 ? null : _propertyBodyFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context.Parent?.Parent));
        return frame is not null && ReferenceEquals(frame.Owner, context.Parent?.Parent) ? frame : null;
    }

    private EventBodyFrame? TryGetEventBodyFrame(CILParser.EventDeclContext context)
    {
        EventBodyFrame? frame =
            _eventBodyFrames.Count == 0 ? null : _eventBodyFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context.Parent?.Parent));
        return frame is not null && ReferenceEquals(frame.Owner, context.Parent?.Parent) ? frame : null;
    }
}

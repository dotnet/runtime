// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly Stack<SerializationSequenceFrame> _serializationSequenceFrames = new();

    private sealed class SerializationSequenceFrame
    {
        public SerializationSequenceFrame(ParserRuleContext owner)
        {
            Owner = owner;
        }

        public ParserRuleContext Owner { get; }

        public BlobBuilder? Value { get; set; }

        public ImmutableArray<ClassSequenceElementValue>.Builder? ClassValues { get; set; }

        public ImmutableArray<SerializedInitializerValue>.Builder? ObjectValues { get; set; }
    }

    internal void BeginSerializationSequence(ParserRuleContext context)
        => _serializationSequenceFrames.Push(new(context));

    internal void AddFloat32SequenceValue(ParserRuleContext context, double value)
    {
        if (TryGetSerializationSequenceFrame(context) is { } frame)
        {
            (frame.Value ??= new BlobBuilder()).WriteSingle((float)value);
        }
    }

    internal void AddFloat32SequenceValue(ParserRuleContext context, IToken value)
    {
        if (TryGetSerializationSequenceFrame(context) is { } frame)
        {
            (frame.Value ??= new BlobBuilder()).WriteSingle(ParseInt32(value));
        }
    }

    internal void AddFloat64SequenceValue(ParserRuleContext context, double value)
    {
        if (TryGetSerializationSequenceFrame(context) is { } frame)
        {
            (frame.Value ??= new BlobBuilder()).WriteDouble(value);
        }
    }

    internal void AddFloat64SequenceValue(ParserRuleContext context, IToken value)
    {
        if (TryGetSerializationSequenceFrame(context) is { } frame)
        {
            (frame.Value ??= new BlobBuilder()).WriteDouble(ParseInt64(value));
        }
    }

    internal void AddInt64SequenceValue(ParserRuleContext context, IToken value)
    {
        if (TryGetSerializationSequenceFrame(context) is { } frame)
        {
            (frame.Value ??= new BlobBuilder()).WriteInt64(ParseInt64(value));
        }
    }

    internal void AddInt32SequenceValue(ParserRuleContext context, IToken value)
    {
        if (TryGetSerializationSequenceFrame(context) is { } frame)
        {
            (frame.Value ??= new BlobBuilder()).WriteInt32(ParseInt32(value));
        }
    }

    internal void AddInt16SequenceValue(ParserRuleContext context, IToken value)
    {
        if (TryGetSerializationSequenceFrame(context) is { } frame)
        {
            (frame.Value ??= new BlobBuilder()).WriteInt16((short)ParseInt32(value));
        }
    }

    internal void AddInt8SequenceValue(ParserRuleContext context, IToken value)
    {
        if (TryGetSerializationSequenceFrame(context) is { } frame)
        {
            (frame.Value ??= new BlobBuilder()).WriteByte((byte)ParseInt32(value));
        }
    }

    internal void AddBooleanSequenceValue(ParserRuleContext context, bool value)
    {
        if (TryGetSerializationSequenceFrame(context) is { } frame)
        {
            (frame.Value ??= new BlobBuilder()).WriteBoolean(value);
        }
    }

    internal void AddStringSequenceValue(ParserRuleContext context, IToken value)
    {
        if (TryGetSerializationSequenceFrame(context) is { } frame)
        {
            (frame.Value ??= new BlobBuilder()).WriteSerializedString(
                value.Type == CILParser.NULLREF
                    ? null
                    : StringHelpers.ParseQuotedString(value.Text));
        }
    }

    internal void AddClassSequenceValue(ParserRuleContext context, object? value)
    {
        if (TryGetSerializationSequenceFrame(context) is { } frame &&
            value is ClassSequenceElementValue element)
        {
            (frame.ClassValues ??=
                ImmutableArray.CreateBuilder<ClassSequenceElementValue>()).Add(element);
        }
    }

    internal void AddObjectSequenceValue(ParserRuleContext context, object? value)
    {
        if (TryGetSerializationSequenceFrame(context) is { } frame)
        {
            (frame.ObjectValues ??=
                ImmutableArray.CreateBuilder<SerializedInitializerValue>())
                .Add(GetSerializedInitializerValue(value));
        }
    }

    internal BlobBuilder EndSerializationSequence(ParserRuleContext context)
    {
        if (TryGetSerializationSequenceFrame(context) is not { } frame)
        {
            return new BlobBuilder();
        }

        _serializationSequenceFrames.Pop();
        return frame.Value ?? new BlobBuilder();
    }

    internal object EndClassSerializationSequence(ParserRuleContext context)
    {
        if (TryGetSerializationSequenceFrame(context) is not { } frame)
        {
            return new ClassSerializedSequenceValue([]);
        }

        _serializationSequenceFrames.Pop();
        return new ClassSerializedSequenceValue(frame.ClassValues?.ToImmutable() ?? []);
    }

    internal object EndObjectSerializationSequence(ParserRuleContext context)
    {
        if (TryGetSerializationSequenceFrame(context) is not { } frame)
        {
            return new ObjectSerializedSequenceValue([]);
        }

        _serializationSequenceFrames.Pop();
        return new ObjectSerializedSequenceValue(frame.ObjectValues?.ToImmutable() ?? []);
    }

    private SerializationSequenceFrame? TryGetSerializationSequenceFrame(ParserRuleContext context)
    {
        Debug.Assert(_serializationSequenceFrames.Count > 0);
        SerializationSequenceFrame? frame =
            _serializationSequenceFrames.Count == 0 ? null : _serializationSequenceFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    internal object CreateNullClassSequenceValue()
        => new StringClassSequenceElementValue(null);

    internal object CreateQuotedClassSequenceValue(IToken value)
        => new StringClassSequenceElementValue(StringHelpers.ParseQuotedString(value.Text));

    internal object CreateClassSequenceValue(object? className)
        => new TypeClassSequenceElementValue(GetClassNameValue(className));

    private BlobBuilder MaterializeSerializedSequence(SerializedSequenceValue sequence)
    {
        if (sequence is RawSerializedSequenceValue raw)
        {
            return raw.Value;
        }

        BlobBuilder blob = new();
        switch (sequence)
        {
            case ClassSerializedSequenceValue classes:
                foreach (ClassSequenceElementValue value in classes.Values)
                {
                    MaterializeClassSequenceElement(value).WriteContentTo(blob);
                }
                break;
            case ObjectSerializedSequenceValue objects:
                foreach (SerializedInitializerValue value in objects.Values)
                {
                    SerializedInitializerValue initializer = value;
                    while (initializer is ObjectSerializedInitializerValue boxed)
                    {
                        initializer = boxed.Value;
                    }

                    MaterializeSerializationType(initializer.Type).WriteContentTo(blob);
                    MaterializeSerializedInitializer(initializer).WriteContentTo(blob);
                }
                break;
        }

        return blob;
    }

    private BlobBuilder MaterializeClassSequenceElement(ClassSequenceElementValue value)
    {
        BlobBuilder blob = new();
        blob.WriteSerializedString(value switch
        {
            StringClassSequenceElementValue text => text.Value,
            TypeClassSequenceElementValue type => GetReflectionNotation(type.ClassName),
            _ => null
        });
        return blob;
    }

}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal void AddFloat32SequenceValue(BlobBuilder builder, double value)
        => builder.WriteSingle((float)value);

    internal void AddFloat32SequenceValue(BlobBuilder builder, IToken value)
        => builder.WriteSingle(ParseInt32(value));

    internal void AddFloat64SequenceValue(BlobBuilder builder, double value)
        => builder.WriteDouble(value);

    internal void AddFloat64SequenceValue(BlobBuilder builder, IToken value)
        => builder.WriteDouble(ParseInt64(value));

    internal void AddInt64SequenceValue(BlobBuilder builder, IToken value)
        => builder.WriteInt64(ParseInt64(value));

    internal void AddInt32SequenceValue(BlobBuilder builder, IToken value)
        => builder.WriteInt32(ParseInt32(value));

    internal void AddInt16SequenceValue(BlobBuilder builder, IToken value)
        => builder.WriteInt16((short)ParseInt32(value));

    internal void AddInt8SequenceValue(BlobBuilder builder, IToken value)
        => builder.WriteByte((byte)ParseInt32(value));

    internal void AddBooleanSequenceValue(BlobBuilder builder, bool value)
        => builder.WriteBoolean(value);

    internal void AddStringSequenceValue(BlobBuilder builder, IToken value)
        => builder.WriteSerializedString(
            value.Type == CILParser.NULLREF
                ? null
                : StringHelpers.ParseQuotedString(value.Text));

    internal ClassSequenceElementValue CreateNullClassSequenceValue()
        => new StringClassSequenceElementValue(null);

    internal ClassSequenceElementValue CreateQuotedClassSequenceValue(IToken value)
        => new StringClassSequenceElementValue(StringHelpers.ParseQuotedString(value.Text));

    internal ClassSequenceElementValue CreateClassSequenceValue(ClassNameValue className)
        => new TypeClassSequenceElementValue(className);

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

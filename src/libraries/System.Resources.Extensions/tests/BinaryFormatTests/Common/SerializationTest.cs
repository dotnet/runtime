// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Runtime.Serialization;
using System.Formats.Nrbf;
using System.Runtime.Serialization.Formatters;

namespace System.Resources.Extensions.Tests.Common;

[ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
public abstract class SerializationTest<TSerializer> where TSerializer : ISerializer
{
    public static TheoryData<FormatterTypeStyle, FormatterAssemblyStyle> FormatterOptions => new()
    {
        // XsdString always writes strings inline (never as a record). Despite FormatterTypeStyle
        // not having [Flags] it is treated as flags in the serializer. If you don't explicitly set
        // TypesAlways, TypesWhenNeeded is the default.
        { FormatterTypeStyle.TypesWhenNeeded, FormatterAssemblyStyle.Full },
        { FormatterTypeStyle.TypesWhenNeeded, FormatterAssemblyStyle.Simple },
        { FormatterTypeStyle.TypesAlways, FormatterAssemblyStyle.Full },
        { FormatterTypeStyle.TypesAlways, FormatterAssemblyStyle.Simple },
        { FormatterTypeStyle.TypesAlways | FormatterTypeStyle.XsdString, FormatterAssemblyStyle.Full },
        { FormatterTypeStyle.TypesAlways | FormatterTypeStyle.XsdString, FormatterAssemblyStyle.Simple },
        { FormatterTypeStyle.TypesWhenNeeded | FormatterTypeStyle.XsdString, FormatterAssemblyStyle.Full },
        { FormatterTypeStyle.TypesWhenNeeded | FormatterTypeStyle.XsdString, FormatterAssemblyStyle.Simple },
    };

    private protected static Stream Serialize(
        object value,
        SerializationBinder? binder = null,
        ISurrogateSelector? surrogateSelector = null,
        FormatterTypeStyle typeStyle = FormatterTypeStyle.TypesAlways) =>
        TSerializer.Serialize(value, binder, surrogateSelector, typeStyle);

    private protected static object Deserialize(
        Stream stream,
        SerializationBinder? binder = null,
        FormatterAssemblyStyle assemblyMatching = FormatterAssemblyStyle.Simple,
        ISurrogateSelector? surrogateSelector = null) =>
        TSerializer.Deserialize(stream, binder, assemblyMatching, surrogateSelector);

    private protected static TObject RoundTrip<TObject>(
        TObject value,
        SerializationBinder? binder = null,
        ISurrogateSelector? surrogateSelector = null,
        FormatterTypeStyle typeStyle = FormatterTypeStyle.TypesAlways,
        FormatterAssemblyStyle assemblyMatching = FormatterAssemblyStyle.Simple) where TObject : notnull
    {
        // TODO: Use array pool
        return (TObject)Deserialize(Serialize(value, binder, surrogateSelector, typeStyle), binder, assemblyMatching, surrogateSelector);
    }

    private protected static object DeserializeFromBase64Chars(
        ReadOnlySpan<char> chars,
        SerializationBinder? binder = null,
        FormatterAssemblyStyle assemblyMatching = FormatterAssemblyStyle.Simple,
        ISurrogateSelector? surrogateSelector = null)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(chars.Length);
        if (!Convert.TryFromBase64Chars(chars, buffer, out _))
        {
            throw new InvalidOperationException();
        }

        MemoryStream stream = new(buffer);
        try
        {
            return Deserialize(stream, binder, assemblyMatching, surrogateSelector);
        }
        finally
        {
            stream.Dispose();
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private protected static SurrogateSelector CreateSurrogateSelector<TSurrogated>(ISerializationSurrogate surrogate)
    {
        SurrogateSelector selector = new();
        selector.AddSurrogate(
            typeof(TSurrogated),
            new StreamingContext(StreamingContextStates.All),
            surrogate);

        return selector;
    }

    public static bool IsBinaryFormatterDeserializer => false;

    protected static void WriteSerializedStreamHeader(BinaryWriter writer, int major = 1, int minor = 0)
    {
        writer.Write((byte)SerializationRecordType.SerializedStreamHeader);
        writer.Write(1); // root ID
        writer.Write(1); // header ID
        writer.Write(major); // major version
        writer.Write(minor); // minor version
    }

    protected static void WriteBinaryLibrary(BinaryWriter writer, int objectId, string libraryName)
    {
        writer.Write((byte)SerializationRecordType.BinaryLibrary);
        writer.Write(objectId);
        writer.Write(libraryName);
    }

    protected static void WriteClassInfo(BinaryWriter writer, int objectId, string typeName, params string[] memberNames)
    {
        writer.Write((byte)SerializationRecordType.ClassWithMembersAndTypes);
        writer.Write(objectId);
        writer.Write(typeName);
        writer.Write(memberNames.Length);

        foreach (string memberName in memberNames)
        {
            writer.Write(memberName);
        }
    }

    protected static void WriteClassFieldInfo(BinaryWriter writer, string typeName, int libraryId)
    {
        writer.Write((byte)BinaryType.Class);
        writer.Write(typeName);
        writer.Write(libraryId);
    }

    protected static void WriteMemberReference(BinaryWriter writer, int referencedObjectId)
    {
        writer.Write((byte)SerializationRecordType.MemberReference);
        writer.Write(referencedObjectId);
    }

    protected static void WriteMessageEnd(BinaryWriter writer)
    {
        writer.Write((byte)SerializationRecordType.MessageEnd);
    }
}

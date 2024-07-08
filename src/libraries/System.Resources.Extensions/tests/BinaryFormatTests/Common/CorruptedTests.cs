// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Resources.Extensions.Tests.Common.TestTypes;
using System.Resources.Extensions.Tests.FormattedObject;
using System.Runtime.Serialization;
using System.Formats.Nrbf;
using System.Text;

namespace System.Resources.Extensions.Tests.Common;

public class CorruptedTests : SerializationTest<FormattedObjectSerializer>
{
    private const int ClassId = 1, LibraryId = 2;

    [Fact]
    public void ValueTypeReferencesSelf()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        WriteBinaryLibrary(writer, LibraryId, typeof(NodeStruct).Assembly.FullName!);
        WriteClassInfo(writer, ClassId, typeof(NodeStruct).FullName!, ["Node"]);
        WriteClassFieldInfo(writer, typeof(NodeWithNodeStruct).FullName!, LibraryId);
        writer.Write(LibraryId);
        WriteMemberReference(writer, ClassId);
        WriteMessageEnd(writer);

        stream.Position = 0;

        // This fails in the SerializationConstructor in BinaryFormattedObject's deserializer because the
        // type isn't convertible to NodeWithNodeStruct. In BinaryFormatter it fails with fixups.
        Assert.Throws<SerializationException>(() => Deserialize(stream));
    }

    [Fact]
    public void ValueTypeReferencesSelf2()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        WriteBinaryLibrary(writer, LibraryId, typeof(StructWithObject).Assembly.FullName!);
        WriteClassInfo(writer, ClassId, typeof(StructWithObject).FullName!, ["Value"]);
        WriteClassFieldInfo(writer, typeof(StructWithObject).FullName!, LibraryId);
        writer.Write(LibraryId);
        WriteMemberReference(writer, ClassId);
        WriteMessageEnd(writer);

        stream.Position = 0;

        Assert.Throws<SerializationException>(() => Deserialize(stream));
    }

    [Fact]
    public void ValueTypeReferencesSelf3()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        WriteBinaryLibrary(writer, LibraryId, typeof(StructWithTwoObjects).Assembly.FullName!);
        WriteClassInfo(writer, ClassId, typeof(StructWithTwoObjects).FullName!, ["Value", "Value2"]);
        writer.Write((byte)BinaryType.Object);
        writer.Write((byte)BinaryType.Object);
        writer.Write(LibraryId);
        WriteMemberReference(writer, ClassId);
        WriteMemberReference(writer, ClassId);
        WriteMessageEnd(writer);

        // Both deserializers create this where every boxed struct is the exact same boxed instance.
        stream.Position = 0;

        Assert.Throws<SerializationException>(() => Deserialize(stream));
    }

    [Fact]
    public virtual void ValueTypeReferencesSelf4()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        WriteBinaryLibrary(writer, LibraryId, typeof(StructWithTwoObjectsISerializable).Assembly.FullName!);
        WriteClassInfo(writer, ClassId, typeof(StructWithTwoObjectsISerializable).FullName!, ["Value", "Value2"]);
        writer.Write((byte)BinaryType.Object);
        writer.Write((byte)BinaryType.Object);
        writer.Write(LibraryId);
        WriteMemberReference(writer, ClassId);
        WriteMemberReference(writer, ClassId);
        WriteMessageEnd(writer);

        stream.Position = 0;
        Deserialize(stream);
    }

    [Fact]
    public virtual void ValueTypeReferencesSelf5()
    {
        const int NextClassId = 3;
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        WriteBinaryLibrary(writer, LibraryId, typeof(StructWithTwoObjectsISerializable).Assembly.FullName!);
        WriteClassInfo(writer, ClassId, typeof(StructWithTwoObjectsISerializable).FullName!, ["Value", "Value2"]);
        writer.Write((byte)BinaryType.Object);
        writer.Write((byte)BinaryType.Object);
        writer.Write(LibraryId);
        WriteMemberReference(writer, NextClassId);
        WriteMemberReference(writer, NextClassId);
        // ClassWithId
        writer.Write((byte)SerializationRecordType.ClassWithId);
        writer.Write(NextClassId); // id
        writer.Write(ClassId); // id of the class that provides metadata
        WriteMemberReference(writer, ClassId);
        WriteMemberReference(writer, NextClassId);
        WriteMessageEnd(writer);

        stream.Position = 0;
        Deserialize(stream);
    }
}

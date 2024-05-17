using System.IO;
using System.Text;
using Xunit;

namespace System.Runtime.Serialization.BinaryFormat.Tests;

public class InvalidInputTests : ReadTests
{
    [Fact]
    public void ThrowsOnInvalidUtf8Input()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        byte[] invalidUtf8 = [(byte)'a', (byte)'b', 0xC0, (byte)'x', (byte)'y'];

        writer.Write((byte)RecordType.BinaryObjectString);
        writer.Write((int)1); // object ID
#if NETFRAMEWORK
        typeof(BinaryWriter).GetMethod("Write7BitEncodedInt",
            Reflection.BindingFlags.Instance | Reflection.BindingFlags.NonPublic).Invoke(writer, [invalidUtf8.Length]);
#else
        writer.Write7BitEncodedInt(invalidUtf8.Length);
#endif
        writer.Write(invalidUtf8);
        writer.Write((byte)RecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<DecoderFallbackException>(() => PayloadReader.Read(stream));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    public void ThrowsOnInvalidHeaderVersion(int major, int minor)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer, major, minor);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => PayloadReader.Read(stream));
    }

    [Theory]
    [InlineData(10, RecordType.ArraySingleString)] // ObjectNullMultiple256
    [InlineData(byte.MaxValue + 1, RecordType.ArraySingleString)] // ObjectNullMultiple
    [InlineData(10, RecordType.ArraySingleObject)]
    [InlineData(byte.MaxValue + 1, RecordType.ArraySingleObject)]
    public void ThrowsWhenNumberOfNullsIsLargerThanArraySize(int nullCount, RecordType recordType)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)recordType);
        writer.Write(1); // object ID
        writer.Write(nullCount - 1); // length
        writer.Write((byte)RecordType.ObjectNullMultiple);
        writer.Write(nullCount); // null count
        writer.Write((byte)RecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => PayloadReader.Read(stream));
    }

    [Fact]
    public void ThrowWhenArrayOfStringsContainsReferenceToNonString()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        const int LibraryId = 2;
        WriteBinaryLibrary(writer, LibraryId, "LibraryName.dll");

        writer.Write((byte)RecordType.ArraySingleString);
        writer.Write(1); // array Id
        writer.Write(1); // array length
        writer.Write((byte)RecordType.MemberReference);
        writer.Write(LibraryId); // reference to the library
        writer.Write((byte)RecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => ((ArrayRecord<string>)PayloadReader.Read(stream)).ToArray());
    }

    [Theory]
    [InlineData("TypeName, Hacked.dll")] // assembly names are NOT allowed
    [InlineData("InvalidTypeName[]]")] // invalid type name
    public void ThrowsWhenTypeNameIsInvalid(string typeName)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)RecordType.SystemClassWithMembersAndTypes);
        writer.Write(1); // class Id
        writer.Write(typeName);
        writer.Write(0); // member count
        writer.Write((byte)RecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => PayloadReader.Read(stream));
    }

    [Theory]
    [InlineData("TypeName, Hacked.dll")] // assembly names are NOT allowed
    [InlineData("InvalidTypeName[]]")] // invalid type name
    public void ThrowsWhenMemberTypeNameIsInvalid_BinaryTypeSystemClass(string typeName)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)RecordType.SystemClassWithMembersAndTypes);
        writer.Write(1); // class Id
        writer.Write("ValidTypeName");
        writer.Write(1); // member count

        // The Member
        writer.Write(3); // BinaryType.SystemClass
        writer.Write(typeName);
        writer.Write((byte)RecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => PayloadReader.Read(stream));
    }


    [Theory]
    [InlineData("TypeName, Hacked.dll")] // assembly names are NOT allowed
    [InlineData("InvalidTypeName[]]")] // invalid type name
    public void ThrowsWhenMemberTypeNameIsInvalid_BinaryTypeClass(string typeName)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)RecordType.SystemClassWithMembersAndTypes);
        writer.Write(1); // class Id
        writer.Write("ValidTypeName");
        writer.Write(1); // member count

        // The Member
        writer.Write(4); // BinaryType.Class (the difference!)
        writer.Write(typeName); // type name
        writer.Write(10); // class id
        writer.Write((byte)RecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => PayloadReader.Read(stream));
    }

    [Fact]
    public void ThrowsWhenLibraryNameIsInvalid()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)RecordType.BinaryLibrary);
        writer.Write(1); // library Id
        writer.Write("Esc\\[aped"); // library name
        writer.Write((byte)RecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => PayloadReader.Read(stream));
    }
}
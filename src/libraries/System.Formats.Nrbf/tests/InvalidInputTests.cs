using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Xunit;

namespace System.Formats.Nrbf.Tests;

public class InvalidInputTests : ReadTests
{
    [Fact]
    public void ThrowsOnInvalidUtf8Input()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        byte[] invalidUtf8 = [(byte)'a', (byte)'b', 0xC0, (byte)'x', (byte)'y'];

        writer.Write((byte)SerializationRecordType.BinaryObjectString);
        writer.Write((int)1); // object ID
#if NETFRAMEWORK
        typeof(BinaryWriter).GetMethod("Write7BitEncodedInt",
            Reflection.BindingFlags.Instance | Reflection.BindingFlags.NonPublic).Invoke(writer, [invalidUtf8.Length]);
#else
        writer.Write7BitEncodedInt(invalidUtf8.Length);
#endif
        writer.Write(invalidUtf8);
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<DecoderFallbackException>(() => NrbfDecoder.Decode(stream));
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
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream));
    }

    [Theory]
    [InlineData(10, SerializationRecordType.ArraySingleString)] // ObjectNullMultiple256
    [InlineData(byte.MaxValue + 1, SerializationRecordType.ArraySingleString)] // ObjectNullMultiple
    [InlineData(10, SerializationRecordType.ArraySingleObject)]
    [InlineData(byte.MaxValue + 1, SerializationRecordType.ArraySingleObject)]
    public void ThrowsWhenNumberOfNullsIsLargerThanArraySize(int nullCount, SerializationRecordType recordType)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)recordType);
        writer.Write(1); // object ID
        writer.Write(nullCount - 1); // length
        writer.Write((byte)SerializationRecordType.ObjectNullMultiple);
        writer.Write(nullCount); // null count
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream));
    }

    [Theory]
    [InlineData(0, SerializationRecordType.ObjectNullMultiple256)]
    [InlineData(0, SerializationRecordType.ObjectNullMultiple)] 
    [InlineData(-1, SerializationRecordType.ObjectNullMultiple)]
    public void ThrowsWhenNumberOfNullsIsInvalid(int nullCount, SerializationRecordType recordType)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)SerializationRecordType.ArraySingleString);
        writer.Write(1); // object ID
        writer.Write(1); // length
        writer.Write((byte)recordType);

        if (recordType == SerializationRecordType.ObjectNullMultiple256)
        {
            writer.Write((byte)nullCount); // null count
        }
        else
        {
            writer.Write(nullCount); // null count
        }

        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream));
    }

    [Fact]
    public void ThrowWhenArrayOfStringsContainsReferenceToNonString()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        const int LibraryId = 2;
        WriteBinaryLibrary(writer, LibraryId, "LibraryName.dll");

        writer.Write((byte)SerializationRecordType.ArraySingleString);
        writer.Write(1); // array Id
        writer.Write(1); // array length
        writer.Write((byte)SerializationRecordType.MemberReference);
        writer.Write(LibraryId); // reference to the library
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => ((SZArrayRecord<string>)NrbfDecoder.Decode(stream)).GetArray());
    }

    [Theory]
    [InlineData("TypeName, Hacked.dll", true)] // assembly names are NOT allowed
    [InlineData("TypeName, Hacked.dll", false)]
    [InlineData("InvalidTypeName[]]", true)] // invalid type name
    [InlineData("InvalidTypeName[]]", false)]
    public void ThrowsWhenTypeNameIsInvalid(string typeName, bool mangling)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)SerializationRecordType.SystemClassWithMembersAndTypes);
        writer.Write(1); // class Id
        writer.Write(typeName);
        writer.Write(0); // member count
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        PayloadOptions options = new() { UndoTruncatedTypeNames = mangling };
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream, options));
    }

    [Theory]
    [InlineData("TypeName, Hacked.dll", true)] // assembly names are NOT allowed
    [InlineData("TypeName, Hacked.dll", false)]
    [InlineData("InvalidTypeName[]]", true)] // invalid type name
    [InlineData("InvalidTypeName[]]", false)]
    public void ThrowsWhenMemberTypeNameIsInvalid_BinaryTypeSystemClass(string typeName, bool mangling)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)SerializationRecordType.SystemClassWithMembersAndTypes);
        writer.Write(1); // class Id
        writer.Write("ValidTypeName");
        writer.Write(1); // member count

        // The Member
        writer.Write(3); // BinaryType.SystemClass
        writer.Write(typeName);
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        PayloadOptions options = new() { UndoTruncatedTypeNames = mangling };
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream, options));
    }

    [Theory]
    [InlineData("TypeName, Hacked.dll", true)] // assembly names are NOT allowed
    [InlineData("TypeName, Hacked.dll", false)]
    [InlineData("InvalidTypeName[]]", true)] // invalid type name
    [InlineData("InvalidTypeName[]]", false)]
    public void ThrowsWhenMemberTypeNameIsInvalid_BinaryTypeClass(string typeName, bool mangling)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)SerializationRecordType.SystemClassWithMembersAndTypes);
        writer.Write(1); // class Id
        writer.Write("ValidTypeName");
        writer.Write(1); // member count

        // The Member
        writer.Write(4); // BinaryType.Class (the difference!)
        writer.Write(typeName); // type name
        writer.Write(10); // class id
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        PayloadOptions options = new() { UndoTruncatedTypeNames = mangling };
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream, options));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ThrowsWhenLibraryNameIsInvalid(bool mangling)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)SerializationRecordType.BinaryLibrary);
        writer.Write(2); // library Id
        writer.Write("Esc\\[aped"); // library name

        writer.Write((byte)SerializationRecordType.ClassWithMembersAndTypes);
        writer.Write(1); // class Id
        writer.Write("ValidTypeName");
        writer.Write(0); // member count
        writer.Write(2); // library Id

        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        PayloadOptions options = new() { UndoTruncatedTypeNames = mangling };
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream, options));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UndoTruncatedTypeNamesIsRespected(bool mangling)
    {
        const string TypeName = "System.Collections.Generic.Dictionary`2[[System.String";
        const string LibraryName = "mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.String, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]";

        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)SerializationRecordType.BinaryLibrary);
        writer.Write(2); // library Id
        writer.Write(LibraryName);

        writer.Write((byte)SerializationRecordType.ClassWithMembersAndTypes);
        writer.Write(1); // class Id
        writer.Write(TypeName);
        writer.Write(0); // member count
        writer.Write(2); // library Id

        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        PayloadOptions options = new() { UndoTruncatedTypeNames = mangling };

        if (mangling)
        {
            ClassRecord classRecord = (ClassRecord)NrbfDecoder.Decode(stream, options);
            Assert.Equal($"{TypeName}, {LibraryName}", classRecord.TypeName.FullName);
        }
        else
        {
            Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream, options));
        }
    }

    [Theory]
    [InlineData(SerializationRecordType.ArraySingleObject)]
    [InlineData(SerializationRecordType.ArraySinglePrimitive)]
    [InlineData(SerializationRecordType.ArraySingleString)]
    public void ThrowsForNegativeSingleArrayLength(SerializationRecordType recordType)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)recordType);
        writer.Write(1); // object Id
        writer.Write(-1); // length!
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream));
    }

    [Theory]
    [InlineData((byte)BinaryArrayType.Single)]
    [InlineData((byte)BinaryArrayType.Jagged)]
    [InlineData((byte)BinaryArrayType.Rectangular)]
    public void ThrowsForNegativeArrayLength(byte arrayType)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)SerializationRecordType.BinaryArray);
        writer.Write(1); // object Id
        writer.Write(arrayType);
        writer.Write(1); // rank
        writer.Write(-1); // length!
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream));
    }

    [Theory]
    [InlineData(-1, (byte)BinaryArrayType.Single)]
    [InlineData(0, (byte)BinaryArrayType.Single)]
    [InlineData(-1, (byte)BinaryArrayType.Jagged)]
    [InlineData(0, (byte)BinaryArrayType.Jagged)]
    [InlineData(-1, (byte)BinaryArrayType.Rectangular)]
    [InlineData(0, (byte)BinaryArrayType.Rectangular)]
    public void ThrowsForInvalidArrayRank(int rank, byte arrayType)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)SerializationRecordType.BinaryArray);
        writer.Write(1); // object Id
        writer.Write(arrayType);
        writer.Write(rank); // rank!
        writer.Write(1); // length
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream));
    }

    [Theory]
    [InlineData(2, (byte)BinaryArrayType.Single)]
    [InlineData(2, (byte)BinaryArrayType.Jagged)]
    public void ThrowsForInvalidPositiveArrayRank(int rank, byte arrayType)
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);

        writer.Write((byte)SerializationRecordType.BinaryArray);
        writer.Write(1); // object Id
        writer.Write(arrayType);
        writer.Write(rank); // rank!
        writer.Write(1); // length
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream));
    }

    [Theory]
    [InlineData(SerializationRecordType.ClassWithMembersAndTypes)]
    [InlineData(SerializationRecordType.SystemClassWithMembersAndTypes)]
    public void ThrowsForInvalidBinaryType(SerializationRecordType recordType)
    {
        const int LibraryId = 2;
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        WriteBinaryLibrary(writer, LibraryId, "LibraryName");

        // ClassInfo (always present)
        writer.Write((byte)recordType);
        writer.Write(1); // object ID
        writer.Write("TypeName"); // type name
        writer.Write(1); // member count
        writer.Write("MemberName");
        writer.Write((byte)8);

        if (recordType is SerializationRecordType.ClassWithMembersAndTypes)
        {
            writer.Write(LibraryId);
        }
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream));
    }

    [Theory]
    [InlineData(SerializationRecordType.ClassWithMembersAndTypes)]
    [InlineData(SerializationRecordType.SystemClassWithMembersAndTypes)]
    public void ThrowsForDuplicateMemberNames(SerializationRecordType recordType)
    {
        const int LibraryId = 2;
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        if (recordType is SerializationRecordType.ClassWithMembersAndTypes)
        {
            WriteBinaryLibrary(writer, LibraryId, "LibraryName");
        }

        // ClassInfo (always present)
        writer.Write((byte)recordType);
        writer.Write(1); // object ID
        writer.Write("TypeName"); // type name
        writer.Write(2); // member count
        writer.Write("MemberName");
        writer.Write("MemberName");
        writer.Write(1); // BinaryType.String
        writer.Write(1); // BinaryType.String

        if (recordType is SerializationRecordType.ClassWithMembersAndTypes)
        {
            writer.Write(LibraryId);
        }
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream));
    }

    public static IEnumerable<object[]> ThrowsForInvalidPrimitiveType_Arguments()
    {
        foreach (SerializationRecordType recordType in new[] { SerializationRecordType.ClassWithMembersAndTypes, SerializationRecordType.SystemClassWithMembersAndTypes })
        {
            foreach (byte binaryType in new byte[] { (byte)0 /* BinaryType.Primitive */, (byte)7 /* BinaryType.PrimitiveArray */ })
            {
                yield return new object[] { recordType, binaryType, (byte)4 }; // value not used by the spec
                yield return new object[] { recordType, binaryType, (byte)19 };
            }
        }
    }

    [Theory]
    [MemberData(nameof(ThrowsForInvalidPrimitiveType_Arguments))]
    public void ThrowsForInvalidPrimitiveType(SerializationRecordType recordType, byte binaryType, byte invalidPrimitiveType)
    {
        const int LibraryId = 2;
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        WriteBinaryLibrary(writer, LibraryId, "LibraryName");

        // ClassInfo (always present)
        writer.Write((byte)recordType);
        writer.Write(1); // object ID
        writer.Write("TypeName"); // type name
        writer.Write(1); // member count
        writer.Write("MemberName");
        writer.Write(binaryType);
        writer.Write(invalidPrimitiveType);

        if (recordType is SerializationRecordType.ClassWithMembersAndTypes)
        {
            writer.Write(LibraryId);
        }
        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream));
    }

    [Fact]
    public void ThrowsOnInvalidArrayType()
    {
        using MemoryStream stream = new();
        BinaryWriter writer = new(stream, Encoding.UTF8);

        WriteSerializedStreamHeader(writer);
        writer.Write((byte)SerializationRecordType.BinaryArray);
        writer.Write(1); // object id
        writer.Write((byte)6);

        writer.Write((byte)SerializationRecordType.MessageEnd);

        stream.Position = 0;
        Assert.Throws<SerializationException>(() => NrbfDecoder.Decode(stream));
    }
}

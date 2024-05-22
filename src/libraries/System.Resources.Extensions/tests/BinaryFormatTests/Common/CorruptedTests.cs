// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FormatTests.Common.TestTypes;
using System.Runtime.Serialization;
using System.Windows.Forms.BinaryFormat;

namespace FormatTests.Common;

public abstract class CorruptedTests<T> : SerializationTest<T> where T : ISerializer
{
    [Fact]
    public void ValueTypeReferencesSelf()
    {
        MemoryStream stream = new();

        using (BinaryFormatWriterScope writer = new(stream))
        {
            new BinaryLibrary(2, typeof(NodeStruct).Assembly.FullName!).Write(writer);

            new ClassWithMembersAndTypes(
                new ClassInfo(1, typeof(NodeStruct).FullName!, ["Node"]),
                2,
                new MemberTypeInfo(
                    (BinaryType.Class, new ClassTypeInfo(typeof(NodeWithNodeStruct).FullName!, 2))),
                new MemberReference(1)).Write(writer);
        }

        stream.Position = 0;

        // This fails in the SerializationConstructor in BinaryFormattedObject's deserializer because the
        // type isn't convertible to NodeWithNodeStruct. In BinaryFormatter it fails with fixups.
        Action action = () => Deserialize(stream);
        action.Should().Throw<SerializationException>();
    }

    [Fact]
    public virtual void ValueTypeReferencesSelf2()
    {
        MemoryStream stream = new();

        using (BinaryFormatWriterScope writer = new(stream))
        {
            new BinaryLibrary(2, typeof(StructWithObject).Assembly.FullName!).Write(writer);

            new ClassWithMembersAndTypes(
                new ClassInfo(1, typeof(StructWithObject).FullName!, ["Value"]),
                2,
                new MemberTypeInfo(
                    (BinaryType.Class, new ClassTypeInfo(typeof(StructWithObject).FullName!, 2))),
                new MemberReference(1)).Write(writer);
        }

        stream.Position = 0;
        Deserialize(stream);
    }

    [Fact]
    public virtual void ValueTypeReferencesSelf3()
    {
        MemoryStream stream = new();

        using (BinaryFormatWriterScope writer = new(stream))
        {
            new BinaryLibrary(2, typeof(StructWithTwoObjects).Assembly.FullName!).Write(writer);

            new ClassWithMembersAndTypes(
                new ClassInfo(1, typeof(StructWithTwoObjects).FullName!, ["Value", "Value2"]),
                2,
                new MemberTypeInfo(
                    (BinaryType.Object, null),
                    (BinaryType.Object, null)),
                new MemberReference(1),
                new MemberReference(1)).Write(writer);
        }

        // Both deserializers create this where every boxed struct is the exact same boxed instance.
        stream.Position = 0;
        Deserialize(stream);
    }

    [Fact]
    public virtual void ValueTypeReferencesSelf4()
    {
        MemoryStream stream = new();

        using (BinaryFormatWriterScope writer = new(stream))
        {
            new BinaryLibrary(2, typeof(StructWithTwoObjectsISerializable).Assembly.FullName!).Write(writer);

            new ClassWithMembersAndTypes(
                new ClassInfo(1, typeof(StructWithTwoObjectsISerializable).FullName!, ["Value", "Value2"]),
                2,
                new MemberTypeInfo(
                    (BinaryType.Object, null),
                    (BinaryType.Object, null)),
                new MemberReference(1),
                new MemberReference(1)).Write(writer);
        }

        stream.Position = 0;
        Deserialize(stream);
    }

    [Fact]
    public virtual void ValueTypeReferencesSelf5()
    {
        MemoryStream stream = new();

        using (BinaryFormatWriterScope writer = new(stream))
        {
            new BinaryLibrary(2, typeof(StructWithTwoObjectsISerializable).Assembly.FullName!).Write(writer);

            ClassWithMembersAndTypes root = new(
                new ClassInfo(1, typeof(StructWithTwoObjectsISerializable).FullName!, ["Value", "Value2"]),
                2,
                new MemberTypeInfo(
                    (BinaryType.Object, null),
                    (BinaryType.Object, null)),
                new MemberReference(3),
                new MemberReference(3));

            root.Write(writer);

            new ClassWithId(
                3,
                root,
                new MemberReference(1),
                new MemberReference(3)).Write(writer);
        }

        stream.Position = 0;
        Deserialize(stream);
    }
}

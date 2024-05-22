// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BinaryFormatTests;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;

namespace FormatTests.FormattedObject;

public class BasicObjectTests : Common.BasicObjectTests<FormattedObjectSerializer>
{
    private static readonly SHA256 s_md5 = SHA256.Create();

    private protected override bool SkipOffsetArrays => true;

    [Theory]
    [MemberData(nameof(SerializableObjects))]
    public void BasicObjectsRoundTripAndMatch(object value, TypeSerializableValue[] _)
    {
        // We need to round trip through the BinaryFormatter as a few objects in tests remove
        // serialized data on deserialization.
        BinaryFormatter formatter = new();
        MemoryStream serialized = new();
        formatter.Serialize(serialized, value);
        serialized.Position = 0;
        object bfdeserialized = formatter.Deserialize(serialized);
        serialized.Position = 0;
        serialized.SetLength(0);
        formatter.Serialize(serialized, bfdeserialized);
        serialized.Position = 0;

        // Now deserialize with BinaryFormattedObject
        object deserialized = Deserialize(serialized);

        // And reserialize what we serialized with the BinaryFormatter
        MemoryStream deserializedSerialized = new();
        formatter.Serialize(deserializedSerialized, deserialized);

        deserializedSerialized.Position = 0;
        serialized.Position = 0;

        // Now compare the two streams to ensure they are identical
        deserializedSerialized.Length.Should().Be(serialized.Length);

        byte[] firstHash = s_md5.ComputeHash(serialized);
        byte[] secondHash = s_md5.ComputeHash(deserializedSerialized);

        firstHash.Should().BeEquivalentTo(secondHash);
    }
}

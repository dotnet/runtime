// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.Serialization.Formatters;
using BinaryFormatTests;
using BinaryFormatTests.FormatterTests;

namespace System.Resources.Extensions.Tests.Common;

public abstract class BasicObjectTests<T> : SerializationTest<T> where T : ISerializer
{
    private protected abstract bool SkipOffsetArrays { get; }

    [Theory]
    [MemberData(nameof(SerializableObjects))]
    public void DeserializeStoredObjects(object value, TypeSerializableValue[] serializedData)
    {
        // Following call may change the contents of the fields by invoking lazy-evaluated properties.
        EqualityExtensions.CheckEquals(value, value);

        int platformIndex = serializedData.GetPlatformIndex();
        for (int i = 0; i < serializedData.Length; i++)
        {
            for (FormatterAssemblyStyle assemblyMatching = 0; assemblyMatching <= FormatterAssemblyStyle.Full; assemblyMatching++)
            {
                object deserialized = DeserializeFromBase64Chars(serializedData[i].Base64Blob, assemblyMatching: assemblyMatching);

                if (deserialized is StringComparer)
                {
                    // StringComparer derived classes are not public and they don't serialize the actual type.
                    value.Should().BeAssignableTo<StringComparer>();
                }
                else
                {
                    deserialized.Should().BeOfType(value.GetType());
                }

                bool isSamePlatform = i == platformIndex;
                EqualityExtensions.CheckEquals(value, deserialized, isSamePlatform);
            }
        }
    }

    [Theory]
    [MemberData(nameof(BasicObjectsRoundtrip_MemberData))]
    public void BasicObjectsRoundtrip(
        object value,
        FormatterAssemblyStyle assemblyMatching,
        FormatterTypeStyle typeStyle)
    {
        // Following call may change the contents of the fields by invoking lazy-evaluated properties.
        EqualityExtensions.CheckEquals(value, value);

        object deserialized = RoundTrip(value, typeStyle: typeStyle, assemblyMatching: assemblyMatching);

        // string.Empty and DBNull are both singletons
        if (!ReferenceEquals(value, string.Empty)
            && value is not DBNull
            && value is Array array
            && array.Length > 0)
        {
            deserialized.Should().NotBeSameAs(value);
        }

        EqualityExtensions.CheckEquals(value, deserialized, isSamePlatform: true);
    }

    public static EnumerableTupleTheoryData<object, TypeSerializableValue[]> SerializableObjects()
    {
        // Can add a .Skip() to get to the failing scenario easier when debugging.
        return new EnumerableTupleTheoryData<object, TypeSerializableValue[]>((
            // Explicitly not supporting offset arrays
            from value in BinaryFormatterTests.RawSerializableObjects()
            where value.Item1 is not Array array || array.GetLowerBound(0) == 0
            select value).ToArray());
    }

    public static EnumerableTupleTheoryData<object, FormatterAssemblyStyle, FormatterTypeStyle> BasicObjectsRoundtrip_MemberData()
    {
        return new EnumerableTupleTheoryData<object, FormatterAssemblyStyle, FormatterTypeStyle>((
            // Explicitly not supporting offset arrays
            from value in BinaryFormatterTests.RawSerializableObjects()
            from FormatterAssemblyStyle assemblyFormat in new[] { FormatterAssemblyStyle.Full, FormatterAssemblyStyle.Simple }
            from FormatterTypeStyle typeFormat in new[] { FormatterTypeStyle.TypesAlways, FormatterTypeStyle.TypesAlways | FormatterTypeStyle.XsdString }
            where value.Item1 is not Array array || array.GetLowerBound(0) == 0
            select (value.Item1, assemblyFormat, typeFormat)).ToArray());
    }
}

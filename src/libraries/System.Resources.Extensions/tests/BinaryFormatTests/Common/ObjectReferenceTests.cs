// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Text;

namespace System.Resources.Extensions.Tests.Common;

public abstract class ObjectReferenceTests<T> : SerializationTest<T> where T : ISerializer
{
    [Fact]
    public void DBNull_Deserialize()
    {
        object deserialized = RoundTrip(DBNull.Value);
        deserialized.Should().BeSameAs(DBNull.Value);
    }

    [Theory]
    [MemberData(nameof(SupportedTypesTestData))]
    public void SupportedTypes_Deserialize(object value)
    {
        object deserialized = RoundTrip(value);
        deserialized.Should().NotBeNull();
    }

    public static TheoryData<object> SupportedTypesTestData { get; } = new()
    {
        ObjectReferenceNoFields.Value,
        new SerializableWithNestedSurrogate { Message = "Hello"}
    };

    [Fact]
    public void Singleton_NoFields_Deserialize()
    {
        // Representing singletons is the most common pattern for IObjectReference.
        object deserialized = RoundTrip(ObjectReferenceNoFields.Value);
        deserialized.Should().BeSameAs(ObjectReferenceNoFields.Value);
    }

    [Serializable]
    public sealed class ObjectReferenceNoFields : IObjectReference
    {
        public static ObjectReferenceNoFields Value { get; } = new();

        private ObjectReferenceNoFields() { }

        object IObjectReference.GetRealObject(StreamingContext context) => Value;
    }

    [Fact]
    public void NestedSurrogate_Deserialize()
    {
        object deserialized = RoundTrip(new SerializableWithNestedSurrogate { Message = "Hello" });
        deserialized.Should().BeOfType<SerializableWithNestedSurrogate>().Which.Message.Should().Be("Hello");
    }

    [Serializable]
#pragma warning disable CA2229 // Implement serialization constructors
    public sealed class SerializableWithNestedSurrogate : ISerializable
#pragma warning restore CA2229
    {
        public string Message { get; set; } = string.Empty;

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(SerializationSurrogate));
            info.AddValue(nameof(Message), Encoding.UTF8.GetBytes(Message));
        }

        [Serializable]
        private sealed class SerializationSurrogate : IObjectReference, ISerializable
        {
            private readonly byte[] _bytes;

            private SerializationSurrogate(SerializationInfo info, StreamingContext context)
            {
                _bytes = (byte[])(info.GetValue(nameof(Message), typeof(byte[])) ?? throw new InvalidOperationException());
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) =>
                throw new InvalidOperationException();

            object IObjectReference.GetRealObject(StreamingContext context)
                => new SerializableWithNestedSurrogate() { Message = Encoding.UTF8.GetString(_bytes) };
        }
    }

    [Fact]
    public void NestedSurrogate_NullableEnum_Deserialize()
    {
        object deserialized = RoundTrip(new SerializableWithNestedSurrogate_NullableEnum());
        deserialized.Should().BeOfType<SerializableWithNestedSurrogate_NullableEnum>().Which.Day.Should().BeNull();

        deserialized = RoundTrip(new SerializableWithNestedSurrogate_NullableEnum { Day = DayOfWeek.Monday });
        deserialized.Should().BeOfType<SerializableWithNestedSurrogate_NullableEnum>().Which.Day.Should().Be(DayOfWeek.Monday);
    }

    [Serializable]
#pragma warning disable CA2229 // Implement serialization constructors
    public sealed class SerializableWithNestedSurrogate_NullableEnum : ISerializable
#pragma warning restore CA2229
    {
        public DayOfWeek? Day { get; set; }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.SetType(typeof(SerializationSurrogate));
            info.AddValue(nameof(Day), Day);
        }

        [Serializable]
        private sealed class SerializationSurrogate : IObjectReference, ISerializable
        {
            private readonly DayOfWeek? _day;

            private SerializationSurrogate(SerializationInfo info, StreamingContext context)
            {
                _day = (DayOfWeek?)(info.GetValue(nameof(Day), typeof(DayOfWeek?)));
            }

            void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) =>
                throw new InvalidOperationException();

            object IObjectReference.GetRealObject(StreamingContext context)
                => new SerializableWithNestedSurrogate_NullableEnum() { Day = _day };
        }
    }
}

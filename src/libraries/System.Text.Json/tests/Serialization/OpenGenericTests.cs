// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class OpenGenericTests_Span : OpenGenericTests
    {
        public OpenGenericTests_Span() : base(SerializationWrapper.SpanSerializer) { }
    }

    public class OpenGenericTests_String : OpenGenericTests
    {
        public OpenGenericTests_String() : base(SerializationWrapper.StringSerializer) { }
    }

    public class OpenGenericTests_Stream : OpenGenericTests
    {
        public OpenGenericTests_Stream() : base(SerializationWrapper.StreamSerializer) { }
    }

    public class OpenGenericTests_StreamWithSmallBuffer : OpenGenericTests
    {
        public OpenGenericTests_StreamWithSmallBuffer() : base(SerializationWrapper.StreamSerializerWithSmallBuffer) { }
    }

    public class OpenGenericTests_Writer : OpenGenericTests
    {
        public OpenGenericTests_Writer() : base(SerializationWrapper.WriterSerializer) { }
    }

    public abstract class OpenGenericTests
    {
        private SerializationWrapper Serializer { get; }

        public OpenGenericTests(SerializationWrapper serializer)
        {
            Serializer = serializer;
        }

        [Fact]
        public void DeserializeOpenGeneric()
        {
            foreach (object[] obj in TypesWithOpenGenerics_ToDeserialize())
            {
                Type type = (Type)obj[0];

                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize("", type));
                Assert.Contains(type.ToString(), ex.ToString());
            }
        }

        [Fact]
        public void SerializeOpenGeneric()
        {
            object obj;

            InvalidOperationException ex;

            foreach (object[] types in TypesWithOpenGenerics_ToSerialize())
            {
                Type type = (Type)types[0];

                if (type.GetGenericArguments().Length == 1)
                {
                    obj = Activator.CreateInstance(type.MakeGenericType(typeof(int)));
                }
                else
                {
                    obj = Activator.CreateInstance(type.MakeGenericType(typeof(string), typeof(int)));
                }

                Assert.Throws<ArgumentException>(() => Serializer.Serialize(obj, type));

                ex = Assert.Throws<InvalidOperationException>(() => Serializer.Serialize(null, type));
                Assert.Contains(type.ToString(), ex.ToString());
            }

            Type openNullableType = typeof(Nullable<>);

            obj = Activator.CreateInstance(openNullableType.MakeGenericType(typeof(int)));
            ex = Assert.Throws<InvalidOperationException>(() => Serializer.Serialize(obj, openNullableType));
            Assert.Contains(openNullableType.ToString(), ex.ToString());
        }

        private class Test<T> { }

        private static IEnumerable<object[]> TypesWithOpenGenerics_ToDeserialize()
        {
            yield return new object[] { typeof(Test<>) };
            yield return new object[] { typeof(Nullable<>) };
            yield return new object[] { typeof(List<>) };
            yield return new object[] { typeof(List<>).MakeGenericType(typeof(Test<>)) };
            yield return new object[] { typeof(Test<>).MakeGenericType(typeof(List<>)) };
            yield return new object[] { typeof(Dictionary<,>) };
            yield return new object[] { typeof(Dictionary<,>).MakeGenericType(typeof(string), typeof(Nullable<>)) };
            yield return new object[] { typeof(Dictionary<,>).MakeGenericType(typeof(Nullable<>), typeof(string)) };
        }

        private static IEnumerable<object[]> TypesWithOpenGenerics_ToSerialize()
        {
            yield return new object[] { typeof(Test<>) };
            yield return new object[] { typeof(List<>) };
            yield return new object[] { typeof(Dictionary<,>) };
        }
    }
}

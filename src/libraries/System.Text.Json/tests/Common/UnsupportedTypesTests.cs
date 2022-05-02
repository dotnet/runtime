// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract class UnsupportedTypesTests : SerializerTests
    {
        private bool SupportsJsonPathOnSerialize { get; init; }

        public UnsupportedTypesTests(
            JsonSerializerWrapper serializerWrapper,
            bool supportsJsonPathOnSerialize) : base(serializerWrapper)
        {
            SupportsJsonPathOnSerialize = supportsJsonPathOnSerialize;
        }

        [Fact]
        public async Task DeserializeUnsupportedType()
        {
            // Any test payload is fine.
            string json = @"""Some string""";

            await RunTest<Type>(json);
            await RunTest<SerializationInfo>(json);
            await RunTest<IntPtr>(json);
            await RunTest<IntPtr?>(json); // One nullable variation.
            await RunTest<UIntPtr>(json);
#if NETCOREAPP
            await RunTest<DateOnly>(json);
            await RunTest<TimeOnly>(json);
#endif
#if BUILDING_SOURCE_GENERATOR_TESTS
            await RunTest<IAsyncEnumerable<int>>(json);
            await RunTest<ClassThatImplementsIAsyncEnumerable>(json);
#endif

            async Task RunTest<T>(string json)
            {
                Type type = GetNullableOfTUnderlyingType(typeof(T), out bool isNullableOfT);
                string fullName = type.FullName;

                NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<T>(json));
                string exAsStr = ex.ToString();
                Assert.Contains(fullName, exAsStr);
                Assert.Contains("$", exAsStr);

                json = $@"{{""Prop"":{json}}}";

                ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.DeserializeWrapper<ClassWithType<T>>(json));
                exAsStr = ex.ToString();
                Assert.Contains(fullName, exAsStr);
                Assert.Contains("$.Prop", exAsStr);

                // Verify Nullable<> semantics. NSE is not thrown because the serializer handles null.
                if (isNullableOfT)
                {
                    Assert.Null(JsonSerializer.Deserialize<T>("null"));

                    json = $@"{{""Prop"":null}}";
                    ClassWithType<T> obj = await Serializer.DeserializeWrapper<ClassWithType<T>>(json);
                    Assert.Null(obj.Prop);
                }
            }
        }

        [Fact]
        public async Task SerializeUnsupportedType()
        {
            // TODO refactor to Xunit theory
            await RunTest(typeof(int));
            await RunTest(new SerializationInfo(typeof(Type), new FormatterConverter()));
            await RunTest((IntPtr)123);
            await RunTest<IntPtr?>(new IntPtr(123)); // One nullable variation.
            await RunTest((UIntPtr)123);
#if NETCOREAPP
            await RunTest(DateOnly.MaxValue);
            await RunTest(TimeOnly.MinValue);
#endif
#if BUILDING_SOURCE_GENERATOR_TESTS
            await RunTest(new ClassThatImplementsIAsyncEnumerable());
#endif

            async Task RunTest<T>(T value)
            {
                Type type = GetNullableOfTUnderlyingType(typeof(T), out bool isNullableOfT);
                string fullName = type.FullName;

                NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.SerializeWrapper(value));
                string exAsStr = ex.ToString();
                Assert.Contains(fullName, exAsStr);
                Assert.Contains("$", exAsStr);

                ClassWithType<T> obj = new ClassWithType<T> { Prop = value };
                ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.SerializeWrapper(obj));
                exAsStr = ex.ToString();
                Assert.Contains(fullName, exAsStr);

                if (SupportsJsonPathOnSerialize)
                {
                    Assert.Contains("$.Prop", exAsStr);
                }
                else
                {
                    Assert.Contains("$.", exAsStr);
                    Assert.DoesNotContain("$.Prop", exAsStr);
                }

                // Verify null semantics. NSE is not thrown because the serializer handles null.
                if (!type.IsValueType || isNullableOfT)
                {
                    string serialized = await Serializer.SerializeWrapper<T>((T)(object)null);
                    Assert.Equal("null", serialized);

                    obj.Prop = (T)(object)null;
                    serialized = await Serializer.SerializeWrapper(obj);
                    Assert.Equal(@"{""Prop"":null}", serialized);

                    serialized = await Serializer.SerializeWrapper(obj, new JsonSerializerOptions { IgnoreNullValues = true });
                    Assert.Equal(@"{}", serialized);
                }

#if !BUILDING_SOURCE_GENERATOR_TESTS
                Type runtimeType = GetNullableOfTUnderlyingType(value.GetType(), out bool _);

                ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.SerializeWrapper<object>(value));
                exAsStr = ex.ToString();
                Assert.Contains(runtimeType.FullName, exAsStr);
                Assert.Contains("$", exAsStr);

                ClassWithType<object> polyObj = new ClassWithType<object> { Prop = value };
                ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await Serializer.SerializeWrapper(polyObj));
                exAsStr = ex.ToString();
                Assert.Contains(runtimeType.FullName, exAsStr);
#endif
            }
        }

        public class ClassWithIntPtr
        {
            public IntPtr MyIntPtr { get; set; }
        }

        public class ClassWithIntPtrConverter
        {
            [JsonConverter(typeof(IntPtrConverter))]
            public IntPtr MyIntPtr { get; set; }
        }

        public class IntPtrConverter : JsonConverter<IntPtr>
        {
            public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                int value = reader.GetInt32();
                return new IntPtr(value);
            }

            public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.ToInt32());
            }
        }

        [Fact]
        public async Task RuntimeConverterIsSupported_IntPtr()
        {
            const string Json = "{\"MyIntPtr\":42}";
            string serialized;
            JsonSerializerOptions options = new();
            options.Converters.Add(new IntPtrConverter());

            serialized = await Serializer.SerializeWrapper(new IntPtr(42), options);
            Assert.Equal("42", serialized);

            IntPtr intPtr = await Serializer.DeserializeWrapper<IntPtr>("42", options);
            Assert.Equal(42, intPtr.ToInt32());

            ClassWithIntPtr obj = new() { MyIntPtr = new IntPtr(42) };
            serialized = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal(Json, serialized);

            obj = await Serializer.DeserializeWrapper<ClassWithIntPtr>(Json, options);
            Assert.Equal(42, obj.MyIntPtr.ToInt32());
        }

        [Fact]
        public async Task CompileTimeConverterIsSupported_IntPtr()
        {
            const string Json = "{\"MyIntPtr\":42}";

            ClassWithIntPtrConverter obj = new() { MyIntPtr = new IntPtr(42) };
            string serialized = await Serializer.SerializeWrapper(obj);
            Assert.Equal(Json, serialized);

            obj = await Serializer.DeserializeWrapper<ClassWithIntPtrConverter>(Json);
            Assert.Equal(42, obj.MyIntPtr.ToInt32());
        }

        public class ClassWithAsyncEnumerableConverter
        {
            [JsonConverter(typeof(AsyncEnumerableConverter))]
            public ClassThatImplementsIAsyncEnumerable MyAsyncEnumerable { get; set; }
        }

        public class ClassThatImplementsIAsyncEnumerable : IAsyncEnumerable<int>
        {
            public string Status { get; set; } = "Created";

            // Should not be called.
            IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken cancellationToken) => throw new NotImplementedException();
        }

        public class AsyncEnumerableConverter : JsonConverter<ClassThatImplementsIAsyncEnumerable>
        {
            public override ClassThatImplementsIAsyncEnumerable Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Assert.Equal(JsonTokenType.StartArray, reader.TokenType);
                reader.Read();
                Assert.Equal(JsonTokenType.EndArray, reader.TokenType);
                return new ClassThatImplementsIAsyncEnumerable { Status = "Read" };
            }

            public override void Write(Utf8JsonWriter writer, ClassThatImplementsIAsyncEnumerable value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
                value.Status = "Write";
            }
        }

        [Fact]
        public async Task RuntimeConverterIsSupported_AsyncEnumerable()
        {
            const string Json = "{\"MyAsyncEnumerable\":[]}";

            string serialized;
            JsonSerializerOptions options = new();
            options.Converters.Add(new AsyncEnumerableConverter());

            ClassThatImplementsIAsyncEnumerable obj = new();
            Assert.Equal("Created", obj.Status);
            serialized = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("[]", serialized);
            Assert.Equal("Write", obj.Status);
            obj = await Serializer.DeserializeWrapper<ClassThatImplementsIAsyncEnumerable>("[]", options);
            Assert.Equal("Read", obj.Status);

            ClassWithAsyncEnumerableConverter poco = new();
            poco.MyAsyncEnumerable = new ClassThatImplementsIAsyncEnumerable();
            Assert.Equal("Created", poco.MyAsyncEnumerable.Status);
            serialized = await Serializer.SerializeWrapper(poco, options);
            Assert.Equal(Json, serialized);
            Assert.Equal("Write", poco.MyAsyncEnumerable.Status);
            poco = await Serializer.DeserializeWrapper<ClassWithAsyncEnumerableConverter>(Json, options);
            Assert.Equal("Read", poco.MyAsyncEnumerable.Status);
        }

        [Fact]
        public async Task CompileTimeConverterIsSupported_AsyncEnumerable()
        {
            const string Json = "{\"MyAsyncEnumerable\":[]}";

            ClassWithAsyncEnumerableConverter obj = new();
            obj.MyAsyncEnumerable = new ClassThatImplementsIAsyncEnumerable();
            Assert.Equal("Created", obj.MyAsyncEnumerable.Status);

            string serialized = await Serializer.SerializeWrapper(obj);
            Assert.Equal(Json, serialized);
            Assert.Equal("Write", obj.MyAsyncEnumerable.Status);

            obj = await Serializer.DeserializeWrapper<ClassWithAsyncEnumerableConverter>(Json);
            Assert.Equal("Read", obj.MyAsyncEnumerable.Status);
        }

        public static Type GetNullableOfTUnderlyingType(Type type, out bool isNullableOfT)
        {
            isNullableOfT = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
            return isNullableOfT ? type.GetGenericArguments()[0] : type;
        }
    }
}

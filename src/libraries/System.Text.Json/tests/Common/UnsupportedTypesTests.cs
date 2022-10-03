// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
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

        [Theory]
        [MemberData(nameof(GetUnsupportedValues))]
        public async Task DeserializeUnsupportedType<T>(ValueWrapper<T> wrapper)
        {
            _ = wrapper; // only used to instantiate T

            string json = @"""Some string"""; // Any test payload is fine.

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

        [Theory]
        [MemberData(nameof(GetUnsupportedValues))]
        public async Task SerializeUnsupportedType<T>(ValueWrapper<T> wrapper)
        {
            T value = wrapper.value;

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

        public static IEnumerable<object[]> GetUnsupportedValues()
        {
            yield return WrapArgs(typeof(int));
            yield return WrapArgs(typeof(ClassWithExtensionProperty).GetConstructor(Array.Empty<Type>()));
            yield return WrapArgs(typeof(ClassWithExtensionProperty).GetProperty(nameof(ClassWithExtensionProperty.MyInt)));
            yield return WrapArgs(new SerializationInfo(typeof(Type), new FormatterConverter()));
            yield return WrapArgs((IntPtr)123);
            yield return WrapArgs<IntPtr?>(new IntPtr(123)); // One nullable variation.
            yield return WrapArgs((UIntPtr)123);

            static object[] WrapArgs<T>(T value) => new object[] { new ValueWrapper<T>(value) };
        }

        // Helper record used to path both value & type information to generic theories.
        // This is needed e.g. when passing System.Type instances whose runtime type
        // actually is System.Reflection.RuntimeType.
        public record ValueWrapper<T>(T value)
        {
            public override string ToString() => value.ToString();
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

#if !BUILDING_SOURCE_GENERATOR_TESTS
        [Fact]
        public async Task TypeWithNullConstructorParameterName_ThrowsNotSupportedException()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/58690
            Type type = Assembly.GetExecutingAssembly().GetType("System.Runtime.CompilerServices.NullableContextAttribute")!;
            ConstructorInfo ctorInfo = type.GetConstructor(new Type[] { typeof(byte) });
            Assert.True(string.IsNullOrEmpty(ctorInfo.GetParameters()[0].Name));
            object value = ctorInfo.Invoke(new object[] { (byte)0 });

            await Assert.ThrowsAnyAsync<NotSupportedException>(() => Serializer.SerializeWrapper(value));
            await Assert.ThrowsAnyAsync<NotSupportedException>(() => Serializer.DeserializeWrapper("{}", type));
        }
#endif

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

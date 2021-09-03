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
        private bool SupportsIAsyncEnumerable { get; init; }

        public UnsupportedTypesTests(
            JsonSerializerWrapperForString serializerWrapper,
            bool supportsIAsyncEnumerable,
            bool supportsJsonPathOnSerialize) : base(serializerWrapper)
        {
            SupportsIAsyncEnumerable = supportsIAsyncEnumerable;
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

            if (!SupportsIAsyncEnumerable)
            {
                await RunTest<IAsyncEnumerable<int>>(json);
                await RunTest<IntAsyncEnumerable>(json);
            }

            async Task RunTest<T>(string json)
            {
                Type type = GetNullableOfTUnderlyingType(typeof(T));
                string fullName = type.FullName;

                NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<T>(json));
                string exAsStr = ex.ToString();
                Assert.Contains(fullName, exAsStr);
                Assert.Contains("$", exAsStr);

                json = $@"{{""Prop"":{json}}}";

                ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithType<T>>(json));
                exAsStr = ex.ToString();
                Assert.Contains(fullName, exAsStr);
                Assert.Contains("$.Prop", exAsStr);

                // NSE is not thrown because the serializer handles null.
                if (typeof(T).IsValueType)
                {
                    bool nullableOfTUsed = typeof(T) != type;
                    if (nullableOfTUsed)
                    {
                        Assert.Null(JsonSerializer.Deserialize<T>("null"));

                        json = $@"{{""Prop"":null}}";
                        ClassWithType<T> obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithType<T>>(json);
                        Assert.Null(obj.Prop);
                    }
                }
            }
        }

        [Fact]
        public async Task SerializeUnsupportedType()
        {
            await RunTest(typeof(int));
            await RunTest(new SerializationInfo(typeof(Type), new FormatterConverter()));
            await RunTest((IntPtr)123);
            await RunTest<IntPtr?>(new IntPtr(123)); // One nullable variation.
            await RunTest((UIntPtr)123);
#if NETCOREAPP
            await RunTest(DateOnly.MaxValue);
            await RunTest(TimeOnly.MinValue);
#endif

            if (!SupportsIAsyncEnumerable)
            {
                await RunTest(new IntAsyncEnumerable());
            }

            async Task RunTest<T>(T value)
            {
                Type type = GetNullableOfTUnderlyingType(typeof(T));
                string fullName = type.FullName;

                NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(value));
                string exAsStr = ex.ToString();
                Assert.Contains(fullName, exAsStr);
                Assert.Contains("$", exAsStr);

                ClassWithType<T> obj = new ClassWithType<T> { Prop = value };

                ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(obj));
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

                if (type.IsValueType)
                {
                    // NSE is not thrown because the serializer handles null.
                    if (typeof(T).IsValueType)
                    {
                        bool nullableOfTUsed = typeof(T) != type;
                        if (nullableOfTUsed)
                        {
                            // Null is handled by the serializer and doesn't throw.
                            string serialized = await JsonSerializerWrapperForString.SerializeWrapper<T>((T)(object)null);
                            Assert.Equal("null", serialized);

                            obj.Prop = (T)(object)null;
                            serialized = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                            Assert.Equal(@"{""Prop"":null}", serialized);

                            serialized = await JsonSerializerWrapperForString.SerializeWrapper(obj, new JsonSerializerOptions { IgnoreNullValues = true });
                            Assert.Equal(@"{}", serialized);
                        }
                    }
                }
                else
                {
                    string serialized = await JsonSerializerWrapperForString.SerializeWrapper((T)(object)null);
                    Assert.Equal("null", serialized);

                    obj.Prop = (T)(object)null;
                    serialized = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                    Assert.Equal(@"{""Prop"":null}", serialized);

                    serialized = await JsonSerializerWrapperForString.SerializeWrapper(obj, new JsonSerializerOptions { IgnoreNullValues = true });
                    Assert.Equal(@"{}", serialized);
                }
            }
        }

        [Fact]
        public async Task RuntimeConverterIsSupported()
        {
            const string Json = "{\"MyIntPtr\":42}";
            string serialized;
            JsonSerializerOptions options = new();
            options.Converters.Add(new IntPtrConverter());

            serialized = await JsonSerializerWrapperForString.SerializeWrapper(new IntPtr(42), options);
            Assert.Equal("42", serialized);

            IntPtr intPtr = await JsonSerializerWrapperForString.DeserializeWrapper<IntPtr>("42", options);
            Assert.Equal(42, intPtr.ToInt32());

            ClassWithIntPtr obj = new() { MyIntPtr = new IntPtr(42) };
            serialized = await JsonSerializerWrapperForString.SerializeWrapper(obj, options);
            Assert.Equal(Json, serialized);

            obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithIntPtr>(Json, options);
            Assert.Equal(42, obj.MyIntPtr.ToInt32());
        }

        [Fact]
        public async Task CompileTimeConverterIsSupported()
        {
            const string Json = "{\"MyIntPtr\":42}";

            ClassWithIntPtrConverter obj = new() { MyIntPtr = new IntPtr(42) };
            string serialized = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.Equal(Json, serialized);

            obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithIntPtrConverter>(Json);
            Assert.Equal(42, obj.MyIntPtr.ToInt32());
        }

        public class IntAsyncEnumerable : IAsyncEnumerable<int>
        {
            // Should not be called.
            IAsyncEnumerator<int> IAsyncEnumerable<int>.GetAsyncEnumerator(CancellationToken cancellationToken) => throw new NotImplementedException();
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

        public static Type GetNullableOfTUnderlyingType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return type.GetGenericArguments()[0];
            }

            return type;
        }
    }
}

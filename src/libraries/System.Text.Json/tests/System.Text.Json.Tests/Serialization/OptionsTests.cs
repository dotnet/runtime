// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Tests;
using System.Text.Unicode;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class OptionsTests
    {
        private class TestConverter : JsonConverter<bool>
        {
            public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public static void SetOptionsFail()
        {
            var options = new JsonSerializerOptions();

            TestIListNonThrowingOperationsWhenMutable(options.Converters, () => new TestConverter());
            TestIListNonThrowingOperationsWhenMutable(options.TypeInfoResolverChain, () => new DefaultJsonTypeInfoResolver());

            // Now set DefaultTypeInfoResolver
            options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
            TestIListNonThrowingOperationsWhenMutable((options.TypeInfoResolver as DefaultJsonTypeInfoResolver).Modifiers, () => (ti) => { });

            // Add one item for later.
            Action<JsonTypeInfo> tiModifier = (ti) => { };
            TestConverter tc = new TestConverter();
            options.Converters.Add(tc);
            (options.TypeInfoResolver as DefaultJsonTypeInfoResolver).Modifiers.Add(tiModifier);

            TestIListThrowingOperationsWhenMutable(options.Converters);
            TestIListThrowingOperationsWhenMutable(options.TypeInfoResolverChain);
            TestIListThrowingOperationsWhenMutable((options.TypeInfoResolver as DefaultJsonTypeInfoResolver).Modifiers);

            // Perform serialization.
            JsonSerializer.Deserialize<int>("1", options);

            // Verify defaults and ensure getters do not throw.
            Assert.False(options.AllowTrailingCommas);
            Assert.Equal(16 * 1024, options.DefaultBufferSize);
            Assert.Null(options.DictionaryKeyPolicy);
            Assert.Null(options.Encoder);
            Assert.False(options.IgnoreNullValues);
            Assert.Equal(0, options.MaxDepth);
            Assert.False(options.PropertyNameCaseInsensitive);
            Assert.Null(options.PropertyNamingPolicy);
            Assert.Equal(JsonCommentHandling.Disallow, options.ReadCommentHandling);
            Assert.Equal(JsonUnmappedMemberHandling.Skip, options.UnmappedMemberHandling);
            Assert.False(options.WriteIndented);

            TestIListNonThrowingOperationsWhenImmutable(options.Converters, tc);
            TestIListNonThrowingOperationsWhenImmutable(options.TypeInfoResolverChain, options.TypeInfoResolver);
            TestIListNonThrowingOperationsWhenImmutable((options.TypeInfoResolver as DefaultJsonTypeInfoResolver).Modifiers, tiModifier);

            // Setters should always throw; we don't check to see if the value is the same or not.
            Assert.Throws<InvalidOperationException>(() => options.AllowTrailingCommas = options.AllowTrailingCommas);
            Assert.Throws<InvalidOperationException>(() => options.DefaultBufferSize = options.DefaultBufferSize);
            Assert.Throws<InvalidOperationException>(() => options.DictionaryKeyPolicy = options.DictionaryKeyPolicy);
            Assert.Throws<InvalidOperationException>(() => options.Encoder = JavaScriptEncoder.Default);
            Assert.Throws<InvalidOperationException>(() => options.IgnoreNullValues = options.IgnoreNullValues);
            Assert.Throws<InvalidOperationException>(() => options.MaxDepth = options.MaxDepth);
            Assert.Throws<InvalidOperationException>(() => options.PropertyNameCaseInsensitive = options.PropertyNameCaseInsensitive);
            Assert.Throws<InvalidOperationException>(() => options.PropertyNamingPolicy = options.PropertyNamingPolicy);
            Assert.Throws<InvalidOperationException>(() => options.ReadCommentHandling = options.ReadCommentHandling);
            Assert.Throws<InvalidOperationException>(() => options.UnmappedMemberHandling = options.UnmappedMemberHandling);
            Assert.Throws<InvalidOperationException>(() => options.WriteIndented = options.WriteIndented);
            Assert.Throws<InvalidOperationException>(() => options.TypeInfoResolver = options.TypeInfoResolver);

            TestIListThrowingOperationsWhenImmutable(options.Converters, tc);
            TestIListThrowingOperationsWhenImmutable(options.TypeInfoResolverChain, options.TypeInfoResolver);
            TestIListThrowingOperationsWhenImmutable((options.TypeInfoResolver as DefaultJsonTypeInfoResolver).Modifiers, tiModifier);

            static void TestIListNonThrowingOperationsWhenMutable<T>(IList<T> list, Func<T> newT)
            {
                list.Clear();
                T el = newT();
                list.Add(el);
                Assert.Equal(1, list.Count);
                list.Insert(0, newT());
                Assert.Equal(2, list.Count);
                list.Remove(el);
                Assert.Equal(1, list.Count);
                list.RemoveAt(0);
                Assert.Equal(0, list.Count);
                Assert.False(list.IsReadOnly, "List should not be read-only");
            }

            static void TestIListThrowingOperationsWhenMutable<T>(IList<T> list) where T : class
            {
                // Verify collection throws on null adds.
                Assert.Throws<ArgumentNullException>(() => list.Add(null));
                Assert.Throws<ArgumentNullException>(() => list.Insert(0, null));
                Assert.Throws<ArgumentNullException>(() => list[0] = null);
            }

            static void TestIListNonThrowingOperationsWhenImmutable<T>(IList<T> list, T onlyElement)
            {
                Assert.Equal(onlyElement, list[0]);
                Assert.True(list.Contains(onlyElement));
                list.CopyTo(new T[1] { default(T) }, 0);
                Assert.Equal(1, list.Count);
                Assert.False(list.Equals(onlyElement));
                Assert.NotNull(list.GetEnumerator());
                Assert.Equal(0, list.IndexOf(onlyElement));
                Assert.True(list.IsReadOnly, "List should be read-only");
            }

            static void TestIListThrowingOperationsWhenImmutable<T>(IList<T> list, T firstElement)
            {
                Assert.Throws<InvalidOperationException>(() => list[0] = firstElement);
                Assert.Throws<InvalidOperationException>(() => list.Clear());
                Assert.Throws<InvalidOperationException>(() => list.Add(firstElement));
                Assert.Throws<InvalidOperationException>(() => list.Insert(0, firstElement));
                Assert.Throws<InvalidOperationException>(() => list.Remove(firstElement));
                Assert.Throws<InvalidOperationException>(() => list.RemoveAt(0));
            }
        }

        [Fact]
        public static void NewDefaultOptions_TypeInfoResolverIsNull()
        {
            var options = new JsonSerializerOptions();
            Assert.Null(options.TypeInfoResolver);
        }

        [Fact]
        public static void NewDefaultOptions_IsNotReadOnly()
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
            Assert.False(options.IsReadOnly);

            options.MakeReadOnly();
            Assert.True(options.IsReadOnly);
            options.MakeReadOnly(); // Is idempotent operation
            Assert.True(options.IsReadOnly);

            Assert.Throws<InvalidOperationException>(() => options.MaxDepth = 13);
            Assert.Throws<InvalidOperationException>(() => options.Converters.Add(new JsonStringEnumConverter()));
            Assert.Throws<InvalidOperationException>(() => options.AddContext<JsonContext>());
            Assert.Throws<InvalidOperationException>(() => options.TypeInfoResolver = null);

            options.MakeReadOnly(); // Is still idempotent operation
            Assert.True(options.IsReadOnly);
        }

        [Fact]
        public static void NewDefaultOptions_MakeReadOnly_NoTypeInfoResolver_ThrowsInvalidOperation()
        {
            var options = new JsonSerializerOptions();
            Assert.False(options.IsReadOnly);

            Assert.Throws<InvalidOperationException>(() => options.MakeReadOnly());
            Assert.False(options.IsReadOnly);

            options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
            options.MakeReadOnly();
            Assert.True(options.IsReadOnly);
        }

        [Fact]
        public static void TypeInfoResolverCannotBeSetAfterAddingContext()
        {
            var options = new JsonSerializerOptions();
            Assert.False(options.IsReadOnly);

            options.AddContext<JsonContext>();
            Assert.True(options.IsReadOnly);

            Assert.IsType<JsonContext>(options.TypeInfoResolver);
            Assert.Throws<InvalidOperationException>(() => options.TypeInfoResolver = new DefaultJsonTypeInfoResolver());
        }

        [Fact]
        public static void TypeInfoResolverCannotBeSetOnOptionsCreatedFromContext()
        {
            var context = new JsonContext();
            var options = context.Options;
            Assert.Same(context, options.TypeInfoResolver);
            Assert.Throws<InvalidOperationException>(() => options.TypeInfoResolver = new DefaultJsonTypeInfoResolver());
        }

        [Fact]
        public static void WhenAddingContextTypeInfoResolverAsContextOptionsAreSameAsOptions()
        {
            var options = new JsonSerializerOptions();
            options.AddContext<JsonContext>();
            Assert.Same(options, (options.TypeInfoResolver as JsonContext).Options);
        }

        [Fact]
        public static void WhenAddingContext_SettingResolverToNullThrowsInvalidOperationException()
        {
            var options = new JsonSerializerOptions();
            options.AddContext<JsonContext>();
            Assert.Throws<InvalidOperationException>(() => options.TypeInfoResolver = null);
        }

        [Fact]
        public static void TypeInfoResolverCanBeSetAfterContextIsSetThroughTypeInfoResolver()
        {
            var options = new JsonSerializerOptions();
            IJsonTypeInfoResolver resolver = new JsonContext();
            options.TypeInfoResolver = resolver;
            Assert.Same(resolver, options.TypeInfoResolver);

            resolver = new DefaultJsonTypeInfoResolver();
            options.TypeInfoResolver = resolver;
            Assert.Same(resolver, options.TypeInfoResolver);
        }

        [Fact]
        public static void DefaultBufferSizeFail()
        {
            Assert.Throws<ArgumentException>(() => new JsonSerializerOptions().DefaultBufferSize = 0);
            Assert.Throws<ArgumentException>(() => new JsonSerializerOptions().DefaultBufferSize = -1);
        }

        [Fact]
        public static void DefaultBufferSize()
        {
            var options = new JsonSerializerOptions();

            Assert.Equal(16 * 1024, options.DefaultBufferSize);

            options.DefaultBufferSize = 1;
            Assert.Equal(1, options.DefaultBufferSize);
        }

        [Fact]
        public static void AllowTrailingCommas()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int[]>("[1,]"));

            var options = new JsonSerializerOptions();
            options.AllowTrailingCommas = true;

            int[] value = JsonSerializer.Deserialize<int[]>("[1,]", options);
            Assert.Equal(1, value[0]);
        }

        [Fact]
        public static void WriteIndented()
        {
            var obj = new BasicCompany();
            obj.Initialize();

            // Verify default value.
            string json = JsonSerializer.Serialize(obj);
            Assert.DoesNotContain(Environment.NewLine, json);

            // Verify default value on options.
            var options = new JsonSerializerOptions();
            json = JsonSerializer.Serialize(obj, options);
            Assert.DoesNotContain(Environment.NewLine, json);

            // Change the value on options.
            options = new JsonSerializerOptions();
            options.WriteIndented = true;
            json = JsonSerializer.Serialize(obj, options);
            Assert.Contains(Environment.NewLine, json);
        }

        [Fact]
        public static void ExtensionDataUsesReaderOptions()
        {
            // We just verify trailing commas.
            const string json = @"{""MyIntMissing"":2,}";

            // Verify baseline without options.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithExtensionProperty>(json));

            // Verify baseline with options.
            var options = new JsonSerializerOptions();
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithExtensionProperty>(json, options));

            // Set AllowTrailingCommas to true.
            options = new JsonSerializerOptions();
            options.AllowTrailingCommas = true;
            JsonSerializer.Deserialize<ClassWithExtensionProperty>(json, options);
        }

        [Fact]
        public static void ExtensionDataUsesWriterOptions()
        {
            // We just verify whitespace.

            ClassWithExtensionProperty obj = JsonSerializer.Deserialize<ClassWithExtensionProperty>(@"{""MyIntMissing"":2}");

            // Verify baseline without options.
            string json = JsonSerializer.Serialize(obj);
            Assert.False(HasNewLine());

            // Verify baseline with options.
            var options = new JsonSerializerOptions();
            json = JsonSerializer.Serialize(obj, options);
            Assert.False(HasNewLine());

            // Set AllowTrailingCommas to true.
            options = new JsonSerializerOptions();
            options.WriteIndented = true;
            json = JsonSerializer.Serialize(obj, options);
            Assert.True(HasNewLine());

            bool HasNewLine()
            {
                int iEnd = json.IndexOf("2", json.IndexOf("MyIntMissing"));
                return json.Substring(iEnd + 1).StartsWith(Environment.NewLine);
            }
        }

        [Fact]
        public static void ReadCommentHandling()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>("/* comment */"));

            var options = new JsonSerializerOptions();

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>("/* comment */", options));

            options = new JsonSerializerOptions();
            options.ReadCommentHandling = JsonCommentHandling.Skip;

            int value = JsonSerializer.Deserialize<int>("1 /* comment */", options);
            Assert.Equal(1, value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData((int)JsonCommentHandling.Allow)]
        [InlineData(3)]
        [InlineData(byte.MaxValue)]
        [InlineData(byte.MaxValue + 3)] // Other values, like byte.MaxValue + 1 overflows to 0 (i.e. JsonCommentHandling.Disallow), which is valid.
        [InlineData(byte.MaxValue + 4)]
        public static void ReadCommentHandlingDoesNotSupportAllow(int enumValue)
        {
            var options = new JsonSerializerOptions();

            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.ReadCommentHandling = (JsonCommentHandling)enumValue);
        }

        [Theory]
        [InlineData(-1)]
        public static void TestDepthInvalid(int depth)
        {
            var options = new JsonSerializerOptions();
            Assert.Throws<ArgumentOutOfRangeException>("value", () => options.MaxDepth = depth);
        }

        [Fact]
        public static void MaxDepthRead()
        {
            JsonSerializer.Deserialize<BasicCompany>(BasicCompany.s_data);

            var options = new JsonSerializerOptions();

            JsonSerializer.Deserialize<BasicCompany>(BasicCompany.s_data, options);

            options = new JsonSerializerOptions();
            options.MaxDepth = 1;

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BasicCompany>(BasicCompany.s_data, options));
        }

        private class TestClassForEncoding
        {
            public string MyString { get; set; }
        }

        // This is a copy of the test data in System.Text.Json.Tests.JsonEncodedTextTests.JsonEncodedTextStringsCustom
        public static IEnumerable<object[]> JsonEncodedTextStringsCustom
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { "age", "\\u0061\\u0067\\u0065" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\"\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\u0022\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\\u0022\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\\\\\u0075\\u0030\\u0030\\u0032\\u0032\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9>>>>>\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\u003E\\u003E\\u003E\\u003E\\u003E\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\\u003e\\u003e\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\\\\\u0075\\u0030\\u0030\\u0033\\u0065\\\\\\u0075\\u0030\\u0030\\u0033\\u0065\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\\u003E\\u003E\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\\\\\\u0075\\u0030\\u0030\\u0033\\u0045\\\\\\u0075\\u0030\\u0030\\u0033\\u0045\u00EA\u00EA\u00EA\u00EA\u00EA" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(JsonEncodedTextStringsCustom))]
        public static void CustomEncoderAllowLatin1Supplement(string message, string expectedMessage)
        {
            // Latin-1 Supplement block starts from U+0080 and ends at U+00FF
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.Latin1Supplement);

            var options = new JsonSerializerOptions();
            options.Encoder = encoder;

            var obj = new TestClassForEncoding();
            obj.MyString = message;

            string baselineJson = JsonSerializer.Serialize(obj);
            Assert.DoesNotContain(expectedMessage, baselineJson);

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Contains(expectedMessage, json);

            obj = JsonSerializer.Deserialize<TestClassForEncoding>(json);
            Assert.Equal(obj.MyString, message);
        }

        public static IEnumerable<object[]> JsonEncodedTextStringsCustomAll
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { "\u00E9\u00E9\u00E9\u00E9\u00E9\u00EA\u00EA\u00EA\u00EA\u00EA", "\u00E9\u00E9\u00E9\u00E9\u00E9\u00EA\u00EA\u00EA\u00EA\u00EA" },
                    new object[] { "a\u0467\u0466a", "a\u0467\u0466a" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(JsonEncodedTextStringsCustomAll))]
        public static void JsonEncodedTextStringsCustomAllowAll(string message, string expectedMessage)
        {
            // Allow all unicode values (except forbidden characters which we don't have in test data here)
            JavaScriptEncoder encoder = JavaScriptEncoder.Create(UnicodeRanges.All);

            var options = new JsonSerializerOptions();
            options.Encoder = encoder;

            var obj = new TestClassForEncoding();
            obj.MyString = message;

            string baselineJson = JsonSerializer.Serialize(obj);
            Assert.DoesNotContain(expectedMessage, baselineJson);

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Contains(expectedMessage, json);

            obj = JsonSerializer.Deserialize<TestClassForEncoding>(json);
            Assert.Equal(obj.MyString, message);
        }

        [Fact]
        public static void Options_GetConverterForObjectJsonElement_GivesCorrectConverter()
        {
            GenericObjectOrJsonElementConverterTestHelper<object>("ObjectConverter", new object(), "{}");
            JsonElement element = JsonDocument.Parse("[3]").RootElement;
            GenericObjectOrJsonElementConverterTestHelper<JsonElement>("JsonElementConverter", element, "[3]");
        }

        [Fact]
        public static void Options_JsonSerializerContext_DoesNotFallbackToReflection()
        {
            var options = JsonContext.Default.Options;
            JsonSerializer.Serialize(new WeatherForecastWithPOCOs(), options); // type supported by context should succeed serialization

            var unsupportedValue = new MyClass();
            Assert.Null(JsonContext.Default.GetTypeInfo(unsupportedValue.GetType()));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(unsupportedValue, unsupportedValue.GetType(), JsonContext.Default));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(unsupportedValue, options));
        }

        [Fact]
        public static void JsonSerializer_IsReflectionEnabledByDefault_DefaultsToTrue()
        {
            Assert.True(JsonSerializer.IsReflectionEnabledByDefault);
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void Options_DisablingIsReflectionEnabledByDefaultSwitch_DefaultOptionsDoesNotSupportReflection()
        {
            var options = new RemoteInvokeOptions
            {
                RuntimeConfigurationOptions =
                {
                    ["System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault"] = false
                }
            };

            RemoteExecutor.Invoke(static () =>
            {
                Assert.False(JsonSerializer.IsReflectionEnabledByDefault);

                var options = JsonSerializerOptions.Default;
                Assert.True(options.IsReadOnly);

                Assert.NotNull(options.TypeInfoResolver);
                Assert.True(options.TypeInfoResolver is not DefaultJsonTypeInfoResolver);
                IList<IJsonTypeInfoResolver> resolverList = Assert.IsAssignableFrom<IList<IJsonTypeInfoResolver>>(options.TypeInfoResolver);

                Assert.Empty(resolverList);
                Assert.Empty(options.TypeInfoResolverChain);

                Assert.Throws<NotSupportedException>(() => options.GetTypeInfo(typeof(string)));
                Assert.Throws<NotSupportedException>(() => options.GetConverter(typeof(string)));
                Assert.False(options.TryGetTypeInfo(typeof(string), out JsonTypeInfo? typeInfo));
                Assert.Null(typeInfo);

                Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize("string"));
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize("string", options));

                Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<string>("\"string\""));
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<string>("\"string\"", options));

            }, options).Dispose();
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void Options_DisablingIsReflectionEnabledByDefaultSwitch_NewOptionsDoesNotSupportReflection()
        {
            var options = new RemoteInvokeOptions
            {
                RuntimeConfigurationOptions =
                {
                    ["System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault"] = false
                }
            };

            RemoteExecutor.Invoke(static () =>
            {
                Assert.False(JsonSerializer.IsReflectionEnabledByDefault);

                var options = new JsonSerializerOptions();
                Assert.False(options.IsReadOnly);

                Assert.Null(options.TypeInfoResolver);
                Assert.Empty(options.TypeInfoResolverChain);

                Assert.Throws<NotSupportedException>(() => options.GetTypeInfo(typeof(string)));
                Assert.Throws<NotSupportedException>(() => options.GetConverter(typeof(string)));

                Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize("string", options));
                Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<string>("\"string\"", options));
                Assert.False(options.TryGetTypeInfo(typeof(string), out JsonTypeInfo? typeInfo));
                Assert.Null(typeInfo);

                Assert.False(options.IsReadOnly); // failed operations should not lock the instance

                // Can still use reflection via explicit configuration
                options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
                Assert.Equal(new[] { options.TypeInfoResolver }, options.TypeInfoResolverChain);

                Assert.NotNull(options.GetTypeInfo(typeof(string)));
                Assert.NotNull(options.GetConverter(typeof(string)));

                string json = JsonSerializer.Serialize("string", options);
                string value = JsonSerializer.Deserialize<string>(json, options);
                Assert.Equal("string", value);

            }, options).Dispose();
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void Options_DisablingIsReflectionEnabledByDefaultSwitch_CanUseSourceGen()
        {
            var options = new RemoteInvokeOptions
            {
                RuntimeConfigurationOptions =
                {
                    ["System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault"] = false
                }
            };

            RemoteExecutor.Invoke(static () =>
            {
                Assert.False(JsonSerializer.IsReflectionEnabledByDefault);

                var options = new JsonSerializerOptions();
                options.TypeInfoResolverChain.Add(JsonContext.Default);

                string json = JsonSerializer.Serialize(new WeatherForecastWithPOCOs(), options);
                WeatherForecastWithPOCOs result = JsonSerializer.Deserialize<WeatherForecastWithPOCOs>(json, options);
                Assert.NotNull(result);

            }, options).Dispose();
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public static void Options_JsonSerializerContext_GetConverter_DoesNotFallBackToReflectionConverter(bool isCompatibilitySwitchExplicitlyDisabled)
        {
            var options = new RemoteInvokeOptions();

            if (isCompatibilitySwitchExplicitlyDisabled)
            {
                options.RuntimeConfigurationOptions.Add("System.Text.Json.Serialization.EnableSourceGenReflectionFallback", false);
            }

            RemoteExecutor.Invoke(static () =>
            {
                JsonContext context = JsonContext.Default;
                var unsupportedValue = new MyClass();

                // Default converters have not been rooted yet
                Assert.Null(context.GetTypeInfo(typeof(MyClass)));
                Assert.Throws<NotSupportedException>(() => context.Options.GetConverter(typeof(MyClass)));
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(unsupportedValue, context.Options));

                // Root converters process-wide using a default options instance
                var options = new JsonSerializerOptions();
                JsonConverter converter = options.GetConverter(typeof(MyClass));
                Assert.IsAssignableFrom<JsonConverter<MyClass>>(converter);

                // We still can't resolve metadata for MyClass or get a converter using the rooted converters.
                Assert.Null(context.GetTypeInfo(typeof(MyClass)));
                Assert.Throws<NotSupportedException>(() => context.Options.GetConverter(typeof(MyClass)));
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(unsupportedValue, context.Options));

            }, options).Dispose();
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void Options_JsonSerializerContext_Net6CompatibilitySwitch_FallsBackToReflectionResolver()
        {
            var options = new RemoteInvokeOptions
            {
                RuntimeConfigurationOptions =
                {
                    ["System.Text.Json.Serialization.EnableSourceGenReflectionFallback"] = true
                }
            };
            
            RemoteExecutor.Invoke(static () =>
            {
                var unsupportedValue = new MyClass { Value = "value" };

                // JsonSerializerContext does not return metadata for the type
                Assert.Null(JsonContext.Default.GetTypeInfo(typeof(MyClass)));

                // Serialization fails using the JsonSerializerContext overload
                Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(unsupportedValue, unsupportedValue.GetType(), JsonContext.Default));

                // Serialization uses reflection fallback using the JsonSerializerOptions overload
                string json = JsonSerializer.Serialize(unsupportedValue, JsonContext.Default.Options);
                JsonTestHelper.AssertJsonEqual("""{"Value":"value", "Thing":null}""", json);

                // A converter can be resolved when looking up JsonSerializerOptions
                JsonConverter converter = JsonContext.Default.Options.GetConverter(typeof(MyClass));
                Assert.IsAssignableFrom<JsonConverter<MyClass>>(converter);

                // Serialization using JsonSerializerContext now uses the reflection fallback.
                json = JsonSerializer.Serialize(unsupportedValue, unsupportedValue.GetType(), JsonContext.Default);
                JsonTestHelper.AssertJsonEqual("""{"Value":"value", "Thing":null}""", json);

            }, options).Dispose();
        }

        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void Options_JsonSerializerContext_Net6CompatibilitySwitch_IsOverriddenByDisablingIsReflectionEnabledByDefault()
        {
            var options = new RemoteInvokeOptions
            {
                RuntimeConfigurationOptions =
                {
                    ["System.Text.Json.JsonSerializer.IsReflectionEnabledByDefault"] = false,
                    ["System.Text.Json.Serialization.EnableSourceGenReflectionFallback"] = true
                }
            };

            RemoteExecutor.Invoke(static () =>
            {
                Assert.False(JsonSerializer.IsReflectionEnabledByDefault);

                JsonContext context = JsonContext.Default;
                var unsupportedValue = new MyClass();

                Assert.Null(context.GetTypeInfo(typeof(MyClass)));
                Assert.Throws<NotSupportedException>(() => context.Options.GetConverter(typeof(MyClass)));
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(unsupportedValue, context.Options));

            }, options).Dispose();
        }

        [Fact]
        public static void Options_JsonSerializerContext_Combine_FallbackToReflection()
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = JsonTypeInfoResolver.Combine(JsonContext.Default, new DefaultJsonTypeInfoResolver())
            };

            var value = new MyClass();
            string json = JsonSerializer.Serialize(value, options);
            JsonTestHelper.AssertJsonEqual("""{"Value":null,"Thing":null}""", json);
        }

        private static void GenericObjectOrJsonElementConverterTestHelper<T>(string converterName, object objectValue, string stringValue)
        {
            var options = new JsonSerializerOptions();

            JsonConverter<T> converter = (JsonConverter<T>)options.GetConverter(typeof(T));
            Assert.Equal(converterName, converter.GetType().Name);

            ReadOnlySpan<byte> data = Encoding.UTF8.GetBytes(stringValue);
            Utf8JsonReader reader = new Utf8JsonReader(data);
            reader.Read();
            T readValue = converter.Read(ref reader, typeof(T), options);

            if (readValue is JsonElement element)
            {
                JsonTestHelper.AssertJsonEqual(stringValue, element.ToString());
            }
            else
            {
                Assert.True(false, "Must be JsonElement");
            }

            using (var stream = new MemoryStream())
            using (var writer = new Utf8JsonWriter(stream))
            {
                converter.Write(writer, (T)objectValue, options);
                writer.Flush();
                Assert.Equal(stringValue, Encoding.UTF8.GetString(stream.ToArray()));

                writer.Reset(stream);
                converter.Write(writer, (T)objectValue, null); // Test with null option
                writer.Flush();
                Assert.Equal(stringValue + stringValue, Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        [Fact]
        public static void Options_GetConverter_GivesCorrectDefaultConverterAndReadWriteSuccess()
        {
            var options = new JsonSerializerOptions();
            GenericConverterTestHelper<bool>("BooleanConverter", true, "true", options);
            GenericConverterTestHelper<byte>("ByteConverter", (byte)128, "128", options);
            GenericConverterTestHelper<char>("CharConverter", 'A', "\"A\"", options);
            GenericConverterTestHelper<double>("DoubleConverter", 15.1d, "15.1", options);
            GenericConverterTestHelper<SampleEnum>("EnumConverter`1", SampleEnum.Two, "2", options);
            GenericConverterTestHelper<short>("Int16Converter", (short)5, "5", options);
            GenericConverterTestHelper<int>("Int32Converter", -100, "-100", options);
            GenericConverterTestHelper<long>("Int64Converter", (long)11111, "11111", options);
            GenericConverterTestHelper<sbyte>("SByteConverter", (sbyte)-121, "-121", options);
            GenericConverterTestHelper<float>("SingleConverter", 14.5f, "14.5", options);
            GenericConverterTestHelper<string>("StringConverter", "Hello", "\"Hello\"", options);
            GenericConverterTestHelper<ushort>("UInt16Converter", (ushort)1206, "1206", options);
            GenericConverterTestHelper<uint>("UInt32Converter", (uint)3333, "3333", options);
            GenericConverterTestHelper<ulong>("UInt64Converter", (ulong)44444, "44444", options);
            GenericConverterTestHelper<decimal>("DecimalConverter", 3.3m, "3.3", options);
            GenericConverterTestHelper<byte[]>("ByteArrayConverter", new byte[] { 1, 2, 3, 4 }, "\"AQIDBA==\"", options);
            GenericConverterTestHelper<DateTime>("DateTimeConverter", new DateTime(2018, 12, 3), "\"2018-12-03T00:00:00\"", options);
            GenericConverterTestHelper<DateTimeOffset>("DateTimeOffsetConverter", new DateTimeOffset(new DateTime(2018, 12, 3, 00, 00, 00, DateTimeKind.Utc)), "\"2018-12-03T00:00:00+00:00\"", options);
            Guid testGuid = new Guid();
            GenericConverterTestHelper<Guid>("GuidConverter", testGuid, $"\"{testGuid}\"", options);
            GenericConverterTestHelper<Uri>("UriConverter", new Uri("http://test.com"), "\"http://test.com\"", options);
        }

        [Fact]
        // KeyValuePair converter is not a primitive JsonConverter<T>, so there's no way to properly flow the ReadStack state in the direct call to the serializer.
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50205")]
        public static void Options_GetConverter_GivesCorrectKeyValuePairConverter()
        {
            GenericConverterTestHelper<KeyValuePair<string, string>>(
                converterName: "KeyValuePairConverter`2",
                objectValue: new KeyValuePair<string, string>("key", "value"),
                stringValue: @"{""Key"":""key"",""Value"":""value""}",
                options: new JsonSerializerOptions(),
                nullOptionOkay: false);
        }

        [Fact]
        public static void Options_GetConverter_GivesCorrectCustomConverterAndReadWriteSuccess()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new CustomConverterTests.LongArrayConverter());
            GenericConverterTestHelper<long[]>("LongArrayConverter", new long[] { 1, 2, 3, 4 }, "\"1,2,3,4\"", options);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(Poco))]
        [InlineData(typeof(Dictionary<int, string>))]
        public static void Options_GetConverter_CustomResolver_DoesNotReturnConverterForUnsupportedType(Type type)
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = new NullResolver() };
            Assert.Throws<NotSupportedException>(() => options.GetConverter(type));
        }

        public class NullResolver : IJsonTypeInfoResolver
        {
            public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) => null;
        }

        private static void GenericConverterTestHelper<T>(string converterName, object objectValue, string stringValue, JsonSerializerOptions options, bool nullOptionOkay = true)
        {
            JsonConverter<T> converter = (JsonConverter<T>)options.GetConverter(typeof(T));

            Assert.True(converter.CanConvert(typeof(T)));
            Assert.Equal(converterName, converter.GetType().Name);

            ReadOnlySpan<byte> data = Encoding.UTF8.GetBytes(stringValue);
            Utf8JsonReader reader = new Utf8JsonReader(data);
            reader.Read();

            T valueRead = converter.Read(ref reader, typeof(T), nullOptionOkay ? null: options);
            Assert.Equal(objectValue, valueRead);

            if (reader.TokenType != JsonTokenType.EndObject)
            {
                valueRead = converter.Read(ref reader, typeof(T), options);  // Test with given option if reader position haven't advanced.
                Assert.Equal(objectValue, valueRead);
            }

            using (var stream = new MemoryStream())
            using (var writer = new Utf8JsonWriter(stream))
            {
                converter.Write(writer, (T)objectValue, options);
                writer.Flush();
                Assert.Equal(stringValue, Encoding.UTF8.GetString(stream.ToArray()));

                writer.Reset(stream);
                converter.Write(writer, (T)objectValue, nullOptionOkay ? null : options);
                writer.Flush();
                Assert.Equal(stringValue + stringValue, Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        [Fact]
        public static void CopyConstructor_OriginalLocked()
        {
            JsonSerializerOptions options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            Assert.False(options.IsReadOnly);

            // Perform serialization with options, after which it will be locked.
            JsonSerializer.Serialize("1", options);

            Assert.True(options.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => options.ReferenceHandler = ReferenceHandler.Preserve);

            var newOptions = new JsonSerializerOptions(options);
            Assert.False(newOptions.IsReadOnly);
            VerifyOptionsEqual(options, newOptions);

            // No exception is thrown on mutating the new options instance because it is "unlocked".
            newOptions.ReferenceHandler = ReferenceHandler.Preserve;
        }

        [Fact]
        public static void CopyConstructor_MaxDepth()
        {
            static void RunTest(int maxDepth, int effectiveMaxDepth)
            {
                var options = new JsonSerializerOptions { MaxDepth = maxDepth };
                var newOptions = new JsonSerializerOptions(options);

                Assert.Equal(maxDepth, options.MaxDepth);
                Assert.Equal(maxDepth, newOptions.MaxDepth);

                // Test for default effective max depth in exception message.
                var myList = new List<object>();
                myList.Add(myList);

                string effectiveMaxDepthAsStr = effectiveMaxDepth.ToString();

                JsonException ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(myList, options));
                Assert.Contains(effectiveMaxDepthAsStr, ex.ToString());

                ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(myList, newOptions));
                Assert.Contains(effectiveMaxDepthAsStr, ex.ToString());
            }

            // Zero max depth
            RunTest(0, 64);

            // Specified max depth
            RunTest(25, 25);
        }

        [Fact]
        public static void CopyConstructor_CopiesAllPublicProperties()
        {
            JsonSerializerOptions options = GetFullyPopulatedOptionsInstance();
            var newOptions = new JsonSerializerOptions(options);
            VerifyOptionsEqual(options, newOptions);
        }

        [Fact]
        public static void CopyConstructor_CopiesJsonSerializerContext()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.AddContext<JsonContext>();
            JsonContext original = Assert.IsType<JsonContext>(options.TypeInfoResolver);

            // copy constructor copies the JsonSerializerContext
            var newOptions = new JsonSerializerOptions(options);
            Assert.Same(original, newOptions.TypeInfoResolver);

            // resolving metadata returns metadata tied to the new options
            JsonTypeInfo typeInfo = newOptions.TypeInfoResolver.GetTypeInfo(typeof(int), newOptions);
            Assert.Same(typeInfo.Options, newOptions);

            // it is possible to reset the resolver
            newOptions.TypeInfoResolver = null;
            Assert.Null(newOptions.TypeInfoResolver);
        }

        [Fact]
        public static void CopyConstructor_NullInput()
        {
            ArgumentNullException ex = Assert.Throws<ArgumentNullException>(() => new JsonSerializerOptions(null));
            Assert.Contains("options", ex.ToString());
        }

        [Fact]
        public static void JsonSerializerOptions_Default_MatchesDefaultConstructorWithDefaultResolver()
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = JsonSerializerOptions.Default.TypeInfoResolver };
            JsonSerializerOptions optionsSingleton = JsonSerializerOptions.Default;
            VerifyOptionsEqual(options, optionsSingleton);
        }

        [Fact]
        public static void JsonSerializerOptions_Default_ReturnsSameInstance()
        {
            Assert.Same(JsonSerializerOptions.Default, JsonSerializerOptions.Default);
        }

        [Fact]
        public static void JsonSerializerOptions_Default_IsReadOnly()
        {
            var optionsSingleton = JsonSerializerOptions.Default;
            Assert.True(optionsSingleton.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => optionsSingleton.IncludeFields = true);
            Assert.Throws<InvalidOperationException>(() => optionsSingleton.Converters.Add(new JsonStringEnumConverter()));
            Assert.Throws<InvalidOperationException>(() => optionsSingleton.TypeInfoResolverChain.Add(new DefaultJsonTypeInfoResolver()));
            Assert.Throws<InvalidOperationException>(() => optionsSingleton.AddContext<JsonContext>());
            Assert.Throws<InvalidOperationException>(() => new JsonContext(optionsSingleton));

            DefaultJsonTypeInfoResolver resolver = Assert.IsType<DefaultJsonTypeInfoResolver>(optionsSingleton.TypeInfoResolver);
            Assert.Throws<InvalidOperationException>(() => resolver.Modifiers.Clear());
            Assert.Throws<InvalidOperationException>(() => resolver.Modifiers.Add(ti => { }));
            Assert.Throws<InvalidOperationException>(() => resolver.Modifiers.Insert(0, ti => { }));

            optionsSingleton.MakeReadOnly(); // MakeReadOnly is idempotent.
        }

        [Theory]
        [MemberData(nameof(GetInitialTypeInfoResolversAndExpectedChains))]
        public static void TypeInfoResolverChain_SetTypeInfoResolver_ReturnsExpectedChain(
            IJsonTypeInfoResolver? initialResolver,
            IJsonTypeInfoResolver[] expectedChainValues)
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = initialResolver };

            Assert.Equal(expectedChainValues, options.TypeInfoResolverChain);

            // We materialized the resolver chain without mutating it.
            // This should not have impact on the reported resolver
            Assert.Same(initialResolver, options.TypeInfoResolver);
        }

        [Theory]
        [MemberData(nameof(GetInitialTypeInfoResolversAndExpectedChains))]
        public static void TypeInfoResolverChain_Add_UpdatesTypeInfoResolver(
            IJsonTypeInfoResolver? initialResolver,
            IJsonTypeInfoResolver[] expectedChainValues)
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = initialResolver };
            Assert.Equal(expectedChainValues, options.TypeInfoResolverChain);
            Assert.Same(initialResolver, options.TypeInfoResolver);

            var appendedResolver = new DefaultJsonTypeInfoResolver();
            options.TypeInfoResolverChain.Add(appendedResolver);

            Assert.Equal(expectedChainValues.Concat(new[] { appendedResolver }), options.TypeInfoResolverChain);
            Assert.NotSame(initialResolver, options.TypeInfoResolver);
            Assert.Same(options.TypeInfoResolverChain, options.TypeInfoResolver);
        }

        [Theory]
        [MemberData(nameof(GetInitialTypeInfoResolversAndExpectedChains))]
        public static void TypeInfoResolverChain_Clear_UpdatesTypeInfoResolver(
            IJsonTypeInfoResolver? initialResolver,
            IJsonTypeInfoResolver[] expectedChainValues)
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = initialResolver };
            Assert.Equal(expectedChainValues, options.TypeInfoResolverChain);
            Assert.Same(initialResolver, options.TypeInfoResolver);

            options.TypeInfoResolverChain.Clear();

            Assert.Empty(options.TypeInfoResolverChain);
            Assert.Same(options.TypeInfoResolverChain, options.TypeInfoResolver);
        }

        public static IEnumerable<object[]> GetInitialTypeInfoResolversAndExpectedChains()
        {
            yield return WrapArgs(null, Array.Empty<IJsonTypeInfoResolver>());

            var defaultResolver = new DefaultJsonTypeInfoResolver();
            yield return WrapArgs(defaultResolver, new[] { defaultResolver });
            yield return WrapArgs(JsonContext.Default, new[] { JsonContext.Default });
            yield return WrapArgs(JsonTypeInfoResolver.Combine(), Array.Empty<IJsonTypeInfoResolver>());
            yield return WrapArgs(
                JsonTypeInfoResolver.Combine(JsonContext.Default, defaultResolver, null, JsonContext.Default),
                new IJsonTypeInfoResolver[] { JsonContext.Default, defaultResolver, JsonContext.Default });

            var optionsWithChain = new JsonSerializerOptions { TypeInfoResolverChain = { JsonContext.Default, defaultResolver, JsonContext.Default } };
            yield return WrapArgs(optionsWithChain.TypeInfoResolver, new IJsonTypeInfoResolver[] { JsonContext.Default, defaultResolver, JsonContext.Default });

            static object[] WrapArgs(IJsonTypeInfoResolver? jsonTypeInfoResolver, IJsonTypeInfoResolver[] expectedChainValues)
                => new object[] { jsonTypeInfoResolver, expectedChainValues };
        }

        [Fact]
        public static void TypeInfoResolverChain_SetToOtherOptions_UsesCopy()
        {
            var defaultResolver = new DefaultJsonTypeInfoResolver();
            var originalOptions = new JsonSerializerOptions
            {
                TypeInfoResolverChain = { JsonContext.Default, JsonContext.Default, defaultResolver }
            };

            // The TypeInfoResolver of the original options should be the chain itself
            Assert.Same(originalOptions.TypeInfoResolver, originalOptions.TypeInfoResolverChain);
            Assert.Equal(new IJsonTypeInfoResolver[] { JsonContext.Default, JsonContext.Default, defaultResolver }, originalOptions.TypeInfoResolverChain);

            var optionsCopy = new JsonSerializerOptions
            {
                TypeInfoResolver = originalOptions.TypeInfoResolver
            };

            // The new options should preserve the original TypeInfoResolver but use a copy for the chain that it owns
            Assert.Same(originalOptions.TypeInfoResolver, optionsCopy.TypeInfoResolver);
            Assert.Equal(new IJsonTypeInfoResolver[] { JsonContext.Default, JsonContext.Default, defaultResolver }, optionsCopy.TypeInfoResolverChain);
            Assert.NotSame(originalOptions.TypeInfoResolverChain, optionsCopy.TypeInfoResolverChain);

            // Mutating the resolver chain should update the TypeInfoResolver property but not impact the original options
            var appendedResolver = new DefaultJsonTypeInfoResolver();
            optionsCopy.TypeInfoResolverChain.Add(appendedResolver);

            Assert.NotSame(originalOptions.TypeInfoResolver, optionsCopy.TypeInfoResolver);
            Assert.Equal(new IJsonTypeInfoResolver[] { JsonContext.Default, JsonContext.Default, defaultResolver }, originalOptions.TypeInfoResolverChain);
            Assert.Equal(new IJsonTypeInfoResolver[] { JsonContext.Default, JsonContext.Default, defaultResolver, appendedResolver }, optionsCopy.TypeInfoResolverChain);
        }

        [Fact]
        public static void TypeInfoResolverChain_UpdateTypeInfoResolver_ResetsResolverChain()
        {
            var defaultResolver = new DefaultJsonTypeInfoResolver();
            var options = new JsonSerializerOptions { TypeInfoResolverChain = { JsonContext.Default, JsonContext.Default, defaultResolver } };
            Assert.Equal(new IJsonTypeInfoResolver[] { JsonContext.Default, JsonContext.Default, defaultResolver }, options.TypeInfoResolverChain);

            options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();

            Assert.Equal(new IJsonTypeInfoResolver[] { options.TypeInfoResolver }, options.TypeInfoResolverChain);
        }

        [Fact]
        public static void TypeInfoResolverChain_AppendTypeInfoResolverToChain_ThrowsInvalidOperationException()
        {
            var defaultResolver = new DefaultJsonTypeInfoResolver();
            var options = new JsonSerializerOptions { TypeInfoResolver = defaultResolver };

            Assert.Throws<InvalidOperationException>(() => options.TypeInfoResolverChain.Add(options.TypeInfoResolver));
        }

        [Fact]
        public static void TypeInfoResolverChain_AppendTypeInfoResolverChainToChain_ThrowsInvalidOperationException()
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolverChain = { JsonContext.Default, JsonContext.Default }
            };

            var castChain = (IJsonTypeInfoResolver)options.TypeInfoResolverChain;
            Assert.Throws<InvalidOperationException>(() => options.TypeInfoResolverChain.Add(castChain));
        }

        [Fact]
        public static void TypeInfoResolverChain_IsSameAsResolver_SettingChainToResolver_PreservesConfiguration()
        {
            var options = new JsonSerializerOptions
            {
                TypeInfoResolverChain = { JsonContext.Default, JsonContext.Default }
            };

            Assert.Same(options.TypeInfoResolverChain, options.TypeInfoResolver);

            options.TypeInfoResolver = options.TypeInfoResolver;

            Assert.Equal(new[] { JsonContext.Default, JsonContext.Default }, options.TypeInfoResolverChain);
            Assert.Same(options.TypeInfoResolverChain, options.TypeInfoResolver);
        }

        [Fact]
        public static void TypeInfoResolverChain_ResolvesInExpectedOrder()
        {
            int resolver1Invocations = 0;
            int resolver2Invocations = 0;
            int resolver3Invocations = 0;
            int resolver4Invocations = 0;

            var resolver1 = new TestResolver((type, options) =>
            {
                resolver1Invocations++;
                return null;
            });

            var resolver2 = new TestResolver((type, options) =>
            {
                resolver2Invocations++;
                return (type == typeof(int)) ? JsonTypeInfo.CreateJsonTypeInfo(type, options) : null;
            });

            var resolver3 = new TestResolver((type, options) =>
            {
                resolver3Invocations++;
                return (type == typeof(int) || type == typeof(string)) ? JsonTypeInfo.CreateJsonTypeInfo(type, options) : null;
            });

            var resolver4 = new TestResolver((type, options) =>
            {
                resolver4Invocations++;
                return (type == typeof(int) || type == typeof(string) || type == typeof(double)) ? JsonTypeInfo.CreateJsonTypeInfo(type, options) : null;
            });

            var options = new JsonSerializerOptions
            {
                TypeInfoResolverChain = { resolver1, resolver2, resolver3, resolver4 }
            };

            options.GetTypeInfo(typeof(int));
            Assert.Equal(1, resolver1Invocations);
            Assert.Equal(1, resolver2Invocations);
            Assert.Equal(0, resolver3Invocations);
            Assert.Equal(0, resolver4Invocations);

            options.GetTypeInfo(typeof(string));
            Assert.Equal(2, resolver1Invocations);
            Assert.Equal(2, resolver2Invocations);
            Assert.Equal(1, resolver3Invocations);
            Assert.Equal(0, resolver4Invocations);

            options.GetTypeInfo(typeof(double));
            Assert.Equal(3, resolver1Invocations);
            Assert.Equal(3, resolver2Invocations);
            Assert.Equal(2, resolver3Invocations);
            Assert.Equal(1, resolver4Invocations);
        }

        [Fact]
        public static void DefaultSerializerOptions_General()
        {
            var options = new JsonSerializerOptions();
            var newOptions = new JsonSerializerOptions(JsonSerializerDefaults.General);
            Assert.False(newOptions.IsReadOnly);
            VerifyOptionsEqual(options, newOptions);
        }

        [Fact]
        public static void PredefinedSerializerOptions_Web()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            Assert.True(options.PropertyNameCaseInsensitive);
            Assert.Same(JsonNamingPolicy.CamelCase, options.PropertyNamingPolicy);
            Assert.Equal(JsonNumberHandling.AllowReadingFromString, options.NumberHandling);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(2)]
        public static void PredefinedSerializerOptions_UnhandledDefaults(int enumValue)
        {
            var outOfRangeSerializerDefaults = (JsonSerializerDefaults)enumValue;
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonSerializerOptions(outOfRangeSerializerDefaults));
        }

        private static JsonSerializerOptions GetFullyPopulatedOptionsInstance()
        {
            var options = new JsonSerializerOptions();

            foreach (PropertyInfo property in typeof(JsonSerializerOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Type propertyType = property.PropertyType;

                if (propertyType == typeof(bool))
                {
                    if (property.Name is not nameof(JsonSerializerOptions.IgnoreNullValues) // IgnoreNullValues and DefaultIgnoreCondition cannot be active at the same time.
                                        and not nameof(JsonSerializerOptions.IsReadOnly)) // Property is not structural and cannot be set.
                    {
                        property.SetValue(options, true);
                    }
                }
                if (propertyType == typeof(int))
                {
                    property.SetValue(options, 32);
                }
                else if (propertyType == typeof(IList<JsonConverter>))
                {
                    options.Converters.Add(new JsonStringEnumConverter());
                    options.Converters.Add(new ConverterForInt32());
                }
                else if (propertyType == typeof(JavaScriptEncoder))
                {
                    options.Encoder = JavaScriptEncoder.Default;
                }
                else if (propertyType == typeof(JsonNamingPolicy))
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.DictionaryKeyPolicy = new SimpleSnakeCasePolicy();
                }
                else if (propertyType == typeof(ReferenceHandler))
                {
                    options.ReferenceHandler = ReferenceHandler.Preserve;
                }
                else if (propertyType == typeof(IJsonTypeInfoResolver))
                {
                    options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
                }
                else if (propertyType == typeof(IList<IJsonTypeInfoResolver>))
                {
                    // Do nothing -- setting the TypeInfoResolver property should ensure this is populated.
                }
                else if (propertyType.IsValueType)
                {
                    options.ReadCommentHandling = JsonCommentHandling.Disallow;
                    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                    options.NumberHandling = JsonNumberHandling.AllowReadingFromString;
                    options.UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode;
                    options.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow;
                }
                else
                {
                    // An exception thrown from here means this test should be updated
                    // to reflect any newly added properties on JsonSerializerOptions.
                    property.SetValue(options, Activator.CreateInstance(propertyType));
                }
            }

            return options;
        }

        private static void VerifyOptionsEqual(JsonSerializerOptions options, JsonSerializerOptions newOptions)
        {
            foreach (PropertyInfo property in typeof(JsonSerializerOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                Type propertyType = property.PropertyType;

                if (property.Name == nameof(JsonSerializerOptions.IsReadOnly))
                {
                    continue; // readonly-ness is not a structural property of JsonSerializerOptions.
                }
                else if (propertyType == typeof(IList<JsonConverter>))
                {
                    var list1 = (IList<JsonConverter>)property.GetValue(options);
                    var list2 = (IList<JsonConverter>)property.GetValue(newOptions);
                    Assert.Equal(list1, list2);
                }
                else if (propertyType == typeof(IList<IJsonTypeInfoResolver>))
                {
                    var list1 = (IList<IJsonTypeInfoResolver>)property.GetValue(options);
                    var list2 = (IList<IJsonTypeInfoResolver>)property.GetValue(newOptions);
                    Assert.Equal(list1, list2);
                }
                else if (propertyType.IsValueType)
                {
                    Assert.Equal(property.GetValue(options), property.GetValue(newOptions));
                }
                else
                {
                    Assert.Same(property.GetValue(options), property.GetValue(newOptions));
                }
            }
        }

        [Fact]
        public static void CopyConstructor_IgnoreNullValuesCopied()
        {
            var options = new JsonSerializerOptions { IgnoreNullValues = true };
            var newOptions = new JsonSerializerOptions(options);
            VerifyOptionsEqual(options, newOptions);
        }

        [Fact]
        public static void CannotSetBoth_IgnoreNullValues_And_DefaultIgnoreCondition()
        {
            // Set IgnoreNullValues first.
            JsonSerializerOptions options = new JsonSerializerOptions { IgnoreNullValues = true };

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault);
            string exAsStr = ex.ToString();
            Assert.Contains("IgnoreNullValues", exAsStr);
            Assert.Contains("DefaultIgnoreCondition", exAsStr);

            options.IgnoreNullValues = false;
            // We can set the property now.
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;

            // Set DefaultIgnoreCondition first.

            options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
            Assert.Throws<InvalidOperationException>(
                () => options.IgnoreNullValues = true);

            options.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
            // We can set the property now.
            options.IgnoreNullValues = true;
        }

        [Fact]
        public static void CannotSet_DefaultIgnoreCondition_To_Always()
        {
            Assert.Throws<ArgumentException>(() => new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.Always });
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36605")]
        public static void ConverterRead_VerifyInvalidTypeToConvertFails()
        {
            var options = new JsonSerializerOptions();
            Type typeToConvert = typeof(KeyValuePair<int, int>);
            byte[] bytes = Encoding.UTF8.GetBytes(@"{""Key"":1,""Value"":2}");

            JsonConverter<KeyValuePair<int, int>> converter =
                (JsonConverter<KeyValuePair<int, int>>)options.GetConverter(typeToConvert);

            // Baseline
            var reader = new Utf8JsonReader(bytes);
            reader.Read();
            KeyValuePair<int, int> kvp = converter.Read(ref reader, typeToConvert, options);
            Assert.Equal(1, kvp.Key);
            Assert.Equal(2, kvp.Value);

            // Test
            reader = new Utf8JsonReader(bytes);
            reader.Read();
            try
            {
                converter.Read(ref reader, typeof(Dictionary<string, int>), options);
            }
            catch (Exception ex)
            {
                if (!(ex is InvalidOperationException))
                {
                    throw ex;
                }
            }
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(object))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(Dictionary<int, string>))]
        public static void GetTypeInfo_MutableOptionsInstance(Type type)
        {
            var options = new JsonSerializerOptions();

            // An unset resolver results in NotSupportedException.
            Assert.Throws<NotSupportedException>(() => options.GetTypeInfo(type));
            // And returns false in the Try-method
            Assert.False(options.TryGetTypeInfo(type, out JsonTypeInfo? typeInfo));
            Assert.Null(typeInfo);

            options.TypeInfoResolver = new DefaultJsonTypeInfoResolver();
            typeInfo = options.GetTypeInfo(type);
            Assert.Equal(type, typeInfo.Type);
            Assert.False(typeInfo.IsReadOnly);

            JsonTypeInfo typeInfo2 = options.GetTypeInfo(type);
            Assert.Equal(type, typeInfo2.Type);
            Assert.False(typeInfo2.IsReadOnly);

            Assert.NotSame(typeInfo, typeInfo2);

            Assert.True(options.TryGetTypeInfo(type, out JsonTypeInfo? typeInfo3));
            Assert.Equal(type, typeInfo3.Type);
            Assert.False(typeInfo3.IsReadOnly);

            Assert.NotSame(typeInfo, typeInfo3);

            options.WriteIndented = true; // can mutate without issue
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(object))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(Dictionary<int, string>))]
        public static void GetTypeInfo_ImmutableOptionsInstance(Type type)
        {
            var options = new JsonSerializerOptions();
            JsonSerializer.Serialize(42, options);

            JsonTypeInfo typeInfo = options.GetTypeInfo(type);
            Assert.Equal(type, typeInfo.Type);
            Assert.True(typeInfo.IsReadOnly);

            JsonTypeInfo typeInfo2 = options.GetTypeInfo(type);
            Assert.Same(typeInfo, typeInfo2);

            Assert.True(options.TryGetTypeInfo(type, out JsonTypeInfo? typeInfo3));
            Assert.Same(typeInfo, typeInfo3);
        }

        [Fact]
        public static void GetTypeInfo_MutableOptions_CanModifyMetadata()
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
            JsonTypeInfo<TestClassForEncoding> jti = (JsonTypeInfo<TestClassForEncoding>)options.GetTypeInfo(typeof(TestClassForEncoding));

            Assert.False(jti.IsReadOnly);
            Assert.Equal(1, jti.Properties.Count);
            jti.Properties.Clear();

            var value = new TestClassForEncoding { MyString = "SomeValue" };
            Assert.False(options.IsReadOnly);

            string json = JsonSerializer.Serialize(value, jti);
            Assert.Equal("{}", json);

            // Using JsonTypeInfo will lock JsonSerializerOptions
            Assert.True(options.IsReadOnly);
            Assert.True(jti.IsReadOnly);
            Assert.Throws<InvalidOperationException>(() => options.IncludeFields = false);

            // Getting JsonTypeInfo now should return a fresh immutable instance
            JsonTypeInfo<TestClassForEncoding> jti2 = (JsonTypeInfo<TestClassForEncoding>)options.GetTypeInfo(typeof(TestClassForEncoding));
            Assert.NotSame(jti, jti2);
            Assert.True(jti2.IsReadOnly);
            Assert.Equal(1, jti2.Properties.Count);
            Assert.Throws<InvalidOperationException>(() => jti2.Properties.Clear());

            // Subsequent requests return the same cached value
            Assert.Same(jti2, options.GetTypeInfo(typeof(TestClassForEncoding)));

            // Default contract should produce expected JSON
            json = JsonSerializer.Serialize(value, options);
            Assert.Equal("""{"MyString":"SomeValue"}""", json);

            // Default contract should not impact contract of original JsonTypeInfo
            json = JsonSerializer.Serialize(value, jti);
            Assert.Equal("{}", json);
        }

        [Fact]
        public static void GetTypeInfo_NullInput_ThrowsArgumentNullException()
        {
            var options = new JsonSerializerOptions();
            Assert.Throws<ArgumentNullException>(() => options.GetTypeInfo(null));
            Assert.Throws<ArgumentNullException>(() => options.TryGetTypeInfo(null, out JsonTypeInfo? _));
        }

        [Fact]
        public static void GetTypeInfo_RecursiveResolver_StackOverflows()
        {
            var resolver = new RecursiveResolver();
            var options = new JsonSerializerOptions { TypeInfoResolver = resolver };

            Assert.Throws<NotSupportedException>(() => options.GetTypeInfo(typeof(TestClassForEncoding)));
            Assert.True(resolver.IsThresholdReached);
        }

        private class RecursiveResolver : IJsonTypeInfoResolver
        {
            private const int MaxDepth = 10;

            [ThreadStatic]
            private int _isResolverEntered = 0;

            public bool IsThresholdReached { get; private set; }

            public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
            {
                if (_isResolverEntered == MaxDepth)
                {
                    IsThresholdReached = true;
                    return null;
                }

                _isResolverEntered++;
                try
                {
                    return options.GetTypeInfo(type);
                }
                finally
                {
                    _isResolverEntered--;
                }
            }
        }

        [Theory]
        [InlineData(typeof(void))]
        [InlineData(typeof(Dictionary<,>))]
        [InlineData(typeof(List<>))]
        [InlineData(typeof(Nullable<>))]
        [InlineData(typeof(int*))]
        [InlineData(typeof(Span<int>))]
        public static void GetTypeInfo_InvalidInput_ThrowsArgumentException(Type type)
        {
            var options = new JsonSerializerOptions();
            Assert.Throws<ArgumentException>(() => options.GetTypeInfo(type));
            Assert.Throws<ArgumentException>(() => options.TryGetTypeInfo(type, out JsonTypeInfo? _));
        }

        [Fact]
        public static void GetTypeInfo_ResolverWithoutMetadata_ThrowsNotSupportedException()
        {
            var options = new JsonSerializerOptions();
            options.AddContext<JsonContext>();

            Assert.Throws<NotSupportedException>(() => options.GetTypeInfo(typeof(BasicCompany)));

            Assert.False(options.TryGetTypeInfo(typeof(BasicCompany), out JsonTypeInfo? typeInfo));
            Assert.Null(typeInfo);
        }

        [Theory]
        [MemberData(nameof(GetTypeInfo_ResultsAreGeneric_Values))]
        public static void GetTypeInfo_ResultsAreGeneric<T>(T value, string expectedJson)
        {
            var options = new JsonSerializerOptions { TypeInfoResolver = new DefaultJsonTypeInfoResolver() };
            JsonTypeInfo<T> jsonTypeInfo = (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
            string json = JsonSerializer.Serialize(value, jsonTypeInfo);
            Assert.Equal(expectedJson, json);
            JsonSerializer.Deserialize(json, jsonTypeInfo);
        }

        public static IEnumerable<object[]> GetTypeInfo_ResultsAreGeneric_Values()
        {
            yield return WrapArgs(42, "42");
            yield return WrapArgs("string", "\"string\"");
            yield return WrapArgs(new { Value = 42, String = "str" }, """{"Value":42,"String":"str"}""");
            yield return WrapArgs(new List<int> { 1, 2, 3, 4, 5 }, """[1,2,3,4,5]""");
            yield return WrapArgs(new Dictionary<string, int> { ["key"] = 42 }, """{"key":42}""");

            static object[] WrapArgs<T>(T value, string json) => new object[] { value, json };
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Diagnostics.Runtime.ICorDebug;
using Newtonsoft.Json;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public class RequiredKeywordTests_Span : RequiredKeywordTests
    {
        public RequiredKeywordTests_Span() : base(JsonSerializerWrapper.SpanSerializer) { }
    }

    public class RequiredKeywordTests_String : RequiredKeywordTests
    {
        public RequiredKeywordTests_String() : base(JsonSerializerWrapper.StringSerializer) { }
    }

    public class RequiredKeywordTests_AsyncStream : RequiredKeywordTests
    {
        public RequiredKeywordTests_AsyncStream() : base(JsonSerializerWrapper.AsyncStreamSerializer) { }
    }

    public class RequiredKeywordTests_AsyncStreamWithSmallBuffer : RequiredKeywordTests
    {
        public RequiredKeywordTests_AsyncStreamWithSmallBuffer() : base(JsonSerializerWrapper.AsyncStreamSerializerWithSmallBuffer) { }
    }

    public class RequiredKeywordTests_SyncStream : RequiredKeywordTests
    {
        public RequiredKeywordTests_SyncStream() : base(JsonSerializerWrapper.SyncStreamSerializer) { }
    }

    public class RequiredKeywordTests_Writer : RequiredKeywordTests
    {
        public RequiredKeywordTests_Writer() : base(JsonSerializerWrapper.ReaderWriterSerializer) { }
    }

    public class RequiredKeywordTests_Document : RequiredKeywordTests
    {
        public RequiredKeywordTests_Document() : base(JsonSerializerWrapper.DocumentSerializer) { }
    }

    public class RequiredKeywordTests_Element : RequiredKeywordTests
    {
        public RequiredKeywordTests_Element() : base(JsonSerializerWrapper.ElementSerializer) { }
    }

    public class RequiredKeywordTests_Node : RequiredKeywordTests
    {
        public RequiredKeywordTests_Node() : base(JsonSerializerWrapper.NodeSerializer) { }
    }

    public abstract partial class RequiredKeywordTests : SerializerTests
    {
        public RequiredKeywordTests(JsonSerializerWrapper serializer) : base(serializer)
        {
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void ClassWithRequiredKeywordDeserialization(bool ignoreNullValues)
        {
            JsonSerializerOptions options = new()
            {
                IgnoreNullValues = ignoreNullValues
            };

            AssertJsonTypeInfoHasRequiredProperties(GetTypeInfo<PersonWithRequiredMembers>(options),
                nameof(PersonWithRequiredMembers.FirstName),
                nameof(PersonWithRequiredMembers.LastName));

            var obj = new PersonWithRequiredMembers()
            {
                FirstName = "foo",
                LastName = "bar"
            };

            string json = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar"}""", json);

            PersonWithRequiredMembers deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembers>(json, options);
            Assert.Equal(obj.FirstName, deserialized.FirstName);
            Assert.Equal(obj.MiddleName, deserialized.MiddleName);
            Assert.Equal(obj.LastName, deserialized.LastName);

            json = """{"LastName":"bar"}""";
            JsonException exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<PersonWithRequiredMembers>(json, options));
            Assert.Contains("FirstName", exception.Message);
            Assert.DoesNotContain("LastName", exception.Message);
            Assert.DoesNotContain("MiddleName", exception.Message);

            json = """{"LastName":null}""";
            exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<PersonWithRequiredMembers>(json, options));
            Assert.Contains("FirstName", exception.Message);
            Assert.DoesNotContain("LastName", exception.Message);
            Assert.DoesNotContain("MiddleName", exception.Message);

            json = "{}";
            exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<PersonWithRequiredMembers>(json, options));
            Assert.Contains("FirstName", exception.Message);
            Assert.Contains("LastName", exception.Message);
            Assert.DoesNotContain("MiddleName", exception.Message);
        }

        [Fact]
        public async void RequiredPropertyOccuringTwiceInThePayloadWorksAsExpected()
        {
            string json = """{"FirstName":"foo","MiddleName":"","LastName":"bar","FirstName":"newfoo"}""";
            PersonWithRequiredMembers deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembers>(json);
            Assert.Equal("newfoo", deserialized.FirstName);
            Assert.Equal("", deserialized.MiddleName);
            Assert.Equal("bar", deserialized.LastName);
        }

        private class PersonWithRequiredMembers
        {
            public required string FirstName { get; set; }
            public string MiddleName { get; set; } = "";
            public required string LastName { get; set; }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void ClassWithRequiredKeywordAndSmallParametrizedCtorFailsDeserialization(bool ignoreNullValues)
        {
            JsonSerializerOptions options = new()
            {
                IgnoreNullValues = ignoreNullValues
            };

            AssertJsonTypeInfoHasRequiredProperties(GetTypeInfo<PersonWithRequiredMembersAndSmallParametrizedCtor>(options),
                nameof(PersonWithRequiredMembersAndSmallParametrizedCtor.FirstName),
                nameof(PersonWithRequiredMembersAndSmallParametrizedCtor.LastName),
                nameof(PersonWithRequiredMembersAndSmallParametrizedCtor.Info1),
                nameof(PersonWithRequiredMembersAndSmallParametrizedCtor.Info2));

            var obj = new PersonWithRequiredMembersAndSmallParametrizedCtor("badfoo", "badbar")
            {
                // note: these must be set during initialize or otherwise we get compiler errors
                FirstName = "foo",
                LastName = "bar",
                Info1 = "info1",
                Info2 = "info2",
            };

            string json = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar","Info1":"info1","Info2":"info2"}""", json);

            var deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndSmallParametrizedCtor>(json, options);
            Assert.Equal(obj.FirstName, deserialized.FirstName);
            Assert.Equal(obj.MiddleName, deserialized.MiddleName);
            Assert.Equal(obj.LastName, deserialized.LastName);
            Assert.Equal(obj.Info1, deserialized.Info1);
            Assert.Equal(obj.Info2, deserialized.Info2);

            json = """{"FirstName":"foo","MiddleName":"","LastName":null,"Info1":null,"Info2":"info2"}""";
            deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndSmallParametrizedCtor>(json, options);
            Assert.Equal(obj.FirstName, deserialized.FirstName);
            Assert.Equal(obj.MiddleName, deserialized.MiddleName);
            Assert.Null(deserialized.LastName);
            Assert.Null(deserialized.Info1);
            Assert.Equal(obj.Info2, deserialized.Info2);

            json = """{"LastName":"bar","Info1":"info1"}""";
            JsonException exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndSmallParametrizedCtor>(json, options));
            Assert.Contains("FirstName", exception.Message);
            Assert.DoesNotContain("LastName", exception.Message);
            Assert.DoesNotContain("MiddleName", exception.Message);
            Assert.DoesNotContain("Info1", exception.Message);
            Assert.Contains("Info2", exception.Message);

            json = """{"LastName":null,"Info1":null}""";
            exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndSmallParametrizedCtor>(json, options));
            Assert.Contains("FirstName", exception.Message);
            Assert.DoesNotContain("LastName", exception.Message);
            Assert.DoesNotContain("MiddleName", exception.Message);
            Assert.DoesNotContain("Info1", exception.Message);
            Assert.Contains("Info2", exception.Message);

            json = "{}";
            exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndSmallParametrizedCtor>(json, options));
            Assert.Contains("FirstName", exception.Message);
            Assert.Contains("LastName", exception.Message);
            Assert.DoesNotContain("MiddleName", exception.Message);
            Assert.Contains("Info1", exception.Message);
            Assert.Contains("Info2", exception.Message);
        }

        private class PersonWithRequiredMembersAndSmallParametrizedCtor
        {
            public required string FirstName { get; set; }
            public string MiddleName { get; set; } = "";
            public required string LastName { get; set; }
            public required string Info1 { get; set; }
            public required string Info2 { get; set; }

            public PersonWithRequiredMembersAndSmallParametrizedCtor(string firstName, string lastName)
            {
                FirstName = firstName;
                LastName = lastName;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async void ClassWithRequiredKeywordAndLargeParametrizedCtorFailsDeserialization(bool ignoreNullValues)
        {
            JsonSerializerOptions options = new()
            {
                IgnoreNullValues = ignoreNullValues
            };

            AssertJsonTypeInfoHasRequiredProperties(GetTypeInfo<PersonWithRequiredMembersAndLargeParametrizedCtor>(options),
                nameof(PersonWithRequiredMembersAndLargeParametrizedCtor.AProp),
                nameof(PersonWithRequiredMembersAndLargeParametrizedCtor.BProp),
                nameof(PersonWithRequiredMembersAndLargeParametrizedCtor.CProp),
                nameof(PersonWithRequiredMembersAndLargeParametrizedCtor.DProp),
                nameof(PersonWithRequiredMembersAndLargeParametrizedCtor.EProp),
                nameof(PersonWithRequiredMembersAndLargeParametrizedCtor.FProp),
                nameof(PersonWithRequiredMembersAndLargeParametrizedCtor.GProp),
                nameof(PersonWithRequiredMembersAndLargeParametrizedCtor.HProp),
                nameof(PersonWithRequiredMembersAndLargeParametrizedCtor.IProp));

            var obj = new PersonWithRequiredMembersAndLargeParametrizedCtor("bada", "badb", "badc", "badd", "bade", "badf", "badg")
            {
                // note: these must be set during initialize or otherwise we get compiler errors
                AProp = "a",
                BProp = "b",
                CProp = "c",
                DProp = "d",
                EProp = "e",
                FProp = "f",
                GProp = "g",
                HProp = "h",
                IProp = "i",
            };

            string json = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("""{"AProp":"a","BProp":"b","CProp":"c","DProp":"d","EProp":"e","FProp":"f","GProp":"g","HProp":"h","IProp":"i"}""", json);

            var deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndLargeParametrizedCtor>(json, options);
            Assert.Equal(obj.AProp, deserialized.AProp);
            Assert.Equal(obj.BProp, deserialized.BProp);
            Assert.Equal(obj.CProp, deserialized.CProp);
            Assert.Equal(obj.DProp, deserialized.DProp);
            Assert.Equal(obj.EProp, deserialized.EProp);
            Assert.Equal(obj.FProp, deserialized.FProp);
            Assert.Equal(obj.GProp, deserialized.GProp);
            Assert.Equal(obj.HProp, deserialized.HProp);
            Assert.Equal(obj.IProp, deserialized.IProp);

            json = """{"AProp":"a","BProp":"b","CProp":"c","DProp":"d","EProp":null,"FProp":"f","GProp":"g","HProp":null,"IProp":"i"}""";
            deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndLargeParametrizedCtor>(json, options);
            Assert.Equal(obj.AProp, deserialized.AProp);
            Assert.Equal(obj.BProp, deserialized.BProp);
            Assert.Equal(obj.CProp, deserialized.CProp);
            Assert.Equal(obj.DProp, deserialized.DProp);
            Assert.Null(deserialized.EProp);
            Assert.Equal(obj.FProp, deserialized.FProp);
            Assert.Equal(obj.GProp, deserialized.GProp);
            Assert.Null(deserialized.HProp);
            Assert.Equal(obj.IProp, deserialized.IProp);

            json = """{"AProp":"a","IProp":"i"}""";
            JsonException exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndLargeParametrizedCtor>(json, options));
            Assert.DoesNotContain("AProp", exception.Message);
            Assert.Contains("BProp", exception.Message);
            Assert.Contains("CProp", exception.Message);
            Assert.Contains("DProp", exception.Message);
            Assert.Contains("EProp", exception.Message);
            Assert.Contains("FProp", exception.Message);
            Assert.Contains("GProp", exception.Message);
            Assert.Contains("HProp", exception.Message);
            Assert.DoesNotContain("IProp", exception.Message);

            json = """{"AProp":null,"IProp":null}""";
            exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndLargeParametrizedCtor>(json, options));
            Assert.DoesNotContain("AProp", exception.Message);
            Assert.Contains("BProp", exception.Message);
            Assert.Contains("CProp", exception.Message);
            Assert.Contains("DProp", exception.Message);
            Assert.Contains("EProp", exception.Message);
            Assert.Contains("FProp", exception.Message);
            Assert.Contains("GProp", exception.Message);
            Assert.Contains("HProp", exception.Message);
            Assert.DoesNotContain("IProp", exception.Message);

            json = """{"BProp":"b","CProp":"c","DProp":"d","EProp":"e","FProp":"f","HProp":"h"}""";
            exception = await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndLargeParametrizedCtor>(json, options));
            Assert.Contains("AProp", exception.Message);
            Assert.DoesNotContain("BProp", exception.Message);
            Assert.DoesNotContain("CProp", exception.Message);
            Assert.DoesNotContain("DProp", exception.Message);
            Assert.DoesNotContain("EProp", exception.Message);
            Assert.DoesNotContain("FProp", exception.Message);
            Assert.Contains("GProp", exception.Message);
            Assert.DoesNotContain("HProp", exception.Message);
            Assert.Contains("IProp", exception.Message);
        }

        private class PersonWithRequiredMembersAndLargeParametrizedCtor
        {
            // Using suffix for names so that checking if required property is missing can be done with simple string.Contains without false positives
            public required string AProp { get; set; }
            public required string BProp { get; set; }
            public required string CProp { get; set; }
            public required string DProp { get; set; }
            public required string EProp { get; set; }
            public required string FProp { get; set; }
            public required string GProp { get; set; }
            public required string HProp { get; set; }
            public required string IProp { get; set; }

            public PersonWithRequiredMembersAndLargeParametrizedCtor(string aprop, string bprop, string cprop, string dprop, string eprop, string fprop, string gprop)
            {
                AProp = aprop;
                BProp = bprop;
                CProp = cprop;
                DProp = dprop;
                EProp = eprop;
                FProp = fprop;
                GProp = gprop;
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async void ClassWithRequiredKeywordAndSetsRequiredMembersOnCtorWorks(bool useContext)
        {
            JsonSerializerOptions options = useContext ? SetsRequiredMembersTestsContext.Default.Options : JsonSerializerOptions.Default;
            AssertJsonTypeInfoHasRequiredProperties(GetTypeInfo<PersonWithRequiredMembersAndSetsRequiredMembers>(options)
                /* no required members */);

            var obj = new PersonWithRequiredMembersAndSetsRequiredMembers()
            {
                FirstName = "foo",
                LastName = "bar"
            };

            string json = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar"}""", json);

            json = """{"LastName":"bar"}""";
            var deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndSetsRequiredMembers>(json, options);
            Assert.Equal("", deserialized.FirstName);
            Assert.Equal("", deserialized.MiddleName);
            Assert.Equal("bar", deserialized.LastName);
        }

        private class PersonWithRequiredMembersAndSetsRequiredMembers
        {
            public required string FirstName { get; set; }
            public string MiddleName { get; set; } = "";
            public required string LastName { get; set; }

            [SetsRequiredMembers]
            public PersonWithRequiredMembersAndSetsRequiredMembers()
            {
                FirstName = "";
                LastName = "";
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async void ClassWithRequiredKeywordSmallParametrizedCtorAndSetsRequiredMembersOnCtorWorks(bool useContext)
        {
            JsonSerializerOptions options = useContext ? SetsRequiredMembersTestsContext.Default.Options : JsonSerializerOptions.Default;
            AssertJsonTypeInfoHasRequiredProperties(GetTypeInfo<PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers>(options)
                /* no required members */);

            var obj = new PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers("foo", "bar");

            string json = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar"}""", json);

            var deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers>(json, options);
            Assert.Equal("foo", deserialized.FirstName);
            Assert.Equal("", deserialized.MiddleName);
            Assert.Equal("bar", deserialized.LastName);
        }

        private class PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers
        {
            public required string FirstName { get; set; }
            public string MiddleName { get; set; } = "";
            public required string LastName { get; set; }

            [SetsRequiredMembers]
            public PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers(string firstName, string lastName)
            {
                FirstName = firstName;
                LastName = lastName;
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async void ClassWithRequiredKeywordLargeParametrizedCtorAndSetsRequiredMembersOnCtorWorks(bool useContext)
        {
            JsonSerializerOptions options = useContext ? SetsRequiredMembersTestsContext.Default.Options : JsonSerializerOptions.Default;
            AssertJsonTypeInfoHasRequiredProperties(GetTypeInfo<PersonWithRequiredMembersAndLargeParametrizedCtorAndSetsRequiredMembers>(options)
                /* no required members */);

            var obj = new PersonWithRequiredMembersAndLargeParametrizedCtorAndSetsRequiredMembers("a", "b", "c", "d", "e", "f", "g");

            string json = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("""{"A":"a","B":"b","C":"c","D":"d","E":"e","F":"f","G":"g"}""", json);

            var deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndLargeParametrizedCtorAndSetsRequiredMembers>(json, options);
            Assert.Equal("a", deserialized.A);
            Assert.Equal("b", deserialized.B);
            Assert.Equal("c", deserialized.C);
            Assert.Equal("d", deserialized.D);
            Assert.Equal("e", deserialized.E);
            Assert.Equal("f", deserialized.F);
            Assert.Equal("g", deserialized.G);
        }

        private class PersonWithRequiredMembersAndLargeParametrizedCtorAndSetsRequiredMembers
        {
            public required string A { get; set; }
            public required string B { get; set; }
            public required string C { get; set; }
            public required string D { get; set; }
            public required string E { get; set; }
            public required string F { get; set; }
            public required string G { get; set; }

            [SetsRequiredMembers]
            public PersonWithRequiredMembersAndLargeParametrizedCtorAndSetsRequiredMembers(string a, string b, string c, string d, string e, string f, string g)
            {
                A = a;
                B = b;
                C = c;
                D = d;
                E = e;
                F = f;
                G = g;
            }
        }

        [JsonSerializable(typeof(PersonWithRequiredMembersAndSetsRequiredMembers))]
        [JsonSerializable(typeof(PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers))]
        [JsonSerializable(typeof(PersonWithRequiredMembersAndLargeParametrizedCtorAndSetsRequiredMembers))]
        private partial class SetsRequiredMembersTestsContext : JsonSerializerContext { }

        [Fact]
        public async void RemovingPropertiesWithRequiredKeywordAllowsDeserialization()
        {
            JsonSerializerOptions options = new()
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                {
                    Modifiers =
                    {
                        (ti) =>
                        {
                            for (int i = 0; i < ti.Properties.Count; i++)
                            {
                                if (ti.Properties[i].Name == nameof(PersonWithRequiredMembers.FirstName))
                                {
                                    Assert.True(ti.Properties[i].IsRequired);
                                    JsonPropertyInfo property = ti.CreateJsonPropertyInfo(typeof(string), nameof(PersonWithRequiredMembers.FirstName));
                                    property.Get = (obj) => ((PersonWithRequiredMembers)obj).FirstName;
                                    property.Set = (obj, val) => ((PersonWithRequiredMembers)obj).FirstName = (string)val;
                                    ti.Properties[i] = property;
                                }
                                else if (ti.Properties[i].Name == nameof(PersonWithRequiredMembers.LastName))
                                {
                                    Assert.True(ti.Properties[i].IsRequired);
                                    JsonPropertyInfo property = ti.CreateJsonPropertyInfo(typeof(string), nameof(PersonWithRequiredMembers.LastName));
                                    property.Get = (obj) => ((PersonWithRequiredMembers)obj).LastName;
                                    property.Set = (obj, val) => ((PersonWithRequiredMembers)obj).LastName = (string)val;
                                    ti.Properties[i] = property;
                                }
                                else
                                {
                                    Assert.False(ti.Properties[i].IsRequired);
                                }
                            }
                        }
                    }
                }
            };

            var obj = new PersonWithRequiredMembers()
            {
                FirstName = "foo",
                LastName = "bar"
            };

            string json = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar"}""", json);

            json = """{"LastName":"bar"}""";
            PersonWithRequiredMembers deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembers>(json, options);
            Assert.Null(deserialized.FirstName);
            Assert.Equal("", deserialized.MiddleName);
            Assert.Equal("bar", deserialized.LastName);
        }

        [Fact]
        public async void ChangingPropertiesWithRequiredKeywordToNotBeRequiredAllowsDeserialization()
        {
            JsonSerializerOptions options = new()
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                {
                    Modifiers =
                    {
                        (ti) =>
                        {
                            for (int i = 0; i < ti.Properties.Count; i++)
                            {
                                ti.Properties[i].IsRequired = false;
                            }
                        }
                    }
                }
            };

            var obj = new PersonWithRequiredMembers()
            {
                FirstName = "foo",
                LastName = "bar"
            };

            string json = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar"}""", json);

            json = """{"LastName":"bar"}""";
            PersonWithRequiredMembers deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembers>(json, options);
            Assert.Null(deserialized.FirstName);
            Assert.Equal("", deserialized.MiddleName);
            Assert.Equal("bar", deserialized.LastName);
        }

        [Fact]
        public async void RequiredNonDeserializablePropertyThrows()
        {
            JsonSerializerOptions options = new()
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                {
                    Modifiers =
                    {
                        (ti) =>
                        {
                            for (int i = 0; i < ti.Properties.Count; i++)
                            {
                                if (ti.Properties[i].Name == nameof(PersonWithRequiredMembers.FirstName))
                                {
                                    ti.Properties[i].Set = null;
                                }
                            }
                        }
                    }
                }
            };

            string json = """{"FirstName":"foo","MiddleName":"","LastName":"bar"}""";
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<PersonWithRequiredMembers>(json, options));
            Assert.Contains(nameof(PersonWithRequiredMembers.FirstName), exception.Message);
        }

        [Fact]
        public async void RequiredInitOnlyPropertyDoesNotThrow()
        {
            string json = """{"Prop":"foo"}""";
            ClassWithInitOnlyRequiredProperty deserialized = await Serializer.DeserializeWrapper<ClassWithInitOnlyRequiredProperty>(json);
            Assert.Equal("foo", deserialized.Prop);
        }

        private class ClassWithInitOnlyRequiredProperty
        {
            public required string Prop { get; init; }
        }

        [Fact]
        public async void RequiredExtensionDataPropertyThrows()
        {
            string json = """{"Foo":"foo","Bar":"bar"}""";
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await Serializer.DeserializeWrapper<ClassWithRequiredExtensionDataProperty>(json));
            Assert.Contains(nameof(ClassWithRequiredExtensionDataProperty.TestExtensionData), exception.Message);
        }

        private class ClassWithRequiredExtensionDataProperty
        {
            [JsonExtensionData]
            public required Dictionary<string, JsonElement>? TestExtensionData { get; set; }
        }

        [Fact]
        public async void RequiredKeywordAndJsonRequiredCustomAttributeWorkCorrectlyTogether()
        {
            JsonSerializerOptions options = JsonSerializerOptions.Default;
            JsonTypeInfo typeInfo = GetTypeInfo<ClassWithRequiredKeywordAndJsonRequiredCustomAttribute>(options);
            AssertJsonTypeInfoHasRequiredProperties(typeInfo,
                nameof(ClassWithRequiredKeywordAndJsonRequiredCustomAttribute.SomeProperty));

            ClassWithRequiredKeywordAndJsonRequiredCustomAttribute obj = new()
            {
                SomeProperty = "foo"
            };

            string json = await Serializer.SerializeWrapper(obj, options);
            Assert.Equal("""{"SomeProperty":"foo"}""", json);

            var deserialized = await Serializer.DeserializeWrapper<ClassWithRequiredKeywordAndJsonRequiredCustomAttribute>(json, options);
            Assert.Equal(obj.SomeProperty, deserialized.SomeProperty);

            json = "{}";
            JsonException exception = await Assert.ThrowsAsync<JsonException>(
                async () => await Serializer.DeserializeWrapper<ClassWithRequiredKeywordAndJsonRequiredCustomAttribute>(json, options));

            Assert.Contains(nameof(ClassWithRequiredKeywordAndJsonRequiredCustomAttribute.SomeProperty), exception.Message);
        }

        private class ClassWithRequiredKeywordAndJsonRequiredCustomAttribute
        {
            [JsonRequired]
            public required string SomeProperty { get; set; }
        }

        private static JsonTypeInfo GetTypeInfo<T>(JsonSerializerOptions options)
        {
            // For some variations of test (i.e. AsyncStreamWithSmallBuffer)
            // we don't use options directly and use copy with changed settings.
            // Because of that options.GetTypeInfo might throw even when Serializer.(De)SerializeWrapper was called..
            // We call into Serialize here to ensure that options are locked and options.GetTypeInfo queried.
            JsonSerializer.Serialize<T>(default(T), options);
            return options.GetTypeInfo(typeof(T));
        }

        private static void AssertJsonTypeInfoHasRequiredProperties(JsonTypeInfo typeInfo, params string[] requiredProperties)
        {
            HashSet<string> requiredPropertiesSet = new(requiredProperties);

            foreach (var property in typeInfo.Properties)
            {
                if (requiredPropertiesSet.Remove(property.Name))
                {
                    Assert.True(property.IsRequired);
                }
                else
                {
                    Assert.False(property.IsRequired);
                }
            }

            Assert.Empty(requiredPropertiesSet);
        }
    }
}

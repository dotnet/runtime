// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class RequiredKeywordTests : SerializerTests
    {
        public RequiredKeywordTests(JsonSerializerWrapper serializer) : base(serializer)
        {
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ClassWithRequiredKeywordDeserialization(bool ignoreNullValues)
        {
            JsonSerializerOptions options = Serializer.CreateOptions(makeReadOnly: false);
            options.IgnoreNullValues = ignoreNullValues;

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
        public async Task RequiredPropertyOccuringTwiceInThePayloadWorksAsExpected()
        {
            string json = """{"FirstName":"foo","MiddleName":"","LastName":"bar","FirstName":"newfoo"}""";
            PersonWithRequiredMembers deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembers>(json);
            Assert.Equal("newfoo", deserialized.FirstName);
            Assert.Equal("", deserialized.MiddleName);
            Assert.Equal("bar", deserialized.LastName);
        }

        public class PersonWithRequiredMembers
        {
            public required string FirstName { get; set; }
            public string MiddleName { get; set; } = "";
            public required string LastName { get; set; }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ClassWithRequiredKeywordAndSmallParametrizedCtorFailsDeserialization(bool ignoreNullValues)
        {
            JsonSerializerOptions options = Serializer.CreateOptions(makeReadOnly: false);
            options.IgnoreNullValues = ignoreNullValues;

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

        [Fact]
        public async Task InheritedPersonWithRequiredMembersWorksAsExpected()
        {
            var options = new JsonSerializerOptions(Serializer.DefaultOptions);
            options.MakeReadOnly();

            JsonTypeInfo typeInfo = options.GetTypeInfo(typeof(InheritedPersonWithRequiredMembers));
            Assert.Equal(3, typeInfo.Properties.Count);

            AssertJsonTypeInfoHasRequiredProperties(GetTypeInfo<InheritedPersonWithRequiredMembers>(options),
                nameof(InheritedPersonWithRequiredMembers.FirstName),
                nameof(InheritedPersonWithRequiredMembers.LastName));

            JsonException exception = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper("{}", typeInfo));
            Assert.Contains("FirstName", exception.Message);
            Assert.Contains("LastName", exception.Message);
            Assert.DoesNotContain("MiddleName", exception.Message);
        }

        [Fact]
        public async Task InheritedPersonWithRequiredMembersWithAdditionalRequiredMembersWorksAsExpected()
        {
            var options = new JsonSerializerOptions(Serializer.DefaultOptions);
            options.MakeReadOnly();

            JsonTypeInfo typeInfo = options.GetTypeInfo(typeof(InheritedPersonWithRequiredMembersWithAdditionalRequiredMembers));
            Assert.Equal(4, typeInfo.Properties.Count);

            AssertJsonTypeInfoHasRequiredProperties(GetTypeInfo<InheritedPersonWithRequiredMembersWithAdditionalRequiredMembers>(options),
                nameof(InheritedPersonWithRequiredMembersWithAdditionalRequiredMembers.FirstName),
                nameof(InheritedPersonWithRequiredMembersWithAdditionalRequiredMembers.LastName),
                nameof(InheritedPersonWithRequiredMembersWithAdditionalRequiredMembers.Post));

            JsonException exception = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper("{}", typeInfo));
            Assert.Contains("FirstName", exception.Message);
            Assert.Contains("LastName", exception.Message);
            Assert.Contains("Post", exception.Message);
            Assert.DoesNotContain("MiddleName", exception.Message);
        }

        [Theory]
        [MemberData(nameof(InheritedPersonWithRequiredMembersSetsRequiredMembersWorksAsExpectedSources))]
        public async Task InheritedPersonWithRequiredMembersSetsRequiredMembersWorksAsExpected(string jsonValue,
            InheritedPersonWithRequiredMembersSetsRequiredMembers expectedValue)
        {
            var options = new JsonSerializerOptions(Serializer.DefaultOptions);
            options.MakeReadOnly();

            JsonTypeInfo typeInfo = options.GetTypeInfo(typeof(InheritedPersonWithRequiredMembersSetsRequiredMembers));
            Assert.Equal(3, typeInfo.Properties.Count);

            AssertJsonTypeInfoHasRequiredProperties(GetTypeInfo<InheritedPersonWithRequiredMembersSetsRequiredMembers>(options));

            InheritedPersonWithRequiredMembersSetsRequiredMembers actualValue =
                await Serializer.DeserializeWrapper<InheritedPersonWithRequiredMembersSetsRequiredMembers>(jsonValue, options);
            Assert.Equal(expectedValue.FirstName, actualValue.FirstName);
            Assert.Equal(expectedValue.LastName, actualValue.LastName);
            Assert.Equal(expectedValue.MiddleName, actualValue.MiddleName);
        }

        public class InheritedPersonWithRequiredMembers : PersonWithRequiredMembers
        {
        }

        public class InheritedPersonWithRequiredMembersWithAdditionalRequiredMembers : PersonWithRequiredMembers
        {
            public required string Post { get; set; }
        }

        public class InheritedPersonWithRequiredMembersSetsRequiredMembers : PersonWithRequiredMembers
        {
            [SetsRequiredMembers]
            public InheritedPersonWithRequiredMembersSetsRequiredMembers()
            {
                FirstName = "FirstNameValueFromConstructor";
                LastName = "LastNameValueFromConstructor";
                MiddleName = "MiddleNameValueFromConstructor";
            }
        }

        public class PersonWithRequiredMembersAndSmallParametrizedCtor
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
        public async Task ClassWithRequiredKeywordAndLargeParametrizedCtorFailsDeserialization(bool ignoreNullValues)
        {
            JsonSerializerOptions options = Serializer.CreateOptions(makeReadOnly: false);
            options.IgnoreNullValues = ignoreNullValues;

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

        public class PersonWithRequiredMembersAndLargeParametrizedCtor
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

        [Fact]
        public async Task ClassWithRequiredKeywordAndSetsRequiredMembersOnCtorWorks()
        {
            AssertJsonTypeInfoHasRequiredProperties(GetTypeInfo<PersonWithRequiredMembersAndSetsRequiredMembers>(Serializer.DefaultOptions)
                /* no required members */);

            var obj = new PersonWithRequiredMembersAndSetsRequiredMembers()
            {
                FirstName = "foo",
                LastName = "bar"
            };

            string json = await Serializer.SerializeWrapper(obj);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar"}""", json);

            json = """{"LastName":"bar"}""";
            var deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndSetsRequiredMembers>(json);
            Assert.Equal("", deserialized.FirstName);
            Assert.Equal("", deserialized.MiddleName);
            Assert.Equal("bar", deserialized.LastName);
        }

        public class PersonWithRequiredMembersAndSetsRequiredMembers
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

        [Fact]
        public async Task ClassWithRequiredKeywordSmallParametrizedCtorAndSetsRequiredMembersOnCtorWorks()
        {
            AssertJsonTypeInfoHasRequiredProperties(GetTypeInfo<PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers>(Serializer.DefaultOptions)
                /* no required members */);

            var obj = new PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers("foo", "bar");

            string json = await Serializer.SerializeWrapper(obj);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar"}""", json);

            var deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers>(json);
            Assert.Equal("foo", deserialized.FirstName);
            Assert.Equal("", deserialized.MiddleName);
            Assert.Equal("bar", deserialized.LastName);
        }

        public class PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers
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

        [Fact]
        public async Task ClassWithRequiredKeywordLargeParametrizedCtorAndSetsRequiredMembersOnCtorWorks()
        {
            AssertJsonTypeInfoHasRequiredProperties(GetTypeInfo<PersonWithRequiredMembersAndLargeParametrizedCtorAndSetsRequiredMembers>(Serializer.DefaultOptions)
                /* no required members */);

            var obj = new PersonWithRequiredMembersAndLargeParametrizedCtorAndSetsRequiredMembers("a", "b", "c", "d", "e", "f", "g");

            string json = await Serializer.SerializeWrapper(obj);
            Assert.Equal("""{"A":"a","B":"b","C":"c","D":"d","E":"e","F":"f","G":"g"}""", json);

            var deserialized = await Serializer.DeserializeWrapper<PersonWithRequiredMembersAndLargeParametrizedCtorAndSetsRequiredMembers>(json);
            Assert.Equal("a", deserialized.A);
            Assert.Equal("b", deserialized.B);
            Assert.Equal("c", deserialized.C);
            Assert.Equal("d", deserialized.D);
            Assert.Equal("e", deserialized.E);
            Assert.Equal("f", deserialized.F);
            Assert.Equal("g", deserialized.G);
        }

        public class PersonWithRequiredMembersAndLargeParametrizedCtorAndSetsRequiredMembers
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

        [Fact]
        public async Task ClassWithRequiredFieldWorksAsExpected()
        {
            var options = new JsonSerializerOptions(Serializer.DefaultOptions) { IncludeFields = true };
            options.MakeReadOnly();

            JsonTypeInfo typeInfo = options.GetTypeInfo(typeof(ClassWithRequiredField));
            Assert.Equal(1, typeInfo.Properties.Count);

            JsonPropertyInfo jsonPropertyInfo = typeInfo.Properties[0];
            Assert.True(jsonPropertyInfo.IsRequired);

            await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper("{}", typeInfo));
        }

        public class ClassWithRequiredField
        {
            public required int RequiredField;
        }

        [Fact]
        public async Task RemovingPropertiesWithRequiredKeywordAllowsDeserialization()
        {
            JsonSerializerOptions options = Serializer.GetDefaultOptionsWithMetadataModifier(static ti =>
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
            });

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
        public async Task ChangingPropertiesWithRequiredKeywordToNotBeRequiredAllowsDeserialization()
        {
            JsonSerializerOptions options = Serializer.GetDefaultOptionsWithMetadataModifier(static ti =>
            {
                for (int i = 0; i < ti.Properties.Count; i++)
                {
                    ti.Properties[i].IsRequired = false;
                }
            });

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
        public async Task RequiredNonDeserializablePropertyThrows()
        {
            JsonSerializerOptions options = Serializer.GetDefaultOptionsWithMetadataModifier(static ti =>
            {
                for (int i = 0; i < ti.Properties.Count; i++)
                {
                    if (ti.Properties[i].Name == nameof(PersonWithRequiredMembers.FirstName))
                    {
                        ti.Properties[i].Set = null;
                    }
                }
            });

            string json = """{"FirstName":"foo","MiddleName":"","LastName":"bar"}""";
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<PersonWithRequiredMembers>(json, options));
            Assert.Contains(nameof(PersonWithRequiredMembers.FirstName), exception.Message);
        }

        [Fact]
        public async Task RequiredInitOnlyPropertyDoesNotThrow()
        {
            string json = """{"Prop":"foo"}""";
            ClassWithInitOnlyRequiredProperty deserialized = await Serializer.DeserializeWrapper<ClassWithInitOnlyRequiredProperty>(json);
            Assert.Equal("foo", deserialized.Prop);
        }

        public class ClassWithInitOnlyRequiredProperty
        {
            public required string Prop { get; init; }
        }

        [Fact]
        public async Task RequiredExtensionDataPropertyThrows()
        {
            string json = """{"Foo":"foo","Bar":"bar"}""";
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await Serializer.DeserializeWrapper<ClassWithRequiredExtensionDataProperty>(json));
            Assert.Contains(nameof(ClassWithRequiredExtensionDataProperty.TestExtensionData), exception.Message);
        }

        public class ClassWithRequiredExtensionDataProperty
        {
            [JsonExtensionData]
            public required Dictionary<string, JsonElement>? TestExtensionData { get; set; }
        }

        [Fact]
        public async Task RequiredKeywordAndJsonRequiredCustomAttributeWorkCorrectlyTogether()
        {
            JsonSerializerOptions options = Serializer.CreateOptions();
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

        public class ClassWithRequiredKeywordAndJsonRequiredCustomAttribute
        {
            [JsonRequired]
            public required string SomeProperty { get; set; }
        }

        [Fact]
        public async Task ClassWithCustomRequiredPropertyName_Roundtrip()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/82730
            var value = new ClassWithCustomRequiredPropertyName { Property = 42, PropertyWithInitOnlySetter = 43 };
            string json = await Serializer.SerializeWrapper(value);
            Assert.Equal("""{"Prop":42,"PropWithInit":43}""", json);

            value = await Serializer.DeserializeWrapper<ClassWithCustomRequiredPropertyName>(json);
            Assert.Equal(42, value.Property);
            Assert.Equal(43, value.PropertyWithInitOnlySetter);
        }

        public class ClassWithCustomRequiredPropertyName
        {
            [JsonPropertyName("Prop")]
            public required int Property { get; set; }

            [JsonPropertyName("PropWithInit")]
            public required int PropertyWithInitOnlySetter { get; init; }
        }

        [Fact]
        public async Task DerivedClassWithRequiredProperty()
        {
            var value = new DerivedClassWithRequiredInitOnlyProperty { MyInt = 42, MyBool = true, MyString = "42", MyLong = 4242 };
            string json = await Serializer.SerializeWrapper(value);
            Assert.Equal("""{"MyInt":42,"MyBool":true,"MyString":"42","MyLong":4242}""", json);

            value = await Serializer.DeserializeWrapper<DerivedClassWithRequiredInitOnlyProperty>(json);
            Assert.Equal(42, value.MyInt);
            Assert.True(value.MyBool);
            Assert.Equal("42", value.MyString);
            Assert.Equal(4242, value.MyLong);
        }

        public class BaseClassWithInitOnlyProperty
        {
            public int MyInt { get; init; }
            public bool MyBool { get; init; }
            public string MyString { get; set; }
        }

        public class DerivedClassWithRequiredInitOnlyProperty : BaseClassWithInitOnlyProperty
        {
            public new required int MyInt { get; init; }
            public new required bool MyBool { get; set; }
            public new string MyString { get; init; }
            public required long MyLong { get; init; }
        }

        public static IEnumerable<object[]> InheritedPersonWithRequiredMembersSetsRequiredMembersWorksAsExpectedSources()
        {
            yield return new object[]
            {
                "{}",
                new InheritedPersonWithRequiredMembersSetsRequiredMembers()
            };
            yield return new object[]
            {
                """{"FirstName": "FirstNameFromJson"}""",
                new InheritedPersonWithRequiredMembersSetsRequiredMembers
                {
                    FirstName = "FirstNameFromJson"
                }
            };
        }

        private static JsonTypeInfo GetTypeInfo<T>(JsonSerializerOptions options)
        {
            options.TypeInfoResolver ??= JsonSerializerOptions.Default.TypeInfoResolver;
            options.MakeReadOnly();
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

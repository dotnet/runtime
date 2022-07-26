// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
#if !NETFRAMEWORK
    public static partial class RequiredKeywordTests
    {
        [Fact]
        public static void ClassWithRequiredKeywordFailsDeserialization()
        {
            var obj = new PersonWithRequiredMembers()
            {
                FirstName = "foo",
                LastName = "bar"
            };

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar"}""", json);

            json = """{"LastName":"bar"}""";
            // Related: https://github.com/dotnet/runtime/issues/29861
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<PersonWithRequiredMembers>(json));
        }

        private class PersonWithRequiredMembers
        {
            public required string FirstName { get; set; }
            public string MiddleName { get; set; } = "";
            public required string LastName { get; set; }
        }

        [Fact]
        public static void ClassWithRequiredKeywordAndSmallParametrizedCtorFailsDeserialization()
        {
            var obj = new PersonWithRequiredMembersAndSmallParametrizedCtor("badfoo", "badbar")
            {
                // note: these must be set during initialize or otherwise we get compiler errors
                FirstName = "foo",
                LastName = "bar"
            };

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar"}""", json);

            json = """{"LastName":"bar"}""";
            // Related: https://github.com/dotnet/runtime/issues/29861
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<PersonWithRequiredMembersAndSmallParametrizedCtor>(json));
        }

        private class PersonWithRequiredMembersAndSmallParametrizedCtor
        {
            public required string FirstName { get; set; }
            public string MiddleName { get; set; } = "";
            public required string LastName { get; set; }

            public PersonWithRequiredMembersAndSmallParametrizedCtor(string firstName, string lastName)
            {
                FirstName = firstName;
                LastName = lastName;
            }
        }

        [Fact]
        public static void ClassWithRequiredKeywordAndLargeParametrizedCtorFailsDeserialization()
        {
            var obj = new PersonWithRequiredMembersAndLargeParametrizedCtor("bada", "badb", "badc", "badd", "bade", "badf", "badg")
            {
                // note: these must be set during initialize or otherwise we get compiler errors
                A = "a",
                B = "b",
                C = "c",
                D = "d",
                E = "e",
                F = "f",
                G = "g",
            };

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal("""{"A":"a","B":"b","C":"c","D":"d","E":"e","F":"f","G":"g"}""", json);

            // Related: https://github.com/dotnet/runtime/issues/29861
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<PersonWithRequiredMembersAndLargeParametrizedCtor>(json));
        }

        private class PersonWithRequiredMembersAndLargeParametrizedCtor
        {
            public required string A { get; set; }
            public required string B { get; set; }
            public required string C { get; set; }
            public required string D { get; set; }
            public required string E { get; set; }
            public required string F { get; set; }
            public required string G { get; set; }

            public PersonWithRequiredMembersAndLargeParametrizedCtor(string a, string b, string c, string d, string e, string f, string g)
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
        public static void ClassWithRequiredKeywordAndSetsRequiredMembersOnCtorWorks()
        {
            var obj = new PersonWithRequiredMembersAndSetsRequiredMembers()
            {
                FirstName = "foo",
                LastName = "bar"
            };

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar"}""", json);

            json = """{"LastName":"bar"}""";
            var deserialized = JsonSerializer.Deserialize<PersonWithRequiredMembersAndSetsRequiredMembers>(json);
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

        [Fact]
        public static void ClassWithRequiredKeywordSmallParametrizedCtorAndSetsRequiredMembersOnCtorWorks()
        {
            var obj = new PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers("foo", "bar");

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar"}""", json);

            var deserialized = JsonSerializer.Deserialize<PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers>(json);
            Assert.Equal("foo", deserialized.FirstName);
            Assert.Equal("", deserialized.MiddleName);
            Assert.Equal("bar", deserialized.LastName);
        }

        private class PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers
        {
            public required string FirstName { get; init; }
            public string MiddleName { get; init; } = "";
            public required string LastName { get; init; }

            [SetsRequiredMembers]
            public PersonWithRequiredMembersAndSmallParametrizedCtorAndSetsRequiredMembers(string firstName, string lastName)
            {
                FirstName = firstName;
                LastName = lastName;
            }
        }

        [Fact]
        public static void ClassWithRequiredKeywordLargeParametrizedCtorAndSetsRequiredMembersOnCtorWorks()
        {
            var obj = new PersonWithRequiredMembersAndLargeParametrizedCtorndSetsRequiredMembers("a", "b", "c", "d", "e", "f", "g");

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal("""{"A":"a","B":"b","C":"c","D":"d","E":"e","F":"f","G":"g"}""", json);

            var deserialized = JsonSerializer.Deserialize<PersonWithRequiredMembersAndLargeParametrizedCtorndSetsRequiredMembers>(json);
            Assert.Equal("a", deserialized.A);
            Assert.Equal("b", deserialized.B);
            Assert.Equal("c", deserialized.C);
            Assert.Equal("d", deserialized.D);
            Assert.Equal("e", deserialized.E);
            Assert.Equal("f", deserialized.F);
            Assert.Equal("g", deserialized.G);
        }

        private class PersonWithRequiredMembersAndLargeParametrizedCtorndSetsRequiredMembers
        {
            public required string A { get; set; }
            public required string B { get; set; }
            public required string C { get; set; }
            public required string D { get; set; }
            public required string E { get; set; }
            public required string F { get; set; }
            public required string G { get; set; }

            [SetsRequiredMembers]
            public PersonWithRequiredMembersAndLargeParametrizedCtorndSetsRequiredMembers(string a, string b, string c, string d, string e, string f, string g)
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
        public static void RemovingRequiredPropertiesAllowsDeserialization()
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
                                    JsonPropertyInfo property = ti.CreateJsonPropertyInfo(typeof(string), nameof(PersonWithRequiredMembers.FirstName));
                                    property.Get = (obj) => ((PersonWithRequiredMembers)obj).FirstName;
                                    property.Set = (obj, val) => ((PersonWithRequiredMembers)obj).FirstName = (string)val;
                                    ti.Properties[i] = property;
                                }
                                else if (ti.Properties[i].Name == nameof(PersonWithRequiredMembers.LastName))
                                {
                                    JsonPropertyInfo property = ti.CreateJsonPropertyInfo(typeof(string), nameof(PersonWithRequiredMembers.LastName));
                                    property.Get = (obj) => ((PersonWithRequiredMembers)obj).LastName;
                                    property.Set = (obj, val) => ((PersonWithRequiredMembers)obj).LastName = (string)val;
                                    ti.Properties[i] = property;
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

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Equal("""{"FirstName":"foo","MiddleName":"","LastName":"bar"}""", json);

            json = """{"LastName":"bar"}""";
            PersonWithRequiredMembers deserialized = JsonSerializer.Deserialize<PersonWithRequiredMembers>(json, options);
            Assert.Null(deserialized.FirstName);
            Assert.Equal("", deserialized.MiddleName);
            Assert.Equal("bar", deserialized.LastName);
        }
    }
#endif
}

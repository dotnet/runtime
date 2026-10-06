// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Context.Tests
{
    internal class MetadataDefinitionMembers
    {
        public int Field = 1;
        public MetadataDefinitionMembers() { }
        public MetadataDefinitionMembers(int value) { }
        public void Method() { }
        public void Method<T>() { }
        public int Property { get; set; }
        public event EventHandler Event { add { } remove { } }
        public class Nested { }
    }

    internal class GenericMetadataDefinitionMembers<T>
    {
        public T Field = default;
        public void Method(T value) { }
        public T Property { get; set; }
        public event EventHandler Event { add { } remove { } }
    }

    public class HasSameMetadataDefinitionAsTests
    {
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        private readonly CustomReflectionContext _customReflectionContext = new TestCustomReflectionContext();

        [Fact]
        public void MappedType_MatchesMappingsBySameContextOnly()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(MetadataDefinitionMembers).GetTypeInfo());
            TypeInfo customTypeAgain = _customReflectionContext.MapType(typeof(MetadataDefinitionMembers).GetTypeInfo());
            TypeInfo otherContextType = new TestCustomReflectionContext().MapType(typeof(MetadataDefinitionMembers).GetTypeInfo());

            Assert.True(customType.HasSameMetadataDefinitionAs(customType));
            Assert.True(customType.HasSameMetadataDefinitionAs(customTypeAgain));
            Assert.False(customType.HasSameMetadataDefinitionAs(_customReflectionContext.MapType(typeof(TestObject).GetTypeInfo())));
            Assert.False(customType.HasSameMetadataDefinitionAs(typeof(MetadataDefinitionMembers)));
            Assert.False(customType.HasSameMetadataDefinitionAs(otherContextType));
        }

        [Fact]
        public void MappedGenericType_MatchesItsDefinition()
        {
            TypeInfo customDefinition = _customReflectionContext.MapType(typeof(GenericMetadataDefinitionMembers<>).GetTypeInfo());
            TypeInfo customConstructed = _customReflectionContext.MapType(typeof(GenericMetadataDefinitionMembers<int>).GetTypeInfo());

            Assert.True(customDefinition.HasSameMetadataDefinitionAs(customConstructed));
            Assert.True(customConstructed.HasSameMetadataDefinitionAs(customDefinition));
        }

        [Theory]
        [InlineData(typeof(MetadataDefinitionMembers))]
        [InlineData(typeof(GenericMetadataDefinitionMembers<int>))]
        public void MappedMembers_MatchAsTheirUnderlyingMembersDo(Type type)
        {
            TypeInfo customType = _customReflectionContext.MapType(type.GetTypeInfo());
            MemberInfo[] members = customType.GetMembers(All);
            MemberInfo[] membersAgain = _customReflectionContext.MapType(type.GetTypeInfo()).GetMembers(All);
            Dictionary<string, MemberInfo> runtimeMembers = type.GetMembers(All).ToDictionary(Key);

            Assert.Equal(runtimeMembers.Count, members.Length);
            Assert.Contains(members, m => m.MemberType == MemberTypes.Constructor);
            Assert.Contains(members, m => m.MemberType == MemberTypes.Event);
            Assert.Contains(members, m => m.MemberType == MemberTypes.Field);
            Assert.Contains(members, m => m.MemberType == MemberTypes.Method);
            Assert.Contains(members, m => m.MemberType == MemberTypes.Property);

            foreach (MemberInfo member in members)
            {
                MemberInfo runtimeMember = runtimeMembers[Key(member)];
                Assert.False(member.HasSameMetadataDefinitionAs(runtimeMember));

                foreach (MemberInfo other in membersAgain)
                {
                    bool expected = runtimeMember.HasSameMetadataDefinitionAs(runtimeMembers[Key(other)]);
                    Assert.Equal(expected, member.HasSameMetadataDefinitionAs(other));
                }
            }
        }

        [Fact]
        public void MappedNestedType_MatchesMappingsBySameContextOnly()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(MetadataDefinitionMembers).GetTypeInfo());
            Type nested = customType.GetNestedType(nameof(MetadataDefinitionMembers.Nested));

            Assert.True(nested.HasSameMetadataDefinitionAs(_customReflectionContext.MapType(typeof(MetadataDefinitionMembers.Nested).GetTypeInfo())));
            Assert.False(nested.HasSameMetadataDefinitionAs(typeof(MetadataDefinitionMembers.Nested)));
        }

        [Fact]
        public void GetMemberWithSameMetadataDefinitionAs_ReturnsMemberOfConstructedType()
        {
            TypeInfo customDefinition = _customReflectionContext.MapType(typeof(GenericMetadataDefinitionMembers<>).GetTypeInfo());
            TypeInfo customConstructed = _customReflectionContext.MapType(typeof(GenericMetadataDefinitionMembers<int>).GetTypeInfo());

            foreach (MemberInfo member in customDefinition.GetMembers(All))
            {
                MemberInfo result = customConstructed.GetMemberWithSameMetadataDefinitionAs(member);
                Assert.Equal(customConstructed.GetMembers(All).Single(m => Key(m) == Key(result)), result);
                Assert.Equal(member.Name, result.Name);
                Assert.Equal(customConstructed, result.DeclaringType);
            }

            FieldInfo field = typeof(GenericMetadataDefinitionMembers<>).GetField(nameof(GenericMetadataDefinitionMembers<int>.Field));
            Assert.Throws<ArgumentException>(() => customConstructed.GetMemberWithSameMetadataDefinitionAs(field));
        }

        [Fact]
        public void AddedMembers_MatchThemselvesWhateverTheReflectedType()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            TypeInfo customTypeAgain = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            TypeInfo derivedType = _customReflectionContext.MapType(typeof(DerivedTestObject).GetTypeInfo());

            PropertyInfo number = customType.GetProperty("number");
            PropertyInfo inheritedNumber = derivedType.GetProperty("number");
            Assert.NotEqual(number, inheritedNumber);

            Assert.True(number.HasSameMetadataDefinitionAs(number));
            Assert.True(number.HasSameMetadataDefinitionAs(customTypeAgain.GetProperty("number")));
            Assert.True(number.HasSameMetadataDefinitionAs(inheritedNumber));
            Assert.True(inheritedNumber.HasSameMetadataDefinitionAs(number));
            Assert.True(inheritedNumber.HasSameMetadataDefinitionAs(inheritedNumber));
            Assert.False(number.HasSameMetadataDefinitionAs(customType.GetProperty("text")));
            Assert.False(inheritedNumber.HasSameMetadataDefinitionAs(derivedType.GetProperty("text")));
            Assert.False(number.HasSameMetadataDefinitionAs(customType.GetProperty(nameof(TestObject.A))));
            Assert.False(customType.GetProperty(nameof(TestObject.A)).HasSameMetadataDefinitionAs(number));

            MethodInfo getter = number.GetGetMethod();
            MethodInfo inheritedGetter = inheritedNumber.GetGetMethod();
            Assert.True(getter.HasSameMetadataDefinitionAs(getter));
            Assert.True(getter.HasSameMetadataDefinitionAs(inheritedGetter));
            Assert.True(inheritedGetter.HasSameMetadataDefinitionAs(getter));
            Assert.False(getter.HasSameMetadataDefinitionAs(number.GetSetMethod()));
            Assert.False(inheritedGetter.HasSameMetadataDefinitionAs(inheritedNumber.GetSetMethod()));
            Assert.False(getter.HasSameMetadataDefinitionAs(customType.GetMethod(nameof(TestObject.GetMessage))));
            Assert.False(getter.HasSameMetadataDefinitionAs(number));
        }

        [Fact]
        public void GetMemberWithSameMetadataDefinitionAs_FindsAddedMembers()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            TypeInfo derivedType = _customReflectionContext.MapType(typeof(DerivedTestObject).GetTypeInfo());
            PropertyInfo number = customType.GetProperty("number");

            Assert.Equal(number, customType.GetMemberWithSameMetadataDefinitionAs(number));
            Assert.Equal(derivedType.GetProperty("number"), derivedType.GetMemberWithSameMetadataDefinitionAs(number));

            MethodInfo getter = number.GetGetMethod();
            MemberInfo derivedGetter = derivedType.GetMemberWithSameMetadataDefinitionAs(getter);
            Assert.Contains(derivedGetter, derivedType.GetMethods());
            Assert.True(derivedGetter.HasSameMetadataDefinitionAs(getter));
            Assert.Equal(getter.Name, derivedGetter.Name);

            FieldInfo field = customType.GetField("<A>k__BackingField", All);
            Assert.Equal(field, customType.GetMemberWithSameMetadataDefinitionAs(field));
        }

        [Fact]
        public void Null_ThrowsArgumentNullException()
        {
            TypeInfo customType = _customReflectionContext.MapType(typeof(TestObject).GetTypeInfo());
            TypeInfo derivedType = _customReflectionContext.MapType(typeof(DerivedTestObject).GetTypeInfo());
            TypeInfo membersType = _customReflectionContext.MapType(typeof(MetadataDefinitionMembers).GetTypeInfo());
            List<MemberInfo> members =
            [
                customType,
                .. customType.GetMembers(All),
                .. derivedType.GetMembers(BindingFlags.Public | BindingFlags.Instance),
                derivedType.GetMethod("get_number"),
                .. membersType.GetMembers(All),
            ];

            foreach (MemberInfo member in members)
            {
                AssertExtensions.Throws<ArgumentNullException>("other", () => member.HasSameMetadataDefinitionAs(null));
            }
        }

        private static string Key(MemberInfo member) => $"{member.MemberType} {member}";
    }
}

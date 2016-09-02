// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineNestedType
    {
        public static IEnumerable<object[]> TypeAttributes_TestData()
        {
            yield return new object[] { TypeAttributes.Abstract | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.AnsiClass | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.AutoClass | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.AutoLayout | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.BeforeFieldInit | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.Class | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.ClassSemanticsMask | TypeAttributes.Abstract | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.ExplicitLayout | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.Import | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.Sealed | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.SequentialLayout | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.Serializable | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.SpecialName | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.StringFormatMask | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.UnicodeClass | TypeAttributes.NestedPublic };
            yield return new object[] { TypeAttributes.VisibilityMask };
        }

        [Theory]
        [InlineData("abc")]
        [InlineData("a\0b\0cd")]
        public void DefineNestedType_String(string name)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            TypeBuilder nestedType = type.DefineNestedType(name);
            Helpers.VerifyType(nestedType, type.Module, type, name, TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.NestedPrivate, typeof(object), 0, PackingSize.Unspecified, new Type[0]);
        }

        [Theory]
        [MemberData(nameof(TypeAttributes_TestData))]
        public void DefineNestedType_String_TypeAttributes(TypeAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            foreach (string name in new string[] { "abc", "a\0b\0cd" })
            {
                TypeBuilder nestedType = type.DefineNestedType(name, attributes);
                Assert.Equal(name, nestedType.Name);
                Assert.Equal(attributes, nestedType.Attributes);

                TypeAttributes noBaseTypeAttributes = TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class | TypeAttributes.NestedPublic | TypeAttributes.ClassSemanticsMask | TypeAttributes.Abstract;
                Type expectedBaseType = attributes == noBaseTypeAttributes ? null : typeof(object);
                Helpers.VerifyType(nestedType, type.Module, type, name, attributes, expectedBaseType, 0, PackingSize.Unspecified, new Type[0]);
            }
        }

        [Theory]
        [MemberData(nameof(TypeAttributes_TestData))]
        public void DefineNestedType_String_TypeAttributes_Type(TypeAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            Type parent = type.GetType();
            foreach (string name in new string[] { "abc", "a\0b\0cd" })
            {
                TypeBuilder nestedType = type.DefineNestedType(name, attributes, parent);
                Helpers.VerifyType(nestedType, type.Module, type, name, attributes, parent, 0, PackingSize.Unspecified, new Type[0]);
            }
        }

        [Theory]
        [MemberData(nameof(TypeAttributes_TestData))]
        public void DefineNestedType_String_TypeAttributes_Type_Int(TypeAttributes attributes)
        {
            foreach (int size in new int[] { 2048, int.MaxValue, int.MinValue })
            {
                TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
                Type parent = type.GetType();
                foreach (string name in new string[] { "abc", "a\0b\0cd" })
                {
                    TypeBuilder nestedType = type.DefineNestedType(name, attributes, parent, size);
                    Helpers.VerifyType(nestedType, type.Module, type, name, attributes, parent, size, PackingSize.Unspecified, new Type[0]);
                }
            }
        }

        [Theory]
        [MemberData(nameof(TypeAttributes_TestData))]
        public void DefineNestedType_String_TypeAttributes_Type_PackingSize(TypeAttributes attributes)
        {
            foreach (PackingSize packagingSize in new PackingSize[] { PackingSize.Size1, PackingSize.Size128, PackingSize.Size16, PackingSize.Size2, PackingSize.Size32, PackingSize.Size4, PackingSize.Size64, PackingSize.Size8, PackingSize.Unspecified })
            {
                TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
                Type parent = type.GetType();
                foreach (string name in new string[] { "abc", "a\0b\0cd" })
                {
                    TypeBuilder nestedType = type.DefineNestedType(name, attributes, parent, packagingSize);
                    Helpers.VerifyType(nestedType, type.Module, type, name, attributes, parent, 0, packagingSize, new Type[0]);
                }
            }
        }

        [Theory]
        [MemberData(nameof(TypeAttributes_TestData))]
        public void DefineNestedType_String_TypeAttributes_Type_TypeArray(TypeAttributes attributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            Type parent = type.GetType();
            foreach (string name in new string[] { "abc", "a\0b\0cd" })
            {
                TypeBuilder nestedType = type.DefineNestedType(name, attributes, parent, new Type[] { typeof(IComparable) });
                Helpers.VerifyType(nestedType, type.Module, type, name, attributes, parent, 0, PackingSize.Unspecified, new Type[] { typeof(IComparable) });
            }
        }

        [Fact]
        public void DefineNestedType_NullName_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            Assert.Throws<ArgumentNullException>("fullname", () => type.DefineNestedType(null));
            Assert.Throws<ArgumentNullException>("fullname", () => type.DefineNestedType(null, TypeAttributes.Public));
            Assert.Throws<ArgumentNullException>("fullname", () => type.DefineNestedType(null, TypeAttributes.Public, type.GetType()));
            Assert.Throws<ArgumentNullException>("fullname", () => type.DefineNestedType(null, TypeAttributes.Public, type.GetType(), 2048));
            Assert.Throws<ArgumentNullException>("fullname", () => type.DefineNestedType(null, TypeAttributes.Public, type.GetType(), PackingSize.Size8));
            Assert.Throws<ArgumentNullException>("fullname", () => type.DefineNestedType(null, TypeAttributes.Public, type.GetType(), new Type[0]));
        }

        [Fact]
        public void DefineNestedType_EmptyName_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            Assert.Throws<ArgumentException>("fullname", () => type.DefineNestedType(""));
            Assert.Throws<ArgumentException>("fullname", () => type.DefineNestedType("", TypeAttributes.Public));
            Assert.Throws<ArgumentException>("fullname", () => type.DefineNestedType("", TypeAttributes.Public, type.GetType()));
            Assert.Throws<ArgumentException>("fullname", () => type.DefineNestedType("", TypeAttributes.Public, type.GetType(), 2048));
            Assert.Throws<ArgumentException>("fullname", () => type.DefineNestedType("", TypeAttributes.Public, type.GetType(), PackingSize.Size8));
            Assert.Throws<ArgumentException>("fullname", () => type.DefineNestedType("", TypeAttributes.Public, type.GetType(), new Type[0]));
        }

        [Fact]
        public void DefineNestedType_NullParentType_SetsObjectAsParentType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            TypeBuilder nestedType1 = type.DefineNestedType("TestType1", TypeAttributes.NestedPublic, null);
            Assert.Equal(typeof(object), nestedType1.BaseType);

            TypeBuilder nestedType2 = type.DefineNestedType("TestType2", TypeAttributes.NestedPublic, null, 2048);
            Assert.Equal(typeof(object), nestedType2.BaseType);

            TypeBuilder nestedType3 = type.DefineNestedType("TestType3", TypeAttributes.NestedPublic, null, PackingSize.Size8);
            Assert.Equal(typeof(object), nestedType3.BaseType);

            TypeBuilder nestedType4 = type.DefineNestedType("TestType4", TypeAttributes.NestedPublic, null, new Type[0]);
            Assert.Equal(typeof(object), nestedType3.BaseType);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineNestedType
    {
        public static IEnumerable<TypeAttributes> TypeAttributes_TestData()
        {
            yield return TypeAttributes.Abstract | TypeAttributes.NestedPublic;
            yield return TypeAttributes.AnsiClass | TypeAttributes.NestedPublic;
            yield return TypeAttributes.AutoClass | TypeAttributes.NestedPublic;
            yield return TypeAttributes.AutoLayout | TypeAttributes.NestedPublic;
            yield return TypeAttributes.BeforeFieldInit | TypeAttributes.NestedPublic;
            yield return TypeAttributes.Class | TypeAttributes.NestedPublic;
            yield return TypeAttributes.ClassSemanticsMask | TypeAttributes.Abstract | TypeAttributes.NestedPublic;
            yield return TypeAttributes.ExplicitLayout | TypeAttributes.NestedPublic;
            yield return TypeAttributes.Import | TypeAttributes.NestedPublic;
            yield return TypeAttributes.Interface | TypeAttributes.Abstract | TypeAttributes.NestedPublic;
            yield return TypeAttributes.Sealed | TypeAttributes.NestedPublic;
            yield return TypeAttributes.SequentialLayout | TypeAttributes.NestedPublic;
            yield return TypeAttributes.Serializable | TypeAttributes.NestedPublic;
            yield return TypeAttributes.SpecialName | TypeAttributes.NestedPublic;
            yield return TypeAttributes.StringFormatMask | TypeAttributes.NestedPublic;
            yield return TypeAttributes.UnicodeClass | TypeAttributes.NestedPublic;
            yield return TypeAttributes.VisibilityMask;
            yield return TypeAttributes.NestedPrivate;
        }

        public static IEnumerable<object[]> TestData()
        {
            foreach (TypeAttributes attributes in TypeAttributes_TestData())
            {
                yield return new object[] { "TestName", attributes, null, PackingSize.Unspecified, 0, new Type[0] };
                yield return new object[] { "testname", attributes, typeof(TypeBuilderDefineNestedType), PackingSize.Size1, 0, new Type[0] };
                yield return new object[] { "class", attributes, typeof(TypeBuilderDefineNestedType), PackingSize.Size2, 0, new Type[0] };
                yield return new object[] { "\uD800\uDC00", attributes, typeof(bool), PackingSize.Size4, 0, new Type[0] };
                yield return new object[] { "a\0b\0c", attributes, typeof(int).MakePointerType(), PackingSize.Size8, 0, new Type[0] };
                yield return new object[] { "Test Name With Spaces", attributes, typeof(DateTime), PackingSize.Size16, 0, new Type[0] };
                yield return new object[] { "TestName", attributes, typeof(int[]), PackingSize.Size32, 0, new Type[0] };
                yield return new object[] { "TestName", attributes, typeof(string), PackingSize.Size64, 0, new Type[0] };
                yield return new object[] { "TestName", attributes, typeof(EmptyGenericClass<int>), PackingSize.Size128, 0, new Type[0] };
                yield return new object[] { "TestName", attributes, typeof(EmptyEnum), PackingSize.Unspecified, 2048, new Type[0] };
                yield return new object[] { "TestName", attributes, typeof(Delegate), PackingSize.Unspecified, int.MaxValue, new Type[0] };
                yield return new object[] { "TestName", attributes, typeof(EmptyGenericClass<>), PackingSize.Unspecified, int.MinValue, new Type[0] };

                yield return new object[] { "TestName", attributes, typeof(object), PackingSize.Unspecified, 0, null };
                yield return new object[] { "Name", attributes, null, PackingSize.Unspecified, 0, new Type[] { typeof(IComparable) } };
            }
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void DefineNestedType(string name, TypeAttributes attributes, Type parent, PackingSize packingSize, int typesize, Type[] implementedInterfaces)
        {
            bool isDefaultImplementedInterfaces = implementedInterfaces?.Length == 0;
            bool isDefaultPackingSize = packingSize == PackingSize.Unspecified;
            bool isDefaultSize = typesize == 0;
            bool isDefaultParent = parent == null;
            bool isDefaultAttributes = attributes == TypeAttributes.NestedPrivate;

            Action<TypeBuilder, TypeBuilder> verify = (type, declaringType) =>
            {
                bool allowsNullParent = attributes.HasFlag(TypeAttributes.Abstract) && attributes.HasFlag(TypeAttributes.ClassSemanticsMask);
                Type baseType = allowsNullParent ? parent : (parent ?? typeof(object));
                Helpers.VerifyType(type, declaringType.Module, declaringType, name, attributes, baseType, typesize, packingSize, implementedInterfaces);
            };

            if (isDefaultImplementedInterfaces)
            {
                if (isDefaultSize && isDefaultPackingSize)
                {
                    if (isDefaultParent)
                    {
                        if (isDefaultAttributes)
                        {
                            // Use DefineNestedType(string)
                            TypeBuilder type1 = Helpers.DynamicType(TypeAttributes.Public);
                            verify(type1.DefineNestedType(name), type1);
                        }
                        // Use DefineNestedType(string, TypeAttributes)
                        TypeBuilder type2 = Helpers.DynamicType(TypeAttributes.Public);
                        verify(type2.DefineNestedType(name, attributes), type2);
                    }
                    // Use DefineNestedType(string, TypeAttributes, Type)
                    TypeBuilder type3 = Helpers.DynamicType(TypeAttributes.Public);
                    verify(type3.DefineNestedType(name, attributes, parent), type3);
                }
                else if (isDefaultSize)
                {
                    // Use DefineNestedType(string, TypeAttributes, Type, PackingSize)
                    TypeBuilder type4 = Helpers.DynamicType(TypeAttributes.Public);
                    verify(type4.DefineNestedType(name, attributes, parent, packingSize), type4);
                }
                else if (isDefaultPackingSize)
                {
                    // Use DefineNestedType(string, TypeAttributes, Type, int)
                    TypeBuilder type5 = Helpers.DynamicType(TypeAttributes.Public);
                    verify(type5.DefineNestedType(name, attributes, parent, typesize), type5);
                }
                // Use DefineNestedType(string, TypeAttributes, Type, PackingSize, int);
                TypeBuilder type6 = Helpers.DynamicType(TypeAttributes.Public);
                verify(type6.DefineNestedType(name, attributes, parent, packingSize, typesize), type6);
            }
            else
            {
                // Use DefineNestedType(string, TypeAttributes, Type, Type[])
                Assert.True(isDefaultSize && isDefaultPackingSize); // Sanity check
                TypeBuilder type7 = Helpers.DynamicType(TypeAttributes.Public);
                verify(type7.DefineNestedType(name, attributes, parent, implementedInterfaces), type7);
            }
        }

        [Fact]
        public void DefineNestedType_NullName_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            AssertExtensions.Throws<ArgumentNullException>("fullname", () => type.DefineNestedType(null));
            AssertExtensions.Throws<ArgumentNullException>("fullname", () => type.DefineNestedType(null, TypeAttributes.Public));
            AssertExtensions.Throws<ArgumentNullException>("fullname", () => type.DefineNestedType(null, TypeAttributes.Public, type.GetType()));
            AssertExtensions.Throws<ArgumentNullException>("fullname", () => type.DefineNestedType(null, TypeAttributes.Public, type.GetType(), 2048));
            AssertExtensions.Throws<ArgumentNullException>("fullname", () => type.DefineNestedType(null, TypeAttributes.Public, type.GetType(), PackingSize.Size8));
            AssertExtensions.Throws<ArgumentNullException>("fullname", () => type.DefineNestedType(null, TypeAttributes.Public, type.GetType(), new Type[0]));
        }

        [Theory]
        [InlineData("")]
        [InlineData("\0")]
        [InlineData("\0TestName")]
        public void DefineNestedType_EmptyName_ThrowsArgumentException(string fullname)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            AssertExtensions.Throws<ArgumentException>("fullname", () => type.DefineNestedType(fullname));
            AssertExtensions.Throws<ArgumentException>("fullname", () => type.DefineNestedType(fullname, TypeAttributes.Public));
            AssertExtensions.Throws<ArgumentException>("fullname", () => type.DefineNestedType(fullname, TypeAttributes.Public, type.GetType()));
            AssertExtensions.Throws<ArgumentException>("fullname", () => type.DefineNestedType(fullname, TypeAttributes.Public, type.GetType(), 2048));
            AssertExtensions.Throws<ArgumentException>("fullname", () => type.DefineNestedType(fullname, TypeAttributes.Public, type.GetType(), PackingSize.Size8));
            AssertExtensions.Throws<ArgumentException>("fullname", () => type.DefineNestedType(fullname, TypeAttributes.Public, type.GetType(), new Type[0]));
        }

        [Fact]
        public void DefineNestedType_LongName_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.NotPublic);
            AssertExtensions.Throws<ArgumentException>("fullname", () => type.DefineNestedType(new string('a', 1024)));
        }
        
        [Theory]
        [InlineData(TypeAttributes.Public, "attr")]
        [InlineData(TypeAttributes.NotPublic, "attr")]
        [InlineData(TypeAttributes.Interface, "attr")]
        [InlineData(TypeAttributes.LayoutMask, "attr")]
        [InlineData(TypeAttributes.LayoutMask | TypeAttributes.Public, "attr")]
        [InlineData((TypeAttributes)0x00040800, "attr")]
        [InlineData((TypeAttributes)(-1), null)]
        [InlineData((TypeAttributes)(-5000), "attr")]
        public void DefineNestedType_InvalidAttributes_ThrowsArgumentException(TypeAttributes attributes, string paramName)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentException>(paramName, () => type.DefineNestedType("Name", attributes));
        }
        
        [Fact]
        public void DefineNestedType_InvalidParent_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentException>("attr", () => type.DefineNestedType("Name", TypeAttributes.Public, typeof(int).MakeByRefType()));
            AssertExtensions.Throws<ArgumentException>("attr", () => type.DefineNestedType("Name", TypeAttributes.Public, typeof(EmptyNonGenericInterface1)));
        }
        
        [Theory]
        [InlineData(typeof(void))]
        [InlineData(typeof(EmptyNonGenericStruct))]
        [InlineData(typeof(EmptyGenericStruct<>))]
        [InlineData(typeof(EmptyGenericStruct<int>))]
        [InlineData(typeof(SealedClass))]
        public void DefineNestedType_ParentNotInheritable_ThrowsTypeLoadExceptionOnCreation(Type parentType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            TypeBuilder nestedType = type.DefineNestedType("NestedType", TypeAttributes.NestedPublic, parentType);

            Assert.Throws<TypeLoadException>(() => nestedType.CreateTypeInfo());
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(StaticClass))]
        [InlineData(typeof(int*))]
        [InlineData(typeof(EmptyNonGenericClass[]))]
        public void DefineNestedType_ParentHasNoDefaultConstructor_ThrowsNotSupportedExceptionOnCreation(Type parentType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            TypeBuilder nestedType = type.DefineNestedType("NestedType", TypeAttributes.NestedPublic, parentType);

            Assert.Throws<NotSupportedException>(() => nestedType.CreateTypeInfo());
        }

        [Fact]
        public void DefineNestedType_NullInterface_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentNullException>("interfaces", () => type.DefineNestedType("Name", TypeAttributes.NestedPublic, typeof(object), new Type[] { null }));
        }

        [Fact]
        public void DefineNestedType_ByRefInterfaceType_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            AssertExtensions.Throws<ArgumentException>(null, () => type.DefineNestedType("Name", TypeAttributes.NestedPublic, typeof(object), new Type[] { typeof(int).MakeByRefType() }));
        }

        public static IEnumerable<object[]> InvalidInterfaceType_TestData()
        {
            yield return new object[] { typeof(EmptyNonGenericClass) };
            yield return new object[] { typeof(EmptyNonGenericStruct) };
            yield return new object[] { typeof(EmptyGenericClass<int>) };
            yield return new object[] { typeof(EmptyGenericStruct<>) };
            yield return new object[] { typeof(EmptyGenericStruct<int>) };
            yield return new object[] { typeof(EmptyGenericInterface<int>).GetGenericArguments()[0] };
            yield return new object[] { typeof(EmptyGenericInterface<>).GetGenericArguments()[0] };
        }

        [Theory]
        [MemberData(nameof(InvalidInterfaceType_TestData))]
        public void DefineNestedType_InvalidInterfaceType_ThrowsTypeLoadExceptionOnCreation(Type interfaceType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            TypeBuilder nestedType = type.DefineNestedType("Name", TypeAttributes.NestedPublic, typeof(object), new Type[] { interfaceType });

            Assert.Throws<TypeLoadException>(() => nestedType.CreateTypeInfo());
        }

        [Theory]
        [InlineData(typeof(EmptyGenericClass<>))]
        [InlineData(typeof(EmptyGenericInterface<>))]
        public void DefineNestedType_OpenGenericInterfaceType_ThrowsBadImageFormatExceptionOnCreation(Type interfaceType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            TypeBuilder nestedType = type.DefineNestedType("Name", TypeAttributes.NestedPublic, typeof(object), new Type[] { interfaceType });

            Assert.Throws<BadImageFormatException>(() => nestedType.CreateTypeInfo());
        }

        [Fact]
        public void GetNestedType_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => type.AsType().GetNestedType("Any", Helpers.AllFlags));
        }

        [Fact]
        public void GetNestedTypes_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => type.AsType().GetNestedTypes(Helpers.AllFlags));
        }
    }
}

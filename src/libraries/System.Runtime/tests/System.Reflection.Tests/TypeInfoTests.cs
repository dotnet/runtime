// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Reflection.Tests
{
    [Guid("FD80F123-BEDD-4492-B50A-5D46AE94DD4E")]
    public class TypeInfoTests
    {
        [Theory]
        [InlineData(typeof(TI_BaseClass), 2)]
        [InlineData(typeof(TI_SubClass), 2)]
        [InlineData(typeof(ClassWithStaticConstructor), 0)]
        [InlineData(typeof(ClassWithMultipleConstructors), 4)]
        public void DeclaredConstructors(Type type, int expectedCount)
        {
            ConstructorInfo[] constructors = type.GetTypeInfo().DeclaredConstructors.Where(ctorInfo => !ctorInfo.IsStatic).ToArray();
            Assert.Equal(expectedCount, constructors.Length);
            foreach (ConstructorInfo constructorInfo in constructors)
            {
                Assert.NotNull(constructorInfo);
                Assert.True(constructorInfo.IsSpecialName);
            }
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.EventPublic), true, "EventHandler")]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.EventPublicStatic), true, "EventHandler")]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.StuffHappened), true, "Action`1")]
        [InlineData(typeof(TI_BaseClass), "NoSuchEvent", false, "EventHandler")]
        [InlineData(typeof(TI_BaseClass), "", false, "EventHandler")]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.EventPublicNew), true, "EventHandler")]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.EventPublic), true, "EventHandler")]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.EventPublicStatic), false, "EventHandler")]
        public void DeclaredEvents(Type type, string name, bool exists, string eventHandlerTypeName)
        {
            IEnumerable<EventInfo> events = type.GetTypeInfo().DeclaredEvents;
            Assert.Equal(exists, events.Any(eventInfo => eventInfo.Name.Equals(name)));

            EventInfo declaredEventInfo = type.GetTypeInfo().GetDeclaredEvent(name);
            if (exists)
            {
                Assert.Equal(name, declaredEventInfo.Name);
                Assert.Equal(eventHandlerTypeName, declaredEventInfo.EventHandlerType.Name);
            }
            else
            {
                Assert.Null(declaredEventInfo);
            }
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass._field1), true, typeof(string), false)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass._field2), true, typeof(string), false)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass._readonlyField), true, typeof(string), false)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass._volatileField), true, typeof(string), false)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.s_field), true, typeof(string), false)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.s_readonlyField), true, typeof(string), false)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.s_volatileField), true, typeof(string), false)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.s_arrayField), true,  typeof(string[]), false)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.StuffHappened), true, typeof(Action<int>), true)]
        [InlineData(typeof(TI_BaseClass), "_privateField", true, typeof(int), true)]
        [InlineData(typeof(TI_BaseClass), "NoSuchField", false, default(Type), default(Boolean))]
        [InlineData(typeof(TI_BaseClass), "", false, default(Type), default(Boolean))]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass._field2), true, typeof(string), false)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass._readonlyField), true, typeof(string), false)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass._volatileField), true, typeof(string), false)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.s_field), true, typeof(string), false)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.s_readonlyField), true, typeof(string), false)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.s_volatileField), true, typeof(string), false)]
        public void DeclaredFields(Type type, string name, bool exists, Type fieldType, bool isPrivate)
        {
            IEnumerable<string> fields = type.GetTypeInfo().DeclaredFields.Select(fieldInfo => fieldInfo.Name);
            FieldInfo declaredFieldInfo = type.GetTypeInfo().GetDeclaredField(name);
            if (exists)
            {
                Assert.Equal(name, declaredFieldInfo.Name);
                Assert.Contains(name, fields);
                Assert.Equal(fieldType, declaredFieldInfo.FieldType);
                Assert.Equal(isPrivate, declaredFieldInfo.IsPrivate);
            }
            else
            {
                Assert.Null(declaredFieldInfo);
                Assert.DoesNotContain(name, fields);
            }
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), new string[] { "_field1", "_field2", "_readonlyField", "_volatileField", "s_field", "s_readonlyField", "s_volatileField", "s_arrayField" })]
        [InlineData(typeof(TI_SubClass), new string[] { "_field2", "_readonlyField", "_volatileField", "s_field", "s_readonlyField", "s_volatileField", "s_arrayField" })]
        public void DeclaredMembers(Type type, string[] expected)
        {
            HashSet<string> members = new HashSet<string>(type.GetTypeInfo().DeclaredMembers.Select(memberInfo => memberInfo.Name));
            Assert.Superset(new HashSet<string>(expected), members);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.VoidMethodReturningVoid1), true)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.StringMethodReturningVoid), true)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.VoidMethodReturningVoid2), true)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.VirtualVoidMethodReturningVoid1), true)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.VirtualVoidMethodReturningVoid2), true)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.StaticVoidMethodReturningVoid), true)]
        [InlineData(typeof(TI_BaseClass), "add_StuffHappened", true)]
        [InlineData(typeof(TI_BaseClass), "remove_StuffHappened", true)]
        [InlineData(typeof(TI_BaseClass), "set_StringProperty1", true)]
        [InlineData(typeof(TI_BaseClass), "get_StringProperty1", true)]
        [InlineData(typeof(TI_BaseClass), "NoSuchMethod", false)]
        [InlineData(typeof(TI_BaseClass), "", false)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.VoidMethodReturningVoid2), true)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.VirtualVoidMethodReturningVoid1), true)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.VirtualVoidMethodReturningVoid2), true)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.StaticVoidMethodReturningVoid), true)]
        public void DeclaredMethods(Type type, string name, bool exists)
        {
            IEnumerable<string> methods = type.GetTypeInfo().DeclaredMethods.Select(methodInfo => methodInfo.Name);
            MethodInfo declaredMethodInfo = type.GetTypeInfo().GetDeclaredMethod(name);
            if (exists)
            {
                Assert.Equal(name, declaredMethodInfo.Name);
                Assert.Contains(name, methods);
            }
            else
            {
                Assert.Null(declaredMethodInfo);
                Assert.DoesNotContain(name, methods);
            }
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.PublicNestedClass1), true)]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.PublicNestedClass2), true)]
        [InlineData(typeof(TI_BaseClass), "", false)]
        [InlineData(typeof(TI_BaseClass), "NoSuchType", false)]
        [InlineData(typeof(TI_SubClass), "ProtectedNestedClass", false)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.InternalNestedClass), false)]
        [InlineData(typeof(TI_SubClass), "PrivateNestedClass", false)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.PublicNestedClass1), true)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.PublicNestedClass2), false)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.NestPublic3), true)]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.NESTPUBLIC3), true)]
        [InlineData(typeof(MultipleNestedClass), nameof(MultipleNestedClass.Nest1), true)]
        [InlineData(typeof(MultipleNestedClass.Nest1), nameof(MultipleNestedClass.Nest1.Nest2), true)]
        [InlineData(typeof(MultipleNestedClass.Nest1.Nest2), nameof(MultipleNestedClass.Nest1.Nest2.Nest3), true)]
        public void DeclaredNestedTypes(Type type, string name, bool exists)
        {
            IEnumerable<string> nestedTypes = type.GetTypeInfo().DeclaredNestedTypes.Select(nestedType => nestedType.Name);

            TypeInfo typeInfo = type.GetTypeInfo().GetDeclaredNestedType(name);
            if (exists)
            {
                Assert.Equal(name, typeInfo.Name);
                Assert.Contains(name, nestedTypes);
            }
            else
            {
                Assert.Null(typeInfo);
                Assert.DoesNotContain(name, nestedTypes);
            }
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.StringProperty1))]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.StringProperty2))]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.VirtualStringProperty))]
        [InlineData(typeof(TI_BaseClass), nameof(TI_BaseClass.StaticStringProperty))]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.StringProperty1))]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.VirtualStringProperty))]
        [InlineData(typeof(TI_SubClass), nameof(TI_SubClass.StaticStringProperty))]
        public void DeclaredProperties(Type type, string name)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            IEnumerable<string> properties = typeInfo.DeclaredProperties.Select(property => property.Name);
            Assert.Contains(name, properties);
            Assert.Equal(name, typeInfo.GetDeclaredProperty(name).Name);
        }

        [Fact]
        public void FindInterfaces()
        {
            Type[] interfaces = typeof(ClassWithNoInterfaces).GetTypeInfo().FindInterfaces((Type t, object c) => true, "notused");

            Assert.Equal(0, interfaces.Length);
            interfaces = typeof(TI_ClassWithInterface1).GetTypeInfo().FindInterfaces((Type t, object c) => true, "notused");
            Assert.Equal(1, interfaces.Length);
            Assert.Equal(nameof(TI_NonGenericInterface1), interfaces[0].Name);

            interfaces = typeof(TI_ClassWithInterface1).GetTypeInfo().FindInterfaces((Type t, object c) => t.Name.Equals(c), "TI_NonGenericInterface1");
            Assert.Equal(1, interfaces.Length);
            Assert.Equal(nameof(TI_NonGenericInterface1), interfaces[0].Name);

            interfaces = typeof(ClassWithInterface2Interface3).GetTypeInfo().FindInterfaces((Type t, object c) => true, "notused");
            Assert.Equal(2, interfaces.Length);
            Assert.All(interfaces, m => Assert.Contains("TI_NonGenericInterface", m.Name));

            interfaces = typeof(ClassWithInterface2Interface3).GetTypeInfo().FindInterfaces((Type t, object c) => t.Name.Contains(c.ToString()), "TI_NonGenericInterface");
            Assert.Equal(2, interfaces.Length);
            Assert.All(interfaces, m => Assert.Contains("TI_NonGenericInterface", m.Name));

            interfaces = typeof(SubClassWithInterface1).GetTypeInfo().FindInterfaces((Type t, object c) => true, "notused");
            Assert.Equal(1, interfaces.Length);
            Assert.Equal(nameof(TI_NonGenericInterface1), interfaces[0].Name);

            interfaces = typeof(SubClassWithInterface1).GetTypeInfo().FindInterfaces((Type t, object c) => t.Name.Contains(c.ToString()), "TI_NonGenericInterface");
            Assert.Equal(1, interfaces.Length);
            Assert.Equal(nameof(TI_NonGenericInterface1), interfaces[0].Name);

            interfaces = typeof(SubClassWithInterface1Interface2Interface3).GetTypeInfo().FindInterfaces((Type t, object c) => true, "notused");
            Assert.Equal(3, interfaces.Length);
            Assert.All(interfaces, m => Assert.Contains("TI_NonGenericInterface", m.Name));

            interfaces = typeof(SubClassWithInterface1Interface2Interface3).GetTypeInfo().FindInterfaces((Type t, object c) => t.Name.Contains(c.ToString()), "TI_NonGenericInterface");
            Assert.Equal(3, interfaces.Length);
            Assert.All(interfaces, m => Assert.Contains("TI_NonGenericInterface", m.Name));

            interfaces = typeof(SubClassWithInterface1Interface2Interface3).GetTypeInfo().FindInterfaces((Type t, object c) => t.Name.Contains(c.ToString()), nameof(TI_NonGenericInterface1));
            Assert.Equal(1, interfaces.Length);
            Assert.Equal(nameof(TI_NonGenericInterface1), interfaces[0].Name);
        }

        public static IEnumerable<object[]> GenericTypeArguments_TestData()
        {
            // Interfaces
            yield return new object[] { typeof(TI_NonGenericInterface1), new Type[0], null };
            yield return new object[] { typeof(GenericInterface1<>), new Type[0], null };
            yield return new object[] { typeof(GenericInterface1<int>), new Type[] { typeof(int) }, null };
            yield return new object[] { typeof(GenericInterface2<,>), new Type[0], null };
            yield return new object[] { typeof(GenericInterface2<int, string>), new Type[] { typeof(int), typeof(string) }, null };

            // Structs
            yield return new object[] { typeof(NonGenericStructWithNoInterfaces), new Type[0], null };
            yield return new object[] { typeof(GenericStructWithNoInterfaces1<>), new Type[0], null };
            yield return new object[] { typeof(GenericStructWithNoInterfaces1<int>), new Type[] { typeof(int) }, null };
            yield return new object[] { typeof(GenericStructWithNoInterfaces2<,>), new Type[0], null };
            yield return new object[] { typeof(GenericStructWithNoInterfaces2<int, string>), new Type[] { typeof(int), typeof(string) }, null };

            yield return new object[] { typeof(NonGenericStructWithNonGenericInterface), new Type[0], new string[0] };
            yield return new object[] { typeof(GenericStructWithGenericInterface1<>), new Type[0], new string[] { "TS" } };
            yield return new object[] { typeof(GenericStructWithGenericInterface1<int>), new Type[] { typeof(int) }, new string[] { "Int32" } };
            yield return new object[] { typeof(GenericStructWithGenericInterface2<,>), new Type[0], new string[] { "TS", "VS" } };
            yield return new object[] { typeof(GenericStructWithGenericInterface2<int, string>), new Type[] { typeof(int), typeof(string) }, new string[] { "Int32", "String" } };

            yield return new object[] { typeof(NonGenericStructWithGenericInterface1), new Type[0], new string[] { "Int32" } };
            yield return new object[] { typeof(GenericStructWithGenericInterface3<>), new Type[0], new string[] { "TS", "Int32" } };
            yield return new object[] { typeof(GenericStructWithGenericInterface3<string>), new Type[] { typeof(string) }, new string[] { "String", "Int32" } };
            yield return new object[] { typeof(NonGenericStructWithGenericInterface2), new Type[0], new string[] { "Int32", "Int32" } };

            // Classes
            yield return new object[] { typeof(NonGenericClassWithNoInterfaces), new Type[0], null };
            yield return new object[] { typeof(GenericClassWithNoInterfaces1<>), new Type[0], null };
            yield return new object[] { typeof(GenericClassWithNoInterfaces1<int>), new Type[] { typeof(int) }, null };
            yield return new object[] { typeof(GenericClassWithNoInterfaces2<,>), new Type[0], null };
            yield return new object[] { typeof(GenericClassWithNoInterfaces2<int, string>), new Type[] { typeof(int), typeof(string) }, null };

            yield return new object[] { typeof(NonGenericClassWithNonGenericInterface), new Type[0], new string[0] };
            yield return new object[] { typeof(GenericClassWithGenericInterface1<int>), new Type[] { typeof(int) }, new string[] { "Int32" } };

            yield return new object[] { typeof(GenericClassWithGenericInterface2<,>), new Type[0], new string[] { "T", "V" } };
            yield return new object[] { typeof(GenericClassWithGenericInterface2<int, string>), new Type[] { typeof(int), typeof(string) }, new string[] { "Int32", "String" } };

            yield return new object[] { typeof(NonGenericClassWithGenericInterface1), new Type[0], new string[] { "Int32" } };
            yield return new object[] { typeof(GenericClassWithGenericInterface3<>), new Type[0], new string[] { "T", "Int32" } };
            yield return new object[] { typeof(GenericClassWithGenericInterface3<string>), new Type[] { typeof(string) }, new string[] { "String", "Int32" } };
            yield return new object[] { typeof(NonGenericClassWithGenericInterface2), new Type[0], new string[] { "Int32", "Int32" } };
        }

        [Theory]
        [MemberData(nameof(GenericTypeArguments_TestData))]
        public void GenericTypeArguments(Type type, Type[] expected, string[] baseExpected)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            Type[] genericTypeArguments = typeInfo.GenericTypeArguments;

            Assert.Equal(expected.Length, genericTypeArguments.Length);
            for (int i = 0; i < genericTypeArguments.Length; i++)
            {
                Assert.Equal(expected[i], genericTypeArguments[i]);
            }

            Type baseType = typeInfo.BaseType;
            if (baseType == null || typeInfo.IsEnum)
                return;

            if (baseType == typeof(ValueType) || baseType == typeof(object))
            {
                Type[] interfaces = typeInfo.ImplementedInterfaces.ToArray();
                if (interfaces.Length == 0)
                    return;
                baseType = interfaces[0];
            }

            TypeInfo typeInfoBase = baseType.GetTypeInfo();
            genericTypeArguments = typeInfoBase.GenericTypeArguments;

            Assert.Equal(baseExpected.Length, genericTypeArguments.Length);
            for (int i = 0; i < genericTypeArguments.Length; i++)
            {
                Assert.Equal(baseExpected[i], genericTypeArguments[i].Name);
            }
        }

        public static IEnumerable<object[]> GenericTypeParameters_TestData()
        {
            // Interfaces
            yield return new object[] { typeof(TI_NonGenericInterface1), new string[0], null };
            yield return new object[] { typeof(GenericInterface1<>), new string[] { "TI" }, null };
            yield return new object[] { typeof(GenericInterface1<int>), new string[0], null };
            yield return new object[] { typeof(GenericInterface2<,>), new string[] { "TI", "VI" }, null };
            yield return new object[] { typeof(GenericInterface2<int, string>), new string[0], null };

            // Structs
            yield return new object[] { typeof(NonGenericStructWithNoInterfaces), new string[0], null };
            yield return new object[] { typeof(GenericStructWithNoInterfaces1<>), new string[] { "TS" }, null };
            yield return new object[] { typeof(GenericStructWithNoInterfaces1<int>), new string[0], null };
            yield return new object[] { typeof(GenericStructWithNoInterfaces2<,>), new string[] { "TS", "VS" }, null };
            yield return new object[] { typeof(GenericStructWithNoInterfaces2<int, string>), new string[0], null };

            yield return new object[] { typeof(NonGenericStructWithNonGenericInterface), new string[0], new string[0] };
            yield return new object[] { typeof(GenericStructWithGenericInterface1<>), new string[] { "TS" }, new string[0] };
            yield return new object[] { typeof(GenericStructWithGenericInterface1<int>), new string[0], new string[0] };
            yield return new object[] { typeof(GenericStructWithGenericInterface2<,>), new string[] { "TS", "VS" }, new string[0] };
            yield return new object[] { typeof(GenericStructWithGenericInterface2<int, string>), new string[0], new string[0] };

            yield return new object[] { typeof(NonGenericStructWithGenericInterface1), new string[0], new string[0] };
            yield return new object[] { typeof(GenericStructWithGenericInterface3<>), new string[] { "TS" }, new string[0] };
            yield return new object[] { typeof(GenericStructWithGenericInterface3<string>), new string[0], new string[0] };
            yield return new object[] { typeof(NonGenericStructWithGenericInterface2), new string[0], new string[0] };

            // Classes
            yield return new object[] { typeof(NonGenericClassWithNoInterfaces), new string[0], null };
            yield return new object[] { typeof(GenericClassWithNoInterfaces1<>), new string[] { "T" }, null };
            yield return new object[] { typeof(GenericClassWithNoInterfaces1<int>), new string[0], null };
            yield return new object[] { typeof(GenericClassWithNoInterfaces2<,>), new string[] { "T", "V" }, null };
            yield return new object[] { typeof(GenericClassWithNoInterfaces2<int, string>), new string[0], null };

            yield return new object[] { typeof(NonGenericClassWithNonGenericInterface), new string[0], new string[0] };
            yield return new object[] { typeof(GenericClassWithGenericInterface1<>), new string[] { "T" }, new string[0] };
            yield return new object[] { typeof(GenericClassWithGenericInterface1<int>), new string[0], new string[0] };
            yield return new object[] { typeof(GenericClassWithGenericInterface2<,>), new string[] { "T", "V" }, new string[0] };
            yield return new object[] { typeof(GenericClassWithGenericInterface2<int, string>), new string[0], new string[0] };

            yield return new object[] { typeof(NonGenericClassWithGenericInterface1), new string[0], new string[0] };
            yield return new object[] { typeof(GenericClassWithGenericInterface3<>), new string[] { "T" }, new string[0] };
            yield return new object[] { typeof(GenericClassWithGenericInterface3<string>), new string[0], new string[0] };
            yield return new object[] { typeof(NonGenericClassWithGenericInterface2), new string[0], new string[0] };
        }

        [Theory]
        [MemberData(nameof(GenericTypeParameters_TestData))]
        public void GenericTypeParameters(Type type, string[] expected, string[] baseExpected)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            Type[] genericTypeParameters = typeInfo.GenericTypeParameters;

            Assert.Equal(expected.Length, genericTypeParameters.Length);
            for (int i = 0; i < genericTypeParameters.Length; i++)
            {
                Assert.Equal(expected[i], genericTypeParameters[i].Name);
            }

            Type baseType = typeInfo.BaseType;
            if (baseType == null || typeInfo.IsEnum)
                return;

            if (baseType == typeof(ValueType) || baseType == typeof(object))
            {
                Type[] interfaces = typeInfo.ImplementedInterfaces.ToArray();
                if (interfaces.Length == 0)
                    return;
                baseType = interfaces[0];
            }

            TypeInfo typeInfoBase = baseType.GetTypeInfo();
            genericTypeParameters = typeInfoBase.GenericTypeParameters;

            Assert.Equal(baseExpected.Length, genericTypeParameters.Length);
            for (int i = 0; i < genericTypeParameters.Length; i++)
            {
                Assert.Equal(baseExpected[i], genericTypeParameters[i].Name);
            }
        }

        [Theory]
        [InlineData(1, nameof(IntEnum.Enum1))]
        [InlineData(2, nameof(IntEnum.Enum2))]
        [InlineData(10, nameof(IntEnum.Enum10))]
        [InlineData(45, nameof(IntEnum.Enum45))]
        [InlineData(8, null)]
        public void GetEnumName(object value, string expected)
        {
            Assert.Equal(expected, typeof(IntEnum).GetTypeInfo().GetEnumName(value));
        }

        [Fact]
        public void GetEnumName_Invalid()
        {
            AssertExtensions.Throws<ArgumentException>("value", () => typeof(NonGenericClassWithNoInterfaces).GetTypeInfo().GetEnumName(""));
            Assert.Throws<ArgumentNullException>(() => typeof(IntEnum).GetTypeInfo().GetEnumName(null));
        }

        public static IEnumerable<object[]> GetEnumNames_TestData()
        {
            yield return new object[] { typeof(IntEnum), new string[] { "Enum1", "Enum2", "Enum10", "Enum18", "Enum45" } };
            yield return new object[] { typeof(UIntEnum), new string[] { "A", "B" } };
        }

        [Theory]
        [MemberData(nameof(GetEnumNames_TestData))]
        public static void GetEnumNames(Type enumType, string[] expected)
        {
            Assert.Equal(expected, enumType.GetTypeInfo().GetEnumNames());
        }

        [Fact]
        public void GetEnumNames_TypeNotEnum_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("enumType", () => typeof(NonGenericClassWithNoInterfaces).GetTypeInfo().GetEnumNames());
        }

        [Theory]
        [InlineData(typeof(PublicEnum), typeof(int))]
        [InlineData(typeof(UIntEnum), typeof(uint))]
        public static void GetEnumUnderlyingType(Type enumType, Type expected)
        {
            Assert.Equal(expected, enumType.GetTypeInfo().GetEnumUnderlyingType());
        }

        [Fact]
        public void GetEnumUnderlyingType_TypeNotEnum_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("enumType", () => typeof(NonGenericClassWithNoInterfaces).GetTypeInfo().GetEnumUnderlyingType());
        }

        [Fact]
        public static void GetEnumValues_Int()
        {
            GetEnumValues(typeof(IntEnum), new IntEnum[] { (IntEnum)1, (IntEnum)2, (IntEnum)10, (IntEnum)18, (IntEnum)45 });
        }

        [Fact]
        public static void GetEnumValues_UInt()
        {
            GetEnumValues(typeof(UIntEnum), new UIntEnum[] { (UIntEnum)1, (UIntEnum)10 });
        }

        private static void GetEnumValues(Type enumType, Array expected)
        {
            Assert.Equal(expected, enumType.GetTypeInfo().GetEnumValues());
        }

        [Fact]
        public static void GetEnumValuesAsUnderlyingType_Int()
        {
            GetEnumValuesAsUnderlyingType(typeof(IntEnum), new int[] { 1, 2, 10, 18, 45 });
        }

        [Fact]
        public static void GetEnumValuesAsUnderlyingType_UInt()
        {
            GetEnumValuesAsUnderlyingType(typeof(UIntEnum), new uint[] { 1, 10 });
        }

        private static void GetEnumValuesAsUnderlyingType(Type enumType, Array expected)
        {
            Assert.Equal(expected, enumType.GetTypeInfo().GetEnumValuesAsUnderlyingType());
        }


        [Fact]
        public void GetEnumValues_TypeNotEnum_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("enumType", () => typeof(NonGenericClassWithNoInterfaces).GetTypeInfo().GetEnumUnderlyingType());
        }

        [Theory]
        [InlineData(10, true)]
        [InlineData(5, false)]
        public static void IsEnumDefined(object value, bool expected)
        {
            Assert.Equal(expected, typeof(IntEnum).GetTypeInfo().IsEnumDefined(value));
        }

        [Fact]
        public void IsEnumDefined_Invalid()
        {
            AssertExtensions.Throws<ArgumentException>("enumType", () => typeof(NonGenericClassWithNoInterfaces).GetTypeInfo().IsEnumDefined(10));
            AssertExtensions.Throws<ArgumentException>("enumType", () => typeof(NonGenericClassWithNoInterfaces).GetTypeInfo().IsEnumDefined("10"));
            Assert.Throws<ArgumentNullException>(() => typeof(IntEnum).GetTypeInfo().IsEnumDefined(null));
            Assert.Throws<InvalidOperationException>(() => typeof(IntEnum).GetTypeInfo().IsEnumDefined(new NonGenericClassWithNoInterfaces()));
        }

        [Theory]
        [InlineData(typeof(InheritedInteraface), new Type[] { typeof(TI_NonGenericInterface2) })]
        [InlineData(typeof(StructWithInheritedInterface), new Type[] { typeof(TI_NonGenericInterface2), typeof(InheritedInteraface) })]
        [InlineData(typeof(NonGenericClassWithNonGenericInterface), new Type[] { typeof(TI_NonGenericInterface1) })]
        [InlineData(typeof(CompoundClass1), new Type[] { typeof(TI_NonGenericInterface2), typeof(TI_NonGenericInterface1), typeof(InheritedInteraface) })]
        [InlineData(typeof(CompoundClass2<>), new Type[] { typeof(TI_NonGenericInterface2), typeof(TI_NonGenericInterface1), typeof(InheritedInteraface) })]
        [InlineData(typeof(CompoundClass2<int>), new Type[] { typeof(TI_NonGenericInterface2), typeof(TI_NonGenericInterface1), typeof(InheritedInteraface) })]
        [InlineData(typeof(CompoundClass3<InheritedInteraface>), new Type[] { typeof(GenericInterface1<InheritedInteraface>), typeof(TI_NonGenericInterface1) })]
        [InlineData(typeof(CompoundClass4<>), new Type[] { typeof(GenericInterface1<string>), typeof(TI_NonGenericInterface1) })]
        [InlineData(typeof(CompoundClass4<string>), new Type[] { typeof(GenericInterface1<string>), typeof(TI_NonGenericInterface1) })]
        public void ImplementedInterfaces(Type type, Type[] expected)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            Type[] implementedInterfaces = type.GetTypeInfo().ImplementedInterfaces.ToArray();

            Array.Sort(implementedInterfaces, TypeSortComparer);
            Array.Sort(expected, TypeSortComparer);

            Assert.Equal(expected, implementedInterfaces);
            Assert.All(expected, ti => Assert.True(ti.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo())));
            Assert.All(expected, ti => Assert.True(type.GetTypeInfo().IsAssignableTo(ti.GetTypeInfo())));

            static int TypeSortComparer(Type a, Type b)
            {
                // produces a stable (within this process) ordering of two Type objects
                return a.TypeHandle.Value.CompareTo(b.TypeHandle.Value);
            }
        }

        public static IEnumerable<object[]> IsInstanceOfType_TestData()
        {
            yield return new object[] { typeof(Array), new int[0], true };
            yield return new object[] { typeof(TI_ClassWithInterface1), new TI_ClassWithInterface1(), true };
            yield return new object[] { typeof(TI_NonGenericInterface1), new TI_ClassWithInterface1(), true };
        }

        [Theory]
        [MemberData(nameof(IsInstanceOfType_TestData))]
        public void IsInstanceOfType(Type type, object o, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsInstanceOfType(o));
        }

        [Theory]
        // Inheritance
        [InlineData(typeof(TI_BaseClass), typeof(TI_SubClass), true)]
        [InlineData(typeof(TI_SubClass), typeof(TI_BaseClass), false)]
        [InlineData(typeof(TI_BaseClass), typeof(TI_BaseClass), true)]
        // Misc
        [InlineData(typeof(TI_ClassWithInterface1), typeof(TI_ClassWithInterface1), true)]
        [InlineData(typeof(TI_NonGenericInterface1), typeof(TI_ClassWithInterface1), true)]
        [InlineData(typeof(object), typeof(TI_ClassWithInterface1), true)]
        [InlineData(typeof(int?), typeof(int), true)]
        [InlineData(typeof(List<int>), typeof(List<>), false)]
        [InlineData(typeof(IDisposable), typeof(Stream), true)]
        [InlineData(typeof(IList), typeof(ArrayList), true)]
        [InlineData(typeof(object), typeof(int), true)]
        [InlineData(typeof(object), typeof(string), true)]
        // Null
        [InlineData(typeof(BaseClassWithInterface1Interface2), null, false)]
        // Lists and arrays
        [InlineData(typeof(IList<object>), typeof(object[]), true)]
        [InlineData(typeof(object[]), typeof(IList<object>), false)]
        [InlineData(typeof(BaseClassWithInterface1Interface2), typeof(SubClassWithInterface1Interface2), true)]
        [InlineData(typeof(BaseClassWithInterface1Interface2[]), typeof(SubClassWithInterface1Interface2[]), true)]
        [InlineData(typeof(IList<object>), typeof(BaseClassWithInterface1Interface2[]), true)]
        [InlineData(typeof(IList<BaseClassWithInterface1Interface2>), typeof(BaseClassWithInterface1Interface2[]), true)]
        [InlineData(typeof(IList<BaseClassWithInterface1Interface2>), typeof(SubClassWithInterface1Interface2[]), true)]
        [InlineData(typeof(IList<SubClassWithInterface1Interface2>), typeof(SubClassWithInterface1Interface2[]), true)]
        // Strings and objects
        [InlineData(typeof(GenericClassWithNoInterfaces1<object>), typeof(GenericSubSubClassWithNoInterfaces1<object>), true)]
        [InlineData(typeof(GenericSubClassWithNoInterfaces1<string>), typeof(GenericSubSubClassWithNoInterfaces1<string>), true)]
        [InlineData(typeof(GenericSubClassWithNoInterfaces1<string>), typeof(GenericSubClassWithNoInterfaces1<string>), true)]
        [InlineData(typeof(GenericSubClassWithNoInterfaces1<string>), typeof(GenericSubClassWithNoInterfaces1<object>), false)]
        [InlineData(typeof(GenericSubClassWithNoInterfaces1<object>), typeof(GenericSubClassWithNoInterfaces1<string>), false)]
        [InlineData(typeof(GenericSubSubClassWithNoInterfaces1<object>), typeof(GenericSubClassWithNoInterfaces1<object>), false)]
        [InlineData(typeof(GenericSubClassWithNoInterfaces1<string>), typeof(GenericClassWithNoInterfaces1<string>), false)]
        // Interfaces
        [InlineData(typeof(TI_NonGenericInterface2), typeof(TI_NonGenericInterface2), true)]
        [InlineData(typeof(TI_NonGenericInterface2), typeof(BaseClassWithInterface1Interface2), true)]
        [InlineData(typeof(TI_NonGenericInterface2), typeof(SubClassWithInterface1Interface2), true)]
        [InlineData(typeof(TI_NonGenericInterface2), typeof(GenericSubClassWithInterface1Interface2<>), true)]
        [InlineData(typeof(TI_NonGenericInterface2), typeof(GenericSubClassWithInterface1Interface2<string>), true)]
        [InlineData(typeof(SubClassWithInterface1Interface2), typeof(TI_NonGenericInterface1), false)]
        // Namespaces
        [InlineData(typeof(InnerNamespace.AbstractBaseClass), typeof(InnerNamespace.AbstractSubClass), true)]
        [InlineData(typeof(InnerNamespace.AbstractBaseClass), typeof(InnerNamespace.AbstractSubSubClass), true)]
        [InlineData(typeof(InnerNamespace.AbstractSubClass), typeof(InnerNamespace.AbstractSubSubClass), true)]
        // T[] is assignable to IList<U> iff T[] is assignable to U[]
        [InlineData(typeof(TI_NonGenericInterface1[]), typeof(NonGenericStructWithNonGenericInterface[]), false)]
        [InlineData(typeof(TI_NonGenericInterface1[]), typeof(SubClassWithInterface1Interface2[]), true)]
        [InlineData(typeof(IList<TI_NonGenericInterface1>), typeof(NonGenericStructWithNonGenericInterface[]), false)]
        [InlineData(typeof(IList<TI_NonGenericInterface1>), typeof(SubClassWithInterface1Interface2[]), true)]
        [InlineData(typeof(int[]), typeof(uint[]), true)]
        [InlineData(typeof(uint[]), typeof(int[]), true)]
        [InlineData(typeof(IList<int>), typeof(int[]), true)]
        [InlineData(typeof(IList<uint>), typeof(int[]), true)]
        public void IsAssignable(Type type, Type c, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsAssignableFrom(c));
            Assert.Equal(expected, type.GetTypeInfo().IsAssignableFrom(c?.GetTypeInfo()));

            Assert.Equal(expected, c?.IsAssignableTo(type) ?? false);
            Assert.Equal(expected, c?.GetTypeInfo().IsAssignableTo(type.GetTypeInfo()) ?? false);
        }

        class G<T, U> where T : U
        {
        }

        static volatile object s_boxedInt32;

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/67568", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void IsAssignableNullable()
        {
            Type nubInt = typeof(Nullable<int>);
            Type intType = typeof(int);
            Type objType = typeof(object);
            Type valTypeType = typeof(ValueType);

            // sanity checks
            // Nullable<T>  is assignable from  int
            Assert.True(nubInt.IsAssignableFrom(intType));
            Assert.False(intType.IsAssignableFrom(nubInt));
            Assert.False(nubInt.IsAssignableTo(intType));
            Assert.True(intType.IsAssignableTo(nubInt));

            Type nubOfT = nubInt.GetGenericTypeDefinition();
            Type T = nubOfT.GetTypeInfo().GenericTypeParameters[0];

            // should be true
            Assert.True(T.IsAssignableFrom(T));
            Assert.True(objType.IsAssignableFrom(T));
            Assert.True(valTypeType.IsAssignableFrom(T));

            Assert.True(T.IsAssignableTo(T));
            Assert.True(T.IsAssignableTo(objType));
            Assert.True(T.IsAssignableTo(valTypeType));

            // should be false
            // Nullable<T> is not assignable from T
            Assert.False(nubOfT.IsAssignableFrom(T));
            Assert.False(T.IsAssignableFrom(nubOfT));

            Assert.False(nubOfT.IsAssignableTo(T));
            Assert.False(T.IsAssignableTo(nubOfT));

            // illegal type construction due to T->T?
            Assert.Throws<ArgumentException>(() => typeof(G<,>).MakeGenericType(typeof(int), typeof(int?)));

            // Test trivial object casts
            s_boxedInt32 = (object)1234;
            Assert.True((s_boxedInt32 is int?) && (int?)s_boxedInt32 == 1234);

            // test construction again to catch caching issues
            Assert.Throws<ArgumentException>(() => typeof(G<,>).MakeGenericType(typeof(int), typeof(int?)));
        }

        interface IFace
        {
        }

        class G<T> where T : class, IFace
        {
            //void OpenGenericArrays()
            //{
            //    // this is valid, reflection checks below should agree
            //    IFace[] arr2 = default(T[]);
            //    IEnumerable<IFace> ie = default(T[]);
            //}
        }

        class GG<T, U> where T : class, U
        {
            //void OpenGenericArrays()
            //{
            //    // this is valid, reflection checks below should agree
            //    U[] arr2 = default(T[]);
            //    IEnumerable<U> ie = default(T[]);
            //}
        }

        [Fact]
        public void OpenGenericArrays()
        {
            Type a = typeof(G<>).GetGenericArguments()[0].MakeArrayType();
            Assert.True(typeof(IFace[]).IsAssignableFrom(a));
            Assert.True(typeof(IEnumerable<IFace>).IsAssignableFrom(a));

            Assert.True(a.IsAssignableTo(typeof(IFace[])));
            Assert.True(a.IsAssignableTo(typeof(IEnumerable<IFace>)));

            Type a1 = typeof(GG<,>).GetGenericArguments()[0].MakeArrayType();
            Type a2 = typeof(GG<,>).GetGenericArguments()[1].MakeArrayType();
            Assert.True(a2.IsAssignableFrom(a1));
            Assert.True(a1.IsAssignableTo(a2));

            Type ie = typeof(IEnumerable<>).MakeGenericType(typeof(GG<,>).GetGenericArguments()[1]);
            Assert.True(ie.IsAssignableFrom(a1));
            Assert.True(a1.IsAssignableTo(ie));
        }

        public static IEnumerable<object[]> IsEquivalentTo_TestData()
        {
            yield return new object[] { typeof(string), typeof(string), true };
            yield return new object[] { typeof(object), typeof(string), false };
            object o = "stringAsObject";
            string s = "stringAsString";
            yield return new object[] { o.GetType(), s.GetType(), true };
            yield return new object[] { typeof(ClassWithNoInterfaces), typeof(ClassWithNoInterfaces), true };
        }

        [Theory]
        [MemberData(nameof(IsEquivalentTo_TestData))]
        public void IsEquivalentTo(Type type, Type other, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsEquivalentTo(other));
        }

        [Theory]
        [InlineData(BindingFlags.Default, new int[] { 0, 2 })]
        [InlineData(BindingFlags.Public | BindingFlags.Instance, new int[] { 0, 2 })]
        [InlineData(BindingFlags.NonPublic | BindingFlags.Instance, new int[] { 1 })]
        public void GetConstructors(BindingFlags bindingAttributes, int[] constructorParameterCounts)
        {
            TypeInfo typeInfo = typeof(MembersClass).GetTypeInfo();
            if (bindingAttributes == BindingFlags.Default)
            {
                Assert.Equal(constructorParameterCounts, typeInfo.GetConstructors().Select(constructor => constructor.GetParameters().Length));
            }
            else
            {
                Assert.Equal(constructorParameterCounts, typeInfo.GetConstructors(bindingAttributes).Select(constructor => constructor.GetParameters().Length));
            }
        }

        public static IEnumerable<object[]> GetConstructor_TestData()
        {
            yield return new object[] { new Type[0], 0 };
            yield return new object[] { new Type[] { typeof(int), typeof(string) }, 2 };
            yield return new object[] { new Type[0], 0 };
            yield return new object[] { new Type[] { typeof(string), typeof(int) }, null };
            yield return new object[] { new Type[] { typeof(string) }, null };
        }

        [Theory]
        [MemberData(nameof(GetConstructor_TestData))]
        public void GetConstructor(Type[] types, int? expected)
        {
            ConstructorInfo constructor = typeof(MembersClass).GetTypeInfo().GetConstructor(types);
            Assert.Equal(expected == null, constructor == null);
            Assert.Equal(expected, constructor?.GetParameters().Length);
        }

        [Fact]
        public static void FindMembers()
        {
            MemberInfo[] members = typeof(MembersClass).GetTypeInfo().FindMembers(MemberTypes.All, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(28, members.Length);

            members = typeof(MembersClass).GetTypeInfo().FindMembers(MemberTypes.Constructor, BindingFlags.Public | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(2, members.Length);
            Assert.All(members, m => Assert.Equal(".ctor", m.Name));

            members = typeof(MembersClass).GetTypeInfo().FindMembers(MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(1, members.Length);
            Assert.Equal(1, ((ConstructorInfo)members[0]).GetParameters().Length);

            members = typeof(MembersClass).GetTypeInfo().FindMembers(MemberTypes.Constructor, BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => ((ConstructorInfo)memberInfo).GetParameters().Length >= Convert.ToInt32(c), "1");
            Assert.Equal(1, members.Length);
            Assert.Equal(".ctor", members[0].Name);

            members = typeof(MembersClass).GetTypeInfo().FindMembers(MemberTypes.Event, BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(1, members.Length);

            members = typeof(MembersClass).GetTypeInfo().FindMembers(MemberTypes.Event, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => memberInfo.Name.Contains(c.ToString()), "Event");
            Assert.Equal(2, members.Length);
            Assert.All(members, m => Assert.Contains("Event", m.Name));

            members = typeof(MembersClass).GetTypeInfo().FindMembers(MemberTypes.Property, BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(1, members.Length);
            Assert.Equal("PrivateProp", members[0].Name);

            members = typeof(MembersClass).GetTypeInfo().FindMembers(MemberTypes.Property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => memberInfo.Name.Contains(c.ToString()), "Prop");
            Assert.Equal(2, members.Length);
            Assert.All(members, m => Assert.Contains("Prop", m.Name));

            members = typeof(MembersClass).GetTypeInfo().FindMembers(MemberTypes.Method, BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(7, members.Length);

            members = typeof(MembersClass).GetTypeInfo().FindMembers(MemberTypes.Method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => memberInfo.Name.Contains(c.ToString()), "get");
            Assert.Equal(2, members.Length);
            Assert.All(members, m => Assert.Contains("Prop", m.Name));
            Assert.All(members, m => Assert.Contains("get_", m.Name));

            members = typeof(MembersClass).GetTypeInfo().FindMembers(MemberTypes.NestedType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, (MemberInfo memberInfo, object c) => true, "notused");
            Assert.Equal(1, members.Length);
            Assert.Contains("EventHandler", members[0].Name);
        }

        [Theory]
        [InlineData(BindingFlags.Default, 1)]
        [InlineData(BindingFlags.Public | BindingFlags.Instance, 1)]
        [InlineData(BindingFlags.NonPublic | BindingFlags.Instance, 1)]
        [InlineData(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, 2)]
        public void GetProperties(BindingFlags bindingAttributes, int expected)
        {
            TypeInfo typeInfo = typeof(MembersClass).GetTypeInfo();
            if (bindingAttributes == BindingFlags.Default)
            {
                Assert.Equal(expected, typeInfo.GetProperties().Length);
            }
            else
            {
                Assert.Equal(expected, typeInfo.GetProperties(bindingAttributes).Length);
            }
        }

        [Fact]
        public void GetProperty()
        {
            PropertyInfo prop = typeof(MembersClass).GetTypeInfo().GetProperty(nameof(MembersClass.PublicProp));
            Assert.NotNull(prop);

            prop = typeof(MembersClass).GetTypeInfo().GetProperty(nameof(MembersClass.PublicProp), typeof(int));
            Assert.NotNull(prop);

            prop = typeof(MembersClass).GetTypeInfo().GetProperty(nameof(MembersClass.PublicProp), Type.EmptyTypes);
            Assert.NotNull(prop);

            prop = typeof(MembersClass).GetTypeInfo().GetProperty(nameof(MembersClass.PublicProp), typeof(int), Type.EmptyTypes);
            Assert.NotNull(prop);

            prop = typeof(MembersClass).GetTypeInfo().GetProperty(nameof(MembersClass.PublicProp), typeof(int), Type.EmptyTypes, null);
            Assert.NotNull(prop);
        }

        [Fact]
        public void GetMethod()
        {
            MethodInfo[] methods = typeof(MembersClass).GetTypeInfo().GetMethods();
            Assert.Equal(9, methods.Length);

            methods = typeof(MembersClass).GetTypeInfo().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.Equal(16, methods.Length);
        }

        [Fact]
        public void GetMethod_Invalid()
        {
            Assert.Throws<ArgumentNullException>(() => typeof(MembersClass).GetTypeInfo().GetMethod(null));
            Assert.Throws<ArgumentNullException>(() => typeof(MembersClass).GetTypeInfo().GetMethod("p", null));
            Assert.Throws<ArgumentNullException>(() => typeof(MembersClass).GetTypeInfo().GetMethod("p", new Type[] { typeof(int), null }));
        }

        [Theory]
        [InlineData(BindingFlags.Default, 9)]
        [InlineData(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, 16)]
        public void GetMethods(BindingFlags bindingAttributes, int length)
        {
            TypeInfo typeInfo = typeof(MembersClass).GetTypeInfo();
            if (bindingAttributes == BindingFlags.Default)
            {
                Assert.Equal(length, typeInfo.GetMethods().Length);
            }
            else
            {
                Assert.Equal(length, typeInfo.GetMethods(bindingAttributes).Length);
            }
        }

        [Theory]
        [InlineData(nameof(MembersClass.EventHandler), BindingFlags.Default, true)]
        [InlineData(nameof(MembersClass.EventHandler), BindingFlags.Public | BindingFlags.Instance, true)]
        public void GetNestedType(string name, BindingFlags bindingAttributes, bool exists)
        {
            TypeInfo typeInfo = typeof(MembersClass).GetTypeInfo();
            if (bindingAttributes == BindingFlags.Default)
            {
                Assert.Equal(exists, typeInfo.GetNestedType(name) != null);
            }
            else
            {
                Assert.Equal(exists, typeInfo.GetNestedType(name, bindingAttributes) != null);
            }
        }

        [Theory]
        [InlineData(BindingFlags.Default, 1)]
        [InlineData(BindingFlags.Public | BindingFlags.Instance, 1)]
        public void GetNestedTypes(BindingFlags bindingAttributes, int length)
        {
            TypeInfo typeInfo = typeof(MembersClass).GetTypeInfo();
            if (bindingAttributes == BindingFlags.Default)
            {
                Assert.Equal(length, typeInfo.GetNestedTypes().Length);
            }
            else
            {
                Assert.Equal(length, typeInfo.GetNestedTypes(bindingAttributes).Length);
            }
        }

        [Theory]
        [InlineData("Public*", BindingFlags.Default, 4)]
        [InlineData("EventHandler", BindingFlags.Default, 1)]
        [InlineData("P*", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, 10)]
        [InlineData(".ctor", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, 3)]
        public void GetMember(string name, BindingFlags bindingAttributes, int length)
        {
            TypeInfo typeInfo = typeof(MembersClass).GetTypeInfo();
            if (bindingAttributes == BindingFlags.Default)
            {
                Assert.Equal(length, typeInfo.GetMember(name).Length);
            }
            else
            {
                Assert.Equal(length, typeInfo.GetMember(name, bindingAttributes).Length);
            }
        }

        [Theory]
        [InlineData(BindingFlags.Default, 15)]
        [InlineData(BindingFlags.NonPublic | BindingFlags.Instance, 13)]
        [InlineData(BindingFlags.Public | BindingFlags.Instance, 15)]
        public void GetMembers(BindingFlags bindingAttributes, int length)
        {
            TypeInfo typeInfo = typeof(MembersClass).GetTypeInfo();
            if (bindingAttributes == BindingFlags.Default)
            {
                Assert.Equal(length, typeInfo.GetMembers().Length);
            }
            else
            {
                Assert.Equal(length, typeInfo.GetMembers(bindingAttributes).Length);
            }
        }

        [Theory]
        [InlineData(typeof(MembersClass), new Type[] { typeof(TI_NonGenericInterface1), typeof(TI_NonGenericInterface2) })]
        [InlineData(typeof(TI_NonGenericInterface2), new Type[0])]
        public void GetInterfaces(Type type, Type[] expected)
        {
            Assert.Equal(expected.OrderBy(t => t.Name), type.GetTypeInfo().GetInterfaces().OrderBy(t => t.Name));
        }

        [SkipOnMono("Mono returns a RuntimeType[] rather than a Type[]. https://github.com/dotnet/runtime/pull/96589")]
        [Fact]
        public void GetInterfaces_InvariantArrayType()
        {
            Assert.Equal(typeof(Type[]), typeof(int[]).GetInterfaces().GetType());
            Assert.Equal(typeof(Type[]), typeof(int[]).GetTypeInfo().GetInterfaces().GetType());
        }

        [Theory]
        [InlineData(typeof(List<>), new string[] { "T" })]
        [InlineData(typeof(Dictionary<,>), new string[] { "TKey", "TValue" })]
        [InlineData(typeof(GenericClassWithNoInterfaces2<,>), new string[] { "T", "V" })]
        [InlineData(typeof(GenericClassWithNoInterfaces2<int, string>), new string[] { "Int32", "String" })]
        public void GetGenericArguments(Type type, string[] expectedNames)
        {
            Type[] genericArguments = type.GetTypeInfo().GetGenericArguments();
            Assert.Equal(expectedNames.Length, genericArguments.Length);
            Assert.Equal(expectedNames, genericArguments.Select(genericArgument => genericArgument.Name));
        }

        [Theory]
        [InlineData(nameof(MembersClass.PublicEvent), BindingFlags.Default, true)]
        [InlineData("PrivateEvent", BindingFlags.Default, false)]
        [InlineData("PrivateEvent", BindingFlags.NonPublic | BindingFlags.Instance, true)]
        public void GetEvent(string name, BindingFlags bindingAttributes, bool exists)
        {
            TypeInfo typeInfo = typeof(MembersClass).GetTypeInfo();
            if (bindingAttributes == BindingFlags.Default)
            {
                Assert.Equal(exists, typeInfo.GetEvent(name) != null);
            }
            else
            {
                Assert.Equal(exists, typeInfo.GetEvent(name, bindingAttributes) != null);
            }
        }

        [Fact]
        public void GetEvent_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => typeof(MembersClass).GetTypeInfo().GetEvent(null));
        }

        [Theory]
        [InlineData(BindingFlags.Default, new string[] { "PublicEvent" })]
        [InlineData(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, new string[] { "PublicEvent", "PrivateEvent" })]
        [InlineData(BindingFlags.NonPublic | BindingFlags.Instance, new string[] { "PrivateEvent" })]
        public void GetEvents(BindingFlags bindingAttributes, string[] expectedNames)
        {
            TypeInfo typeInfo = typeof(MembersClass).GetTypeInfo();
            if (bindingAttributes == BindingFlags.Default)
            {
                Assert.Equal(expectedNames.OrderBy(e => e), typeInfo.GetEvents().Select(eventInfo => eventInfo.Name).OrderBy(e => e));
            }
            else
            {
                Assert.Equal(expectedNames.OrderBy(e => e), typeInfo.GetEvents(bindingAttributes).Select(eventInfo => eventInfo.Name).OrderBy(e => e));
            }
        }

        [Theory]
        [InlineData(nameof(MembersClass.PublicField), BindingFlags.Default, true)]
        [InlineData("PrivateField", BindingFlags.Default, false)]
        [InlineData("PrivateField", BindingFlags.NonPublic | BindingFlags.Instance, true)]
        public void GetField(string name, BindingFlags bindingAttributes, bool exists)
        {
            TypeInfo typeInfo = typeof(MembersClass).GetTypeInfo();
            if (bindingAttributes == BindingFlags.Default)
            {
                Assert.Equal(exists, typeInfo.GetField(name) != null);
            }
            else
            {
                Assert.Equal(exists, typeInfo.GetField(name, bindingAttributes) != null);
            }
        }

        [Fact]
        public void GetField_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => typeof(MembersClass).GetTypeInfo().GetField(null));
        }

        [Theory]
        [InlineData(BindingFlags.Default, new string[] { "PublicField" })]
        [InlineData(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, new string[] { "PublicField", "PrivateField", "PublicEvent", "PrivateEvent" })]
        [InlineData(BindingFlags.NonPublic | BindingFlags.Instance, new string[] { "PrivateField", "PublicEvent", "PrivateEvent" })]
        public void GetFields(BindingFlags bindingAttributes, string[] expectedNames)
        {
            TypeInfo typeInfo = typeof(MembersClass).GetTypeInfo();
            if (bindingAttributes == BindingFlags.Default)
            {
                Assert.Equal(expectedNames.OrderBy(f => f), typeInfo.GetFields().Select(field => field.Name).OrderBy(f => f));
            }
            else
            {
                Assert.Equal(expectedNames.OrderBy(f => f), typeInfo.GetFields(bindingAttributes).Select(field => field.Name).OrderBy(f => f));
            }
        }

        [Theory]
        [InlineData(typeof(MembersClass), new string[] { "PublicField" })]
        [InlineData(typeof(TI_NonGenericInterface1), new string[0])]
        [InlineData(typeof(MembersClass.EventHandler), new string[0])]
        public void GetDefaultMembers(Type type, string[] expectedNames)
        {
            Assert.Equal(expectedNames.OrderBy(m => m), type.GetTypeInfo().GetDefaultMembers().Select(member => member.Name).OrderBy(m => m));
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass))]
        [InlineData(typeof(TI_BaseClass.PublicNestedClass1))]
        public void AsType(Type type)
        {
            Assert.Equal(type, type.GetTypeInfo().AsType());
        }

        public static IEnumerable<object[]> GetArrayRank_TestData()
        {
            yield return new object[] { new int[] { 1, 2, 3, 4, 5, 6, 7 }, 1 };
            yield return new object[] { new string[] { "hello" }, 1 };
        }

        [Theory]
        [MemberData(nameof(GetArrayRank_TestData))]
        public void GetArrayRank(Array array, int expected)
        {
            Assert.Equal(expected, array.GetType().GetTypeInfo().GetArrayRank());
        }

        [Fact]
        public void GetDeclaredEvent_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => typeof(TI_BaseClass).GetTypeInfo().GetDeclaredEvent(null));
        }

        [Fact]
        public void GetDeclaredField_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => typeof(TI_BaseClass).GetTypeInfo().GetDeclaredField(null));
        }

        [Fact]
        public void GetDeclaredMethod_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => typeof(TI_BaseClass).GetTypeInfo().GetDeclaredMethod(null));
        }

        [Theory]
        [InlineData(nameof(TI_BaseClass.MethodWithSameName), 4)]
        [InlineData("NoSuchMethod", 0)]
        public void GetDeclaredMethods(string name, int count)
        {
            IEnumerable<MethodInfo> methods = typeof(TI_BaseClass).GetTypeInfo().GetDeclaredMethods(name);
            Assert.Equal(count, methods.Count());
            Assert.All(methods, method => method.Name.Equals(name));
        }

        [Fact]
        public void GetDeclaredNestedType_NullName_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => typeof(TI_BaseClass).GetTypeInfo().GetDeclaredNestedType(null));
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), null)]
        [InlineData(typeof(string[]), typeof(string))]
        [InlineData(typeof(int[]), typeof(int))]
        public void GetElementType(Type type, Type expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().GetElementType());
        }

        [Fact]
        public void GenericParameterConstraints()
        {
            Type[] genericTypeParameters = typeof(MethodClassWithConstraints<,>).GetTypeInfo().GenericTypeParameters;
            Assert.Equal(2, genericTypeParameters.Length);

            Assert.Equal(new Type[] { typeof(TI_BaseClass), typeof(TI_NonGenericInterface1) }, genericTypeParameters[0].GetTypeInfo().GetGenericParameterConstraints());
            Assert.Empty(genericTypeParameters[1].GetTypeInfo().GetGenericParameterConstraints());
        }

        [Theory]
        [InlineData(typeof(GenericClassWithNoInterfaces1<int>), typeof(GenericClassWithNoInterfaces1<>))]
        public void GetGenericTypeDefinition(Type type, Type expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().GetGenericTypeDefinition());
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), typeof(TI_SubClass), false)]
        [InlineData(typeof(TI_SubClass), typeof(TI_BaseClass), true)]
        public void IsSubClassOf(Type type, Type c, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsSubclassOf(c));
        }

        [Theory]
        [InlineData(typeof(string), typeof(string[]))]
        [InlineData(typeof(int), typeof(int[]))]
        public void MakeArrayType(Type type, Type expected)
        {
            Type arrayType = type.GetTypeInfo().MakeArrayType();
            Assert.Equal(expected, arrayType);
            Assert.True(arrayType.IsArray);
            Assert.Equal(1, arrayType.GetArrayRank());
            Assert.Equal(type, arrayType.GetElementType());
        }

        [Theory]
        [InlineData(typeof(int), 2)]
        [InlineData(typeof(char*), 3)]
        [InlineData(typeof(int), 3)]
        public void MakeArrayType_Int(Type type, int rank)
        {
            Type arrayType = type.GetType().MakeArrayType(rank);
            Assert.True(arrayType.IsArray);
            Assert.Equal(rank, arrayType.GetArrayRank());
        }

        [Theory]
        [InlineData(typeof(string))]
        public void MakeArrayType_IntRank1(Type type)
        {
            Type arrayType = type.GetType().MakeArrayType(1);
            Assert.True(arrayType.IsArray);
            Assert.Equal(1, arrayType.GetArrayRank());
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        public void MakeByRefType(Type type)
        {
            Type byRefType = type.GetTypeInfo().MakeByRefType();
            Assert.True(byRefType.IsByRef);
        }

        [Fact]
        public void MakeByRefType_TypeAlreadyByRef_ThrowsTypeLoadException()
        {
            Type byRefType = typeof(string).GetTypeInfo().MakeByRefType();
            Assert.Throws<TypeLoadException>(() => byRefType.GetTypeInfo().MakeByRefType());
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        public void MakePointerType(Type type)
        {
            Type pointerType = type.GetTypeInfo().MakePointerType();
            Assert.True(pointerType.IsPointer);
        }

        public static IEnumerable<object[]> ByRefPonterTypes_IsPublicIsVisible_TestData()
        {
            yield return new object[] { typeof(int).MakeByRefType(), true, true };
            yield return new object[] { typeof(int).MakePointerType(), true, true };
            yield return new object[] { typeof(List<int>), true, true };
            yield return new object[] { typeof(int), true, true };
            yield return new object[] { typeof(TI_BaseClass.InternalNestedClass).MakeByRefType(), true, false };
            yield return new object[] { typeof(TI_BaseClass.InternalNestedClass).MakePointerType(), true, false };
            yield return new object[] { typeof(List<TI_BaseClass.InternalNestedClass>), true, false };
            yield return new object[] { typeof(TI_BaseClass.InternalNestedClass), false, false };
            yield return new object[] { typeof(TI_BaseClass).MakeByRefType(), true, true };
            yield return new object[] { typeof(TI_BaseClass).MakePointerType(), true, true };
            yield return new object[] { typeof(List<TI_BaseClass>), true, true };
        }

        [Theory]
        [MemberData(nameof(ByRefPonterTypes_IsPublicIsVisible_TestData))]
        public void ByRefPonterTypesTypes_IsPublicIsVisible(Type type, bool isPublic, bool isVisible)
        {
            Assert.Equal(isPublic, type.IsPublic);
            Assert.Equal(!isPublic, type.IsNestedAssembly);
            Assert.Equal(isVisible, type.IsVisible);
        }

        public delegate void PublicDelegate(string str);
        internal delegate void InternalDelegate(string str);

        [Fact]
        public void DelegateTypesIsPublic()
        {
            Assert.True(typeof(Delegate).IsPublic);
            Assert.False(typeof(Delegate).IsNotPublic);
            Assert.True(typeof(PublicDelegate).IsNestedPublic);
            Assert.True(typeof(InternalDelegate).IsNestedAssembly);
            Assert.False(typeof(InternalDelegate).IsPublic);
            Assert.False(typeof(InternalDelegate).MakePointerType().IsNestedAssembly);
            Assert.True(typeof(InternalDelegate).MakePointerType().IsPublic);
            Assert.True(typeof(Delegate).MakePointerType().IsPublic);
            Assert.False(typeof(Delegate).MakePointerType().IsNotPublic);
        }

        [Fact]
        public void FunctionPointerTypeIsPublic()
        {
            Assert.True(typeof(delegate*<string, int>).IsPublic);
            Assert.True(typeof(delegate*<string, int>).MakePointerType().IsPublic);
        }

        [Fact]
        public void MakePointerType_TypeAlreadyByRef_ThrowsTypeLoadException()
        {
            Type byRefType = typeof(string).GetTypeInfo().MakeByRefType();
            Assert.Throws<TypeLoadException>(() => byRefType.MakePointerType());
        }

        [Theory]
        [InlineData(typeof(List<>), new Type[] { typeof(string) })]
        public void MakeGenericType(Type type, Type[] typeArguments)
        {
            Type genericType = type.GetTypeInfo().MakeGenericType(typeArguments);
            Assert.True(genericType.GetTypeInfo().IsGenericType);
        }

        [Theory]
        [InlineData(typeof(string), "System.String")]
        [InlineData(typeof(int), "System.Int32")]
        public void ToStringTest(Type type, string expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().ToString());
        }

        [Theory]
        [InlineData(typeof(TI_SubClass), typeof(TI_BaseClass))]
        [InlineData(typeof(TI_BaseClass), typeof(object))]
        public void BaseType(Type type, Type expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().BaseType);
        }

        [Theory]
        [InlineData(typeof(MethodClassWithConstraints<,>), true)]
        [InlineData(typeof(TI_BaseClass), false)]
        public void ContainsGenericParameter(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().ContainsGenericParameters);
        }

        [Fact]
        public void FullName()
        {
            Assert.Equal("System.Int32", typeof(int).GetTypeInfo().FullName);

            Type t = typeof(TI_FullNameTest<>);

            // a generic type parameter
            Assert.Null(t.GetMethod("TypeParam").ReturnType.GetTypeInfo().FullName);

            // an array type, pointer type, or byref type based on a type parameter
            Assert.Null(t.GetMethod("ArrayTypeParam").ReturnType.GetTypeInfo().FullName);
            Assert.Null(t.GetMethod("PointerTypeParam").ReturnType.GetTypeInfo().FullName);
            Assert.Null(t.GetMethod("ByRefTypeParam").ReturnType.GetTypeInfo().FullName);

            // a generic type that is not a generic type definition but contains unresolved type parameters
            Assert.Null(t.GetMethod("ListTypeParam").ReturnType.GetTypeInfo().FullName);

            t = typeof(TI_FullNameTest<int>);

            Assert.Equal("System.Int32", t.GetMethod("TypeParam").ReturnType.GetTypeInfo().FullName);

            Assert.Equal("System.Int32[]", t.GetMethod("ArrayTypeParam").ReturnType.GetTypeInfo().FullName);
            Assert.Equal("System.Int32*", t.GetMethod("PointerTypeParam").ReturnType.GetTypeInfo().FullName);
            Assert.Equal("System.Int32&", t.GetMethod("ByRefTypeParam").ReturnType.GetTypeInfo().FullName);

            Assert.NotNull(t.GetMethod("ListTypeParam").ReturnType.GetTypeInfo().FullName);
        }

        [Fact]
        public void Guid()
        {
            Assert.Equal(new Guid("FD80F123-BEDD-4492-B50A-5D46AE94DD4E"), typeof(TypeInfoTests).GetTypeInfo().GUID);
            Assert.Equal(System.Guid.Empty, typeof(int[]).GetTypeInfo().GUID);
        }

        [Theory]
        [InlineData(typeof(int), false)]
        [InlineData(typeof(int[]), true)]
        public void HasElementType(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().HasElementType);
        }

        [Theory]
        [InlineData(typeof(AbstractClass), true)]
        [InlineData(typeof(TI_BaseClass), false)]
        public void IsAbstract(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsAbstract);
        }

        [Theory]
        [InlineData(typeof(string), true)]
        public void IsAnsiClass(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsAnsiClass);
        }

        [Theory]
        [InlineData(typeof(int[]), true)]
        [InlineData(typeof(int), false)]
        public void IsArray(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsArray);
        }

        public static IEnumerable<object[]> IsByRefType_TestData()
        {
            yield return new object[] { typeof(int).GetTypeInfo().MakeByRefType(), true };
            yield return new object[] { typeof(int), false };
        }

        [Theory]
        [MemberData(nameof(IsByRefType_TestData))]
        public void IsByRefType(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsByRef);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), true)]
        [InlineData(typeof(PublicEnum), false)]
        public void IsClass(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsClass);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), false)]
        [InlineData(typeof(PublicEnum), true)]
        public void IsEnum(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsEnum);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), false)]
        [InlineData(typeof(TI_NonGenericInterface1), true)]
        public void IsInterface(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsInterface);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), false)]
        [InlineData(typeof(TI_BaseClass.PublicNestedClass1), true)]
        public void IsNested(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsNested);
        }

        [Theory]
        [InlineData(typeof(int*), true)]
        [InlineData(typeof(int), false)]
        public void IsPointer(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsPointer);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), false)]
        [InlineData(typeof(int), true)]
        [InlineData(typeof(char), true)]
        public void IsPrimitive(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsPrimitive);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), true)]
        [InlineData(typeof(TI_SubClass), true)]
        [InlineData(typeof(TI_NonGenericInterface1), true)]
        [InlineData(typeof(TI_ClassWithInterface1), true)]
        public void IsPublic(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsPublic);
            Assert.Equal(!expected, type.GetTypeInfo().IsNotPublic);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass.PublicNestedClass1), true)]
        public void IsNestedPublic(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsNestedPublic);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass.PublicNestedClass1), false)]
        public void IsNestedPrivate(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsNestedPrivate);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), false)]
        [InlineData(typeof(SealedClass), true)]
        public void IsSealed(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsSealed);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), false)]
        public void IsSerializable(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsSerializable);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), false)]
        [InlineData(typeof(PublicEnum), true)]
        public void IsValueType(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsValueType);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), true)]
        [InlineData(typeof(TI_NonGenericInterface1), true)]
        [InlineData(typeof(PublicEnum), true)]
        [InlineData(typeof(TI_BaseClass.PublicNestedClass1), true)]
        public void IsVisible(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsVisible);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), "System.Reflection.Tests")]
        [InlineData(typeof(TI_NonGenericInterface1), "System.Reflection.Tests")]
        [InlineData(typeof(PublicEnum), "System.Reflection.Tests")]
        [InlineData(typeof(TI_BaseClass.PublicNestedClass1), "System.Reflection.Tests")]
        [InlineData(typeof(int), "System")]
        [InlineData(typeof(TI_BaseClass[]), "System.Reflection.Tests")]
        [InlineData(typeof(TI_BaseClass.PublicNestedClass1[]), "System.Reflection.Tests")]
        [InlineData(typeof(TI_BaseClass.PublicNestedClass1[][]), "System.Reflection.Tests")]
        [InlineData(typeof(TI_Struct*), "System.Reflection.Tests")]
        public void Namespace(Type type, string expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().Namespace);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), false)]
        [InlineData(typeof(PublicEnum), false)]
        public void IsImport(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsImport);
        }

        public static IEnumerable<object[]> IsUnicodeClass_TestData()
        {
            yield return new object[] { typeof(string), false };
            yield return new object[] { typeof(string).MakeByRefType(), false };
        }

        [Theory]
        [MemberData(nameof(IsUnicodeClass_TestData))]
        public void IsUnicodeClass(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsUnicodeClass);
        }

        public static IEnumerable<object[]> IsAutoClass_TestData()
        {
            yield return new object[] { typeof(string), false };
            yield return new object[] { typeof(string).MakeByRefType(), false };
        }

        [Theory]
        [MemberData(nameof(IsAutoClass_TestData))]
        public void IsAutoClass(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsAutoClass);
        }

        public static IEnumerable<object[]> IsMarshalByRef_TestData()
        {
            yield return new object[] { typeof(string), false };
            yield return new object[] { typeof(string).MakeByRefType(), false };
            yield return new object[] { typeof(string).MakePointerType(), false };
        }

        [Theory]
        [MemberData(nameof(IsMarshalByRef_TestData))]
        public void IsMarshalByRef(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsMarshalByRef);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), false)]
        public void IsNestedAssembly(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsNestedAssembly);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), false)]
        public void IsNestedFamily(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsNestedFamily);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), false)]
        public void IsNestedFamANDAssem(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsNestedFamANDAssem);
        }

        [Theory]
        [InlineData(typeof(TI_BaseClass), false)]
        public void IsNestedFamORAssem(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsNestedFamORAssem);
        }

        [Theory]
        [InlineData(typeof(StructWithoutExplicitStructLayout), LayoutKind.Sequential, CharSet.Ansi, 0)]
        [InlineData(typeof(StructWithExplicitStructLayout), LayoutKind.Explicit, CharSet.Ansi, 1)]
        [InlineData(typeof(ClassWithoutExplicitStructLayout), LayoutKind.Auto, CharSet.Ansi, 0)]
        [InlineData(typeof(ClassWithExplicitStructLayout), LayoutKind.Explicit, CharSet.Unicode, 2)]
        public void StructLayoutAttribute(Type type, LayoutKind kind, CharSet charset, int pack)
        {
            StructLayoutAttribute layour = type.GetTypeInfo().StructLayoutAttribute;
            Assert.Equal(layour.Value, kind);
            Assert.Equal(layour.CharSet, charset);
            Assert.Equal(layour.Pack, pack);
        }

        [Fact]
        public static void TypeInitializer()
        {
            ConstructorInfo constructorInfo = typeof(ClassWithStaticConstructor).GetTypeInfo().TypeInitializer;
            Assert.Equal(".cctor", constructorInfo.Name);

            constructorInfo = typeof(ClassWithNoInterfaces).GetTypeInfo().TypeInitializer;
            Assert.Null(constructorInfo);
        }

        public static IEnumerable<object[]> UnderlyingSystemType_TestData()
        {
            yield return new object[] { typeof(object), typeof(object) };
            yield return new object[] { typeof(int), typeof(int) };
            yield return new object[] { typeof(ClassWithNoInterfaces), typeof(ClassWithNoInterfaces) };

            Type type = typeof(List<>);
            yield return new object[] { type.MakeGenericType(typeof(object)), typeof(List<object>) };
        }

        [Theory]
        [MemberData(nameof(UnderlyingSystemType_TestData))]
        public static void UnderlyingSystemType(Type type, Type expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().UnderlyingSystemType);
        }

        public static IEnumerable<object[]> SZArrayOrNotTypes()
        {
            yield return new object[] { typeof(int[]), true };
            yield return new object[] { typeof(string[]), true };
            yield return new object[] { typeof(void), false };
            yield return new object[] { typeof(int), false };
            yield return new object[] { typeof(int[]).MakeByRefType(), false };
            yield return new object[] { typeof(int[,]), false };
            yield return new object[] { typeof(TypeInfoTests), false };
            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                yield return new object[] { Array.CreateInstance(typeof(int), new[] { 2 }, new[] { -1 }).GetType(), false };
                yield return new object[] { Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 1 }).GetType(), false };
            }
            yield return new object[] { Array.CreateInstance(typeof(int), new[] { 2 }, new[] { 0 }).GetType(), true };
            yield return new object[] { typeof(int[][]), true };
            yield return new object[] { Type.GetType("System.Int32[]"), true };
            yield return new object[] { Type.GetType("System.Int32[*]"), false };
            yield return new object[] { Type.GetType("System.Int32"), false };
            yield return new object[] { typeof(int).MakeArrayType(), true };
            yield return new object[] { typeof(int).MakeArrayType(1), false };
            yield return new object[] { typeof(int).MakeArrayType().MakeArrayType(), true };
            yield return new object[] { typeof(int).MakeArrayType(2), false };
            yield return new object[] { typeof(OutsideTypeInfoTests<int>.InsideTypeInfoTests<string>), false };
            yield return new object[] { typeof(OutsideTypeInfoTests<int>.InsideTypeInfoTests<string>[]), true };
            yield return new object[] { typeof(OutsideTypeInfoTests<int>.InsideTypeInfoTests<string>[,]), false };
            if (PlatformDetection.IsNonZeroLowerBoundArraySupported)
            {
                yield return new object[] { Array.CreateInstance(typeof(OutsideTypeInfoTests<int>.InsideTypeInfoTests<string>), new[] { 2 }, new[] { -1 }).GetType(), false };
            }
        }

        [Theory, MemberData(nameof(SZArrayOrNotTypes))]
        public void IsSZArray(Type type, bool expected)
        {
            Assert.Equal(expected, type.GetTypeInfo().IsSZArray);
        }

        public static IEnumerable<object[]> GetMemberWithSameMetadataDefinitionAsData()
        {
            yield return new object[] { typeof(TI_GenericTypeWithAllMembers<>), typeof(TI_GenericTypeWithAllMembers<int>), true };
            yield return new object[] { typeof(TI_GenericTypeWithAllMembers<>), typeof(TI_GenericTypeWithAllMembers<ClassWithMultipleConstructors>), true};

            yield return new object[] { typeof(TI_GenericTypeWithAllMembers<>), typeof(TI_TypeDerivedFromGenericTypeWithAllMembers<int>), false };
            yield return new object[] { typeof(TI_GenericTypeWithAllMembers<>), typeof(TI_TypeDerivedFromGenericTypeWithAllMembers<ClassWithMultipleConstructors>), false };

            yield return new object[] { typeof(TI_GenericTypeWithAllMembers<>), typeof(TI_TypeDerivedFromGenericTypeWithAllMembersClosed), false };

            static TypeInfo GetTypeDelegator(Type t) => new TypeDelegator(t);
            yield return new object[] { GetTypeDelegator(typeof(TI_GenericTypeWithAllMembers<>)), typeof(TI_GenericTypeWithAllMembers<ClassWithMultipleConstructors>), true };
            yield return new object[] { typeof(TI_GenericTypeWithAllMembers<>), GetTypeDelegator(typeof(TI_GenericTypeWithAllMembers<ClassWithMultipleConstructors>)), true };
            yield return new object[] { GetTypeDelegator(typeof(TI_GenericTypeWithAllMembers<>)), GetTypeDelegator(typeof(TI_GenericTypeWithAllMembers<ClassWithMultipleConstructors>)), true };

            if (RuntimeFeature.IsDynamicCodeSupported)
            {
                (Type generatedType1, Type generatedType2) = CreateGeneratedTypes();

                yield return new object[] { generatedType1, generatedType2, true };
                yield return new object[] { generatedType2, generatedType1, true };
            }
        }

        private static (Type, Type) CreateGeneratedTypes()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("GetMemberWithSameMetadataDefinitionAsGeneratedAssembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = assembly.DefineDynamicModule("GetMemberWithSameMetadataDefinitionAsGeneratedModule");
            TypeBuilder genericType = module.DefineType("GenericGeneratedType");
            genericType.DefineGenericParameters("T0");
            genericType.DefineField("_int", typeof(int), FieldAttributes.Private);
            genericType.DefineProperty("Prop", PropertyAttributes.None, typeof(string), null);

            Type builtGenericType = genericType.CreateType();
            Type closedType = builtGenericType.MakeGenericType(typeof(int));

            return (genericType, closedType);
        }

        [Theory, MemberData(nameof(GetMemberWithSameMetadataDefinitionAsData))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/69244", typeof(PlatformDetection), nameof(PlatformDetection.IsNativeAot))]
        public void GetMemberWithSameMetadataDefinitionAs(Type openGenericType, Type closedGenericType, bool checkDeclaringType)
        {
            BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            foreach (MemberInfo openGenericMember in openGenericType.GetMembers(all))
            {
                MemberInfo closedGenericMember = closedGenericType.GetMemberWithSameMetadataDefinitionAs(openGenericMember);
                Assert.True(closedGenericMember != null, $"'{openGenericMember.Name}' was not found");
                Assert.True(closedGenericMember.HasSameMetadataDefinitionAs(openGenericMember));
                Assert.Equal(closedGenericMember.Name, openGenericMember.Name);
                if (checkDeclaringType && openGenericMember is not Type)
                {
                    Assert.True(closedGenericMember.DeclaringType.Equals(closedGenericType), $"'{closedGenericMember.Name}' doesn't have the right DeclaringType");
                }
            }

            MemberInfo toString = closedGenericType.GetMemberWithSameMetadataDefinitionAs(typeof(object).GetMethod("ToString"));
            Assert.NotNull(toString);
            Assert.IsAssignableFrom<MethodInfo>(toString);
            Assert.Equal("ToString", toString.Name);

            Assert.Throws<ArgumentNullException>(() => openGenericType.GetMemberWithSameMetadataDefinitionAs(null));
            Assert.Throws<ArgumentException>(() => openGenericType.GetMemberWithSameMetadataDefinitionAs(typeof(string).GetMethod("get_Length")));
        }

#pragma warning disable 0067, 0169
        public static class ClassWithStaticConstructor
        {
            static ClassWithStaticConstructor() { }
        }

        public class ClassWithMultipleConstructors
        {
            static ClassWithMultipleConstructors() { }

            public ClassWithMultipleConstructors() { }
            public ClassWithMultipleConstructors(TimeSpan ts) { }
            public ClassWithMultipleConstructors(object obj1, object obj2) { }
            public ClassWithMultipleConstructors(object obj, int i) { }
        }

        public class MultipleNestedClass
        {
            public class Nest1
            {
                public class Nest2
                {
                    public class Nest3 { }
                }
            }
        }

        public class ClassWithNoInterfaces { }
        public class ClassWithInterface2Interface3 : TI_NonGenericInterface2, TI_NonGenericInterface3 { }
        public class SubClassWithInterface1 : TI_ClassWithInterface1 { }
        public class SubClassWithInterface1Interface2Interface3 : ClassWithInterface2Interface3, TI_NonGenericInterface1 { }

        public interface TI_NonGenericInterface2 { }
        public interface TI_NonGenericInterface3 { }
        public interface GenericInterface1<TI> { }
        public interface GenericInterface2<TI, VI> { }

        public struct NonGenericStructWithNoInterfaces { }
        public struct GenericStructWithNoInterfaces1<TS> { }
        public struct GenericStructWithNoInterfaces2<TS, VS> { }

        public struct NonGenericStructWithNonGenericInterface : TI_NonGenericInterface1 { }
        public struct GenericStructWithGenericInterface1<TS> : GenericInterface1<TS> { }
        public struct GenericStructWithGenericInterface2<TS, VS> : GenericInterface2<TS, VS> { }

        public struct NonGenericStructWithGenericInterface1 : GenericInterface1<int> { }
        public struct GenericStructWithGenericInterface3<TS> : GenericInterface2<TS, int> { }
        public struct NonGenericStructWithGenericInterface2 : GenericInterface2<int, int> { }

        public class NonGenericClassWithNoInterfaces { }
        public class GenericClassWithNoInterfaces1<T> { }
        public class GenericClassWithNoInterfaces2<T, V> { }

        public class NonGenericClassWithNonGenericInterface : TI_NonGenericInterface1 { }
        public class GenericClassWithGenericInterface1<T> : GenericInterface1<T> { }
        public class GenericClassWithGenericInterface2<T, V> : GenericInterface2<T, V> { }

        public class NonGenericClassWithGenericInterface1 : GenericInterface1<int> { }
        public class GenericClassWithGenericInterface3<T> : GenericClassWithNoInterfaces2<T, int> { }
        public class NonGenericClassWithGenericInterface2 : GenericClassWithNoInterfaces2<int, int> { }

        public enum UIntEnum : uint
        {
            A = 1,
            B = 10
        }

        public enum IntEnum
        {
            Enum1 = 1,
            Enum2 = 2,
            Enum10 = 10,
            Enum18 = 18,
            Enum45 = 45
        }

        public interface InheritedInteraface : TI_NonGenericInterface2 { }
        public struct StructWithInheritedInterface : InheritedInteraface { }
        public class CompoundClass1 : NonGenericClassWithNonGenericInterface, InheritedInteraface { }
        public class CompoundClass2<T> : NonGenericClassWithNonGenericInterface, InheritedInteraface { }
        public class CompoundClass3<T> : NonGenericClassWithNonGenericInterface, GenericInterface1<T> { }
        public class CompoundClass4<T> : NonGenericClassWithNonGenericInterface, GenericInterface1<string> { }

        public class BaseClassWithInterface1Interface2 : TI_NonGenericInterface1, TI_NonGenericInterface2 { }
        public class SubClassWithInterface1Interface2 : BaseClassWithInterface1Interface2 { }
        public class GenericSubClassWithInterface1Interface2<T> : SubClassWithInterface1Interface2 { }

        public class GenericSubClassWithNoInterfaces1<T> : GenericClassWithNoInterfaces1<T> { }
        public class GenericSubSubClassWithNoInterfaces1<T> : GenericSubClassWithNoInterfaces1<T> { }

        [DefaultMember("PublicField")]
        public class MembersClass : TI_NonGenericInterface1, TI_NonGenericInterface2
        {
            public int PublicField;
            private string PrivateField;

            public MembersClass() { }
            public MembersClass(int intField, string stringField) { }

            private MembersClass(string stringField) { }

            public int PublicProp
            {
                get { return 10; }
                set { }
            }

            private string PrivateProp
            {
                get { return string.Empty; }
                set { }
            }

            public delegate void EventHandler(object sender, EventArgs e);

            public event EventHandler PublicEvent;
            private event EventHandler PrivateEvent;

            public void PublicMethod() { }
            private int PrivateMethod(int x, string y) { return default(int); }
        }

        public sealed class SealedClass { }
        public abstract class AbstractClass { }

        public class MethodClassWithConstraints<T, U>
            where T : TI_BaseClass, TI_NonGenericInterface1
            where U : class, new()
        { }

        public struct StructWithoutExplicitStructLayout
        {
            public int x;
            public string y;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        public struct StructWithExplicitStructLayout
        {
            [FieldOffset(0)]
            public byte x;

            [FieldOffset(1)]
            public short y;

            [FieldOffset(3)]
            public byte z;
        }

        public class ClassWithoutExplicitStructLayout
        {
            public int x;
            public string y;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 2, CharSet = CharSet.Unicode)]
        public class ClassWithExplicitStructLayout
        {
            [FieldOffset(0)]
            public byte x;

            [FieldOffset(1)]
            public short y;
        }
    }
    public struct TI_Struct
    {
        public int _field;
    }
    public class TI_BaseClass
    {
        static TI_BaseClass() { }
        public TI_BaseClass() { }
        public TI_BaseClass(short i) { }

        public event EventHandler EventPublic; // Inherited
        public static event EventHandler EventPublicStatic;
        public event Action<int> StuffHappened;

        public static string[] s_arrayField = new string[5];
        public string _field1 = "";
        public string _field2 = "";
        public readonly string _readonlyField = "";
        public volatile string _volatileField = "";
        public static string s_field = "";
        public static readonly string s_readonlyField = "";
        public static volatile string s_volatileField = "";

        private int _privateField;

        public void VoidMethodReturningVoid1() { }
        public void StringMethodReturningVoid(string str) { }
        public void VoidMethodReturningVoid2() { }
        public virtual void VirtualVoidMethodReturningVoid1() { }
        public virtual void VirtualVoidMethodReturningVoid2() { }
        public static void StaticVoidMethodReturningVoid() { }

        public class PublicNestedClass1 { }
        public class PublicNestedClass2 { }
        private class PrivateNestedClass { } // Private, so not inherited
        internal class InternalNestedClass { } // Internal members are not inherited
        protected class ProtectedNestedClass { }

        public string StringProperty1 { get { return ""; } set { } }
        public string StringProperty2 { get { return ""; } set { } }
        public virtual string VirtualStringProperty { get { return ""; } set { } }
        public static string StaticStringProperty { get { return ""; } set { } }

        public void MethodWithSameName() { }
        public void MethodWithSameName(int i) { }
        public void MethodWithSameName(string s) { }
        public void MethodWithSameName(object o) { }
    }

    public class TI_SubClass : TI_BaseClass
    {
        public TI_SubClass(string s) { }
        public TI_SubClass(short i2) { }

        public new event EventHandler EventPublic; // Overrides event
        public event EventHandler EventPublicNew; // New event

        public static new string[] s_arrayField = new string[10];
        public new string _field2 = "";
        public new readonly string _readonlyField = "";
        public new volatile string _volatileField = "";
        public static new string s_field = "";
        public static new readonly string s_readonlyField = "";
        public static new volatile string s_volatileField = "";

        public new void VoidMethodReturningVoid2() { }
        public new virtual void VirtualVoidMethodReturningVoid1() { }
        public override void VirtualVoidMethodReturningVoid2() { }
        public static new void StaticVoidMethodReturningVoid() { }

        public new class PublicNestedClass1 { }
        public class NestPublic3 { }
        public class NESTPUBLIC3 { }
        private class NestPrivate2 { }

        public new string StringProperty1 { get { return ""; } set { } }
        public new virtual string VirtualStringProperty { get { return ""; } set { } }
        public static new string StaticStringProperty { get { return ""; } set { } }
    }

    public interface TI_NonGenericInterface1 { }
    public class TI_ClassWithInterface1 : TI_NonGenericInterface1 { }

    namespace InnerNamespace
    {
        public abstract class AbstractBaseClass { }
        public abstract class AbstractSubClass : AbstractBaseClass { }
        public class AbstractSubSubClass : AbstractSubClass { }
    }

    public class TI_GenericTypeWithAllMembers<T>
    {
        private static event EventHandler<T> PrivateStaticEvent;
        private static T PrivateStaticField;
        private static T PrivateStaticProperty { get; set; }
        private static T PrivateStaticMethod(T t) => default;
        private static T PrivateStaticMethod(T t, T t2) => default;

        public static event EventHandler<T> PublicStaticEvent;
        public static T PublicStaticField;
        public static T PublicStaticProperty { get; set; }
        public static T PublicStaticMethod(T t) => default;
        public static T PublicStaticMethod(T t, T t2) => default;

        static TI_GenericTypeWithAllMembers() { }

        public TI_GenericTypeWithAllMembers(T t) { }
        private TI_GenericTypeWithAllMembers() { }

        public event EventHandler<T> PublicInstanceEvent;
        public T PublicInstanceField;
        public T PublicInstanceProperty { get; set; }
        public T PublicInstanceMethod(T t) => default;
        public T PublicInstanceMethod(T t, T t2) => default;

        private event EventHandler<T> PrivateInstanceEvent;
        private T PrivateInstanceField;
        private T PrivateInstanceProperty { get; set; }
        private T PrivateInstanceMethod(T t) => default;
        private T PrivateInstanceMethod(T t1, T t2) => default;

        public class Nested
        {
            public T NestedField;
        }
    }

    public class TI_TypeDerivedFromGenericTypeWithAllMembers<T> : TI_GenericTypeWithAllMembers<T>
    {
        public TI_TypeDerivedFromGenericTypeWithAllMembers(T t) : base(t) { }
    }

    public class TI_TypeDerivedFromGenericTypeWithAllMembersClosed : TI_TypeDerivedFromGenericTypeWithAllMembers<int>
    {
        public TI_TypeDerivedFromGenericTypeWithAllMembersClosed(int t) : base(t) { }
    }

#pragma warning restore 0067, 0169

    public class OutsideTypeInfoTests
    {
        public class InsideTypeInfoTests { }
    }

    public class OutsideTypeInfoTests<T>
    {
        public class InsideTypeInfoTests<U> { }
    }

    public class TI_FullNameTest<T> where T : unmanaged
    {
        public static T TypeParam() => throw new Exception();
        public static T[] ArrayTypeParam() => throw new Exception();
        public static unsafe T* PointerTypeParam() => throw new Exception();
        public static ref T ByRefTypeParam() => throw new Exception();
        public static List<T> ListTypeParam() => throw new Exception();
    }
}

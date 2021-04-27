// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Tests
{
    public class TypeTests
    {
        private const BindingFlags DefaultBindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        [Theory]
        [InlineData(typeof(TI_Class), nameof(TI_Class), MemberTypes.TypeInfo)]
        [InlineData(typeof(TI_Class.PublicNestedType), nameof(TI_Class.PublicNestedType), MemberTypes.NestedType)]
        [InlineData(typeof(string), nameof(String), MemberTypes.TypeInfo)]
        public void Properties(Type type, string name, MemberTypes memberType)
        {
            Assert.Equal(name, type.Name);
            Assert.Equal(memberType, type.GetTypeInfo().MemberType);

            Assert.Equal(memberType == MemberTypes.NestedType, type.IsNested);
        }

        [Theory]
        [InlineData(typeof(GenericArrayWrapperClass<string>), new string[] { "System.String Item [Int32]" })]
        [InlineData(typeof(GenericArrayWrapperClass<>), new string[] { "T Item [Int32]" })]
        [InlineData(typeof(GenericClass<>), new string[] { "T ReturnAndSetField(T)" })]
        [InlineData(typeof(GenericClass<int>), new string[] { "Int32 ReturnAndSetField(Int32)" })]
        public void GetDefaultMembers(Type type, string[] expectedNames)
        {
            string[] memberNames = TypeExtensions.GetDefaultMembers(type).Select(member => member.Name).ToArray();
            Assert.Equal(expectedNames.Length, memberNames.Length);
            Assert.All(expectedNames, toString => memberNames.Contains(toString));
        }

        public static IEnumerable<object[]> GetEvents_TestData()
        {
            string[] expectedPublic = new string[] { "WeightChanged" };
            yield return new object[] { DefaultBindingFlags, expectedPublic };

            yield return new object[] { BindingFlags.IgnoreCase, new string[0] };
            yield return new object[] { BindingFlags.DeclaredOnly, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly, new string[0] };

            yield return new object[] { BindingFlags.Instance, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Instance, new string[0] };
            yield return new object[] { BindingFlags.Instance | BindingFlags.DeclaredOnly, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.DeclaredOnly, new string[0] };

            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Static, new string[0] };
            yield return new object[] { BindingFlags.Static, new string[0] };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Static, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static, new string[0] };

            yield return new object[] { BindingFlags.Instance | BindingFlags.Static, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static, new string[0] };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static, new string[0] };

            yield return new object[] { BindingFlags.Public, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Public, new string[0] };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Public, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public, new string[0] };

            yield return new object[] { BindingFlags.Instance | BindingFlags.Public, expectedPublic };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public, expectedPublic };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public, expectedPublic };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public, expectedPublic };

            yield return new object[] { BindingFlags.Public | BindingFlags.Static, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static, new string[0] };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static, new string[0] };

            yield return new object[] { BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, expectedPublic };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, expectedPublic };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, expectedPublic };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, expectedPublic };

            yield return new object[] { BindingFlags.NonPublic, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.NonPublic, new string[0] };

            string[] expectedNonPublic = new string[] { "WeightStayedTheSame" };
            yield return new object[] { BindingFlags.Instance | BindingFlags.NonPublic, expectedNonPublic };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic, expectedNonPublic };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic, expectedNonPublic };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic, expectedNonPublic };

            yield return new object[] { BindingFlags.Static | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic, new string[0] };

            yield return new object[] { BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic, expectedNonPublic };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic, expectedNonPublic };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic, expectedNonPublic };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic, expectedNonPublic };

            yield return new object[] { BindingFlags.Public | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic, new string[0] };

            string[] expectedAll = new string[] { "WeightChanged", "WeightStayedTheSame" };
            yield return new object[] { BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, expectedAll };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, expectedAll };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, expectedAll };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, expectedAll };

            yield return new object[] { BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[0] };

            yield return new object[] { BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, expectedAll };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, expectedAll };
            yield return new object[] { BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, expectedAll };
            yield return new object[] { BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, expectedAll };

            yield return new object[] { BindingFlags.FlattenHierarchy, new string[0] };
        }

        [Theory]
        [MemberData(nameof(GetEvents_TestData))]
        public void GetEvents(BindingFlags bindingAttributes, string[] expectedNames)
        {
            if (bindingAttributes == DefaultBindingFlags)
            {
                string[] eventNames1 = TypeExtensions.GetEvents(typeof(Cat<int>)).Select(eventInfo => eventInfo.Name).ToArray();
                Assert.Equal(expectedNames.Length, eventNames1.Length);
                Assert.All(expectedNames, name => eventNames1.Contains(name));
            }
            string[] eventNames2 = TypeExtensions.GetEvents(typeof(Cat<int>), bindingAttributes).Select(eventInfo => eventInfo.Name).ToArray();
            Assert.Equal(expectedNames.Length, eventNames2.Length);
            Assert.All(expectedNames, name => eventNames2.Contains(name));
        }

        public static IEnumerable<object[]> GetFields_TestData()
        {
            yield return new object[] { typeof(GenericClassUsingNestedInterfaces<string, int>), new string[] { "FieldZero", "FieldOne", "FieldTwo", "FieldThree" } };
            yield return new object[] { typeof(GenericStructWithInterface<string>), new string[] { "field", "field2" } };
            yield return new object[] { typeof(NonGenericClassWithGenericInterface), new string[] { "field" } };
            yield return new object[] { typeof(GenericStruct2TP<int, string>), new string[] { "field", "field2" } };
            yield return new object[] { typeof(GenericClassWithVarArgMethod<>), new string[] { "field" } };
        }

        [Theory]
        [MemberData(nameof(GetFields_TestData))]
        public void GetFields(Type type, string[] expectedNames)
        {
            string[] fieldNames = TypeExtensions.GetFields(type).Select(field => field.Name).ToArray();
            Assert.Equal(expectedNames.Length, fieldNames.Length);
            Assert.All(expectedNames, name => fieldNames.Contains(name));
        }

        [Theory]
        [InlineData(typeof(GenericClass<string>), new Type[0])]
        [InlineData(typeof(IGenericInterface<string>), new Type[0])]
        [InlineData(typeof(Cat<string>), new Type[] { typeof(IConsume) })]
        [InlineData(typeof(Cat<>), new Type[] { typeof(IConsume) })]
        [InlineData(typeof(PackOfCarnivores<Cat<string>>), new Type[0])]
        [InlineData(typeof(IGenericInterfaceInherits<int, string>), new Type[] { typeof(IGenericInterface<int>), typeof(IGenericInterface2<string, int>) })]
        [InlineData(typeof(GenericClassUsingNestedInterfaces<int, string>), new Type[] { typeof(IGenericInterfaceInherits<int, string>), typeof(IGenericInterface<int>), typeof(IGenericInterface2<string, int>) })]
        [InlineData(typeof(NonGenericClassWithGenericInterface), new Type[] { typeof(IGenericInterface<int>) })]
        [InlineData(typeof(TI_StructWithInterfaces), new Type[] { typeof(TI_Interface1), typeof(TI_Interface3) })]
        [InlineData(typeof(TI_StructWithInterfaces?), new Type[0])]
        [InlineData(typeof(TI_Struct?), new Type[0])]
        public void GetInterfaces(Type type, Type[] expected)
        {
            Type[] interfaces = TypeExtensions.GetInterfaces(type);
            Assert.Equal(expected.Length, interfaces.Length);
            Assert.All(expected, interfaceType => interfaces.Equals(interfaceType));
        }

        [Fact]
        public void GetInterfaces_GenericInterfaceWithTypeParameter_ReturnsExpectedToString()
        {
            Type[] interfaces = TypeExtensions.GetInterfaces(typeof(GenericClassWithInterface<>));
            Assert.Equal(1, interfaces.Length);
            Assert.Equal("System.Reflection.Tests.IGenericInterface`1[T]", interfaces[0].ToString());
        }

        public static IEnumerable<object[]> GetMember_TestData()
        {
            yield return new object[] { typeof(GenericClassUsingNestedInterfaces<string, int>), "Field*", DefaultBindingFlags, new string[] { "FieldZero", "FieldOne", "FieldTwo", "FieldThree" } };
            yield return new object[] { typeof(GenericClassUsingNestedInterfaces<string, int>), "Return*", DefaultBindingFlags, new string[] { "ReturnAndSetFieldZero", "ReturnAndSetFieldThree" } };
            yield return new object[] { typeof(GenericClassWithInterface<int>), "*", DefaultBindingFlags, new string[] { "ReturnAndSetFieldZero", "GenericMethod", "ToString", "Equals", "GetHashCode", "GetType", ".ctor", "field" } };
            yield return new object[] { typeof(IGenericInterface<>), "ReturnAndSetFieldZero", DefaultBindingFlags, new string[] { "ReturnAndSetFieldZero" } };

            yield return new object[] { typeof(GenericArrayWrapperClass<>), "*", DefaultBindingFlags, new string[] { "get_myProperty", "set_myProperty", "get_Item", "set_Item", "ToString", "Equals", "GetHashCode", "GetType", ".ctor", "myProperty", "Item" } };

            yield return new object[] { typeof(Cat<int>), "*", DefaultBindingFlags, new string[] { "add_WeightChanged", "remove_WeightChanged", "get_StuffConsumed", "Eat", "Puke", "ToString", "Equals", "GetHashCode", "GetType", ".ctor", "StuffConsumed", "PStuffConsumed", "get_PStuffConsumed", "set_PStuffConsumed", "WeightChanged" } };

            yield return new object[] { typeof(GenericArrayWrapperClass<int>), "*", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, new string[] { "get_myProperty", "set_myProperty", "get_Item", "set_Item", ".ctor", "myProperty", "Item", "_field", "_field1" } };

            yield return new object[] { typeof(GenericClassUsingNestedInterfaces<string, int>), "*", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, new string[] { "ReturnAndSetFieldZero", "SetFieldOne", "SetFieldTwo", "ReturnAndSetFieldThree", ".ctor", "FieldZero", "FieldOne", "FieldTwo", "FieldThree" } };
            yield return new object[] { typeof(GenericClassWithInterface<int>), "*", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, new string[] { "GenericMethod", "ReturnAndSetFieldThree", ".ctor", "field" } };
            yield return new object[] { typeof(IGenericInterface<>), "*", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, new string[] { "ReturnAndSetFieldZero" } };
            yield return new object[] { typeof(GenericArrayWrapperClass<>), "*", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, new string[] { "get_myProperty", "set_myProperty", "get_Item", "set_Item", ".ctor", "myProperty", "Item", "_field", "_field1" } };

            yield return new object[] { typeof(GenericArrayWrapperClass<int>), "*", DefaultBindingFlags, new string[] { "get_myProperty", "set_myProperty", "get_Item", "set_Item", ".ctor", "myProperty", "Item", "ToString", "Equals", "GetHashCode", "GetType" } };
        }

        [Theory]
        [MemberData(nameof(GetMember_TestData))]
        public void GetMember(Type type, string name, BindingFlags bindingAttributes, string[] expectedNames)
        {
            if (bindingAttributes == DefaultBindingFlags)
            {
                string[] memberNames1 = TypeExtensions.GetMember(type, name).Select(member => member.Name).ToArray();
                Assert.Equal(expectedNames.Length, memberNames1.Length);
                Assert.All(expectedNames, expectedName => memberNames1.Contains(expectedName));
                if (name == "*")
                {
                    MemberInfo[] memberNamesFromAsterix1 = type.GetMember(name);
                    MemberInfo[] memberNamesFromMethod1 = type.GetMembers();

                    Assert.Equal(memberNamesFromAsterix1.Length, memberNamesFromMethod1.Length);
                    Assert.All(memberNamesFromAsterix1, memberInfo => memberNamesFromMethod1.Contains(memberInfo));
                }
            }
            string[] memberNames2 = TypeExtensions.GetMember(type, name, bindingAttributes).Select(member => member.Name).ToArray();
            Assert.Equal(expectedNames.Length, memberNames2.Length);
            Assert.All(expectedNames, expectedName => memberNames2.Contains(expectedName));
            if (name == "*")
            {
                MemberInfo[] memberNamesFromAsterix2 = TypeExtensions.GetMember(type, name, bindingAttributes);
                MemberInfo[] memberNamesFromMethod2 = TypeExtensions.GetMembers(type, bindingAttributes);

                Assert.Equal(memberNamesFromAsterix2.Length, memberNamesFromMethod2.Length);
                Assert.All(memberNamesFromAsterix2, memberInfo => memberNamesFromMethod2.Contains(memberInfo));
            }
        }
        public static IEnumerable<object[]> GetMethods_TestData()
        {
            yield return new object[] { typeof(GenericClass<string>), BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance, new string[] { "ReturnAndSetField" } };
            yield return new object[] { typeof(GenericClassUsingNestedInterfaces<string, int>), BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance, new string[] { "ReturnAndSetFieldZero", "SetFieldOne", "SetFieldTwo", "ReturnAndSetFieldThree" } };
            yield return new object[] { typeof(GenericClassWithInterface<>), BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance, new string[] { "ReturnAndSetFieldZero", "GenericMethod" } };
            yield return new object[] { typeof(GenericClassWithInterface<int>), BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance, new string[] { "ReturnAndSetFieldZero", "GenericMethod" } };
            yield return new object[] { typeof(GenericClassWithVarArgMethod<int>), BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance, new string[] { "get_publicField", "set_publicField", "ReturnAndSetField" } };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly, new string[0] };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Instance, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Instance, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance, new string[0] };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static, new string[0] };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Instance | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static, new string[0] };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Public, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Public, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Public, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public, new string[0] };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Instance | BindingFlags.Public, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "GetTypeCode", "GetType" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "GetTypeCode", "GetType" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "GetTypeCode" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "GetTypeCode" } };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Static | BindingFlags.Public, new string[] { "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public, new string[] { "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public, new string[] { "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public, new string[] { "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse" } };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse", "GetTypeCode", "GetType" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse", "GetTypeCode", "GetType" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse", "GetTypeCode" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse", "GetTypeCode" } };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.NonPublic, new string[0] };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Instance | BindingFlags.NonPublic, new string[] { "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType", "Finalize", "MemberwiseClone" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic, new string[] { "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType", "Finalize", "MemberwiseClone" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic, new string[] { "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic, new string[] { "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType" } };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Static | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.DeclaredOnly | BindingFlags.NonPublic, new string[0] };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic, new string[] { "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType", "Finalize", "MemberwiseClone" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic, new string[] { "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType", "Finalize", "MemberwiseClone" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.NonPublic, new string[] { "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic, new string[] { "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType" } };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Public | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.NonPublic, new string[0] };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "GetTypeCode", "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType", "GetType", "Finalize", "MemberwiseClone" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "GetTypeCode", "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType", "GetType", "Finalize", "MemberwiseClone" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "GetTypeCode", "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "GetTypeCode", "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType" } };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse" } };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse", "GetTypeCode", "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType", "GetType", "Finalize", "MemberwiseClone" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse", "GetTypeCode", "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType", "GetType", "Finalize", "MemberwiseClone" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse", "GetTypeCode", "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType" } };
            yield return new object[] { typeof(Int32Impersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "CompareTo", "CompareTo", "Equals", "Equals", "GetHashCode", "ToString", "ToString", "ToString", "ToString", "Parse", "Parse", "Parse", "Parse", "TryParse", "TryParse", "GetTypeCode", "System.IConvertible.ToBoolean", "System.IConvertible.ToChar", "System.IConvertible.ToSByte", "System.IConvertible.ToByte", "System.IConvertible.ToInt16", "System.IConvertible.ToUInt16", "System.IConvertible.ToInt32", "System.IConvertible.ToUInt32", "System.IConvertible.ToInt64", "System.IConvertible.ToUInt64", "System.IConvertible.ToSingle", "System.IConvertible.ToDouble", "System.IConvertible.ToDecimal", "System.IConvertible.ToDateTime", "System.IConvertible.ToType" } };

            yield return new object[] { typeof(Int32Impersonator), BindingFlags.FlattenHierarchy, new string[0] };
        }

        [Theory]
        [ActiveIssue("https://github.com/mono/mono/issues/15320", TestRuntimes.Mono)]
        [MemberData(nameof(GetMethods_TestData))]
        public void GetMethods(Type type, BindingFlags bindingAttributes, string[] expectedNames)
        {
            if (bindingAttributes == DefaultBindingFlags)
            {
                string[] methodNames1 = TypeExtensions.GetMethods(type).Select(method => method.Name).ToArray();
                Assert.Equal(expectedNames.Length, methodNames1.Length);
                Assert.All(expectedNames, name => methodNames1.Contains(name));
            }
            string[] methodNames2 = TypeExtensions.GetMethods(type, bindingAttributes).Select(method => method.Name).ToArray();
            Assert.Equal(expectedNames.Length, methodNames2.Length);
            Assert.All(expectedNames, name => methodNames2.Contains(name));
        }

        public static IEnumerable<object[]> GetProperties_TestData()
        {
            yield return new object[] { typeof(GenericClassWithVarArgMethod<string>), DefaultBindingFlags, new string[] { "publicField" } };
            yield return new object[] { typeof(Cat<int>), DefaultBindingFlags, new string[] { "StuffConsumed", "PStuffConsumed" } };
            yield return new object[] { typeof(GenericClassWithVarArgMethod<int>), DefaultBindingFlags, new string[] { "publicField" } };
            yield return new object[] { typeof(GenericClassWithVarArgMethod<>), DefaultBindingFlags, new string[] { "publicField" } };
            yield return new object[] { typeof(ClassWithVarArgMethod), DefaultBindingFlags, new string[] { "publicField" } };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.DeclaredOnly, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly, new string[0] };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.Instance, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.Instance, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance, new string[0] };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.DeclaredOnly | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Static, new string[0] };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.Instance | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static, new string[0] };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.Public, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.Public, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.DeclaredOnly | BindingFlags.Public, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public, new string[0] };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.Instance | BindingFlags.Public, new string[] { "Chars", "Length" } };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public, new string[] { "Chars", "Length" } };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public, new string[] { "Chars", "Length" } };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public, new string[] { "Chars", "Length" } };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.Public | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static, new string[0] };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static, new string[] { "Chars", "Length" } };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static, new string[] { "Chars", "Length" } };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static, new string[] { "Chars", "Length" } };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static, new string[] { "Chars", "Length" } };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.DeclaredOnly | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.NonPublic, new string[0] };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.Instance | BindingFlags.NonPublic, new string[] { "FirstChar" } };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic, new string[] { "FirstChar" } };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic, new string[] { "FirstChar" } };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic, new string[] { "FirstChar" } };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.Public | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic, new string[0] };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic, new string[0] };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "FirstChar", "Chars", "Length" } };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "FirstChar", "Chars", "Length" } };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "FirstChar", "Chars", "Length" } };
            yield return new object[] { typeof(StringImpersonator), BindingFlags.IgnoreCase | BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, new string[] { "FirstChar", "Chars", "Length" } };

            yield return new object[] { typeof(StringImpersonator), BindingFlags.FlattenHierarchy, new string[0] };
        }

        [Theory]
        [MemberData(nameof(GetProperties_TestData))]
        public void GetProperties(Type type, BindingFlags bindingAttributes, string[] expectedNames)
        {
            if (bindingAttributes == DefaultBindingFlags)
            {
                string[] propertyNames1 = TypeExtensions.GetProperties(type).Select(method => method.Name).ToArray();
                Assert.Equal(expectedNames.Length, propertyNames1.Length);
                Assert.All(expectedNames, name => propertyNames1.Contains(name));
            }
            string[] propertyNames2 = TypeExtensions.GetProperties(type, bindingAttributes).Select(method => method.Name).ToArray();
            Assert.Equal(expectedNames.Length, propertyNames2.Length);
            Assert.All(expectedNames, name => propertyNames2.Contains(name));
        }

        public static IEnumerable<object[]> GetProperty_TestData()
        {
            yield return new object[] { typeof(GenericClassWithVarArgMethod<string>), "publicField", DefaultBindingFlags, typeof(string), new Type[0] };
            yield return new object[] { typeof(Cat<int>), "StuffConsumed", DefaultBindingFlags, null, new Type[0] };
            yield return new object[] { typeof(GenericClassWithVarArgMethod<int>), "publicField", DefaultBindingFlags, typeof(int), new Type[0] };
            yield return new object[] { typeof(GenericClassWithVarArgMethod<>), "publicField", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new Type[0] };
            yield return new object[] { typeof(GenericClassWithVarArgMethod<>), "publicField", DefaultBindingFlags, null, new Type[0] };
            yield return new object[] { typeof(ClassWithVarArgMethod), "publicField", DefaultBindingFlags, null, new Type[0] };
        }

        [Theory]
        [MemberData(nameof(GetProperty_TestData))]
        public void GetProperty(Type type, string name, BindingFlags bindingAttributes, Type returnType, Type[] types)
        {
            if (returnType == null)
            {
                if (bindingAttributes == DefaultBindingFlags)
                {
                    Assert.Equal(name, TypeExtensions.GetProperty(type, name).Name);
                }
                Assert.Equal(name, TypeExtensions.GetProperty(type, name, bindingAttributes).Name);
            }
            else
            {
                if (types.Length == 0)
                {
                    Assert.Equal(name, TypeExtensions.GetProperty(type, name, returnType).Name);
                }
                Assert.Equal(name, TypeExtensions.GetProperty(type, name, returnType, types).Name);
            }
        }

        public static IEnumerable<object[]> IsAssignableFrom_TestData()
        {
            yield return new object[] { typeof(TI_BaseClassWithInterface), null, false };
            yield return new object[] { typeof(IList<object>), typeof(object[]), true };
            yield return new object[] { typeof(object[]), typeof(IList<object>), false };

            yield return new object[] { typeof(TI_BaseClassWithInterface), typeof(TI_SubClassWithInterface), true };
            yield return new object[] { typeof(TI_BaseClassWithInterface[]), typeof(TI_SubClassWithInterface[]), true };
            yield return new object[] { typeof(IList<object>), typeof(TI_BaseClassWithInterface[]), true };
            yield return new object[] { typeof(IList<TI_BaseClassWithInterface>), typeof(TI_BaseClassWithInterface[]), true };
            yield return new object[] { typeof(IList<TI_BaseClassWithInterface>), typeof(TI_SubClassWithInterface[]), true };
            yield return new object[] { typeof(IList<TI_SubClassWithInterface>), typeof(TI_SubClassWithInterface[]), true };

            yield return new object[] { typeof(TI_GenericBaseClass<object>), typeof(TI_GenericSubSubClass<object>), true };
            yield return new object[] { typeof(TI_GenericSubClass<string>), typeof(TI_GenericSubSubClass<string>), true };
            yield return new object[] { typeof(TI_GenericSubClass<string>), typeof(TI_GenericSubClass<string>), true };
            yield return new object[] { typeof(TI_GenericSubClass<string>), typeof(TI_GenericSubClass<object>), false };
            yield return new object[] { typeof(TI_GenericSubClass<object>), typeof(TI_GenericSubClass<string>), false };
            yield return new object[] { typeof(TI_GenericSubSubClass<object>), typeof(TI_GenericSubClass<object>), false };
            yield return new object[] { typeof(TI_GenericSubClass<string>), typeof(TI_GenericBaseClass<string>), false };

            yield return new object[] { typeof(TI_Interface2), typeof(TI_Interface2), true };
            yield return new object[] { typeof(TI_Interface2), typeof(TI_BaseClassWithInterface), true };
            yield return new object[] { typeof(TI_Interface2), typeof(TI_SubClassWithInterface), true };
            yield return new object[] { typeof(TI_Interface2), typeof(TI_GenericSubClassWithInterface<>), true };
            yield return new object[] { typeof(TI_Interface2), typeof(TI_GenericSubClassWithInterface<string>), true };
            yield return new object[] { typeof(TI_SubClassWithInterface), typeof(TI_Interface1), false };

            yield return new object[] { typeof(TI_Interface1), typeof(TI_GenericSubClassWithConstraints<>).GetGenericArguments()[0], true };
            yield return new object[] { typeof(TI_Interface2), typeof(TI_GenericSubClassWithConstraints<>).GetGenericArguments()[0], true };
            yield return new object[] { typeof(TI_GenericSubClassWithInterface<>), typeof(TI_GenericSubClassWithConstraints<>).GetGenericArguments()[0], false };

            yield return new object[] { typeof(CustomNamespace.TI_AbstractBaseClass), typeof(CustomNamespace.TI_AbstractSubClass), true };
            yield return new object[] { typeof(CustomNamespace.TI_AbstractBaseClass), typeof(CustomNamespace.TI_SubClass), true };
            yield return new object[] { typeof(CustomNamespace.TI_AbstractSubClass), typeof(CustomNamespace.TI_SubClass), true };

            yield return new object[] { typeof(TI_GenericClassWithConstraints<>).GetGenericArguments()[0], typeof(TI_Interface1), false };
            yield return new object[] { typeof(TI_GenericClassWithConstraints<>).GetGenericArguments()[0], typeof(TI_BaseClassWithInterface), false };
            yield return new object[] { typeof(TI_Interface1), typeof(TI_GenericClassWithConstraints<>).GetGenericArguments()[0], true };
            yield return new object[] { typeof(TI_BaseClassWithInterface), typeof(TI_GenericClassWithConstraints<>).GetGenericArguments()[0], false };
            yield return new object[] { typeof(TI_Interface1), typeof(TI_GenericSubClassWithConstraints<>).GetGenericArguments()[0], true };
            yield return new object[] { typeof(TI_Interface2), typeof(TI_GenericSubClassWithConstraints<>).GetGenericArguments()[0], true };

            // A T[] is assignable to IList<U> iff T[] is assignable to U[]
            yield return new object[] { typeof(TI_Interface1[]), typeof(TI_StructWithInterface[]), false };
            yield return new object[] { typeof(TI_Interface1[]), typeof(TI_SubClassWithInterface[]), true };
            yield return new object[] { typeof(IList<TI_Interface1>), typeof(TI_StructWithInterface[]), false };
            yield return new object[] { typeof(IList<TI_Interface1>), typeof(TI_SubClassWithInterface[]), true };

            yield return new object[] { typeof(int[]), typeof(uint[]), true };
            yield return new object[] { typeof(uint[]), typeof(int[]), true };
            yield return new object[] { typeof(IList<int>), typeof(uint[]), true };
            yield return new object[] { typeof(IList<uint>), typeof(int[]), true };

            yield return new object[] { typeof(int?), typeof(int), true };
            yield return new object[] { typeof(int), typeof(int?), false };
            yield return new object[] { typeof(int?[]), typeof(int[]), false };

            yield return new object[] { typeof(TI_Interface1), typeof(TI_StructWithInterfaces), true };
            yield return new object[] { typeof(TI_StructWithInterfaces), typeof(TI_Interface1), false };
        }

        [Theory]
        [MemberData(nameof(IsAssignableFrom_TestData))]
        public void IsAssignableFrom(Type type, Type type2, bool expected)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            Assert.Equal(expected, typeInfo.IsAssignableFrom(type2?.GetTypeInfo()));
            Assert.True(typeInfo.IsAssignableFrom(typeInfo));
        }

        public static IEnumerable<object[]> IsInstanceOfType_TestData()
        {
            yield return new object[] { typeof(float), 1.0234F, true };
            yield return new object[] { typeof(string), "this is a string", true };
            yield return new object[] { typeof(int?), 100, true };
        }

        [Theory]
        [MemberData(nameof(IsInstanceOfType_TestData))]
        public void IsInstanceOfType(Type type, object value, bool expected)
        {
            Assert.Equal(expected, TypeExtensions.IsInstanceOfType(type, value));
        }

        [Fact]
        public void IsInstanceOfType_NullableTypeAndValue_ReturnsTrue()
        {
            Assert.True(TypeExtensions.IsInstanceOfType(typeof(int?), (int?)100));
        }

        [Theory]
        [InlineData(typeof(TI_Class))]
        public void Properties_SameAsUnderlyingType(Type type)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            Assert.Equal(type.Name, typeInfo.Name);
            Assert.Equal(type.FullName, typeInfo.FullName);
        }

        [Theory]
        [InlineData(typeof(TI_Class))]
        public void Methods_SameAsUnderlyingType(Type type)
        {
            TypeInfo typeInfo = type.GetTypeInfo();
            BindingFlags declaredFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            Assert.Equal(TypeExtensions.GetMethod(type, "ProtectedMethod", declaredFlags), typeInfo.GetDeclaredMethods("ProtectedMethod").First());
            Assert.Equal(TypeExtensions.GetNestedType(type, "PublicNestedType", declaredFlags), typeInfo.GetDeclaredNestedType("PublicNestedType").AsType());
            Assert.Equal(TypeExtensions.GetProperty(type, "PrivateProperty", declaredFlags), typeInfo.GetDeclaredProperty("PrivateProperty"));

            Assert.All(TypeExtensions.GetFields(type, declaredFlags), field => typeInfo.DeclaredFields.Contains(field));
            Assert.All(TypeExtensions.GetMethods(type, declaredFlags), method => typeInfo.DeclaredMethods.Contains(method));
            Assert.All(TypeExtensions.GetNestedTypes(type, declaredFlags), nestedType => typeInfo.DeclaredNestedTypes.Contains(nestedType.GetTypeInfo()));
            Assert.All(TypeExtensions.GetProperties(type, declaredFlags), property => typeInfo.DeclaredProperties.Contains(property));
            Assert.All(TypeExtensions.GetEvents(type, declaredFlags), eventInfo => typeInfo.DeclaredEvents.Contains(eventInfo));
            Assert.All(TypeExtensions.GetConstructors(type, declaredFlags), constructor => typeInfo.DeclaredConstructors.Contains(constructor));

            Assert.All(TypeExtensions.GetEvents(type), eventInfo => typeInfo.AsType().GetEvents().Contains(eventInfo));
            Assert.All(TypeExtensions.GetFields(type), fieldInfo => typeInfo.AsType().GetFields().Contains(fieldInfo));
            Assert.All(TypeExtensions.GetMethods(type), methodInfo => typeInfo.AsType().GetMethods().Contains(methodInfo));
            Assert.All(TypeExtensions.GetProperties(type), propertyInfo => typeInfo.AsType().GetProperties().Contains(propertyInfo));

            Assert.Equal(type.GetType(), typeInfo.GetType());
            Assert.True(type.Equals(typeInfo));

            BindingFlags allFlags = BindingFlags.DeclaredOnly | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;
            Assert.All(type.GetNestedTypes(allFlags), nestedType => typeInfo.AsType().GetNestedTypes(allFlags).Contains(nestedType));

            Assert.Equal(type.GetTypeInfo().IsClass, typeInfo.IsClass);
            Assert.Equal(type.GetTypeInfo().IsPublic, typeInfo.IsPublic);
            Assert.Equal(type.GetTypeInfo().IsGenericType, typeInfo.IsGenericType);
            Assert.Equal(type.GetTypeInfo().IsImport, typeInfo.IsImport);
            Assert.Equal(type.GetTypeInfo().IsEnum, typeInfo.IsEnum);
            Assert.Equal(type.GetTypeInfo().IsGenericTypeDefinition, typeInfo.IsGenericTypeDefinition);
        }

        [Fact]
        public static void GetType_NullableObject_ReturnsUnderlyingObjectType()
        {
            TI_GenericStruct<Type> notNullable = new TI_GenericStruct<Type>();
            Assert.IsType<TI_GenericStruct<Type>>(notNullable);

            TI_GenericStruct<Type>? nullable = notNullable;
            Assert.IsType<TI_GenericStruct<Type>>(nullable);

            object boxed = nullable;
            Assert.IsType<TI_GenericStruct<Type>>(boxed);

            TI_GenericStruct<Type>? unboxed = (TI_GenericStruct<Type>?)boxed;
            Assert.IsType<TI_GenericStruct<Type>>(boxed);
        }
    }

#pragma warning disable 0169, 0067, 0649
    public class TI_Class
    {
        public int PublicField;
        protected int ProtectedField;
        private int _privateField;
        internal int InternalField;

        public static int PublicStaticField;
        protected static int ProtectedStaticField;
        private static int s_privateStaticField;
        internal static int InternalStaticField;

        public TI_Class() { }
        protected TI_Class(int i) { }
        private TI_Class(int i, int j) { }
        internal TI_Class(int i, int j, int k) { }

        public void PublicMethod() { }
        protected void ProtectedMethod() { }
        private void PrivateMethod() { }
        internal void InternalMethod() { }

        public static void PublicStaticMethod() { }
        protected static void ProtectedStaticMethod() { }
        private static void PrivateStaticMethod() { }
        internal static void InternalStaticMethod() { }

        public class PublicNestedType { }
        protected class ProtectedNestedType { }
        private class PrivateNestedType { }
        internal class InternalNestedType { }

        public int PublicProperty { get { return default(int); } set { } }
        protected int ProtectedProperty { get { return default(int); } set { } }
        private int PrivateProperty { get { return default(int); } set { } }
        internal int InternalProperty { get { return default(int); } set { } }
    }

    [DefaultMember("ReturnAndSetField")]
    public class GenericClass<T>
    {
        public T field;

        public GenericClass(T a) { field = a; }

        public T ReturnAndSetField(T newFieldValue)
        {
            field = newFieldValue;
            return field;
        }
    }

    public struct GenericStruct2TP<T, W>
    {
        public T field;
        public W field2;

        public T ReturnAndSetField1(T newFieldValue)
        {
            field = newFieldValue;
            return field;
        }

        public W ReturnAndSetField2(W newFieldValue)
        {
            field2 = newFieldValue;
            return field2;
        }
    }

    public class GenericClassWithInterface<T> : IGenericInterface<T>
    {
        public T field;

        public GenericClassWithInterface(T a)
        {
            field = a;
        }

        public T ReturnAndSetFieldZero(T newFieldValue)
        {
            field = newFieldValue;
            return field;
        }

        public W GenericMethod<W>(W a) => a;
    }

    public class NonGenericClassWithGenericInterface : IGenericInterface<int>
    {
        public int field;

        public int ReturnAndSetFieldZero(int newFieldValue)
        {
            field = newFieldValue;
            return field;
        }
    }

    public struct GenericStructWithInterface<T> : IGenericInterface<T>
    {
        public T field;
        public int field2;

        public GenericStructWithInterface(T a)
        {
            field = a;
            field2 = 0;
        }

        public GenericStructWithInterface(T a, int b)
        {
            field = a;
            field2 = b;
        }

        public T ReturnAndSetFieldZero(T newFieldValue)
        {
            field = newFieldValue;
            return field;
        }
    }

    public interface IGenericInterface<T>
    {
        T ReturnAndSetFieldZero(T newFieldValue);
    }

    public interface IGenericInterface2<T, W>
    {
        void SetFieldOne(T newFieldValue);
        void SetFieldTwo(W newFieldValue);
    }

    public interface IGenericInterfaceInherits<U, V> : IGenericInterface<U>, IGenericInterface2<V, U>
    {
        V ReturnAndSetFieldThree(V newFieldValue);
    }

    public class GenericClassUsingNestedInterfaces<X, Y> : IGenericInterfaceInherits<X, Y>
    {
        public X FieldZero;
        public X FieldOne;
        public Y FieldTwo;
        public Y FieldThree;

        public GenericClassUsingNestedInterfaces(X a, X b, Y c, Y d)
        {
            FieldZero = a;
            FieldOne = b;
            FieldTwo = c;
            FieldThree = d;
        }

        public X ReturnAndSetFieldZero(X newFieldValue)
        {
            FieldZero = newFieldValue;
            return FieldZero;
        }

        public void SetFieldOne(Y newFieldValue) => FieldTwo = newFieldValue;

        public void SetFieldTwo(X newFieldValue) => FieldOne = newFieldValue;

        public Y ReturnAndSetFieldThree(Y newFieldValue)
        {
            FieldThree = newFieldValue;
            return FieldThree;
        }
    }

    public class GenericClassWithVarArgMethod<T>
    {
        public T field;

        public T publicField
        {
            get { return field; }
            set { field = value; }
        }

        public T ReturnAndSetField(T newFieldValue, params T[] moreFieldValues)
        {
            field = newFieldValue;

            for (int i = 0; i <= moreFieldValues.Length - 1; i++)
            {
                field = moreFieldValues[i];
            }

            return field;
        }
    }

    public class ClassWithVarArgMethod
    {
        public int field;

        public int publicField
        {
            get { return field; }
            set { field = value; }
        }

        public int ReturnAndSetField(int newFieldValue, params int[] moreFieldValues)
        {
            field = newFieldValue;

            for (int i = 0; i <= moreFieldValues.Length - 1; i++)
            {
                field = moreFieldValues[i];
            }

            return field;
        }
    }

    public interface IConsume
    {
        object[] StuffConsumed { get; }

        void Eat(object ThingEaten);

        object[] Puke(int Amount);
    }

    public class PackOfCarnivores<T> where T : IConsume
    {
        public T[] pPack;
    }

    public class Cat<C> : IConsume
    {
        private List<object> _pStuffConsumed = new List<object>();

        public event EventHandler WeightChanged;

        private event EventHandler WeightStayedTheSame;

        private static EventHandler s_catDisappeared;

        public object[] StuffConsumed { get { return PStuffConsumed.ToArray(); } }

        public List<object> PStuffConsumed
        {
            get { return _pStuffConsumed; }
            set { _pStuffConsumed = value; }
        }

        public void Eat(object ThingEaten) => PStuffConsumed.Add(ThingEaten);

        public object[] Puke(int Amount)
        {
            object[] vomit;
            if (PStuffConsumed.Count < Amount)
            {
                Amount = PStuffConsumed.Count;
            }
            vomit = PStuffConsumed.GetRange(PStuffConsumed.Count - Amount, Amount).ToArray();
            PStuffConsumed.RemoveRange(PStuffConsumed.Count - Amount, Amount);
            return vomit;
        }
    }

    public class GenericArrayWrapperClass<T>
    {
        private T[] _field;
        private int _field1;

        public int myProperty
        {
            get { return 0; }
            set { _field1 = value; }
        }

        public GenericArrayWrapperClass(T[] fieldValues)
        {
            int size = fieldValues.Length;
            _field = new T[size];
            for (int i = 0; i < _field.Length; i++)
            {
                _field[i] = fieldValues[i];
            }
        }

        public T this[int index]
        {
            get { return _field[index]; }
            set { _field[index] = value; }
        }
    }

    public class GenericOuterClass<T>
    {
        public T field;

        public class GenericNestedClass<W>
        {
            public T field;
            public W field2;
        }

        public class NestedClass
        {
            public T field;
            public int field2;
        }
    }

    public class OuterClass
    {
        public int field;

        public class GenericNestedClass<W>
        {
            public int field;
            public W field2;
        }

        public class NestedClass
        {
            public string field;
            public int field2;
        }
    }

    internal interface TI_Interface1 { }
    internal interface TI_Interface2 { }
    internal interface TI_Interface3 { void DoSomething(); }

    internal struct TI_StructWithInterface : TI_Interface1 { }

    internal struct TI_Struct { }
    internal struct TI_StructWithInterfaces : TI_Interface1, TI_Interface3
    {
        public void DoSomething() { }
    }

    internal struct TI_GenericStruct<T> { public T field; }

    internal class TI_BaseClassWithInterface : TI_Interface1, TI_Interface2 { }
    internal class TI_SubClassWithInterface : TI_BaseClassWithInterface { }
    internal class TI_GenericSubClassWithInterface<T> : TI_SubClassWithInterface { }

    internal class TI_GenericBaseClass<T> { }
    internal class TI_GenericSubClass<T> : TI_GenericBaseClass<T> { }
    internal class TI_GenericSubSubClass<T> : TI_GenericSubClass<T> { }

    internal class TI_GenericSubClassWithConstraints<T> where T : TI_GenericSubClassWithInterface<T>, TI_Interface1, TI_Interface2 { }

    internal class StringImpersonator
    {
        [IndexerName("Chars")]
        public char this[int index] { get { throw null; } }
        public int Length { get { throw null; } }
        public static bool IsNullOrEmpty(string value) { throw null; }
        internal char FirstChar { get { throw null; } }
    }

    internal struct Int32Impersonator : IComparable, IFormattable, IConvertible, IComparable<int>, IEquatable<int>
    {
        public int CompareTo(object value) { throw null; }
        public int CompareTo(int value) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public bool Equals(int obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
        public string ToString(string format) { throw null; }
        public string ToString(IFormatProvider provider) { throw null; }
        public string ToString(string format, IFormatProvider provider) { throw null; }
        public static int Parse(string s) { throw null; }
        public static int Parse(string s, NumberStyles style) { throw null; }
        public static int Parse(string s, IFormatProvider provider) { throw null; }
        public static int Parse(string s, NumberStyles style, IFormatProvider provider) { throw null; }
        public static bool TryParse(string s, out int result) { throw null; }
        public static bool TryParse(string s, NumberStyles style, IFormatProvider provider, out int result) { throw null; }
        public TypeCode GetTypeCode() { throw null; }
        bool IConvertible.ToBoolean(IFormatProvider provider) { throw null; }
        char IConvertible.ToChar(IFormatProvider provider) { throw null; }
        sbyte IConvertible.ToSByte(IFormatProvider provider) { throw null; }
        byte IConvertible.ToByte(IFormatProvider provider) { throw null; }
        short IConvertible.ToInt16(IFormatProvider provider) { throw null; }
        ushort IConvertible.ToUInt16(IFormatProvider provider) { throw null; }
        int IConvertible.ToInt32(IFormatProvider provider) { throw null; }
        uint IConvertible.ToUInt32(IFormatProvider provider) { throw null; }
        long IConvertible.ToInt64(IFormatProvider provider) { throw null; }
        ulong IConvertible.ToUInt64(IFormatProvider provider) { throw null; }
        float IConvertible.ToSingle(IFormatProvider provider) { throw null; }
        double IConvertible.ToDouble(IFormatProvider provider) { throw null; }
        decimal IConvertible.ToDecimal(IFormatProvider provider) { throw null; }
        DateTime IConvertible.ToDateTime(IFormatProvider provider) { throw null; }
        object IConvertible.ToType(Type type, IFormatProvider provider) { throw null; }
    }

    namespace CustomNamespace
    {
        internal abstract class TI_AbstractBaseClass { }
        internal abstract class TI_AbstractSubClass : TI_AbstractBaseClass { }
        internal class TI_SubClass : TI_AbstractSubClass { }
    }

    internal class TI_GenericClassWithConstraints<T> where T : TI_Interface1 { }

#pragma warning restore 0169, 0067, 0649
}

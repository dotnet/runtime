// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class CustomAttributeBuilderCtor2
    {
        private const string IntField = "TestInt";
        private const string StringField = "TestStringField";
        private const string GetStringField = "GetString";
        private const string GetIntField = "GetInt";

        public static IEnumerable<object[]> TestData()
        {
            string stringValue1 = "TestString1";
            string stringValue2 = "TestString2";
            int intValue1 = 10;
            int intValue2 = 20;

            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int) },
                new object[] { stringValue1, intValue1 },
                new string[] { IntField },
                new object[] { intValue2 },
                new object[] { intValue2, null, stringValue1, intValue1 }
            };

            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int) },
                new object[] { stringValue1, intValue1 },
                new string[0],
                new object[0],
                new object[] { 0, null, stringValue1, intValue1 }
            };

            yield return new object[]
            {
                new Type[0],
                new object[0],
                new string[0],
                new object[0],
                new object[] { 0, null, null, 0 }
            };

            yield return new object[]
            {
                new Type[0],
                new object[0],
                new string[] { IntField },
                new object[] { intValue1 },
                new object[] { intValue1, null, null, 0 }
            };

            yield return new object[]
            {
                new Type[0],
                new object[0],
                new string[] { IntField, StringField },
                new object[] { intValue1, stringValue1 },
                new object[] { intValue1, stringValue1, null, 0 }
            };

            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int) },
                new object[] { stringValue1, intValue1 },
                new string[] { IntField, StringField },
                new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            yield return new object[]
            {
                new Type[0],
                new object[0],
                new string[] { GetStringField },
                new object[] { stringValue1 },
                new object[] { 0, null, stringValue1, 0 }
            };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void Ctor(Type[] ctorParams, object[] constructorArgs, string[] namedFieldNames, object[] fieldValues, object[] expected)
        {
            Type attribute = typeof(CustomAttributeBuilderTest);
            ConstructorInfo constructor = attribute.GetConstructor(ctorParams);
            FieldInfo[] namedField = Helpers.GetFields(namedFieldNames);

            CustomAttributeBuilder cab = new CustomAttributeBuilder(constructor, constructorArgs, namedField, fieldValues);

            FieldInfo[] verifyFields = Helpers.GetFields(IntField, StringField, GetStringField, GetIntField);
            VerifyCustomAttribute(cab, attribute, verifyFields, expected);
        }

        [Theory]
        [InlineData(new string[] { IntField }, new object[0], "namedFields, fieldValues")]
        [InlineData(new string[] { IntField }, new object[] { "TestString", 10 }, "namedFields, fieldValues")]
        [InlineData(new string[] { IntField, StringField }, new object[] { "TestString", 10 }, null)]
        [InlineData(new string[] { StringField }, new object[] { 10 }, null)]
        public void NamedFieldAndFieldValuesDifferentLengths_ThrowsArgumentException(string[] fieldNames, object[] fieldValues, string paramName)
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = Helpers.GetFields(fieldNames);

            Assert.Throws<ArgumentException>(paramName, () => new CustomAttributeBuilder(constructor, new object[0], namedFields, fieldValues));
        }

        [Fact]
        public void StaticCtor_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(c => c.IsStatic).First();

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void PrivateCtor_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(c => c.IsPrivate).First();

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, new object[] { false }, new FieldInfo[0], new object[0]));
        }

        [Theory]
        [InlineData(new Type[] { typeof(string), typeof(int), typeof(string), typeof(int) }, new object[] { "TestString", 10 })]
        [InlineData(new Type[] { typeof(string), typeof(int) }, new object[] { 10, "TestString" })]
        public void ConstructorArgsDontMatchConstructor_ThrowsArgumentException(Type[] ctorParams, object[] constructorArgs)
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(ctorParams);

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, constructorArgs, new FieldInfo[0], new object[0]));
        }
        
        [Fact]
        public void NullConstructor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("con", () => new CustomAttributeBuilder(null, new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void NullConstructorArgs_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("constructorArgs", () => new CustomAttributeBuilder(constructor, null, new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void NullNamedFields_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("namedFields", () => new CustomAttributeBuilder(constructor, new object[0], (FieldInfo[])null, new object[0]));
        }

        [Fact]
        public void NullFieldValues_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("fieldValues", () => new CustomAttributeBuilder(constructor, new object[0], new FieldInfo[0], null));
        }

        [Fact]
        public void NullObjectInFieldValues_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("fieldValues[0]", () => new CustomAttributeBuilder(constructor, new object[0], Helpers.GetFields(IntField, StringField), new object[] { null, 10 }));
        }

        [Fact]
        public void NullObjectInNamedFields_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("namedFields[0]", () => new CustomAttributeBuilder(constructor, new object[0], Helpers.GetFields(null, IntField), new object[] { "TestString", 10 }));
        }

        private static void VerifyCustomAttribute(CustomAttributeBuilder builder, Type attributeType, FieldInfo[] fieldNames, object[] fieldValues)
        {
            AssemblyName assemblyName = new AssemblyName("VerificationAssembly");
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            assembly.SetCustomAttribute(builder);

            object[] customAttributes = assembly.GetCustomAttributes(attributeType).ToArray();
            Assert.Equal(1, customAttributes.Length);

            object customAttribute = customAttributes[0];
            for (int i = 0; i < fieldNames.Length; ++i)
            {
                FieldInfo field = attributeType.GetField(fieldNames[i].Name);
                object expected = field.GetValue(customAttribute);
                object actual = fieldValues[i];

                Assert.Equal(expected, actual);
            }
        }
    }
}

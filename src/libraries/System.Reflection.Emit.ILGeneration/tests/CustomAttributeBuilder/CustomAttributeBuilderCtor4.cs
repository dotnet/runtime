// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class CustomAttributeBuilderCtor4
    {
        private const string IntField = "TestInt";
        private const string StringField = "TestStringField";
        private const string GetStringField = "GetString";
        private const string GetIntField = "GetInt";
        
        private const string IntProperty = "TestInt32";
        private const string StringProperty = "TestString";
        private const string GetStringProperty = "GetOnlyString";
        private const string GetIntProperty = "GetOnlyInt32";

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
                new string[] { IntProperty, StringProperty },
                new object[] { intValue2, stringValue2 },
                new string[] { IntField, StringField },
                new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int) },
                new object[] { stringValue1, intValue1 },
                new string[] { StringProperty },
                new object[] { stringValue2 },
                new string[] { IntField },
                new object[] { intValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int) },
                new object[] { stringValue1, intValue1 },
                new string[0],
                new object[0],
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
                new string[0],
                new object[0],
                new object[] { 0, null, null, 0 }
            };

            yield return new object[]
            {
                new Type[0],
                new object[0],
                new string[] { IntProperty, StringProperty },
                new object[] { intValue1, stringValue1 },
                new string[] { IntField },
                new object[] { intValue2 },
                new object[] { intValue2, stringValue1, null, 0 }
            };
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void Ctor(Type[] ctorParams, object[] constructorArgs, string[] namedPropertyNames, object[] propertyValues, string[] namedFieldNames, object[] fieldValues, object[] expected)
        {
            Type attribute = typeof(CustomAttributeBuilderTest);
            ConstructorInfo constructor = attribute.GetConstructor(ctorParams);
            FieldInfo[] namedFields = Helpers.GetFields(namedFieldNames);
            PropertyInfo[] namedProperties = Helpers.GetProperties(namedPropertyNames);

            CustomAttributeBuilder cab = new CustomAttributeBuilder(constructor, constructorArgs, namedProperties, propertyValues, namedFields, fieldValues);

            FieldInfo[] fields = Helpers.GetFields(IntField, StringField, GetStringField, GetIntField);
            VerifyCustomAttribute(cab, attribute, fields, expected);
        }

        [Theory]
        [InlineData(new string[] { IntProperty }, new object[0], "namedProperties, propertyValues")]
        [InlineData(new string[0], new object[] { 10 }, "namedProperties, propertyValues")]
        [InlineData(new string[] { IntProperty, StringProperty }, new object[] { "TestString", 10 }, null)]
        [InlineData(new string[] { GetIntProperty }, new object[] { "TestString" }, null)]
        [InlineData(new string[] { GetStringProperty }, new object[] { "TestString" }, null)]
        [InlineData(new string[] { IntProperty }, new object[] { "TestString" }, null)]
        public void NamedPropertyAndPropertyValuesDifferentLengths_ThrowsArgumentException(string[] propertyNames, object[] propertyValues, string paramName)
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            PropertyInfo[] namedProperties = Helpers.GetProperties(propertyNames);

            Assert.Throws<ArgumentException>(paramName, () => new CustomAttributeBuilder(constructor, new object[0], namedProperties, propertyValues, new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void StaticCtor_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(c => c.IsStatic).First();

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void PrivateCtor_ThrowsArgumentException()
        {
            ConstructorInfo constructor = typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic).Where(c => c.IsPrivate).First();

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, new object[] { false }, new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Theory]
        [InlineData(new Type[] { typeof(string), typeof(int), typeof(string), typeof(int) }, new object[] { "TestString", 10 })]
        [InlineData(new Type[] { typeof(string), typeof(int) }, new object[] { 10, "TestString" })]
        public void ConstructorArgsDontMatchConstructor_ThrowsArgumentException(Type[] ctorParams, object[] constructorArgs)
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(ctorParams);

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, constructorArgs, new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void NullConstructor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("con", () => new CustomAttributeBuilder(null, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void NullConstructorArgs_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("constructorArgs", () => new CustomAttributeBuilder(constructor, null, new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void NullNamedProperties_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("namedProperties", () => new CustomAttributeBuilder(constructor, new object[0], (PropertyInfo[])null, new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void NullPropertyValues_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("propertyValues", () => new CustomAttributeBuilder(constructor, new object[0], new PropertyInfo[0], null, new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void NullObjectInPropertyValues_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("propertyValues[0]", () => new CustomAttributeBuilder(constructor, new object[0], Helpers.GetProperties(IntProperty, StringProperty), new object[] { null, 10 }, new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void NullObjectInNamedProperties_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("namedProperties[0]", () => new CustomAttributeBuilder(constructor, new object[0], Helpers.GetProperties(null, IntProperty), new object[] { "TestString", 10 }, new FieldInfo[0], new object[0]));
        }

        [Fact]
        public void NullNamedFields_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("namedFields", () => new CustomAttributeBuilder(constructor, new object[0], new PropertyInfo[0], new object[0], (FieldInfo[])null, new object[0]));
        }

        [Fact]
        public void NullFieldValues_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("fieldValues", () => new CustomAttributeBuilder(constructor, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], null));
        }

        [Fact]
        public void NullObjectInFieldValues_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("fieldValues[0]", () => new CustomAttributeBuilder(constructor, new object[0], new PropertyInfo[0], new object[0], Helpers.GetFields(IntField, StringField), new object[] { null, 10 }));
        }

        [Fact]
        public void NullObjectInNamedFields_ThrowsArgumentNullException()
        {
            ConstructorInfo constructor = typeof(CustomAttributeBuilderTest).GetConstructor(new Type[0]);
            Assert.Throws<ArgumentNullException>("namedFields[0]", () => new CustomAttributeBuilder(constructor, new object[0], new PropertyInfo[0], new object[0], Helpers.GetFields(null, IntField), new object[] { "TestString", 10 }));
        }

        private void VerifyCustomAttribute(CustomAttributeBuilder builder, Type attributeType, FieldInfo[] namedFields, object[] fieldValues)
        {
            AssemblyName assemblyName = new AssemblyName("VerificationAssembly");
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);

            assembly.SetCustomAttribute(builder);

            object[] customAttributes = assembly.GetCustomAttributes(attributeType).ToArray();
            Assert.Equal(1, customAttributes.Length);

            object customAttribute = customAttributes[0];
            for (int i = 0; i < namedFields.Length; ++i)
            {
                FieldInfo field = attributeType.GetField(namedFields[i].Name);
                object expected = field.GetValue(customAttribute);
                object actual = fieldValues[i];

                Assert.Equal(expected, actual);
            }
        }
    }
}

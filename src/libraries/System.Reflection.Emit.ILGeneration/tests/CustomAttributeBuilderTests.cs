// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class CustomAttributeBuilderTests
    {
        public static IEnumerable<object[]> Ctor_TestData()
        {
            string stringValue1 = "TestString1";
            string stringValue2 = "TestString2";
            int intValue1 = 10;
            int intValue2 = 20;

            // 2 ctor, 0 properties, 1 fields
            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int) },
                new object[] { stringValue1, intValue1 },
                new string[0], new object[0],
                new object[] { intValue2, null, stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt) }, new object[] { intValue2 },
                new object[] { intValue2, null, stringValue1, intValue1 }
            };

            // 2 ctor, 0 properties, 0 fields
            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int) }, new object[] { stringValue1, intValue1 },
                new string[0], new object[0],
                new object[] { 0, null, stringValue1, intValue1 },
                new string[0], new object[0],
                new object[] { 0, null, stringValue1, intValue1 }
            };

            // 0 ctor, 0 properties, 0 fields
            yield return new object[]
            {
                new Type[0], new object[0],
                new string[0], new object[0],
                new object[] { 0, null, null, 0 },
                new string[0], new object[0],
                new object[] { 0, null, null, 0 }
            };

            // 0 ctor, 0 properties, 1 field
            yield return new object[]
            {
                new Type[0], new object[0],
                new string[0], new object[0],
                new object[] { intValue1, null, null, 0 },
                new string[] { nameof(TestAttribute.TestInt) }, new object[] { intValue1 },
                new object[] { intValue1, null, null, 0 }
            };

            // 0 ctor, 0 properties, 2 fields
            yield return new object[]
            {
                new Type[0], new object[0],
                new string[0], new object[0],
                new object[] { intValue1, stringValue1, null, 0 },
                new string[] { nameof(TestAttribute.TestInt), nameof(TestAttribute.TestStringField) }, new object[] { intValue1, stringValue1 },
                new object[] { intValue1, stringValue1, null, 0 }
            };
            
            // 2 ctor, 0 properties, 2 fields
            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int) }, new object[] { stringValue1, intValue1 },
                new string[0], new object[0],
                new object[] { intValue2, stringValue2, stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt), nameof(TestAttribute.TestStringField) }, new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            // 0 ctor, 0 properties,1 field
            yield return new object[]
            {
                new Type[0], new object[0],
                new string[0], new object[0],
                new object[] { 0, stringValue1, null, 0 },
                new string[] { nameof(TestAttribute.TestStringField) }, new object[] { stringValue1 },
                new object[] { 0, stringValue1, null, 0 }
            };

            // 2 ctor, 2 properties, 0 fields
            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int) }, new object[] { stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt32), nameof(TestAttribute.TestString) }, new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 },
                new object[0], new object[0],
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            // 2 ctor, 1 property, 0 fields
            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int) }, new object[] { stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt32) }, new object[] { intValue2 },
                new object[] { intValue2, null, stringValue1, intValue1 },
                new object[0], new object[0],
                new object[] { intValue2, null, stringValue1, intValue1 }
            };

            // 0 ctor, 1 property, 0 fields
            yield return new object[]
            {
                new Type[0], new object[0],
                new string[] { nameof(TestAttribute.TestInt32) }, new object[] { intValue2 },
                new object[] { intValue2, null, null, 0 },
                new object[0], new object[0],
                new object[] { intValue2, null, null, 0 }
            };

            // 0 ctor, 2 properties, 0 fields
            yield return new object[]
            {
                new Type[0], new object[0],
                new string[] { nameof(TestAttribute.TestInt32), nameof(TestAttribute.TestString) }, new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, null, 0 },
                new object[0], new object[0],
                new object[] { intValue2, stringValue2, null, 0 }
            };

            // 4 ctor, 0 fields, 2 properties
            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int), typeof(string), typeof(int) }, new object[] { stringValue1, intValue1, stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt32), nameof(TestAttribute.TestString) }, new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 },
                new string[0], new object[0],
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            // 2 ctor, 2 property, 2 field
            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int) }, new object[] { stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt32), nameof(TestAttribute.TestString) }, new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt), nameof(TestAttribute.TestStringField) }, new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            // 2 ctor, 1 property, 1 field
            yield return new object[]
            {
                new Type[] { typeof(string), typeof(int) }, new object[] { stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestString) }, new object[] { stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt) }, new object[] { intValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            // 0 ctor, 2 property, 1 field
            yield return new object[]
            {
                new Type[0], new object[0],
                new string[] { nameof(TestAttribute.TestInt32), nameof(TestAttribute.TestString) }, new object[] { intValue1, stringValue1 },
                new object[] { intValue2, stringValue1, null, 0 },
                new string[] { nameof(TestAttribute.TestInt) }, new object[] { intValue2 },
                new object[] { intValue2, stringValue1, null, 0 }
            };
        }

        [Theory]
        [MemberData(nameof(Ctor_TestData))]
        public static void Ctor(Type[] ctorTypes, object[] constructorArgs,
                                string[] propertyNames, object[] propertyValues,
                                object[] expectedPropertyValues,
                                string[] fieldNames, object[] fieldValues,
                                object[] expectedFieldValues)
        {
            ConstructorInfo constructor = typeof(TestAttribute).GetConstructor(ctorTypes);
            PropertyInfo[] namedProperties = Helpers.GetProperties(propertyNames);
            FieldInfo[] namedFields = Helpers.GetFields(fieldNames);
            
            Action<CustomAttributeBuilder> verify = attr =>
            {
                VerifyCustomAttributeBuilder(attr, TestAttribute.AllProperties, expectedPropertyValues, TestAttribute.AllFields, expectedFieldValues);
            };
            
            if (namedProperties.Length == 0)
            {
                if (namedFields.Length == 0)
                {
                    // Use CustomAttributeBuilder(ConstructorInfo, object[])
                    CustomAttributeBuilder attribute1 = new CustomAttributeBuilder(constructor, constructorArgs);
                    verify(attribute1);
                }
                // Use CustomAttributeBuilder(ConstructorInfo, object[], FieldInfo[], object[])
                CustomAttributeBuilder attribute2 = new CustomAttributeBuilder(constructor, constructorArgs, namedFields, fieldValues);
                verify(attribute2);
            }
            if (namedFields.Length == 0)
            {
                // Use CustomAttributeBuilder(ConstructorInfo, object[], PropertyInfo[], object[])
                CustomAttributeBuilder attribute3 = new CustomAttributeBuilder(constructor, constructorArgs, namedProperties, propertyValues);
                verify(attribute3);
            }
            // Use CustomAttributeBuilder(ConstructorInfo, object[], PropertyInfo[], object[], FieldInfo[], object[])
            CustomAttributeBuilder attribute4 = new CustomAttributeBuilder(constructor, constructorArgs, namedProperties, propertyValues, namedFields, fieldValues);
            verify(attribute4);
        }
        
        private static void VerifyCustomAttributeBuilder(CustomAttributeBuilder builder,
                                                        PropertyInfo[] propertyNames, object[] propertyValues,
                                                        FieldInfo[] fieldNames, object[] fieldValues)
        {
            AssemblyName assemblyName = new AssemblyName("VerificationAssembly");
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            assembly.SetCustomAttribute(builder);

            object[] customAttributes = assembly.GetCustomAttributes().ToArray();
            Assert.Equal(1, customAttributes.Length);

            object customAttribute = customAttributes[0];
            for (int i = 0; i < fieldNames.Length; ++i)
            {
                FieldInfo field = typeof(TestAttribute).GetField(fieldNames[i].Name);
                Assert.Equal(fieldValues[i], field.GetValue(customAttribute));
            }

            for (int i = 0; i < propertyNames.Length; ++i)
            {
                PropertyInfo property = typeof(TestAttribute).GetProperty(propertyNames[i].Name);
                Assert.Equal(propertyValues[i], property.GetValue(customAttribute));
            }
        }

        [Fact]
        public static void NullConstructor_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("con", () => new CustomAttributeBuilder(null, new object[0]));
            Assert.Throws<ArgumentNullException>("con", () => new CustomAttributeBuilder(null, new object[0], new FieldInfo[0], new object[0]));
            Assert.Throws<ArgumentNullException>("con", () => new CustomAttributeBuilder(null, new object[0], new PropertyInfo[0], new object[0]));
            Assert.Throws<ArgumentNullException>("con", () => new CustomAttributeBuilder(null, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public static void StaticConstructor_ThrowsArgumentException()
        {
            ConstructorInfo con = typeof(TestClass).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).First();

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0]));
            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new FieldInfo[0], new object[0]));
            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0]));
            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }
        
        [Fact]
        public static void PrivateConstructor_ThrowsArgumentException()
        {
            ConstructorInfo con = typeof(TestClass).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0]));
            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new FieldInfo[0], new object[0]));
            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0]));
            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public static void NullConstructorArgs_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestClass).GetConstructor(new Type[] { typeof(int) });

            Assert.Throws<ArgumentNullException>("constructorArgs", () => new CustomAttributeBuilder(con, null));
            Assert.Throws<ArgumentNullException>("constructorArgs", () => new CustomAttributeBuilder(con, null, new FieldInfo[0], new object[0]));
            Assert.Throws<ArgumentNullException>("constructorArgs", () => new CustomAttributeBuilder(con, null, new PropertyInfo[0], new object[0]));
            Assert.Throws<ArgumentNullException>("constructorArgs", () => new CustomAttributeBuilder(con, null, new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Theory]
        [InlineData(new Type[] { typeof(int) }, new object[] { 123, false })]
        [InlineData(new Type[] { typeof(int), typeof(bool) }, new object[] { false, 123 })]
        [InlineData(new Type[] { typeof(string), typeof(int), typeof(string), typeof(int) }, new object[] { "TestString", 10 })]
        public void ConstructorInfo_ObjectArray_NonMatching_ThrowsArgumentException(Type[] paramTypes, object[] paramValues)
        {
            ConstructorInfo constructor = typeof(TestClass).GetConstructor(paramTypes);

            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, paramValues));
            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, paramValues, new FieldInfo[0], new object[0]));
            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, paramValues, new PropertyInfo[0], new object[0]));
            Assert.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(constructor, paramValues, new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public static void NullNamedFields_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestClass).GetConstructor(new Type[0]);

            Assert.Throws<ArgumentNullException>("namedFields", () => new CustomAttributeBuilder(con, new object[0], (FieldInfo[])null, new object[0]));
            Assert.Throws<ArgumentNullException>("namedFields", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], null, new object[0]));
        }

        [Fact]
        public static void NullFieldValues_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestClass).GetConstructor(new Type[0]);

            Assert.Throws<ArgumentNullException>("fieldValues", () => new CustomAttributeBuilder(con, new object[0], new FieldInfo[0], null));
            Assert.Throws<ArgumentNullException>("fieldValues", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], null));
        }

        [Fact]
        public static void NullObjectInNamedFields_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestClass).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = new FieldInfo[] { null };

            Assert.Throws<ArgumentNullException>("namedFields[0]", () => new CustomAttributeBuilder(con, new object[0], namedFields, new object[1]));
            Assert.Throws<ArgumentNullException>("namedFields[0]", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], namedFields, new object[1]));
        }

        [Fact]
        public static void NullObjectInFieldValues_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestClass).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = Helpers.GetFields(nameof(TestAttribute.TestInt));
            object[] fieldValues = new object[] { null };

            Assert.Throws<ArgumentNullException>("fieldValues[0]", () => new CustomAttributeBuilder(con, new object[0], namedFields, fieldValues));
            Assert.Throws<ArgumentNullException>("fieldValues[0]", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], namedFields, fieldValues));
        }

        [Theory]
        [InlineData(new string[] { nameof(TestAttribute.TestInt) }, new object[0], "namedFields, fieldValues")]
        [InlineData(new string[] { nameof(TestAttribute.TestInt) }, new object[] { "TestString", 10 }, "namedFields, fieldValues")]
        [InlineData(new string[] { nameof(TestAttribute.TestInt), nameof(TestAttribute.TestStringField) }, new object[] { "TestString", 10 }, null)]
        [InlineData(new string[] { nameof(TestAttribute.TestStringField) }, new object[] { 10 }, null)]
        public void NamedFieldAndFieldValuesDifferentLengths_ThrowsArgumentException(string[] fieldNames, object[] fieldValues, string paramName)
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = Helpers.GetFields(fieldNames);

            Assert.Throws<ArgumentException>(paramName, () => new CustomAttributeBuilder(con, new object[0], namedFields, fieldValues));
            Assert.Throws<ArgumentException>(paramName, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], namedFields, fieldValues));
        }

        [Fact]
        public static void NullNamedProperties_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestClass).GetConstructor(new Type[0]);

            Assert.Throws<ArgumentNullException>("namedProperties", () => new CustomAttributeBuilder(con, new object[0], (PropertyInfo[])null, new object[0]));
            Assert.Throws<ArgumentNullException>("namedProperties", () => new CustomAttributeBuilder(con, new object[0], null, new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public static void NullPropertyValues_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestClass).GetConstructor(new Type[0]);

            Assert.Throws<ArgumentNullException>("propertyValues", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], null));
            Assert.Throws<ArgumentNullException>("propertyValues", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], null, new FieldInfo[0], new object[0]));
        }

        [Fact]
        public static void NullObjectInNamedProperties_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestClass).GetConstructor(new Type[0]);
            PropertyInfo[] namedProperties = new PropertyInfo[] { null };

            Assert.Throws<ArgumentNullException>("namedProperties[0]", () => new CustomAttributeBuilder(con, new object[0], namedProperties, new object[1]));
            Assert.Throws<ArgumentNullException>("namedProperties[0]", () => new CustomAttributeBuilder(con, new object[0], namedProperties, new object[1], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public static void NullObjectInPropertyValues_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestClass).GetConstructor(new Type[0]);
            PropertyInfo[] namedProperties = Helpers.GetProperties(nameof(TestAttribute.TestInt32));
            object[] propertyValues = new object[] { null };

            Assert.Throws<ArgumentNullException>("propertyValues[0]", () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues));
            Assert.Throws<ArgumentNullException>("propertyValues[0]", () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues, new FieldInfo[0], new object[0]));
        }

        [Theory]
        [InlineData(new string[] { nameof(TestAttribute.TestInt32) }, new object[0], "namedProperties, propertyValues")]
        [InlineData(new string[0], new object[] { 10 }, "namedProperties, propertyValues")]
        [InlineData(new string[] { nameof(TestAttribute.TestInt32), nameof(TestAttribute.TestString) }, new object[] { "TestString", 10 }, null)]
        [InlineData(new string[] { nameof(TestAttribute.GetOnlyInt32) }, new object[] { "TestString" }, null)]
        [InlineData(new string[] { nameof(TestAttribute.GetOnlyString) }, new object[] { "TestString" }, null)]
        [InlineData(new string[] { nameof(TestAttribute.TestInt32) }, new object[] { "TestString" }, null)]
        public void NamedPropertyAndPropertyValuesDifferentLengths_ThrowsArgumentException(string[] propertyNames, object[] propertyValues, string paramName)
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            PropertyInfo[] namedProperties = Helpers.GetProperties(propertyNames);

            Assert.Throws<ArgumentException>(paramName, () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues));
            Assert.Throws<ArgumentException>(paramName, () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues, new FieldInfo[0], new object[0]));
        }

        protected class TestClass
        {
            public TestClass() { }
            public TestClass(int i) { }
            public TestClass(int i, bool b) { }
            public TestClass(string s1, int i1, string s2, int i2) { }

            private TestClass(int i, int j, int k) { }

            static TestClass() { }
        }
    }
}

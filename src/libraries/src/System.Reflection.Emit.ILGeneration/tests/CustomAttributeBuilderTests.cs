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
    }
}

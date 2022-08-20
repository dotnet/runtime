// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
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
                typeof(TestAttribute).GetConstructor(new Type[] { typeof(string), typeof(int) }), new object[] { stringValue1, intValue1 },
                new string[0], new object[0],
                new object[] { intValue2, null, stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt) }, new object[] { intValue2 },
                new object[] { intValue2, null, stringValue1, intValue1 }
            };

            // 2 ctor, 0 properties, 0 fields
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[] { typeof(string), typeof(int) }), new object[] { stringValue1, intValue1 },
                new string[0], new object[0],
                new object[] { 0, null, stringValue1, intValue1 },
                new string[0], new object[0],
                new object[] { 0, null, stringValue1, intValue1 }
            };

            // 0 ctor, 0 properties, 0 fields
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[0]), new object[0],
                new string[0], new object[0],
                new object[] { 0, null, null, 0 },
                new string[0], new object[0],
                new object[] { 0, null, null, 0 }
            };

            // 0 ctor, 0 properties, 1 field
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[0]), new object[0],
                new string[0], new object[0],
                new object[] { intValue1, null, null, 0 },
                new string[] { nameof(TestAttribute.TestInt) }, new object[] { intValue1 },
                new object[] { intValue1, null, null, 0 }
            };

            // 0 ctor, 0 properties, 2 fields
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[0]), new object[0],
                new string[0], new object[0],
                new object[] { intValue1, stringValue1, null, 0 },
                new string[] { nameof(TestAttribute.TestInt), nameof(TestAttribute.TestStringField) }, new object[] { intValue1, stringValue1 },
                new object[] { intValue1, stringValue1, null, 0 }
            };

            // 2 ctor, 0 properties, 2 fields
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[] { typeof(string), typeof(int) }), new object[] { stringValue1, intValue1 },
                new string[0], new object[0],
                new object[] { intValue2, stringValue2, stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt), nameof(TestAttribute.TestStringField) }, new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            // 0 ctor, 0 properties,1 field
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[0]), new object[0],
                new string[0], new object[0],
                new object[] { 0, stringValue1, null, 0 },
                new string[] { nameof(TestAttribute.TestStringField) }, new object[] { stringValue1 },
                new object[] { 0, stringValue1, null, 0 }
            };

            // 2 ctor, 2 properties, 0 fields
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[] { typeof(string), typeof(int) }), new object[] { stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt32), nameof(TestAttribute.TestString) }, new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 },
                new object[0], new object[0],
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            // 2 ctor, 1 property, 0 fields
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[] { typeof(string), typeof(int) }), new object[] { stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt32) }, new object[] { intValue2 },
                new object[] { intValue2, null, stringValue1, intValue1 },
                new object[0], new object[0],
                new object[] { intValue2, null, stringValue1, intValue1 }
            };

            // 0 ctor, 1 property, 0 fields
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[0]), new object[0],
                new string[] { nameof(TestAttribute.TestInt32) }, new object[] { intValue2 },
                new object[] { intValue2, null, null, 0 },
                new object[0], new object[0],
                new object[] { intValue2, null, null, 0 }
            };

            // 0 ctor, 2 properties, 0 fields
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[0]), new object[0],
                new string[] { nameof(TestAttribute.TestInt32), nameof(TestAttribute.TestString) }, new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, null, 0 },
                new object[0], new object[0],
                new object[] { intValue2, stringValue2, null, 0 }
            };

            // 4 ctor, 0 fields, 2 properties
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[] { typeof(string), typeof(int), typeof(string), typeof(int) }), new object[] { stringValue1, intValue1, stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt32), nameof(TestAttribute.TestString) }, new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 },
                new string[0], new object[0],
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            // 2 ctor, 2 property, 2 field
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[] { typeof(string), typeof(int) }), new object[] { stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt32), nameof(TestAttribute.TestString) }, new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt), nameof(TestAttribute.TestStringField) }, new object[] { intValue2, stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            // 2 ctor, 1 property, 1 field
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[] { typeof(string), typeof(int) }), new object[] { stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestString) }, new object[] { stringValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 },
                new string[] { nameof(TestAttribute.TestInt) }, new object[] { intValue2 },
                new object[] { intValue2, stringValue2, stringValue1, intValue1 }
            };

            // 0 ctor, 2 property, 1 field
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[0]), new object[0],
                new string[] { nameof(TestAttribute.TestInt32), nameof(TestAttribute.TestString) }, new object[] { intValue1, stringValue1 },
                new object[] { intValue2, stringValue1, null, 0 },
                new string[] { nameof(TestAttribute.TestInt) }, new object[] { intValue2 },
                new object[] { intValue2, stringValue1, null, 0 }
            };

            // 2 ctor, 1 property, 0 field
            string shortString = new string('a', 128);
            string longString = new string('a', 16384);
            yield return new object[]
            {
                typeof(TestAttribute).GetConstructor(new Type[] { typeof(string), typeof(int) }), new object[] { shortString, intValue1 },
                new string[] { nameof(TestAttribute.TestString) }, new object[] { longString },
                new object[] { 0, longString, shortString, intValue1 },
                new string[0], new object[0],
                new object[] { 0, longString, shortString, intValue1 }
            };

            // 0 ctor, 1 property, 1 field
            yield return new object[]
            {
                typeof(SubAttribute).GetConstructor(new Type[0]), new object[0],
                new string[] { nameof(TestAttribute.TestString) }, new object[] { stringValue1 },
                new object[] { intValue1, stringValue1, null, 0 },
                new string[] { nameof(TestAttribute.TestInt) }, new object[] { intValue1 },
                new object[] { intValue1, stringValue1, null, 0 }
            };
        }

        [Theory]
        [MemberData(nameof(Ctor_TestData))]
        public static void Ctor(ConstructorInfo con, object[] constructorArgs,
                                string[] propertyNames, object[] propertyValues,
                                object[] expectedPropertyValues,
                                string[] fieldNames, object[] fieldValues,
                                object[] expectedFieldValues)
        {
            PropertyInfo[] namedProperties = Helpers.GetProperties(typeof(TestAttribute), propertyNames);
            FieldInfo[] namedFields = Helpers.GetFields(typeof(TestAttribute), fieldNames);

            void Verify(CustomAttributeBuilder attr)
            {
                VerifyCustomAttributeBuilder(attr, TestAttribute.AllProperties, expectedPropertyValues, TestAttribute.AllFields, expectedFieldValues);
            }

            if (namedProperties.Length == 0)
            {
                if (namedFields.Length == 0)
                {
                    // Use CustomAttributeBuilder(ConstructorInfo, object[])
                    CustomAttributeBuilder attribute1 = new CustomAttributeBuilder(con, constructorArgs);
                    Verify(attribute1);
                }
                // Use CustomAttributeBuilder(ConstructorInfo, object[], FieldInfo[], object[])
                CustomAttributeBuilder attribute2 = new CustomAttributeBuilder(con, constructorArgs, namedFields, fieldValues);
                Verify(attribute2);
            }
            if (namedFields.Length == 0)
            {
                // Use CustomAttributeBuilder(ConstructorInfo, object[], PropertyInfo[], object[])
                CustomAttributeBuilder attribute3 = new CustomAttributeBuilder(con, constructorArgs, namedProperties, propertyValues);
                Verify(attribute3);
            }
            // Use CustomAttributeBuilder(ConstructorInfo, object[], PropertyInfo[], object[], FieldInfo[], object[])
            CustomAttributeBuilder attribute4 = new CustomAttributeBuilder(con, constructorArgs, namedProperties, propertyValues, namedFields, fieldValues);
            Verify(attribute4);
        }

        private static void VerifyCustomAttributeBuilder(CustomAttributeBuilder builder,
                                                        PropertyInfo[] propertyNames, object[] propertyValues,
                                                        FieldInfo[] fieldNames, object[] fieldValues)
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
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
        public static void Ctor_AllPrimitives()
        {
            ConstructorInfo con = typeof(Primitives).GetConstructors()[0];
            object[] constructorArgs = new object[]
            {
                (sbyte)1, (byte)2, (short)3, (ushort)4, 5, (uint)6, (long)7, (ulong)8,
                (SByteEnum)9, (ByteEnum)10, (ShortEnum)11, (UShortEnum)12, (IntEnum)13, (UIntEnum)14, (LongEnum)15, (ULongEnum)16,
                (char)17, true, 2.0f, 2.1,
                "abc", typeof(object), new int[] { 24, 25, 26 }, null
            };

            PropertyInfo[] namedProperties = Helpers.GetProperties(typeof(Primitives), new string[]
            {
                nameof(Primitives.SByteProperty), nameof(Primitives.ByteProperty), nameof(Primitives.ShortProperty), nameof(Primitives.UShortProperty), nameof(Primitives.IntProperty), nameof(Primitives.UIntProperty), nameof(Primitives.LongProperty), nameof(Primitives.ULongProperty),
                nameof(Primitives.SByteEnumProperty), nameof(Primitives.ByteEnumProperty), nameof(Primitives.ShortEnumProperty), nameof(Primitives.UShortEnumProperty), nameof(Primitives.IntEnumProperty), nameof(Primitives.UIntEnumProperty), nameof(Primitives.LongEnumProperty), nameof(Primitives.ULongEnumProperty),
                nameof(Primitives.CharProperty), nameof(Primitives.BoolProperty), nameof(Primitives.FloatProperty), nameof(Primitives.DoubleProperty),
                nameof(Primitives.StringProperty), nameof(Primitives.TypeProperty), nameof(Primitives.ArrayProperty), nameof(Primitives.ObjectProperty)
            });
            object[] propertyValues = new object[]
            {
                (sbyte)27, (byte)28, (short)29, (ushort)30, 31, (uint)32, (long)33, (ulong)34,
                (SByteEnum)35, (ByteEnum)36, (ShortEnum)37, (UShortEnum)38, (IntEnum)39, (UIntEnum)40, (LongEnum)41, (ULongEnum)42,
                (char)43, false, 4.4f, 4.5,
                "def", typeof(bool), new int[] { 48, 49, 50 }, "stringAsObject"
            };

            FieldInfo[] namedFields = Helpers.GetFields(typeof(Primitives), new string[]
            {
                nameof(Primitives.SByteField), nameof(Primitives.ByteField), nameof(Primitives.ShortField), nameof(Primitives.UShortField), nameof(Primitives.IntField), nameof(Primitives.UIntField), nameof(Primitives.LongField), nameof(Primitives.ULongField),
                nameof(Primitives.SByteEnumField), nameof(Primitives.ByteEnumField), nameof(Primitives.ShortEnumField), nameof(Primitives.UShortEnumField), nameof(Primitives.IntEnumField), nameof(Primitives.UIntEnumField), nameof(Primitives.LongEnumField), nameof(Primitives.ULongEnumField),
                nameof(Primitives.CharField), nameof(Primitives.BoolField), nameof(Primitives.FloatField), nameof(Primitives.DoubleField),
                nameof(Primitives.StringField), nameof(Primitives.TypeField), nameof(Primitives.ArrayField), nameof(Primitives.ObjectField)
            });
            object[] fieldValues = new object[]
            {
                (sbyte)51, (byte)52, (short)53, (ushort)54, 55, (uint)56, (long)57, (ulong)58,
                (SByteEnum)59, (ByteEnum)60, (ShortEnum)61, (UShortEnum)62, (IntEnum)63, (UIntEnum)64, (LongEnum)65, (ULongEnum)66,
                (char)67, true, 6.8f, 6.9,
                null, null, null, 70
            };

            CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(con, constructorArgs, namedProperties, propertyValues, namedFields, fieldValues);

            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.SetCustomAttribute(attributeBuilder);

            object[] customAttributes = assembly.GetCustomAttributes().ToArray();
            Assert.Equal(1, customAttributes.Length);

            Primitives attribute = (Primitives)customAttributes[0];

            // Constructor: primitives
            Assert.Equal(constructorArgs[0], attribute.SByteConstructor);
            Assert.Equal(constructorArgs[1], attribute.ByteConstructor);
            Assert.Equal(constructorArgs[2], attribute.ShortConstructor);
            Assert.Equal(constructorArgs[3], attribute.UShortConstructor);
            Assert.Equal(constructorArgs[4], attribute.IntConstructor);
            Assert.Equal(constructorArgs[5], attribute.UIntConstructor);
            Assert.Equal(constructorArgs[6], attribute.LongConstructor);
            Assert.Equal(constructorArgs[7], attribute.ULongConstructor);

            // Constructors: enums
            Assert.Equal(constructorArgs[8], attribute.SByteEnumConstructor);
            Assert.Equal(constructorArgs[9], attribute.ByteEnumConstructor);
            Assert.Equal(constructorArgs[10], attribute.ShortEnumConstructor);
            Assert.Equal(constructorArgs[11], attribute.UShortEnumConstructor);
            Assert.Equal(constructorArgs[12], attribute.IntEnumConstructor);
            Assert.Equal(constructorArgs[13], attribute.UIntEnumConstructor);
            Assert.Equal(constructorArgs[14], attribute.LongEnumConstructor);
            Assert.Equal(constructorArgs[15], attribute.ULongEnumConstructor);

            // Constructors: other primitives
            Assert.Equal(constructorArgs[16], attribute.CharConstructor);
            Assert.Equal(constructorArgs[17], attribute.BoolConstructor);
            Assert.Equal(constructorArgs[18], attribute.FloatConstructor);
            Assert.Equal(constructorArgs[19], attribute.DoubleConstructor);

            // Constructors: misc
            Assert.Equal(constructorArgs[20], attribute.StringConstructor);
            Assert.Equal(constructorArgs[21], attribute.TypeConstructor);
            Assert.Equal(constructorArgs[22], attribute.ArrayConstructor);
            Assert.Equal(constructorArgs[23], attribute.ObjectConstructor);

            // Field: primitives
            Assert.Equal(fieldValues[0], attribute.SByteField);
            Assert.Equal(fieldValues[1], attribute.ByteField);
            Assert.Equal(fieldValues[2], attribute.ShortField);
            Assert.Equal(fieldValues[3], attribute.UShortField);
            Assert.Equal(fieldValues[4], attribute.IntField);
            Assert.Equal(fieldValues[5], attribute.UIntField);
            Assert.Equal(fieldValues[6], attribute.LongField);
            Assert.Equal(fieldValues[7], attribute.ULongField);

            // Fields: enums
            Assert.Equal(fieldValues[8], attribute.SByteEnumField);
            Assert.Equal(fieldValues[9], attribute.ByteEnumField);
            Assert.Equal(fieldValues[10], attribute.ShortEnumField);
            Assert.Equal(fieldValues[11], attribute.UShortEnumField);
            Assert.Equal(fieldValues[12], attribute.IntEnumField);
            Assert.Equal(fieldValues[13], attribute.UIntEnumField);
            Assert.Equal(fieldValues[14], attribute.LongEnumField);
            Assert.Equal(fieldValues[15], attribute.ULongEnumField);

            // Fields: other primitives
            Assert.Equal(fieldValues[16], attribute.CharField);
            Assert.Equal(fieldValues[17], attribute.BoolField);
            Assert.Equal(fieldValues[18], attribute.FloatField);
            Assert.Equal(fieldValues[19], attribute.DoubleField);

            // Fields: misc
            Assert.Equal(fieldValues[20], attribute.StringField);
            Assert.Equal(fieldValues[21], attribute.TypeField);
            Assert.Equal(fieldValues[22], attribute.ArrayField);
            Assert.Equal(fieldValues[23], attribute.ObjectField);

            // Properties: primitives
            Assert.Equal(propertyValues[0], attribute.SByteProperty);
            Assert.Equal(propertyValues[1], attribute.ByteProperty);
            Assert.Equal(propertyValues[2], attribute.ShortProperty);
            Assert.Equal(propertyValues[3], attribute.UShortProperty);
            Assert.Equal(propertyValues[4], attribute.IntProperty);
            Assert.Equal(propertyValues[5], attribute.UIntProperty);
            Assert.Equal(propertyValues[6], attribute.LongProperty);
            Assert.Equal(propertyValues[7], attribute.ULongProperty);

            // Properties: enums
            Assert.Equal(propertyValues[8], attribute.SByteEnumProperty);
            Assert.Equal(propertyValues[9], attribute.ByteEnumProperty);
            Assert.Equal(propertyValues[10], attribute.ShortEnumProperty);
            Assert.Equal(propertyValues[11], attribute.UShortEnumProperty);
            Assert.Equal(propertyValues[12], attribute.IntEnumProperty);
            Assert.Equal(propertyValues[13], attribute.UIntEnumProperty);
            Assert.Equal(propertyValues[14], attribute.LongEnumProperty);
            Assert.Equal(propertyValues[15], attribute.ULongEnumProperty);

            // Properties: other primitives
            Assert.Equal(propertyValues[16], attribute.CharProperty);
            Assert.Equal(propertyValues[17], attribute.BoolProperty);
            Assert.Equal(propertyValues[18], attribute.FloatProperty);
            Assert.Equal(propertyValues[19], attribute.DoubleProperty);

            // Properties: misc
            Assert.Equal(propertyValues[20], attribute.StringProperty);
            Assert.Equal(propertyValues[21], attribute.TypeProperty);
            Assert.Equal(propertyValues[22], attribute.ArrayProperty);
            Assert.Equal(propertyValues[23], attribute.ObjectProperty);
        }

        public static IEnumerable<object[]> Ctor_RefEmitParameters_TestData()
        {
            AssemblyBuilder assemblyBuilder = Helpers.DynamicAssembly();
            TypeBuilder typeBuilder = assemblyBuilder.DefineDynamicModule("DynamicModule").DefineType("DynamicType", TypeAttributes.Public, typeof(Attribute));

            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[0]);
            constructorBuilder.GetILGenerator().Emit(OpCodes.Ret);

            FieldBuilder fieldBuilder = typeBuilder.DefineField("Field", typeof(int), FieldAttributes.Public);
            FieldBuilder fieldBuilderProperty = typeBuilder.DefineField("PropertyField", typeof(int), FieldAttributes.Public);
            PropertyBuilder propertyBuilder = typeBuilder.DefineProperty("Property", PropertyAttributes.None, typeof(int), new Type[0]);
            MethodBuilder setMethod = typeBuilder.DefineMethod("set_Property", MethodAttributes.Public, typeof(void), new Type[] { typeof(int) });
            ILGenerator setMethodGenerator = setMethod.GetILGenerator();
            setMethodGenerator.Emit(OpCodes.Ldarg_0);
            setMethodGenerator.Emit(OpCodes.Ldarg_1);
            setMethodGenerator.Emit(OpCodes.Stfld, fieldBuilderProperty);
            setMethodGenerator.Emit(OpCodes.Ret);
            propertyBuilder.SetSetMethod(setMethod);

            Type createdType = typeBuilder.CreateType();

            // ConstructorBuilder, PropertyInfo, FieldInfo
            yield return new object[]
            {
                constructorBuilder, new object[0],
                new PropertyInfo[] { createdType.GetProperty(propertyBuilder.Name) }, new object[] { 1 },
                new FieldInfo[] { createdType.GetField(fieldBuilder.Name) }, new object[] { 2 }
            };

            // ConstructorInfo, PropertyBuilder, FieldBuilder
            yield return new object[]
            {
                createdType.GetConstructor(new Type[0]), new object[0],
                new PropertyInfo[] { propertyBuilder }, new object[] { 1 },
                new FieldInfo[] { fieldBuilder }, new object[] { 2 }
            };

            // ConstructorBuilder, PropertyBuilder, FieldBuilder
            yield return new object[]
            {
                constructorBuilder, new object[0],
                new PropertyInfo[] { propertyBuilder }, new object[] { 1 },
                new FieldInfo[] { fieldBuilder }, new object[] { 2 }
            };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [MemberData(nameof(Ctor_RefEmitParameters_TestData))]
        public static void Ctor_RefEmitParameters(ConstructorInfo con, object[] constructorArgs,
                                                  PropertyInfo[] namedProperties, object[] propertyValues,
                                                  FieldInfo[] namedFields, object[] fieldValues)
        {
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, constructorArgs, namedProperties, propertyValues, namedFields, fieldValues);

            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.SetCustomAttribute(attribute);
            object createdAttribute = assembly.GetCustomAttributes().First();
            Assert.Equal(propertyValues[0], createdAttribute.GetType().GetField("PropertyField").GetValue(createdAttribute));
            Assert.Equal(fieldValues[0], createdAttribute.GetType().GetField("Field").GetValue(createdAttribute));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [InlineData(nameof(TestAttribute.ReadonlyField))]
        [InlineData(nameof(TestAttribute.StaticField))]
        [InlineData(nameof(TestAttribute.StaticReadonlyField))]
        public void NamedFields_ContainsReadonlyOrStaticField_Works(string name)
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = new FieldInfo[] { typeof(TestAttribute).GetField(name) };
            object[] fieldValues = new object[] { 5 };
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[0], namedFields, fieldValues);

            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.SetCustomAttribute(attribute);

            object customAttribute = assembly.GetCustomAttributes().First();
            Assert.Equal(fieldValues[0], namedFields[0].GetValue(namedFields[0].IsStatic ? null : customAttribute));
        }

        [Fact]
        public void NamedProperties_StaticProperty_Works()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            PropertyInfo[] namedProperties = new PropertyInfo[] { typeof(TestAttribute).GetProperty(nameof(TestAttribute.StaticProperty)) };
            object[] propertyValues = new object[] { 5 };
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues);

            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.SetCustomAttribute(attribute);

            object customAttribute = assembly.GetCustomAttributes().First();
            Assert.Equal(propertyValues[0], TestAttribute.StaticProperty);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [InlineData(typeof(PrivateAttribute))]
        [InlineData(typeof(NotAnAttribute))]
        public static void ClassNotSupportedAsAttribute_DoesNotThrow_DoesNotSet(Type type)
        {
            ConstructorInfo con = type.GetConstructor(new Type[0]);
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[0]);

            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.SetCustomAttribute(attribute);

            Assert.Empty(assembly.GetCustomAttributes());
        }

        [Fact]
        public static void NullConstructor_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("con", () => new CustomAttributeBuilder(null, new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("con", () => new CustomAttributeBuilder(null, new object[0], new FieldInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("con", () => new CustomAttributeBuilder(null, new object[0], new PropertyInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("con", () => new CustomAttributeBuilder(null, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public static void StaticConstructor_ThrowsArgumentException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).First();

            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new FieldInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public static void PrivateConstructor_ThrowsArgumentException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First();

            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new FieldInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [InlineData(CallingConventions.Any)]
        [InlineData(CallingConventions.VarArgs)]
        public static void ConstructorHasNonStandardCallingConvention_ThrowsArgumentException(CallingConventions callingConvention)
        {
            TypeBuilder typeBuilder = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, callingConvention, new Type[0]);
            constructorBuilder.GetILGenerator().Emit(OpCodes.Ret);

            ConstructorInfo con = typeBuilder.CreateType().GetConstructor(new Type[0]);

            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new FieldInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public static void NullConstructorArgs_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[] { typeof(int) });

            AssertExtensions.Throws<ArgumentNullException>("constructorArgs", () => new CustomAttributeBuilder(con, null));
            AssertExtensions.Throws<ArgumentNullException>("constructorArgs", () => new CustomAttributeBuilder(con, null, new FieldInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("constructorArgs", () => new CustomAttributeBuilder(con, null, new PropertyInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("constructorArgs", () => new CustomAttributeBuilder(con, null, new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        public static IEnumerable<object[]> NotSupportedObject_Constructor_TestData()
        {
            yield return new object[] { new int[0, 0] };
            yield return new object[] { Enum.GetValues(CreateEnum(typeof(char), 'a')).GetValue(0) };
            yield return new object[] { Enum.GetValues(CreateEnum(typeof(bool), true)).GetValue(0) };
        }

        public static IEnumerable<object[]> FloatEnum_DoubleEnum_TestData()
        {
            yield return new object[] { Enum.GetValues(CreateEnum(typeof(float), 0.0f)).GetValue(0) };
            yield return new object[] { Enum.GetValues(CreateEnum(typeof(double), 0.0)).GetValue(0) };
        }

        public static IEnumerable<object[]> NotSupportedObject_Others_TestData()
        {
            yield return new object[] { new Guid() };
            yield return new object[] { new int[5, 5] };
        }

        [Theory]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Netfx doesn't support Enum.GetEnumName for float or double enums.")]
        [MemberData(nameof(FloatEnum_DoubleEnum_TestData))]
        public void ConstructorArgsContainsFloatEnumOrDoubleEnum_ThrowsArgumentException(object value)
        {
            NotSupportedObjectInConstructorArgs_ThrowsArgumentException(value);
        }

        [Theory]
        [MemberData(nameof(NotSupportedObject_Constructor_TestData))]
        [MemberData(nameof(NotSupportedObject_Others_TestData))]
        public static void NotSupportedObjectInConstructorArgs_ThrowsArgumentException(object value)
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[] { typeof(object) });
            object[] constructorArgs = new object[] { value };

            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new FieldInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Theory]
        [InlineData(new Type[] { typeof(int) }, new object[] { 123, false })]
        [InlineData(new Type[] { typeof(int), typeof(bool) }, new object[] { false, 123 })]
        [InlineData(new Type[] { typeof(string), typeof(int), typeof(string), typeof(int) }, new object[] { "TestString", 10 })]
        public void ConstructorAndConstructorArgsDontMatch_ThrowsArgumentException(Type[] constructorTypes, object[] constructorArgs)
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(constructorTypes);

            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, constructorArgs));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, constructorArgs, new FieldInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, constructorArgs, new PropertyInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, constructorArgs, new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        public static IEnumerable<object[]> IntPtrAttributeTypes_TestData()
        {
            yield return new object[] { typeof(IntPtr), (IntPtr)1 };
            yield return new object[] { typeof(UIntPtr), (UIntPtr)1 };
        }

        public static IEnumerable<object[]> InvalidAttributeTypes_TestData()
        {
            yield return new object[] { typeof(Guid), new Guid() };
            yield return new object[] { typeof(int[,]), new int[5, 5] };
            yield return new object[] { CreateEnum(typeof(char), 'a'), 'a' };
            yield return new object[] { CreateEnum(typeof(bool), false), true };
            yield return new object[] { CreateEnum(typeof(float), 1.0f), 1.0f };
            yield return new object[] { CreateEnum(typeof(double), 1.0), 1.0 };
            yield return new object[] { CreateEnum(typeof(IntPtr)), (IntPtr)1 };
            yield return new object[] { CreateEnum(typeof(UIntPtr)), (UIntPtr)1 };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Coreclr fixed an issue where IntPtr/UIntPtr in constructorParameters causes a corrupt created binary.")]
        [MemberData(nameof(IntPtrAttributeTypes_TestData))]
        public void ConstructorParametersContainsIntPtrOrUIntPtrArgument_ThrowsArgumentException(Type type, object value)
        {
            ConstructorParametersNotSupportedInAttributes_ThrowsArgumentException(type, value);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [MemberData(nameof(InvalidAttributeTypes_TestData))]
        public void ConstructorParametersNotSupportedInAttributes_ThrowsArgumentException(Type type, object value)
        {
            TypeBuilder typeBuilder = Helpers.DynamicType(TypeAttributes.Public);
            ConstructorInfo con = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { type });
            object[] constructorArgs = new object[] { value };

            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, constructorArgs));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, constructorArgs, new FieldInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, constructorArgs, new PropertyInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, constructorArgs, new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Used to throw a NullReferenceException, see https://github.com/dotnet/runtime/issues/18552.")]
        public void NullValueForPrimitiveTypeInConstructorArgs_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[] { typeof(int) });
            object[] constructorArgs = new object[] { null };

            AssertExtensions.Throws<ArgumentNullException>("constructorArgs[0]", () => new CustomAttributeBuilder(con, constructorArgs));
            AssertExtensions.Throws<ArgumentNullException>("constructorArgs[0]", () => new CustomAttributeBuilder(con, constructorArgs, new FieldInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("constructorArgs[0]", () => new CustomAttributeBuilder(con, constructorArgs, new PropertyInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("constructorArgs[0]", () => new CustomAttributeBuilder(con, constructorArgs, new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        public static IEnumerable<object[]> NotSupportedPrimitives_TestData()
        {
            yield return new object[] { (IntPtr)1 };
            yield return new object[] { (UIntPtr)1 };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Coreclr fixed an issue where IntPtr/UIntPtr in constructorArgs causes a corrupt created binary.")]
        [MemberData(nameof(NotSupportedPrimitives_TestData))]
        public static void NotSupportedPrimitiveInConstructorArgs_ThrowsArgumentException(object value)
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[] { typeof(object) });
            object[] constructorArgs = new object[] { value };

            AssertExtensions.Throws<ArgumentException>("constructorArgs[0]", () => new CustomAttributeBuilder(con, constructorArgs));
            AssertExtensions.Throws<ArgumentException>("constructorArgs[0]", () => new CustomAttributeBuilder(con, constructorArgs, new FieldInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>("constructorArgs[0]", () => new CustomAttributeBuilder(con, constructorArgs, new PropertyInfo[0], new object[0]));
            AssertExtensions.Throws<ArgumentException>("constructorArgs[0]", () => new CustomAttributeBuilder(con, constructorArgs, new PropertyInfo[0], new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public static void DynamicTypeInConstructorArgs_ThrowsFileNotFoundExceptionOnCreation()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            TypeBuilder type = assembly.DefineDynamicModule("DynamicModule").DefineType("DynamicType");
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[] { typeof(object) });
            object[] constructorArgs = new object[] { type };

            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, constructorArgs);
            assembly.SetCustomAttribute(attribute);

            Assert.Throws<FileNotFoundException>(() => assembly.GetCustomAttributes());
        }

        [Fact]
        public static void NullNamedFields_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);

            AssertExtensions.Throws<ArgumentNullException>("namedFields", () => new CustomAttributeBuilder(con, new object[0], (FieldInfo[])null, new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("namedFields", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], null, new object[0]));
        }

        [Theory]
        [MemberData(nameof(InvalidAttributeTypes_TestData))]
        public void NamedFields_FieldTypeNotSupportedInAttributes_ThrowsArgumentException(Type type, object value)
        {
            TypeBuilder typeBuilder = Helpers.DynamicType(TypeAttributes.Public);
            FieldInfo field = typeBuilder.DefineField("Field", type, FieldAttributes.Public);
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = new FieldInfo[] { field };
            object[] fieldValues = new object[] { value };

            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], namedFields, fieldValues));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], namedFields, fieldValues));
        }

        public static IEnumerable<object[]> FieldDoesntBelongToConstructorDeclaringType_TestData()
        {
            // Different declaring type
            yield return new object[] { typeof(TestAttribute).GetConstructor(new Type[0]), typeof(OtherTestAttribute).GetField(nameof(OtherTestAttribute.Field)) };

            // Base class and sub class declaring types
            yield return new object[] { typeof(TestAttribute).GetConstructor(new Type[0]), typeof(SubAttribute).GetField(nameof(SubAttribute.SubField)) };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [MemberData(nameof(FieldDoesntBelongToConstructorDeclaringType_TestData))]
        public void NamedFields_FieldDoesntBelongToConstructorDeclaringType_ThrowsArgumentException(ConstructorInfo con, FieldInfo field)
        {
            FieldInfo[] namedFields = new FieldInfo[] { field };

            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], namedFields, new object[] { 5 }));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], namedFields, new object[] { 5 }));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public void NamedFields_ContainsConstField_ThrowsArgumentException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = new FieldInfo[] { typeof(TestAttribute).GetField(nameof(TestAttribute.ConstField)) };
            object[] propertyValues = new object[] { 5 };
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[0], namedFields, propertyValues);

            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.SetCustomAttribute(attribute);

            // CustomAttributeFormatException is not exposed on .NET Core
            Exception ex = Assert.ThrowsAny<Exception>(() => assembly.GetCustomAttributes());
            Assert.Equal("System.Reflection.CustomAttributeFormatException", ex.GetType().ToString());
        }

        [Fact]
        public static void NullFieldValues_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);

            AssertExtensions.Throws<ArgumentNullException>("fieldValues", () => new CustomAttributeBuilder(con, new object[0], new FieldInfo[0], null));
            AssertExtensions.Throws<ArgumentNullException>("fieldValues", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], new FieldInfo[0], null));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public static void NullObjectInNamedFields_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = new FieldInfo[] { null };

            AssertExtensions.Throws<ArgumentNullException>("namedFields[0]", () => new CustomAttributeBuilder(con, new object[0], namedFields, new object[1]));
            AssertExtensions.Throws<ArgumentNullException>("namedFields[0]", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], namedFields, new object[1]));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public static void NullObjectInFieldValues_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = Helpers.GetFields(typeof(TestAttribute), nameof(TestAttribute.TestInt));
            object[] fieldValues = new object[] { null };

            AssertExtensions.Throws<ArgumentNullException>("fieldValues[0]", () => new CustomAttributeBuilder(con, new object[0], namedFields, fieldValues));
            AssertExtensions.Throws<ArgumentNullException>("fieldValues[0]", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], namedFields, fieldValues));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [MemberData(nameof(NotSupportedObject_Others_TestData))]
        public static void NotSupportedObjectInFieldValues_ThrowsArgumentException(object value)
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = Helpers.GetFields(typeof(TestAttribute), nameof(TestAttribute.ObjectField));
            object[] fieldValues = new object[] { value };

            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], namedFields, fieldValues));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], namedFields, fieldValues));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public static void ZeroCountMultidimensionalArrayInFieldValues_ChangesToZeroCountJaggedArray()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = Helpers.GetFields(typeof(TestAttribute), nameof(TestAttribute.ObjectField));
            object[] fieldValues = new object[] { new int[0, 0] };

            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[0], namedFields, fieldValues);
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.SetCustomAttribute(attribute);

            TestAttribute customAttribute = (TestAttribute)assembly.GetCustomAttributes().First();
            Array objectField = (Array)customAttribute.ObjectField;
            Assert.IsType<int[]>(objectField);
            Assert.Equal(0, objectField.Length);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [MemberData(nameof(NotSupportedPrimitives_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Coreclr fixed an issue where IntPtr/UIntPtr in fieldValues causes a corrupt created binary.")]
        public static void NotSupportedPrimitiveInFieldValues_ThrowsArgumentException(object value)
        {
            // Used to assert in CustomAttributeBuilder.EmitType(), not writing any CustomAttributeEncoding.
            // This created a blob that (probably) generates a CustomAttributeFormatException. In theory, this
            // could have been something more uncontrolled, so was fixed. See https://github.com/dotnet/runtime/issues/18553.
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = Helpers.GetFields(typeof(TestAttribute), nameof(TestAttribute.ObjectField));
            object[] fieldValues = new object[] { value };

            AssertExtensions.Throws<ArgumentException>("fieldValues[0]", () => new CustomAttributeBuilder(con, new object[0], namedFields, fieldValues));
            AssertExtensions.Throws<ArgumentException>("fieldValues[0]", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new FieldInfo[0], namedFields, fieldValues));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public static void DynamicTypeInPropertyValues_ThrowsFileNotFoundExceptionOnCreation()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            TypeBuilder type = assembly.DefineDynamicModule("DynamicModule").DefineType("DynamicType");
            FieldInfo[] namedFields = Helpers.GetFields(typeof(TestAttribute), nameof(TestAttribute.ObjectField));
            object[] fieldValues = new object[] { type };

            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[0], namedFields, fieldValues);
            assembly.SetCustomAttribute(attribute);

            Assert.Throws<FileNotFoundException>(() => assembly.GetCustomAttributes());
        }

        [Theory]
        [InlineData(new string[] { nameof(TestAttribute.TestInt) }, new object[0], "namedFields, fieldValues")]
        [InlineData(new string[] { nameof(TestAttribute.TestInt) }, new object[] { "TestString", 10 }, "namedFields, fieldValues")]
        [InlineData(new string[] { nameof(TestAttribute.TestInt), nameof(TestAttribute.TestStringField) }, new object[] { "TestString", 10 }, null)]
        [InlineData(new string[] { nameof(TestAttribute.TestStringField) }, new object[] { 10 }, null)]
        public void NamedFieldAndFieldValuesDifferentLengths_ThrowsArgumentException(string[] fieldNames, object[] fieldValues, string paramName)
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            FieldInfo[] namedFields = Helpers.GetFields(typeof(TestAttribute), fieldNames);

            AssertExtensions.Throws<ArgumentException>(paramName, () => new CustomAttributeBuilder(con, new object[0], namedFields, fieldValues));
            AssertExtensions.Throws<ArgumentException>(paramName, () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], new object[0], namedFields, fieldValues));
        }

        [Fact]
        public static void NullNamedProperties_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);

            AssertExtensions.Throws<ArgumentNullException>("namedProperties", () => new CustomAttributeBuilder(con, new object[0], (PropertyInfo[])null, new object[0]));
            AssertExtensions.Throws<ArgumentNullException>("namedProperties", () => new CustomAttributeBuilder(con, new object[0], null, new object[0], new FieldInfo[0], new object[0]));
        }

        [Fact]
        public static void NullPropertyValues_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);

            AssertExtensions.Throws<ArgumentNullException>("propertyValues", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], null));
            AssertExtensions.Throws<ArgumentNullException>("propertyValues", () => new CustomAttributeBuilder(con, new object[0], new PropertyInfo[0], null, new FieldInfo[0], new object[0]));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public static void NullObjectInNamedProperties_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            PropertyInfo[] namedProperties = new PropertyInfo[] { null };

            AssertExtensions.Throws<ArgumentNullException>("namedProperties[0]", () => new CustomAttributeBuilder(con, new object[0], namedProperties, new object[1]));
            AssertExtensions.Throws<ArgumentNullException>("namedProperties[0]", () => new CustomAttributeBuilder(con, new object[0], namedProperties, new object[1], new FieldInfo[0], new object[0]));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public static void IndexerInNamedProperties_ThrowsCustomAttributeFormatExceptionOnCreation()
        {
            ConstructorInfo con = typeof(IndexerAttribute).GetConstructor(new Type[0]);
            PropertyInfo[] namedProperties = new PropertyInfo[] { typeof(IndexerAttribute).GetProperty("Item") };
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[0], namedProperties, new object[] { "abc" });

            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.SetCustomAttribute(attribute);

            // CustomAttributeFormatException is not exposed on .NET Core
            Exception ex = Assert.ThrowsAny<Exception>(() => assembly.GetCustomAttributes());
            Assert.Equal("System.Reflection.CustomAttributeFormatException", ex.GetType().ToString());
        }

        [Theory]
        [MemberData(nameof(InvalidAttributeTypes_TestData))]
        [MemberData(nameof(IntPtrAttributeTypes_TestData))]
        public void NamedProperties_TypeNotSupportedInAttributes_ThrowsArgumentException(Type type, object value)
        {
            TypeBuilder typeBuilder = Helpers.DynamicType(TypeAttributes.Public);
            PropertyBuilder property = typeBuilder.DefineProperty("Property", PropertyAttributes.None, type, new Type[0]);
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            PropertyInfo[] namedProperties = new PropertyInfo[] { property };
            object[] propertyValues = new object[] { value };

            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues, new FieldInfo[0], new object[0]));
        }

        public static IEnumerable<object[]> PropertyDoesntBelongToConstructorDeclaringType_TestData()
        {
            // Different declaring type
            yield return new object[] { typeof(TestAttribute).GetConstructor(new Type[0]), typeof(OtherTestAttribute).GetProperty(nameof(OtherTestAttribute.Property)) };

            // Base class and sub class declaring types
            yield return new object[] { typeof(TestAttribute).GetConstructor(new Type[0]), typeof(SubAttribute).GetProperty(nameof(SubAttribute.SubProperty)) };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [MemberData(nameof(PropertyDoesntBelongToConstructorDeclaringType_TestData))]
        public void NamedProperties_PropertyDoesntBelongToConstructorDeclaringType_ThrowsArgumentException(ConstructorInfo con, PropertyInfo property)
        {
            PropertyInfo[] namedProperties = new PropertyInfo[] { property };

            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], namedProperties, new object[] { 5 }));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], namedProperties, new object[] { 5 }, new FieldInfo[0], new object[0]));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public static void NullObjectInPropertyValues_ThrowsArgumentNullException()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            PropertyInfo[] namedProperties = Helpers.GetProperties(typeof(TestAttribute), nameof(TestAttribute.TestInt32));
            object[] propertyValues = new object[] { null };

            AssertExtensions.Throws<ArgumentNullException>("propertyValues[0]", () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues));
            AssertExtensions.Throws<ArgumentNullException>("propertyValues[0]", () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues, new FieldInfo[0], new object[0]));
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [MemberData(nameof(NotSupportedObject_Others_TestData))]
        public static void NotSupportedObjectInPropertyValues_ThrowsArgumentException(object value)
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            PropertyInfo[] namedProperties = Helpers.GetProperties(typeof(TestAttribute), nameof(TestAttribute.ObjectProperty));
            object[] propertyValues = new object[] { value };

            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues));
            AssertExtensions.Throws<ArgumentException>(null, () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues, new FieldInfo[0], new object[0]));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public static void ZeroCountMultidimensionalArrayInPropertyValues_ChangesToZeroCountJaggedArray()
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            PropertyInfo[] namedProperties = Helpers.GetProperties(typeof(TestAttribute), nameof(TestAttribute.ObjectProperty));
            object[] propertyValues = new object[] { new int[0, 0] };

            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues);
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            assembly.SetCustomAttribute(attribute);

            TestAttribute customAttribute = (TestAttribute)assembly.GetCustomAttributes().First();
            Array objectProperty = (Array)customAttribute.ObjectProperty;
            Assert.IsType<int[]>(objectProperty);
            Assert.Equal(0, objectProperty.Length);
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        [MemberData(nameof(NotSupportedPrimitives_TestData))]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Coreclr fixed an issue where IntPtr/UIntPtr in propertyValues causes a corrupt created binary.")]
        public static void NotSupportedPrimitiveInPropertyValues_ThrowsArgumentException(object value)
        {
            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            PropertyInfo[] namedProperties = Helpers.GetProperties(typeof(TestAttribute), nameof(TestAttribute.ObjectProperty));
            object[] propertyValues = new object[] { value };

            AssertExtensions.Throws<ArgumentException>("propertyValues[0]", () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues));
            AssertExtensions.Throws<ArgumentException>("propertyValues[0]", () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues, new FieldInfo[0], new object[0]));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2383", TestRuntimes.Mono)]
        public static void DynamicTypeInFieldValues_ThrowsFileNotFoundExceptionOnCreation()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            TypeBuilder type = assembly.DefineDynamicModule("DynamicModule").DefineType("DynamicType");
            PropertyInfo[] namedProperties = Helpers.GetProperties(typeof(TestAttribute), nameof(TestAttribute.ObjectProperty));
            object[] propertyValues = new object[] { type };

            ConstructorInfo con = typeof(TestAttribute).GetConstructor(new Type[0]);
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues);
            assembly.SetCustomAttribute(attribute);

            Assert.Throws<FileNotFoundException>(() => assembly.GetCustomAttributes());
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
            PropertyInfo[] namedProperties = Helpers.GetProperties(typeof(TestAttribute), propertyNames);

            AssertExtensions.Throws<ArgumentException>(paramName, () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues));
            AssertExtensions.Throws<ArgumentException>(paramName, () => new CustomAttributeBuilder(con, new object[0], namedProperties, propertyValues, new FieldInfo[0], new object[0]));
        }

        private static Type CreateEnum(Type underlyingType, params object[] literalValues)
        {
            ModuleBuilder module = Helpers.DynamicModule();
            EnumBuilder enumBuilder = module.DefineEnum("Name", TypeAttributes.Public, underlyingType);
            for (int i = 0; i < (literalValues?.Length ?? 0); i++)
            {
                enumBuilder.DefineLiteral("Value" + i, literalValues[i]);
            }
            return enumBuilder.CreateType();
        }
    }

    public class OtherTestAttribute : Attribute
    {
        public int Property { get; set; }
        public int Field;
    }

    class PrivateAttribute : Attribute { }
    public class NotAnAttribute { }

    public class Primitives : Attribute
    {
        public Primitives(sbyte sb, byte b, short s, ushort us, int i, uint ui, long l, ulong ul,
                          SByteEnum sbe, ByteEnum be, ShortEnum se, UShortEnum use, IntEnum ie, UIntEnum uie, LongEnum le, ULongEnum ule,
                          char c, bool bo, float f, double d,
                          string str, Type t, int[] arr, object obj)
        {
            SByteConstructor = sb;
            ByteConstructor = b;
            ShortConstructor = s;
            UShortConstructor = us;
            IntConstructor = i;
            UIntConstructor = ui;
            LongConstructor = l;
            ULongConstructor = ul;

            SByteEnumConstructor = sbe;
            ByteEnumConstructor = be;
            ShortEnumConstructor = se;
            UShortEnumConstructor = use;
            IntEnumConstructor = ie;
            UIntEnumConstructor = uie;
            LongEnumConstructor = le;
            ULongEnumConstructor = ule;

            CharConstructor = c;
            BoolConstructor = bo;
            FloatConstructor = f;
            DoubleConstructor = d;

            StringConstructor = str;
            TypeConstructor = t;
            ArrayConstructor = arr;
            ObjectConstructor = obj;
        }

        public sbyte SByteConstructor;
        public byte ByteConstructor;
        public short ShortConstructor;
        public ushort UShortConstructor;
        public int IntConstructor;
        public uint UIntConstructor;
        public long LongConstructor;
        public ulong ULongConstructor;

        public SByteEnum SByteEnumConstructor;
        public ByteEnum ByteEnumConstructor;
        public ShortEnum ShortEnumConstructor;
        public UShortEnum UShortEnumConstructor;
        public IntEnum IntEnumConstructor;
        public UIntEnum UIntEnumConstructor;
        public LongEnum LongEnumConstructor;
        public ULongEnum ULongEnumConstructor;

        public char CharConstructor;
        public bool BoolConstructor;
        public float FloatConstructor;
        public double DoubleConstructor;

        public string StringConstructor;
        public Type TypeConstructor;
        public int[] ArrayConstructor;
        public object ObjectConstructor;

        public sbyte SByteProperty { get; set; }
        public byte ByteProperty { get; set; }
        public short ShortProperty { get; set; }
        public ushort UShortProperty { get; set; }
        public int IntProperty { get; set; }
        public uint UIntProperty { get; set; }
        public long LongProperty { get; set; }
        public ulong ULongProperty { get; set; }

        public SByteEnum SByteEnumProperty { get; set; }
        public ByteEnum ByteEnumProperty { get; set; }
        public ShortEnum ShortEnumProperty { get; set; }
        public UShortEnum UShortEnumProperty { get; set; }
        public IntEnum IntEnumProperty { get; set; }
        public UIntEnum UIntEnumProperty { get; set; }
        public LongEnum LongEnumProperty { get; set; }
        public ULongEnum ULongEnumProperty { get; set; }

        public char CharProperty { get; set; }
        public bool BoolProperty { get; set; }
        public float FloatProperty { get; set; }
        public double DoubleProperty { get; set; }

        public string StringProperty { get; set; }
        public Type TypeProperty { get; set; }
        public int[] ArrayProperty { get; set; }
        public object ObjectProperty { get; set; }

        public sbyte SByteField;
        public byte ByteField;
        public short ShortField;
        public ushort UShortField;
        public int IntField;
        public uint UIntField;
        public long LongField;
        public ulong ULongField;

        public SByteEnum SByteEnumField;
        public ByteEnum ByteEnumField;
        public ShortEnum ShortEnumField;
        public UShortEnum UShortEnumField;
        public IntEnum IntEnumField;
        public UIntEnum UIntEnumField;
        public LongEnum LongEnumField;
        public ULongEnum ULongEnumField;

        public char CharField;
        public bool BoolField;
        public float FloatField;
        public double DoubleField;

        public string StringField;
        public Type TypeField;
        public int[] ArrayField;
        public object ObjectField;
    }

    public class IndexerAttribute : Attribute
    {
        public IndexerAttribute() { }

        public string this[string s]
        {
            get { return s; }
            set { }
        }
    }

    public enum SByteEnum : sbyte { }
    public enum ByteEnum : byte { }
    public enum ShortEnum : short { }
    public enum UShortEnum : ushort { }
    public enum IntEnum : int { }
    public enum UIntEnum : uint { }
    public enum LongEnum : long { }
    public enum ULongEnum : ulong { }
}

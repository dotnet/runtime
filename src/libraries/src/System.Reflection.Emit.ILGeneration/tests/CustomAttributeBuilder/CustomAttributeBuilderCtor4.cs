// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class CustomAttributeBuilderCtor4
    {
        private const int MinStringLength = 1;
        private const int MaxStringLength = 1024;
        private const string FieldTestInt32Name = "TestInt";
        private const string FieldTestStringName = "TestStringField";
        private const string FieldGetOnlyStringName = "GetString";
        private const string FieldGetOnlyIntName = "GetInt";
        private const string DefaultNotExistFieldName = "DOESNOTEXIST";
        private const BindingFlags FieldBindingFlag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        private const string PropertyTestInt32Name = "TestInt32";
        private const string PropertyTestStringName = "TestString";
        private const string PropertyGetOnlyStringName = "GetOnlyString";
        private const string PropertyGetOnlyIntName = "GetOnlyInt32";
        private const string DefaultNotExistPropertyName = "DOESNOTEXIST";
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        [Fact]
        public void PosTest1()
        {
            string testString1 = null;
            string testString2 = null;
            int testInt1 = 0;
            int testInt2 = 0;

            testString1 = "PosTest1_TestString1";
            testString2 = "PosTest1_TestString2";
            testInt1 = _generator.GetInt32();
            testInt2 = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                typeof(string),
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                testString1,
                testInt1
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(FieldTestInt32Name, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldTestStringName, FieldBindingFlag)
            };
            object[] fieldValues = new object[]
            {
                testInt2,
                testString2
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName)
            };
            object[] propertyValues = new object[]
            {
                testInt2,
                testString2
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues,
                namedFields,
                fieldValues);
            Assert.NotNull(cab);

            FieldInfo[] verifyFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(FieldTestInt32Name, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldTestStringName, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldGetOnlyStringName, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldGetOnlyIntName, FieldBindingFlag),
            };
            object[] verifyFieldValues = new object[]
            {
                testInt2,
                testString2,
                testString1,
                testInt1
            };

            Assert.True(VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        [Fact]
        public void PosTest2()
        {
            string testString1 = null;
            string testString2 = null;
            int testInt1 = 0;
            int testInt2 = 0;

            testString1 = "PosTest2_TestString1";
            testString2 = "PosTest2_TestString2";
            testInt1 = _generator.GetInt32();
            testInt2 = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                typeof(string),
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                testString1,
                testInt1
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName)
            };
            object[] propertyValues = new object[]
            {
                testString2
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(FieldTestInt32Name, FieldBindingFlag)
            };
            object[] fieldValues = new object[]
            {
                testInt2
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues,
                namedFields,
                fieldValues);
            Assert.NotNull(cab);

            FieldInfo[] verifyFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(FieldTestInt32Name, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldTestStringName, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldGetOnlyStringName, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldGetOnlyIntName, FieldBindingFlag),
            };
            object[] verifyFieldValues = new object[]
            {
                testInt2,
                testString2,
                testString1,
                testInt1
            };

            Assert.True(VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        [Fact]
        public void PosTest3()
        {
            string testString1 = null;
            int testInt1 = 0;

            testString1 = "PosTest3_testString1";
            testInt1 = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                typeof(string),
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                testString1,
                testInt1
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
            };
            object[] propertyValues = new object[]
            {
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues,
                namedFields,
                fieldValues);
            Assert.NotNull(cab);

            FieldInfo[] verifyFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(FieldTestInt32Name, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldTestStringName, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldGetOnlyStringName, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldGetOnlyIntName, FieldBindingFlag),
            };
            object[] verifyFieldValues = new object[]
            {
                0,
                null,
                testString1,
                testInt1
            };

            Assert.True(VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        [Fact]
        public void PosTest4()
        {
            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
            };
            object[] propertyValues = new object[]
            {
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues,
                namedFields,
                fieldValues);
            Assert.NotNull(cab);

            FieldInfo[] verifyFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(FieldTestInt32Name, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldTestStringName, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldGetOnlyStringName, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldGetOnlyIntName, FieldBindingFlag),
            };
            object[] verifyFieldValues = new object[]
            {
                0,
                null,
                null,
                0
            };

            Assert.True(VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        [Fact]
        public void PosTest5()
        {
            string testString1 = null;
            int testInt1 = 0;
            int testInt2 = 0;

            testString1 = "PosTest5_testString1";
            testInt1 = _generator.GetInt32();
            testInt2 = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
            };
            object[] propertyValues = new object[]
            {
                testInt1,
                testString1
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(FieldTestInt32Name, FieldBindingFlag)
            };
            object[] fieldValues = new object[]
            {
                testInt2
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues,
                namedFields,
                fieldValues);
            Assert.NotNull(cab);

            FieldInfo[] verifyFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(FieldTestInt32Name, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldTestStringName, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldGetOnlyStringName, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldGetOnlyIntName, FieldBindingFlag),
            };
            object[] verifyFieldValues = new object[]
            {
                testInt2,
                testString1,
                null,
                0
            };

            Assert.True(VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        [Fact]
        public void NegTest1()
        {
            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
            };
            object[] propertyValues = new object[]
            {
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                       CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                       constructorArgs,
                       namedProperty,
                       propertyValues,
                       namedFields,
                       fieldValues);
            });
        }

        [Fact]
        public void NegTest2()
        {
            int testInt1 = 0;

            testInt1 = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
            };
            object[] propertyValues = new object[]
            {
                testInt1
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest3()
        {
            Type[] ctorParams = new Type[] { };
            object[] constructorArgs = new object[] { };
            PropertyInfo[] namedProperty = new PropertyInfo[] { };
            object[] propertyValues = new object[] { };
            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                        .Where(c => c.IsStatic).First(),
                    constructorArgs,
                    namedProperty,
                    propertyValues);
            });
        }

        [Fact]
        public void NegTest4()
        {
            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
                false
            };
            PropertyInfo[] namedProperty = new PropertyInfo[] { };
            object[] propertyValues = new object[] { };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                        .Where(c => c.IsPrivate).First(),
                    constructorArgs,
                    namedProperty,
                    propertyValues);
            });
        }

        [Fact]
        public void NegTest5()
        {
            string testString1 = null;
            int testInt1 = 0;

            testString1 =
                _generator.GetString(false, MinStringLength, MaxStringLength);
            testInt1 = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                typeof(string),
                typeof(int),
                typeof(string),
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                testString1,
                testInt1
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
            };
            object[] propertyValues = new object[]
            {
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };
            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest6()
        {
            string testString1 = null;
            int testInt1 = 0;

            testString1 =
                _generator.GetString(false, MinStringLength, MaxStringLength);
            testInt1 = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                typeof(string),
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                testInt1,
                testString1
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
            };
            object[] propertyValues = new object[]
            {
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest7()
        {
            string testString1 = null;
            int testInt1 = 0;

            testString1 =
                _generator.GetString(false, MinStringLength, MaxStringLength);
            testInt1 = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
            };
            object[] propertyValues = new object[]
            {
                testString1,
                testInt1
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };
            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest8()
        {
            string testString1 = null;

            testString1 =
                _generator.GetString(false, MinStringLength, MaxStringLength);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyIntName)
            };
            object[] propertyValues = new object[]
            {
                testString1
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest9()
        {
            string testString1 = null;


            testString1 =
                _generator.GetString(false, MinStringLength, MaxStringLength);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyStringName)
            };
            object[] propertyValues = new object[]
            {
                testString1
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest10()
        {
            string testString1 = null;

            testString1 =
                _generator.GetString(false, MinStringLength, MaxStringLength);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            object[] constructorArgs = new object[]
            {
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name)
            };
            object[] propertyValues = new object[]
            {
                testString1
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    null,
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest11()
        {
            string testString1 = null;


            testString1 =
                _generator.GetString(false, MinStringLength, MaxStringLength);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name)
            };
            object[] propertyValues = new object[]
            {
                testString1
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    null,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest12()
        {
            string testString1 = null;

            testString1 =
                _generator.GetString(false, MinStringLength, MaxStringLength);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name)
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    null,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest13()
        {
            string testString = null;
            int testInt = 0;

            testString =
                _generator.GetString(false, MinStringLength, MaxStringLength);
            testInt = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                null,
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                testString,
                testInt
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
            };
            object[] propertyValues = new object[]
            {
                testString,
                testInt
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest14()
        {
            string testString = null;
            int testInt = 0;

            testString =
                _generator.GetString(false, MinStringLength, MaxStringLength);
            testInt = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                typeof(string),
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                testString,
                testInt
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                null,
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
            };
            object[] propertyValues = new object[]
            {
                testString,
                testInt
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest15()
        {
            string testString = null;
            int testInt = 0;


            testString =
                _generator.GetString(false, MinStringLength, MaxStringLength);
            testInt = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                typeof(string),
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                testString,
                testInt
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
            };
            object[] propertyValues = new object[]
            {
                null,
                testInt
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest16()
        {
            int testInt = 0;
            string testString = null;


            testString =
                _generator.GetString(false, MinStringLength, MaxStringLength);
            testInt = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                typeof(string),
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                testString,
                testInt
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
            };
            object[] propertyValues = new object[]
            {
                testString,
                testInt
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
                null,
                CustomAttributeBuilderTestType.GetField(FieldTestStringName, FieldBindingFlag)
            };
            object[] fieldValues = new object[]
            {
                testInt,
                testString
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest17()
        {
            int testInt = 0;
            string testString = null;

            testString =
                _generator.GetString(false, MinStringLength, MaxStringLength);
            testInt = _generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                typeof(string),
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                testString,
                testInt
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
            };
            object[] propertyValues = new object[]
            {
                testString,
                testInt
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(FieldTestInt32Name, FieldBindingFlag),
                CustomAttributeBuilderTestType.GetField(FieldTestStringName, FieldBindingFlag)
            };
            object[] fieldValues = new object[]
            {
                null,
                testString
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest18()
        {
            string testString1 = null;

            testString1 =
                _generator.GetString(false, MinStringLength, MaxStringLength);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            object[] propertyValues = new object[]
            {
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    null, // namedProperty,
                    propertyValues,
                    namedFields,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest19()
        {
            string testString1 = null;

            testString1 =
                _generator.GetString(false, MinStringLength, MaxStringLength);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name)
            };
            object[] propertyValues = new object[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    null,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest20()
        {
            string testString1 = null;


            testString1 =
                _generator.GetString(false, MinStringLength, MaxStringLength);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name)
            };
            object[] propertyValues = new object[]
            {
            };
            FieldInfo[] namedFields = new FieldInfo[]
            {
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedProperty,
                    propertyValues,
                    namedFields,
                    null);
            });
        }

        private bool VerifyCustomAttribute(CustomAttributeBuilder builder, Type attributeType, FieldInfo[] namedFields, object[] fieldValues)
        {
            AssemblyName asmName = new AssemblyName("VerificationAssembly");
            bool retVal = true;
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(
                asmName, AssemblyBuilderAccess.Run);

            asmBuilder.SetCustomAttribute(builder);
            // Verify
            object[] customAttributes = asmBuilder.GetCustomAttributes(attributeType).Select(a => (object)a).ToArray();
            // We just support one custom attribute case
            if (customAttributes.Length != 1)
                return false;

            object customAttribute = customAttributes[0];
            for (int i = 0; i < namedFields.Length; ++i)
            {
                FieldInfo field = attributeType.GetField(namedFields[i].Name);
                object expected = field.GetValue(customAttribute);
                object actual = fieldValues[i];

                if (expected == null)
                {
                    if (actual != null)
                    {
                        retVal = false;
                        break;
                    }
                }
                else
                {
                    if (!expected.Equals(actual))
                    {
                        retVal = false;
                        break;
                    }
                }
            }

            return retVal;
        }
    }
}

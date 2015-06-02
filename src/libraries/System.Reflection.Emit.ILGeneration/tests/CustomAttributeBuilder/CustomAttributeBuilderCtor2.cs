// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class CustomAttributeBuilderTest : Attribute
    {
        public string TestString
        {
            get
            {
                return TestStringField;
            }
            set
            {
                TestStringField = value;
            }
        }

        public int TestInt32
        {
            get
            {
                return TestInt;
            }
            set
            {
                TestInt = value;
            }
        }

        public string GetOnlyString
        {
            get
            {
                return GetString;
            }
        }

        public int GetOnlyInt32
        {
            get
            {
                return GetInt;
            }
        }

        public string TestStringField;
        public int TestInt;
        public string GetString;
        public int GetInt;

        public CustomAttributeBuilderTest()
        {
        }

        public CustomAttributeBuilderTest(string getOnlyString, int getOnlyInt32)
        {
            GetString = getOnlyString;
            this.GetInt = getOnlyInt32;
        }

        public CustomAttributeBuilderTest(string testString, int testInt32, string getOnlyString, int getOnlyInt32)
        {
            this.TestStringField = testString;
            this.TestInt = testInt32;
            this.GetString = getOnlyString;
            this.GetInt = getOnlyInt32;
        }
    }

    public class CustomAttributeBuilderCtor2
    {
        private const int c_MIN_STRING_LENGTH = 1;
        private const int c_MAX_STRING_LENGTH = 1024;
        private const string c_FIELD_TESTINT32_NAME = "TestInt";
        private const string c_FIELD_TESTSTRING_NAME = "TestStringField";
        private const string c_FIELD_GETONLYSTRING_NAME = "GetString";
        private const string c_FIELD_GETONLYINT_NAME = "GetInt";
        private const string c_DEFAULT_NOT_EXIST_FIELDNAME = "DOESNOTEXIST";
        private const BindingFlags c_FIELD_BINDING_FLAG = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;

        [Fact]
        public void PosTest1()
        {
            string str1 = null;
            int testInt1 = 0;
            int testInt2 = 0;

            str1 = "PosTest1_Arg";
            testInt1 = TestLibrary.Generator.GetInt32(-55);
            testInt2 = TestLibrary.Generator.GetInt32(-55);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                typeof(string),
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                str1,
                testInt1
            };
            FieldInfo[] namedField = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                testInt2
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedField,
                fieldValues);
            Assert.NotNull(cab);

            FieldInfo[] verifyFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYINT_NAME, c_FIELD_BINDING_FLAG),
            };

            object[] verifyFieldValues = new object[]
            {
                testInt2,
                null,
                str1,
                testInt1
            };

            Assert.False(!VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        public void PosTest2()
        {
            string str1 = null;
            int testInt1 = 0;
            int testInt2 = 0;

            str1 = "PosTest2_STR1";
            testInt1 = TestLibrary.Generator.GetInt32(-55);
            testInt2 = TestLibrary.Generator.GetInt32(-55);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                typeof(string),
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                str1,
                testInt1
            };
            FieldInfo[] namedFiled = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedFiled,
                fieldValues);
            Assert.NotNull(cab);

            FieldInfo[] verifyFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYINT_NAME, c_FIELD_BINDING_FLAG),
            };

            object[] verifyFieldValues = new object[]
            {
                0,
                null,
                str1,
                testInt1
            };

            Assert.False(!VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        [Fact]
        public void PosTest3()
        {
            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            FieldInfo[] namedFiled = new FieldInfo[]
            {
            };
            object[] fieldValues = new object[]
            {
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedFiled,
                fieldValues);
            Assert.NotNull(cab);

            FieldInfo[] verifyFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYINT_NAME, c_FIELD_BINDING_FLAG),
            };
            object[] verifyFieldValues = new object[]
            {
                0,
                null,
                null,
                0
            };

            Assert.False(!VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        [Fact]
        public void PosTest4()
        {
            int testInt = 0;

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);
            testInt = TestLibrary.Generator.GetInt32(-55);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                testInt
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedFiled,
                fieldValues);
            Assert.NotNull(cab);

            FieldInfo[] verifyFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYINT_NAME, c_FIELD_BINDING_FLAG),
            };
            object[] verifyFieldValues = new object[]
            {
                testInt,
                null,
                null,
                0
            };

            Assert.False(!VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        // This test case will be failed if we set testString to the following value
        // 皖笜鮊됲反䣨뭂⁵棱眜⸎촃ᷬ࿏㖉忠ඹṪ䱱錺纅䚀
        [Fact]
        public void PosTest5()
        {
            int testInt = 0;
            string testString = null;

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);
            testInt = TestLibrary.Generator.GetInt32(-55);
            testString = "PosTest5_TestString";

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                testInt,
                testString
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedFiled,
                fieldValues);
            Assert.NotNull(cab);

            FieldInfo[] verifyFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYINT_NAME, c_FIELD_BINDING_FLAG),
            };
            object[] verifyFieldValues = new object[]
            {
                testInt,
                testString,
                null,
                0
            };

            Assert.False(!VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        [Fact]
        public void PosTest6()
        {
            string testString1 = null;
            string testString2 = null;
            int testInt1 = 0;
            int testInt2 = 0;

            testString1 = "PosTest6_TestString1";
            testString2 = "PostTest6_TestString2";
            testInt1 = TestLibrary.Generator.GetInt32(-55);
            testInt2 = TestLibrary.Generator.GetInt32(-55);

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
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                testInt2,
                testString2
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedFiled,
                fieldValues);
            Assert.NotNull(cab);

            FieldInfo[] verifyFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYINT_NAME, c_FIELD_BINDING_FLAG),
            };
            object[] verifyFieldValues = new object[]
            {
                testInt2,
                testString2,
                testString1,
                testInt1
            };

            Assert.False(!VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        [Fact]
        public void PosTest7()
        {
            string testString1 = null;
            testString1 = "PosTest7_TestString1";

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYSTRING_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                testString1
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedFiled,
                fieldValues);

            FieldInfo[] verifyFields = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYSTRING_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYINT_NAME, c_FIELD_BINDING_FLAG),
            };
            object[] verifyFieldValues = new object[]
            {
                0,
                null,
                testString1,
                0
            };

            Assert.False(!VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
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
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                       CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                       constructorArgs,
                       namedFiled,
                       fieldValues);
            });
        }

        [Fact]
        public void NegTest2()
        {
            string testString = null;
            int testInt = 0;

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            testString =
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            testInt = TestLibrary.Generator.GetInt32(-55);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                testString,
                testInt
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedFiled,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest3()
        {
            Type[] ctorParams = new Type[] { };
            object[] constructorArgs = new object[] { };
            FieldInfo[] namedFiled = new FieldInfo[] { };
            object[] fieldValues = new object[] { };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                        .Where(c => c.IsStatic).First(),
                    constructorArgs,
                    namedFiled,
                    fieldValues);
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
            FieldInfo[] namedFiled = new FieldInfo[] { };
            object[] fieldValues = new object[] { };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    typeof(TestConstructor).GetConstructors(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                        .Where(c => c.IsPrivate).First(),
                    constructorArgs,
                    namedFiled,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest5()
        {
            string testString1 = null;
            int testInt1 = 0;

            testString1 =
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            testInt1 = TestLibrary.Generator.GetInt32(-55);

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
            FieldInfo[] namedFiled = new FieldInfo[]
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
                    namedFiled,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest6()
        {
            string testString1 = null;
            int testInt1 = 0;
            testString1 =
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            testInt1 = TestLibrary.Generator.GetInt32(-55);

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
            FieldInfo[] namedFiled = new FieldInfo[]
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
                       namedFiled,
                       fieldValues);
            });
        }

        [Fact]
        public void NegTest7()
        {
            string testString1 = null;
            int testInt1 = 0;

            testString1 =
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            testInt1 = TestLibrary.Generator.GetInt32(-55);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                testString1,
                testInt1
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedFiled,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest8()
        {
            string testString1 = null;

            testString1 =
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_GETONLYINT_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                testString1
            };

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedFiled,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest9()
        {
            string testString1 = null;

            testString1 =
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            object[] constructorArgs = new object[]
            {
            };
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                testString1
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    null,
                    constructorArgs,
                    namedFiled,
                    fieldValues);
            });
        }

        [Fact]
        public void NegTest10()
        {
            string testString1 = null;

            testString1 =
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                testString1
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                null,
                namedFiled,
                fieldValues);
            });
        }

        [Fact]
        public void NegTest11()
        {
            string testString1 = null;

            testString1 =
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            object[] fieldValues = new object[]
            {
                testString1
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                null as FieldInfo[],
                fieldValues);
            });
        }

        [Fact]
        public void NegTest12()
        {
            string testString1 = null;

            testString1 =
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG)
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                    CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                    constructorArgs,
                    namedFiled,
                    null);
            });
        }

        [Fact]
        public void NegTest13()
        {
            string testString = null;
            int testInt = 0;

            testString =
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            testInt = TestLibrary.Generator.GetInt32(-55);

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
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                testString,
                testInt
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedFiled,
                fieldValues);
            });
        }

        [Fact]
        public void NegTest14()
        {
            int testInt = 0;
            string testString = null;

            testString =
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            testInt = TestLibrary.Generator.GetInt32(-55);

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
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTINT32_NAME, c_FIELD_BINDING_FLAG),
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                null,
                testInt
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedFiled,
                fieldValues);
            });
        }

        [Fact]
        public void NegTest15()
        {
            string testString = null;
            int testInt = 0;

            testString =
                TestLibrary.Generator.GetString(-55, false, c_MIN_STRING_LENGTH, c_MAX_STRING_LENGTH);
            testInt = TestLibrary.Generator.GetInt32(-55);

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
            FieldInfo[] namedFiled = new FieldInfo[]
            {
                null,
                CustomAttributeBuilderTestType.GetField(c_FIELD_TESTSTRING_NAME, c_FIELD_BINDING_FLAG)
            };
            object[] fieldValues = new object[]
            {
                testString,
                testInt
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedFiled,
                fieldValues);
            });
        }

        private bool VerifyCustomAttribute(CustomAttributeBuilder builder, Type attributeType, FieldInfo[] fieldNames, object[] fieldValues)
        {
            AssemblyName asmName = new AssemblyName("VerificationAssembly");
            bool retVal = true;
            AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Run);

            asmBuilder.SetCustomAttribute(builder);
            object[] customAttributes = asmBuilder.GetCustomAttributes(attributeType).Select(a => (object)a).ToArray();
            // We just support one custom attribute case
            if (customAttributes.Length != 1)
                return false;

            object customAttribute = customAttributes[0];
            for (int i = 0; i < fieldNames.Length; ++i)
            {
                FieldInfo field = attributeType.GetField(fieldNames[i].Name);
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

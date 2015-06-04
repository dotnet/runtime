// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class CustomAttributeBuilderCtor3
    {
        private const int MinStringLength = 1;
        private const int MaxStringLength = 1024;
        private const string PropertyTestInt32Name = "TestInt32";
        private const string PropertyTestStringName = "TestString";
        private const string PropertyGetOnlyStringName = "GetOnlyString";
        private const string PropertyGetOnlyIntName = "GetOnlyInt32";
        private const string DefaultNotExistPropertyName = "DOESNOTEXIST";

        [Fact]
        public void PosTest1()
        {
            string testString1 = null;
            string testString2 = null;
            int testInt1 = 0;
            int testInt2 = 0;

            testString1 = "PosTest1_TestString1";
            testString2 = "PosTest1_TestString2";
            testInt1 = TestLibrary.Generator.GetInt32();
            testInt2 = TestLibrary.Generator.GetInt32();

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
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
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
                propertyValues);
            Assert.NotNull(cab);

            PropertyInfo[] verifyFields = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyIntName)
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
            int testInt1 = 0;
            int testInt2 = 0;

            testString1 = "PosTest2_TestString1";
            testInt1 = TestLibrary.Generator.GetInt32();
            testInt2 = TestLibrary.Generator.GetInt32();

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
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name)
            };
            object[] propertyValues = new object[]
            {
                testInt2
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            Assert.NotNull(cab);

            PropertyInfo[] verifyFields = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyIntName)
            };
            object[] verifyFieldValues = new object[]
            {
                testInt2,
                null,
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

            testString1 = "PosTest3_TestString1";
            testInt1 = TestLibrary.Generator.GetInt32();

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

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            Assert.NotNull(cab);

            PropertyInfo[] verifyFields = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyIntName)
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

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            Assert.NotNull(cab);

            PropertyInfo[] verifyFields = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyIntName)
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
            int testInt1 = 0;

            testInt1 = TestLibrary.Generator.GetInt32();

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
                testInt1
            };

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            Assert.NotNull(cab);

            PropertyInfo[] verifyFields = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyIntName)
            };
            object[] verifyFieldValues = new object[]
            {
                testInt1,
                null,
                null,
                0
            };

            Assert.True(VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        [Fact]
        public void PosTest6()
        {
            string testString1 = null;
            int testInt1 = 0;

            testString1 = "PosTest6_TestString1";
            testInt1 = TestLibrary.Generator.GetInt32();

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

            CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            Assert.NotNull(cab);

            PropertyInfo[] verifyFields = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyIntName)
            };
            object[] verifyFieldValues = new object[]
            {
                testInt1,
                testString1,
                null,
                0
            };

            Assert.True(VerifyCustomAttribute(cab, CustomAttributeBuilderTestType, verifyFields, verifyFieldValues));
        }

        [Fact]
        public void PosTest7()
        {
            string testString1 = null;
            string testString2 = null;
            int testInt1 = 0;
            int testInt2 = 0;

            testString1 = "PosTest7_TestString1";
            testString2 = "PosTest7_TestString2";
            testInt1 = TestLibrary.Generator.GetInt32();
            testInt2 = TestLibrary.Generator.GetInt32();

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
                testInt1,
                testString1,
                testInt1
            };
            PropertyInfo[] namedProperty = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
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
                propertyValues);
            Assert.NotNull(cab);

            PropertyInfo[] verifyFields = new PropertyInfo[]
            {
                CustomAttributeBuilderTestType.GetProperty(PropertyTestInt32Name),
                CustomAttributeBuilderTestType.GetProperty(PropertyTestStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyStringName),
                CustomAttributeBuilderTestType.GetProperty(PropertyGetOnlyIntName)
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

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                       CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                       constructorArgs,
                       namedProperty,
                       propertyValues);
            });
        }

        [Fact]
        public void NegTest2()
        {
            int testInt1 = 0;
            testInt1 = TestLibrary.Generator.GetInt32();

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

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
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
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);
            testInt1 = TestLibrary.Generator.GetInt32();

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

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            });
        }

        [Fact]
        public void NegTest6()
        {
            string testString1 = null;
            int testInt1 = 0;
            testString1 =
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);
            testInt1 = TestLibrary.Generator.GetInt32();

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

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            });
        }

        [Fact]
        public void NegTest7()
        {
            string testString1 = null;
            int testInt1 = 0;

            testString1 =
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);
            testInt1 = TestLibrary.Generator.GetInt32();

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

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            });
        }

        [Fact]
        public void NegTest8()
        {
            string testString1 = null;

            testString1 =
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);

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

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            });
        }

        [Fact]
        public void NegTest9()
        {
            string testString1 = null;
            testString1 =
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);

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

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            });
        }

        [Fact]
        public void NegTest10()
        {
            string testString1 = null;
            testString1 =
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);

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

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                null,
                constructorArgs,
                namedProperty,
                propertyValues);
            });
        }

        [Fact]
        public void NegTest11()
        {
            string testString1 = null;
            testString1 =
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);

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

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                null,
                namedProperty,
                propertyValues);
            });
        }

        [Fact]
        public void NegTest12()
        {
            string testString1 = null;
            testString1 =
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);

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
            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                null);
            });
        }

        [Fact]
        public void NegTest13()
        {
            string testString = null;
            int testInt = 0;

            testString =
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);
            testInt = TestLibrary.Generator.GetInt32();

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

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            });
        }

        [Fact]
        public void NegTest14()
        {
            string testString = null;
            int testInt = 0;
            testString =
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);
            testInt = TestLibrary.Generator.GetInt32();

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
                typeof(string),
                typeof(int)
            };
            object[] constructorArgs = new object[]
            {
                null,
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

            Assert.Throws<ArgumentException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            });
        }

        [Fact]
        public void NegTest15()
        {
            string testString = null;
            int testInt = 0;
            testString =
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);
            testInt = TestLibrary.Generator.GetInt32();

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

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            });
        }

        [Fact]
        public void NegTest16()
        {
            int testInt = 0;
            string testString = null;

            testString =
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);
            testInt = TestLibrary.Generator.GetInt32();

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
            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                namedProperty,
                propertyValues);
            });
        }

        [Fact]
        public void NegTest17()
        {
            string testString1 = null;

            testString1 =
                TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);

            Type CustomAttributeBuilderTestType = typeof(CustomAttributeBuilderTest);

            Type[] ctorParams = new Type[]
            {
            };
            object[] constructorArgs = new object[]
            {
            };
            object[] propertyValues = new object[]
            {
                testString1
            };

            Assert.Throws<ArgumentNullException>(() =>
            {
                CustomAttributeBuilder cab = new CustomAttributeBuilder(
                CustomAttributeBuilderTestType.GetConstructor(ctorParams),
                constructorArgs,
                null as PropertyInfo[],
                propertyValues);
            });
        }

        private bool VerifyCustomAttribute(CustomAttributeBuilder builder, Type attributeType, PropertyInfo[] namedProperties, object[] propertyValues)
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
            for (int i = 0; i < namedProperties.Length; ++i)
            {
                PropertyInfo property = attributeType.GetProperty(namedProperties[i].Name);
                object expected = property.GetValue(customAttribute, null);
                object actual = propertyValues[i];

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

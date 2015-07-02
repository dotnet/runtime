// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public struct MBTestStruct
    {
        public int IntVal;
        public string StrVal;
    }

    public class ModuleBuilderGetArrayMethod
    {
        private const string DefaultAssemblyName = "ModuleBuilderGetArrayMethod";
        private const AssemblyBuilderAccess DefaultAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const string DefaultModuleName = "DynamicModule";

        private ModuleBuilder TestModuleBuilder
        {
            get
            {
                AssemblyName name = new AssemblyName(DefaultAssemblyName);
                AssemblyBuilder asmBuilder = AssemblyBuilder.DefineDynamicAssembly(name, DefaultAssemblyBuilderAccess);
                _moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(asmBuilder, "Module1");
                return _moduleBuilder;
            }
        }

        private ModuleBuilder _moduleBuilder;

        [Fact]
        public void TestWithValidArrayValuesAndVoidReturnTypeMethod()
        {
            CallingConventions[] conventions = new CallingConventions[] {
                CallingConventions.Any,
                CallingConventions.ExplicitThis,
                CallingConventions.HasThis,
                CallingConventions.Standard,
                CallingConventions.VarArgs
            };
            Type arrayClass = typeof(ModuleBuilderGetArrayMethod[]);
            string methodName = "PosTest1_";

            for (int i = 0; i < conventions.Length; ++i)
            {
                VerificationHelper(TestModuleBuilder,
                    arrayClass,
                    methodName + i.ToString(),
                    conventions[i],
                    typeof(void),
                    new Type[] { });
            }
        }

        [Fact]
        public void TestWithValidArrayValuesAndValueReturnTypeMethod()
        {
            CallingConventions[] conventions = new CallingConventions[] {
                CallingConventions.Any,
                CallingConventions.ExplicitThis,
                CallingConventions.HasThis,
                CallingConventions.Standard,
                CallingConventions.VarArgs
            };
            Type arrayClass = typeof(int[]);
            string methodName = "PosTest2_";

            for (int i = 0; i < conventions.Length; ++i)
            {
                VerificationHelper(TestModuleBuilder,
                    arrayClass,
                    methodName + i.ToString(),
                    conventions[i],
                    typeof(int),
                    new Type[] { });
            }
        }

        [Fact]
        public void TestWithValidArrayValuesAndReferenceReturnTypeMethod()
        {
            CallingConventions[] conventions = new CallingConventions[] {
                CallingConventions.Any,
                CallingConventions.ExplicitThis,
                CallingConventions.HasThis,
                CallingConventions.Standard,
                CallingConventions.VarArgs
            };
            Type arrayClass = typeof(object[]);
            string methodName = "PosTest3_";

            for (int i = 0; i < conventions.Length; ++i)
            {
                VerificationHelper(TestModuleBuilder,
                    arrayClass,
                    methodName + i.ToString(),
                    conventions[i],
                    typeof(object),
                    new Type[] { });
            }
        }

        [Fact]
        public void TestWithValidArrayValuesAndWithValueTypeParameterMethod()
        {
            CallingConventions[] conventions = new CallingConventions[] {
                CallingConventions.Any,
                CallingConventions.ExplicitThis,
                CallingConventions.HasThis,
                CallingConventions.Standard,
                CallingConventions.VarArgs
            };
            Type arrayClass = typeof(object[]);
            string methodName = "PosTest4_";
            Type[] parametersType = new Type[] {
                typeof(int)
            };

            for (int i = 0; i < conventions.Length; ++i)
            {
                VerificationHelper(TestModuleBuilder,
                    arrayClass,
                    methodName + i.ToString(),
                    conventions[i],
                    typeof(int),
                    parametersType);
            }

            parametersType = new Type[] {
                typeof(int),
                typeof(MBTestStruct)
            };

            for (int i = 0; i < conventions.Length; ++i)
            {
                VerificationHelper(TestModuleBuilder,
                    arrayClass,
                    methodName + i.ToString(),
                    conventions[i],
                    typeof(int),
                    parametersType);
            }
        }

        [Fact]
        public void TestWithValidArrayValuesAndReferenceTypeParameterMethod()
        {
            CallingConventions[] conventions = new CallingConventions[] {
                CallingConventions.Any,
                CallingConventions.ExplicitThis,
                CallingConventions.HasThis,
                CallingConventions.Standard,
                CallingConventions.VarArgs
            };
            Type arrayClass = typeof(ModuleBuilderGetArrayMethod[]);
            string methodName = "PosTest5_";
            Type[] parametersType = new Type[] {
                typeof(object)
            };

            for (int i = 0; i < conventions.Length; ++i)
            {
                VerificationHelper(TestModuleBuilder,
                    arrayClass,
                    methodName + i.ToString(),
                    conventions[i],
                    typeof(int),
                    parametersType);
            }

            parametersType = new Type[] {
                typeof(object),
                typeof(string),
                typeof(ModuleBuilderGetArrayMethod)
            };

            for (int i = 0; i < conventions.Length; ++i)
            {
                VerificationHelper(TestModuleBuilder,
                    arrayClass,
                    methodName + i.ToString(),
                    conventions[i],
                    typeof(int),
                    parametersType);
            }
        }

        [Fact]
        public void TestWithValidValuesJaggedDimensionArray()
        {
            CallingConventions[] conventions = new CallingConventions[] {
                CallingConventions.Any,
                CallingConventions.ExplicitThis,
                CallingConventions.HasThis,
                CallingConventions.Standard,
                CallingConventions.VarArgs
            };
            Type arrayClass = typeof(ModuleBuilderGetArrayMethod[][]);
            string methodName = "PosTest6_";
            int errorNo = 1;
            Type[] parametersType = new Type[] {
                typeof(object)
            };

            for (int i = 0; i < conventions.Length; ++i)
            {
                VerificationHelper(TestModuleBuilder,
                    arrayClass,
                    methodName + i.ToString(),
                    conventions[i],
                    typeof(int),
                    parametersType);
                errorNo++;
            }

            parametersType = new Type[] {
                typeof(object),
                typeof(int),
                typeof(ModuleBuilderGetArrayMethod)
            };

            for (int i = 0; i < conventions.Length; ++i)
            {
                VerificationHelper(TestModuleBuilder,
                    arrayClass,
                    methodName + i.ToString(),
                    conventions[i],
                    typeof(int),
                    parametersType);
                errorNo++;
            }
        }

        [Fact]
        public void TestWithValidValuesOnMultiDimensionArray()
        {
            CallingConventions[] conventions = new CallingConventions[] {
                CallingConventions.Any,
                CallingConventions.ExplicitThis,
                CallingConventions.HasThis,
                CallingConventions.Standard,
                CallingConventions.VarArgs
            };
            Type arrayClass = typeof(ModuleBuilderGetArrayMethod[,]);
            string methodName = "PosTest7_";
            int errorNo = 1;
            Type[] parametersType = new Type[] {
                typeof(object)
            };

            for (int i = 0; i < conventions.Length; ++i)
            {
                VerificationHelper(TestModuleBuilder,
                    arrayClass,
                    methodName + i.ToString(),
                    conventions[i],
                    typeof(int),
                    parametersType);
                errorNo++;
            }

            parametersType = new Type[] {
                typeof(object),
                typeof(int),
                typeof(ModuleBuilderGetArrayMethod)
            };

            for (int i = 0; i < conventions.Length; ++i)
            {
                VerificationHelper(TestModuleBuilder,
                    arrayClass,
                    methodName + i.ToString(),
                    conventions[i],
                    typeof(int),
                    parametersType);
                errorNo++;
            }
        }

        [Fact]
        public void TestWithParameterTypesToNull()
        {
            CallingConventions[] conventions = new CallingConventions[] {
                CallingConventions.Any,
                CallingConventions.ExplicitThis,
                CallingConventions.HasThis,
                CallingConventions.Standard,
                CallingConventions.VarArgs
            };
            Type arrayClass = typeof(ModuleBuilderGetArrayMethod[]);
            string methodName = "PosTest8_";

            for (int i = 0; i < conventions.Length; ++i)
            {
                VerificationHelper(TestModuleBuilder,
                    arrayClass,
                    methodName + i.ToString(),
                    conventions[i],
                    typeof(void),
                    null);
            }
        }

        [Fact]
        public void TestThrowsExceptionWhenNotArray()
        {
            VerificationHelper(
                TestModuleBuilder,
                typeof(ModuleBuilderGetArrayMethod),
                "NegTest1_1",
                CallingConventions.Standard,
                typeof(void),
                new Type[] { },
                typeof(ArgumentException));
            VerificationHelper(
                TestModuleBuilder,
                typeof(int),
                "NegTest1_2",
                CallingConventions.Standard,
                typeof(void),
                new Type[] { },
                typeof(ArgumentException));
            VerificationHelper(
                TestModuleBuilder,
                typeof(Array),
                "NegTest1_3",
                CallingConventions.Standard,
                typeof(void),
                new Type[] { },
                typeof(ArgumentException));
            VerificationHelper(
                TestModuleBuilder,
                typeof(void),
                "NegTest1_4",
                CallingConventions.Standard,
                typeof(void),
                new Type[] { },
                typeof(ArgumentException));
        }

        [Fact]
        public void TestThrowsExceptionOnNullArrayClassOrMethodName()
        {
            VerificationHelper(
                TestModuleBuilder,
                null,
                "NegTest2_1",
                CallingConventions.Standard,
                typeof(void),
                new Type[] { },
                typeof(ArgumentNullException));
            VerificationHelper(
                TestModuleBuilder,
                typeof(ArgumentNullException[]),
                null,
                CallingConventions.Standard,
                typeof(void),
                new Type[] { },
                typeof(ArgumentNullException));
            VerificationHelper(
                TestModuleBuilder,
                typeof(ArgumentNullException[]),
                "NegTest2_2",
                CallingConventions.Standard,
                typeof(void),
                new Type[] { null },
                typeof(ArgumentNullException));
        }

        private void VerificationHelper(ModuleBuilder module, Type arrayClass, string methodName, CallingConventions convention, Type returnType, Type[] parameterTypes)
        {
            MethodInfo method = module.GetArrayMethod(arrayClass, methodName, convention, returnType, parameterTypes);

            Assert.True(method.DeclaringType.Equals(arrayClass));
            Assert.Equal(method.Name, methodName);
            Assert.Equal(method.CallingConvention, convention);
            Assert.True(method.ReturnType.Equals(returnType));
        }

        private void VerificationHelper(ModuleBuilder module, Type arrayClass, string methodName, CallingConventions convention, Type returnType, Type[] parameterTypes, Type desiredException)
        {
            Assert.Throws(desiredException, () => { MethodInfo method = module.GetArrayMethod(arrayClass, methodName, convention, returnType, parameterTypes); });
        }
    }
}

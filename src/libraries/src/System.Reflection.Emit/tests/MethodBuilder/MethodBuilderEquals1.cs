// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderEquals1
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const string PosTestDynamicMethodName = "PosDynamicMethod";
        private const string NegTestDynamicMethodName = "NegDynamicMethod";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const int MinStringLength = 1;
        private const int MaxStringLength = 128;

        private TypeBuilder TestTypeBuilder
        {
            get
            {
                if (null == _testTypeBuilder)
                {
                    AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
                    AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                        assemblyName, TestAssemblyBuilderAccess);

                    ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);
                    _testTypeBuilder = moduleBuilder.DefineType(TestDynamicTypeName);
                }

                return _testTypeBuilder;
            }
        }

        private TypeBuilder _testTypeBuilder;

        [Fact]
        public void PosTest1()
        {
            MethodBuilder builder = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                MethodAttributes.Public);

            Assert.True(builder.Equals(builder));
        }

        [Fact]
        public void PosTest2()
        {
            MethodBuilder builder1 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                MethodAttributes.Public);
            MethodBuilder builder2 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                MethodAttributes.Public);

            Assert.True(builder1.Equals(builder2));
        }

        [Fact]
        public void PosTest3()
        {
            MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot;

            MethodBuilder builder1 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes);
            MethodBuilder builder2 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes);

            Assert.True(builder1.Equals(builder2));
        }

        [Fact]
        public void PosTest4()
        {
            MethodAttributes attributes = MethodAttributes.Public;
            Type returnType = typeof(void);
            Type[] paramTypes = new Type[] {
                typeof(int),
                typeof(string)
            };

            MethodBuilder builder1 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType,
                paramTypes);
            MethodBuilder builder2 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType,
                paramTypes);

            Assert.True(builder1.Equals(builder2));
        }

        [Fact]
        public void PosTest5()
        {
            MethodAttributes attributes = MethodAttributes.Public;
            Type returnType1 = typeof(void);
            Type returnType2 = typeof(int);
            Type[] paramTypes = new Type[] {
                typeof(int),
                typeof(string)
            };

            MethodBuilder builder1 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType1,
                paramTypes);
            MethodBuilder builder2 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType2,
                paramTypes);

            Assert.False(builder1.Equals(builder2));
        }

        [Fact]
        public void PosTest6()
        {
            string methodName1 = null;
            string methodName2 = null;

            methodName1 = TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);
            methodName2 = TestLibrary.Generator.GetString(false, MinStringLength, MaxStringLength);

            // Avoid generate same method name
            if (methodName1.Length == methodName2.Length)
            {
                methodName1 += TestLibrary.Generator.GetChar();
            }

            MethodAttributes attributes = MethodAttributes.Public;

            MethodBuilder builder1 = TestTypeBuilder.DefineMethod(methodName1,
                attributes);
            MethodBuilder builder2 = TestTypeBuilder.DefineMethod(methodName2,
                attributes);

            Assert.False(builder1.Equals(builder2));
        }

        [Fact]
        public void PosTest7()
        {
            MethodAttributes attributes1 = MethodAttributes.Public;
            MethodAttributes attributes2 = MethodAttributes.Private;

            MethodBuilder builder1 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes1);
            MethodBuilder builder2 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes2);

            Assert.False(builder1.Equals(builder2));
        }

        [Fact]
        public void PosTest8()
        {
            MethodAttributes attributes = MethodAttributes.Public;
            Type returnType = typeof(void);
            Type[] paramTypes1 = new Type[] {
                typeof(int),
                typeof(float)
            };
            Type[] paramTypes2 = new Type[] {
                typeof(string),
                typeof(float)
            };

            MethodBuilder builder1 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType,
                paramTypes1);
            MethodBuilder builder2 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType,
                paramTypes2);

            Assert.False(builder1.Equals(builder2));
        }

        [Fact]
        public void PosTest9()
        {
            MethodAttributes attributes = MethodAttributes.Public;
            Type returnType = typeof(void);
            Type[] paramTypes1 = new Type[] {
                typeof(int),
                typeof(string)
            };
            Type[] paramTypes2 = new Type[] {
                typeof(string),
                typeof(int)
            };

            MethodBuilder builder1 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType,
                paramTypes1);
            MethodBuilder builder2 = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType,
                paramTypes2);

            Assert.False(builder1.Equals(builder2));
        }

        [Fact]
        public void NegTest1()
        {
            MethodBuilder builder = TestTypeBuilder.DefineMethod(NegTestDynamicMethodName,
                MethodAttributes.Public);

            Assert.False(builder.Equals(null));
        }
    }
}

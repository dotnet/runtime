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
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        private TypeBuilder GetTestTypeBuilder()
        {
            AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName, TestAssemblyBuilderAccess);

            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);
            return moduleBuilder.DefineType(TestDynamicTypeName);
        }

        [Fact]
        public void TestWithItself()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                MethodAttributes.Public);

            Assert.True(builder.Equals(builder));
        }

        [Fact]
        public void TestWithSameName()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder1 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                MethodAttributes.Public);
            MethodBuilder builder2 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                MethodAttributes.Public);

            Assert.True(builder1.Equals(builder2));
        }

        [Fact]
        public void TestWithSameNameAndAttributes()
        {
            MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.NewSlot;
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder1 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes);
            MethodBuilder builder2 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes);

            Assert.True(builder1.Equals(builder2));
        }

        [Fact]
        public void TestWithSameNameAttributeReturnTypeParameterType()
        {
            MethodAttributes attributes = MethodAttributes.Public;
            Type returnType = typeof(void);
            Type[] paramTypes = new Type[] {
                typeof(int),
                typeof(string)
            };

            TypeBuilder typeBuilder = GetTestTypeBuilder();

            MethodBuilder builder1 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType,
                paramTypes);
            MethodBuilder builder2 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType,
                paramTypes);

            Assert.True(builder1.Equals(builder2));
        }

        [Fact]
        public void TestWithDifferentReturnType()
        {
            MethodAttributes attributes = MethodAttributes.Public;
            Type returnType1 = typeof(void);
            Type returnType2 = typeof(int);
            Type[] paramTypes = new Type[] {
                typeof(int),
                typeof(string)
            };

            TypeBuilder typeBuilder = GetTestTypeBuilder();

            MethodBuilder builder1 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType1,
                paramTypes);
            MethodBuilder builder2 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType2,
                paramTypes);

            Assert.False(builder1.Equals(builder2));
        }

        [Fact]
        public void TestWithDifferentName()
        {
            string methodName1 = null;
            string methodName2 = null;

            methodName1 = _generator.GetString(false, MinStringLength, MaxStringLength);
            methodName2 = _generator.GetString(false, MinStringLength, MaxStringLength);

            // Avoid generate same method name
            if (methodName1.Length == methodName2.Length)
            {
                methodName1 += _generator.GetChar();
            }

            MethodAttributes attributes = MethodAttributes.Public;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder1 = typeBuilder.DefineMethod(methodName1,
                attributes);
            MethodBuilder builder2 = typeBuilder.DefineMethod(methodName2,
                attributes);

            Assert.False(builder1.Equals(builder2));
        }

        [Fact]
        public void TestWithDifferentAttributes()
        {
            MethodAttributes attributes1 = MethodAttributes.Public;
            MethodAttributes attributes2 = MethodAttributes.Private;

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder1 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes1);
            MethodBuilder builder2 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes2);

            Assert.False(builder1.Equals(builder2));
        }

        [Fact]
        public void TestWithDifferentParameterList()
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

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder1 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType,
                paramTypes1);
            MethodBuilder builder2 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType,
                paramTypes2);

            Assert.False(builder1.Equals(builder2));
        }

        [Fact]
        public void TestWithDifferentParameterOrder()
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

            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder1 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType,
                paramTypes1);
            MethodBuilder builder2 = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                attributes,
                returnType,
                paramTypes2);

            Assert.False(builder1.Equals(builder2));
        }

        [Fact]
        public void TestWithNull()
        {
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(NegTestDynamicMethodName,
                MethodAttributes.Public);

            Assert.False(builder.Equals(null));
        }
    }
}

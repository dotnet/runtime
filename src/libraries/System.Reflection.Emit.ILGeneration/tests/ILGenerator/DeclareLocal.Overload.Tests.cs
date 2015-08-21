// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class TestClassLocal1
    {
    }

    public struct TestStructLocal1
    {
    }

    public delegate void TestDelegateLocal1(TestStructLocal1 ts);

    public enum TestEnumLocal1
    {
        DEFAULT
    }

    public class TestExceptionLocal1 : Exception
    {
    }

    public class ILGeneratorDeclareLocal1
    {
        private const string AssemblyName = "ILGeneratorDeclareLocal1";
        private const string DefaultModuleName = "DynamicModule";
        private const string DefaultTypeName = "DynamicType";
        private const AssemblyBuilderAccess DefaultAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const MethodAttributes DefaultMethodAttribute = MethodAttributes.Public | MethodAttributes.Static;

        private static TypeBuilder s_testTypeBuilder;

        private static TypeBuilder TestTypeBuilder
        {
            get
            {
                if (s_testTypeBuilder == null)
                {
                    AssemblyName name = new AssemblyName(AssemblyName);
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, DefaultAssemblyBuilderAccess);
                    ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, DefaultModuleName);
                    s_testTypeBuilder = module.DefineType(DefaultTypeName);
                }

                return s_testTypeBuilder;
            }
        }

        [Fact]
        public void PosTest1()
        {
            MethodBuilder method = TestTypeBuilder.DefineMethod("PosTest1_Method", DefaultMethodAttribute);
            ILGenerator generator = method.GetILGenerator();

            int index = 0;
            VerificationHelper(generator, typeof(int), index++, "001.1");
            VerificationHelper(generator, typeof(object), index++, "001.2");
            VerificationHelper(generator, typeof(TestClassLocal1), index++, "001.3");
            VerificationHelper(generator, typeof(TestStructLocal1), index++, "001.4");
            VerificationHelper(generator, typeof(TestDelegateLocal1), index++, "001.5");
            VerificationHelper(generator, typeof(TestEnumLocal1), index++, "001.6");
            VerificationHelper(generator, typeof(TestExceptionLocal1), index++, "001.7");
            VerificationHelper(generator, typeof(void), index++, "001.8");
        }

        [Fact]
        public void PosTest2()
        {
            MethodBuilder method = TestTypeBuilder.DefineMethod("PosTest2_Method", DefaultMethodAttribute);
            ILGenerator generator = method.GetILGenerator();

            generator.BeginScope();

            int index = 0;
            VerificationHelper(generator, typeof(int), index++, "002.1");
            VerificationHelper(generator, typeof(object), index++, "002.2");
            VerificationHelper(generator, typeof(TestClassLocal1), index++, "002.3");
            VerificationHelper(generator, typeof(TestStructLocal1), index++, "002.4");
            VerificationHelper(generator, typeof(TestDelegateLocal1), index++, "002.5");
            VerificationHelper(generator, typeof(TestEnumLocal1), index++, "002.6");
            VerificationHelper(generator, typeof(TestExceptionLocal1), index++, "002.7");
            VerificationHelper(generator, typeof(void), index++, "002.8");
        }

        [Fact]
        public void PosTest3()
        {
            MethodBuilder method = TestTypeBuilder.DefineMethod("PosTest3_Method", DefaultMethodAttribute);
            ILGenerator generator = method.GetILGenerator();

            generator.BeginExceptionBlock();

            int index = 0;
            VerificationHelper(generator, typeof(int), index++, "003.1");
            VerificationHelper(generator, typeof(object), index++, "003.2");
            VerificationHelper(generator, typeof(TestClassLocal1), index++, "003.3");
            VerificationHelper(generator, typeof(TestStructLocal1), index++, "003.4");
            VerificationHelper(generator, typeof(TestDelegateLocal1), index++, "003.5");
            VerificationHelper(generator, typeof(TestEnumLocal1), index++, "003.6");
            VerificationHelper(generator, typeof(TestExceptionLocal1), index++, "003.7");
            VerificationHelper(generator, typeof(void), index++, "003.8");
        }

        [Fact]
        public void PosTest4()
        {
            MethodBuilder method = TestTypeBuilder.DefineMethod("PosTest4_Method", DefaultMethodAttribute);
            ILGenerator generator = method.GetILGenerator();

            generator.BeginExceptionBlock();
            generator.BeginCatchBlock(typeof(TestExceptionLocal1));

            int index = 0;
            VerificationHelper(generator, typeof(int), index++, "004.1");
            VerificationHelper(generator, typeof(object), index++, "004.2");
            VerificationHelper(generator, typeof(TestClassLocal1), index++, "004.3");
            VerificationHelper(generator, typeof(TestStructLocal1), index++, "004.4");
            VerificationHelper(generator, typeof(TestDelegateLocal1), index++, "004.5");
            VerificationHelper(generator, typeof(TestEnumLocal1), index++, "004.6");
            VerificationHelper(generator, typeof(TestExceptionLocal1), index++, "004.7");
            VerificationHelper(generator, typeof(void), index++, "004.8");
        }

        [Fact]
        public void PosTest5()
        {
            MethodBuilder method = TestTypeBuilder.DefineMethod("PosTest5_Method", DefaultMethodAttribute);
            ILGenerator generator = method.GetILGenerator();

            generator.BeginExceptionBlock();
            generator.BeginFinallyBlock();

            int index = 0;
            VerificationHelper(generator, typeof(int), index++, "005.1");
            VerificationHelper(generator, typeof(object), index++, "005.2");
            VerificationHelper(generator, typeof(TestClassLocal1), index++, "005.3");
            VerificationHelper(generator, typeof(TestStructLocal1), index++, "005.4");
            VerificationHelper(generator, typeof(TestDelegateLocal1), index++, "005.5");
            VerificationHelper(generator, typeof(TestEnumLocal1), index++, "005.6");
            VerificationHelper(generator, typeof(TestExceptionLocal1), index++, "005.7");
            VerificationHelper(generator, typeof(void), index++, "005.8");
        }

        [Fact]
        public void PosTest6()
        {
            ModuleBuilder module = TestTypeBuilder.Module as ModuleBuilder;
            TypeBuilder type = module.DefineType("Type_PosTest6", TypeAttributes.Abstract | TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("PosTest6_Method", MethodAttributes.Abstract | MethodAttributes.Public);
            ILGenerator generator = method.GetILGenerator();

            int index = 0;
            VerificationHelper(generator, typeof(int), index++, "006.1");
            VerificationHelper(generator, typeof(object), index++, "006.2");
            VerificationHelper(generator, typeof(TestClassLocal1), index++, "006.3");
            VerificationHelper(generator, typeof(TestStructLocal1), index++, "006.4");
            VerificationHelper(generator, typeof(TestDelegateLocal1), index++, "006.5");
            VerificationHelper(generator, typeof(TestEnumLocal1), index++, "006.6");
            VerificationHelper(generator, typeof(TestExceptionLocal1), index++, "006.7");
            VerificationHelper(generator, typeof(void), index++, "006.8");
        }

        private void VerificationHelper(ILGenerator generator, Type type, int desiredIndex, string errorNo)
        {
            LocalBuilder local = generator.DeclareLocal(type);

            Assert.NotNull(local);
            Assert.False(!local.LocalType.Equals(type) || local.LocalIndex != desiredIndex || local.IsPinned);
        }
    }
}

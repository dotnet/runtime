// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderGetILGenerator1
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestDynamicModuleName = "TestDynamicModule";
        private const string TestDynamicTypeName = "TestDynamicType";
        private const AssemblyBuilderAccess TestAssemblyBuilderAccess = AssemblyBuilderAccess.Run;
        private const TypeAttributes TestTypeAttributes = TypeAttributes.Abstract;
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
                    _testTypeBuilder = moduleBuilder.DefineType(TestDynamicTypeName, TestTypeAttributes);
                }

                return _testTypeBuilder;
            }
        }

        private TypeBuilder _testTypeBuilder;

        [Fact]
        public void PosTest1()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest2()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Assembly);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest3()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.CheckAccessOnOverride);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest4()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.FamANDAssem);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest5()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Family);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest6()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.FamORAssem);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest7()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Final);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest8()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.HasSecurity);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest9()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.HideBySig);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest10()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.MemberAccessMask);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest11()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.NewSlot);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest12()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Private);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest13()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.PrivateScope);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest14()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.RequireSecObject);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest16()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.ReuseSlot);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest17()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.RTSpecialName);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest18()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.SpecialName);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest19()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Static);
            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest20()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.UnmanagedExport);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest21()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Virtual);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest22()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.VtableLayoutMask);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void PosTest23()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Assembly |
                MethodAttributes.CheckAccessOnOverride |
                MethodAttributes.FamORAssem |
                MethodAttributes.Final |
                MethodAttributes.HasSecurity |
                MethodAttributes.HideBySig |
                MethodAttributes.MemberAccessMask |
                MethodAttributes.NewSlot |
                MethodAttributes.Private |
                MethodAttributes.PrivateScope |
                MethodAttributes.RequireSecObject |
                MethodAttributes.RTSpecialName |
                MethodAttributes.SpecialName |
                MethodAttributes.Static |
                MethodAttributes.UnmanagedExport);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void NegTest1()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.PinvokeImpl);

            Assert.Throws<InvalidOperationException>(() => { ILGenerator generator = builder.GetILGenerator(); });
        }

        [Fact]
        public void NegTest2()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            MethodBuilder builder = TestTypeBuilder.DefineMethod(methodName,
                MethodAttributes.Abstract |
                MethodAttributes.PinvokeImpl);

            Assert.Throws<InvalidOperationException>(() => { ILGenerator generator = builder.GetILGenerator(); });
        }
    }
}

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

        private TypeBuilder GetTestTypeBuilder()
        {
            AssemblyName assemblyName = new AssemblyName(TestDynamicAssemblyName);
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                assemblyName, TestAssemblyBuilderAccess);

            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestDynamicModuleName);
            return moduleBuilder.DefineType(TestDynamicTypeName, TestTypeAttributes);
        }

        [Fact]
        public void TestWithPublicAttribute()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Public);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithAssemblyAttribute()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Assembly);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithCheckAccessOnOverride()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.CheckAccessOnOverride);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithFamAndAssem()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.FamANDAssem);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithFamily()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Family);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithFamOrAssem()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.FamORAssem);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithFinal()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Final);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithHasSecurity()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.HasSecurity);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithHideBySig()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.HideBySig);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithMemberAccessMask()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.MemberAccessMask);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithNewSlot()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.NewSlot);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithPrivate()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Private);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithPrivateScope()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.PrivateScope);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithRequireSecObject()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.RequireSecObject);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithReuseSlot()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.ReuseSlot);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithRTSpecialName()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.RTSpecialName);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithSpecialName()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.SpecialName);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithStatic()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Static);
            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithUnManagedExport()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.UnmanagedExport);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithVirtual()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Virtual);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithVTableLayoutMask()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.VtableLayoutMask);

            Assert.NotNull(builder.GetILGenerator());
        }

        [Fact]
        public void TestWithCombinations()
        {
            string methodName = null;
            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
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
        public void TestThrowsExceptionForPInvoke()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.PinvokeImpl);

            Assert.Throws<InvalidOperationException>(() => { ILGenerator generator = builder.GetILGenerator(); });
        }

        [Fact]
        public void TestThrowsExceptionOnInvalidMethodAttributeSet()
        {
            string methodName = null;

            methodName = TestLibrary.Generator.GetString(false, false, true, MinStringLength, MaxStringLength);
            TypeBuilder typeBuilder = GetTestTypeBuilder();
            MethodBuilder builder = typeBuilder.DefineMethod(methodName,
                MethodAttributes.Abstract |
                MethodAttributes.PinvokeImpl);

            Assert.Throws<InvalidOperationException>(() => { ILGenerator generator = builder.GetILGenerator(); });
        }
    }
}

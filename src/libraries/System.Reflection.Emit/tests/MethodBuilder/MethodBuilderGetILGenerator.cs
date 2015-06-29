// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderGetILGenerator
    {
        [Fact]
        public void PosTest1()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;


            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static,
                                                            typeof(int), new Type[] { });

            ILGenerator ilgen = methbuild.GetILGenerator(20);
            int expectedRet = 5;
            ilgen.Emit(OpCodes.Ldc_I4, expectedRet);
            ilgen.Emit(OpCodes.Ret);

            Type tp = tpbuild.CreateTypeInfo().AsType();
            MethodInfo mi = tp.GetMethod("method1");
            int ret = (int)mi.Invoke(null, null);

            Assert.Equal(expectedRet, ret);
        }

        [Fact]
        public void PosTest2()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;


            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static,
                                                            typeof(int), new Type[] { });

            // runtime will use default size and ignore this value
            ILGenerator ilgen = methbuild.GetILGenerator(-10);
            int expectedRet = 5;
            ilgen.Emit(OpCodes.Ldc_I4, expectedRet);
            ilgen.Emit(OpCodes.Ret);

            Type tp = tpbuild.CreateTypeInfo().AsType();
            MethodInfo mi = tp.GetMethod("method1");
            int ret = (int)mi.Invoke(null, null);

            Assert.Equal(expectedRet, ret);
        }

        [Fact]
        public void NegTest1()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.PinvokeImpl);

            Assert.Throws<InvalidOperationException>(() => { ILGenerator ilgen = methbuild.GetILGenerator(10); });
        }
    }
}

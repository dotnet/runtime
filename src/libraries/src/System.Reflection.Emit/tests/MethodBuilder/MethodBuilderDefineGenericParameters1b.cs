// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderDefineGenericParameters1b
    {
        [Fact]
        public void PosTest1()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName(name);

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);

            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tb = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder mbuild = tb.DefineMethod("meth1", MethodAttributes.Public, typeof(int), new Type[] { });
            mbuild.DefineGenericParameters(new string[] { "T", "U", "V" });

            ILGenerator ilgen = mbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ldc_I4, 100);
            ilgen.Emit(OpCodes.Ret);

            Type tp = tb.CreateTypeInfo().AsType();
            MethodInfo mi = tp.GetMethod("meth1");

            Type[] methArgs = { typeof(int), typeof(string), typeof(object) };

            MethodInfo miConstructed = mi.MakeGenericMethod(methArgs);
            Type[] args = miConstructed.GetGenericArguments();

            Assert.Equal("System.Int32", args[0].ToString());
            Assert.Equal("System.String", args[1].ToString());
            Assert.Equal("System.Object", args[2].ToString());
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
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static);


            string[] typeParamNames = { };
            Assert.Throws<ArgumentException>(() =>
            {
                GenericTypeParameterBuilder[] typeParameters = methbuild.DefineGenericParameters(typeParamNames);
            });
        }

        [Fact]
        public void NegTest2()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static);

            Assert.Throws<ArgumentNullException>(() =>
            {
                GenericTypeParameterBuilder[] typeParameters = methbuild.DefineGenericParameters(null);
            });
        }

        [Fact]
        public void NegTest3()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static);

            string s = null;
            string[] typeParamNames = { "T", s };
            Assert.Throws<ArgumentNullException>(() =>
            {
                GenericTypeParameterBuilder[] typeParameters = methbuild.DefineGenericParameters(typeParamNames);
            });
        }

        [Fact]
        public void NegTest4()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static);

            ILGenerator ilgen = methbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            Type tp = tpbuild.CreateTypeInfo().AsType();
            MethodInfo mi = tp.GetMethod("method1");

            string[] typeParamNames = { "T", "U" };
            Assert.Throws<InvalidOperationException>(() =>
            {
                GenericTypeParameterBuilder[] typeParameters = methbuild.DefineGenericParameters(typeParamNames);
            });
        }

        [Fact]
        public void NegTest5()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static);


            methbuild.SetImplementationFlags(MethodImplAttributes.IL |
                                             MethodImplAttributes.Managed |
                                             MethodImplAttributes.Synchronized |
                                             MethodImplAttributes.NoInlining);

            string[] typeParamNames = { "T", "U" };
            Assert.Throws<InvalidOperationException>(() =>
            {
                GenericTypeParameterBuilder[] typeParameters = methbuild.DefineGenericParameters(typeParamNames);
            });
        }

        [Fact]
        public void NegTest6()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static);

            string[] typeParamNames = { "T", "U" };
            GenericTypeParameterBuilder[] typeParameters =
                methbuild.DefineGenericParameters(typeParamNames);

            string[] typeParamNames2 = { "M", "K" };
            Assert.Throws<InvalidOperationException>(() => { methbuild.DefineGenericParameters(typeParamNames2); });
        }
    }
}

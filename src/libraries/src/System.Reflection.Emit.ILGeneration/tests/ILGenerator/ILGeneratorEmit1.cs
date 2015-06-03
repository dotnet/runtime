// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class ILGeneratorEmit1
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

            int expectedRet = 1;

            // generate code for the method that we are going to use as MethodInfo in ILGenerator.Emit()
            ILGenerator ilgen = mbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ldc_I4, expectedRet);
            ilgen.Emit(OpCodes.Ret);

            // create the type where this method is in
            Type tp = tb.CreateTypeInfo().AsType();
            MethodInfo mi = tp.GetMethod("meth1");

            TypeBuilder tb2 = modbuild.DefineType("C2", TypeAttributes.Public);
            MethodBuilder mbuild2 = tb2.DefineMethod("meth2", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { });

            // generate code for the method which will be invoking the first method
            ILGenerator ilgen2 = mbuild2.GetILGenerator();
            ilgen2.Emit(OpCodes.Newobj, tp.GetConstructor(new Type[] { }));
            ilgen2.Emit(OpCodes.Call, mi);
            ilgen2.Emit(OpCodes.Ret);

            // create the type whose method will be invoking the MethodInfo method
            Type tp2 = tb2.CreateTypeInfo().AsType();

            MethodInfo md = tp2.GetMethod("meth2");

            // meth2 should invoke meth1 which should return value from meth1
            int ret = (int)md.Invoke(null, null);

            Assert.Equal(ret, expectedRet);
        }

        [Fact]
        public void PosTest2()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName(name);
            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tb = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder mbuild = tb.DefineMethod("meth1", MethodAttributes.Public, typeof(int), new Type[] { });
            int expectedRet = 12;

            // generate code for the method that we are going to use as MethodInfo in ILGenerator.Emit()
            ILGenerator ilgen = mbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ldc_I4, expectedRet);
            ilgen.Emit(OpCodes.Ret);

            // create the type where this method is in
            Type tp = tb.CreateTypeInfo().AsType();
            MethodInfo mi = tp.GetMethod("meth1");

            TypeBuilder tb2 = modbuild.DefineType("C2", TypeAttributes.Public);
            MethodBuilder mbuild2 = tb2.DefineMethod("meth2", MethodAttributes.Public, typeof(int), new Type[] { });

            // generate code for the method which will be invoking the first method
            ILGenerator ilgen2 = mbuild2.GetILGenerator();
            ilgen2.Emit(OpCodes.Newobj, tp.GetConstructor(new Type[] { }));
            ilgen2.Emit(OpCodes.Callvirt, mi);
            ilgen2.Emit(OpCodes.Ret);

            // create the type whose method will be invoking the MethodInfo method
            Type tp2 = tb2.CreateTypeInfo().AsType();

            MethodInfo md = tp2.GetMethod("meth2");

            // meth2 should invoke meth1 which should return value 'methodRet'
            int ret = (int)md.Invoke(Activator.CreateInstance(tp2), null);


            Assert.Equal(ret, expectedRet);
        }

        [Fact]
        public void PosTest3()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName(name);
            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tb = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder mbuild = tb.DefineMethod("meth1", MethodAttributes.Public, typeof(int), new Type[] { });
            mbuild.DefineGenericParameters(new string[] { "T" });

            int expectedRet = 101;

            // generate code for the method that we are going to use as MethodInfo in ILGenerator.Emit()
            ILGenerator ilgen = mbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ldc_I4, expectedRet);
            ilgen.Emit(OpCodes.Ret);

            // create the type where this method is in
            Type tp = tb.CreateTypeInfo().AsType();
            MethodInfo mi = tp.GetMethod("meth1");
            MethodInfo miConstructed = mi.MakeGenericMethod(typeof(int));

            TypeBuilder tb2 = modbuild.DefineType("C2", TypeAttributes.Public);
            MethodBuilder mbuild2 = tb2.DefineMethod("meth2", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[] { });

            // generate code for the method which will be invoking the first method
            ILGenerator ilgen2 = mbuild2.GetILGenerator();
            ilgen2.Emit(OpCodes.Newobj, tp.GetConstructor(new Type[] { }));
            ilgen2.Emit(OpCodes.Callvirt, miConstructed);
            ilgen2.Emit(OpCodes.Ret);

            // create the type whose method will be invoking the MethodInfo method
            Type tp2 = tb2.CreateTypeInfo().AsType();

            MethodInfo md = tp2.GetMethod("meth2");

            // meth2 should invoke meth1 which should return value 'methodRet'
            int ret = (int)md.Invoke(null, null);
            Assert.Equal(ret, expectedRet);
        }

        [Fact]
        public void PosTest4()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName(name);

            // create a dynamic assembly & module
            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tb = modbuild.DefineType("C1", TypeAttributes.Public);
            tb.DefineGenericParameters(new string[] { "T" });

            MethodBuilder mbuild = tb.DefineMethod("meth1", MethodAttributes.Public, typeof(long), new Type[] { });

            long expectedRet = 500000;
            // generate code for the method that we are going to use as MethodInfo in ILGenerator.Emit()
            ILGenerator ilgen = mbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ldc_I8, expectedRet);
            ilgen.Emit(OpCodes.Ret);


            // create the type where this method is in
            Type tp = tb.CreateTypeInfo().AsType();
            Type tpConstructed = tp.MakeGenericType(typeof(int));
            MethodInfo mi = tpConstructed.GetMethod("meth1");


            TypeBuilder tb2 = modbuild.DefineType("C2", TypeAttributes.Public);
            MethodBuilder mbuild2 = tb2.DefineMethod("meth2", MethodAttributes.Public | MethodAttributes.Static,
                                                        typeof(long), new Type[] { });

            // generate code for the method which will be invoking the first method
            ILGenerator ilgen2 = mbuild2.GetILGenerator();
            ilgen2.Emit(OpCodes.Newobj, tpConstructed.GetConstructor(new Type[] { }));
            ilgen2.Emit(OpCodes.Callvirt, mi);
            ilgen2.Emit(OpCodes.Ret);

            // create the type whose method will be invoking the MethodInfo method
            Type tp2 = tb2.CreateTypeInfo().AsType();

            MethodInfo md = tp2.GetMethod("meth2");

            // meth2 should invoke meth1 which should return value 'methodRet'
            long ret = (long)md.Invoke(null, null);


            Assert.Equal(ret, expectedRet);
        }

        [Fact]
        public void PosTest5()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName(name);

            // create a dynamic assembly & module



            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");


            TypeBuilder tb = modbuild.DefineType("C1", TypeAttributes.Public);
            tb.DefineGenericParameters(new string[] { "T" });

            MethodBuilder mbuild = tb.DefineMethod("meth1", MethodAttributes.Public, typeof(int), new Type[] { });
            mbuild.DefineGenericParameters(new string[] { "U" });

            int expectedRet = 1;
            // generate code for the method that we are going to use as MethodInfo in ILGenerator.Emit()
            ILGenerator ilgen = mbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ldc_I4, expectedRet);
            ilgen.Emit(OpCodes.Ret);


            // create the type where this method is in
            Type tp = tb.CreateTypeInfo().AsType();
            Type tpConstructed = tp.MakeGenericType(typeof(int));
            MethodInfo mi = tpConstructed.GetMethod("meth1");
            MethodInfo miConstructed = mi.MakeGenericMethod(typeof(string));

            TypeBuilder tb2 = modbuild.DefineType("C2", TypeAttributes.Public);
            MethodBuilder mbuild2 = tb2.DefineMethod("meth2", MethodAttributes.Public | MethodAttributes.Static,
                                                        typeof(int), new Type[] { });



            // generate code for the method which will be invoking the first method
            ILGenerator ilgen2 = mbuild2.GetILGenerator();
            ilgen2.Emit(OpCodes.Newobj, tpConstructed.GetConstructor(new Type[] { }));
            ilgen2.Emit(OpCodes.Callvirt, miConstructed);
            ilgen2.Emit(OpCodes.Ret);

            // create the type whose method will be invoking the MethodInfo method
            Type tp2 = tb2.CreateTypeInfo().AsType();

            MethodInfo md = tp2.GetMethod("meth2");

            // meth2 should invoke meth1 which should return value 'methodRet'
            int ret = (int)md.Invoke(null, null);


            Assert.Equal(ret, expectedRet);
        }

        [Fact]
        public void NegTest()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;
            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            MethodBuilder methbuild = modbuild.DefineGlobalMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[] { });

            ILGenerator ilgen = methbuild.GetILGenerator();
            MethodInfo mi = null;

            Assert.Throws<ArgumentNullException>(() => { ilgen.Emit(OpCodes.Call, mi); });
        }
    }
}

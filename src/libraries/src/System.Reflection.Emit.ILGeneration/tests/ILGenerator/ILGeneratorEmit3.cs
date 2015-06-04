// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public struct EmitStruct3 { }

    public class ILGeneratorEmit3
    {
        [Fact]
        public void PosTest1()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName(name);

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tb = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder mbuild = tb.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static,
                                                    typeof(bool), new Type[] { });

            ILGenerator ilgen = mbuild.GetILGenerator();

            LocalBuilder lb0 = ilgen.DeclareLocal(typeof(int));
            LocalBuilder lb1 = ilgen.DeclareLocal(typeof(byte));
            LocalBuilder lb2 = ilgen.DeclareLocal(typeof(double));
            LocalBuilder lb3 = ilgen.DeclareLocal(typeof(bool));
            LocalBuilder lb4 = ilgen.DeclareLocal(typeof(bool));

            Label label1 = ilgen.DefineLabel();
            Label label2 = ilgen.DefineLabel();
            Label label3 = ilgen.DefineLabel();
            Label label4 = ilgen.DefineLabel();

            // emit the locals and check that we get correct values stored 
            ilgen.Emit(OpCodes.Ldc_I4, 5);
            ilgen.Emit(OpCodes.Stloc, lb0);
            ilgen.Emit(OpCodes.Ldloc, lb0);
            ilgen.Emit(OpCodes.Ldc_I4, 5);
            ilgen.Emit(OpCodes.Ceq);
            ilgen.Emit(OpCodes.Stloc, lb4);
            ilgen.Emit(OpCodes.Ldloc, lb4);
            ilgen.Emit(OpCodes.Brtrue_S, label1);

            ilgen.Emit(OpCodes.Ldc_I4, 0);
            ilgen.Emit(OpCodes.Stloc, lb3);
            ilgen.Emit(OpCodes.Br_S, label4);


            ilgen.MarkLabel(label1);
            ilgen.Emit(OpCodes.Ldc_I4, 1);
            ilgen.Emit(OpCodes.Stloc, lb1);
            ilgen.Emit(OpCodes.Ldloc, lb1);
            ilgen.Emit(OpCodes.Ldc_I4, 1);
            ilgen.Emit(OpCodes.Ceq);
            ilgen.Emit(OpCodes.Stloc, lb4);
            ilgen.Emit(OpCodes.Ldloc, lb4);
            ilgen.Emit(OpCodes.Brtrue_S, label2);

            ilgen.Emit(OpCodes.Ldc_I4, 0);
            ilgen.Emit(OpCodes.Stloc, lb3);
            ilgen.Emit(OpCodes.Br_S, label4);

            ilgen.MarkLabel(label2);
            ilgen.Emit(OpCodes.Ldc_R8, 2.5);
            ilgen.Emit(OpCodes.Stloc, lb2);
            ilgen.Emit(OpCodes.Ldloc, lb2);
            ilgen.Emit(OpCodes.Ldc_R8, 2.5);
            ilgen.Emit(OpCodes.Ceq);
            ilgen.Emit(OpCodes.Stloc, lb4);
            ilgen.Emit(OpCodes.Ldloc, lb4);
            ilgen.Emit(OpCodes.Brtrue_S, label3);

            ilgen.Emit(OpCodes.Ldc_I4, 0);
            ilgen.Emit(OpCodes.Stloc, lb3);
            ilgen.Emit(OpCodes.Br_S, label4);

            // should return true if all checks were correct
            ilgen.MarkLabel(label3);
            ilgen.Emit(OpCodes.Ldc_I4, 1);
            ilgen.Emit(OpCodes.Stloc, lb3);
            ilgen.Emit(OpCodes.Br_S, label4);

            ilgen.MarkLabel(label4);
            ilgen.Emit(OpCodes.Ldloc, lb3);
            ilgen.Emit(OpCodes.Ret);

            // create the type where this method is in
            Type tp = tb.CreateTypeInfo().AsType();
            MethodInfo mi = tp.GetMethod("meth1");

            Assert.True((bool)mi.Invoke(null, null));
        }

        [Fact]
        public void PosTest2()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName(name);

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tb = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder mbuild = tb.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static,
                                                    typeof(bool), new Type[] { });

            ILGenerator ilgen = mbuild.GetILGenerator();

            LocalBuilder lb0 = ilgen.DeclareLocal(typeof(EmitStruct3));

            // emit the locals
            ilgen.Emit(OpCodes.Ldloca, lb0);
            ilgen.Emit(OpCodes.Initobj, typeof(EmitStruct3));

            ilgen.Emit(OpCodes.Ldc_I4, 1);
            ilgen.Emit(OpCodes.Ret);

            // create the type where this method is in
            Type tp = tb.CreateTypeInfo().AsType();
            MethodInfo mi = tp.GetMethod("meth1");

            Assert.True((bool)mi.Invoke(null, null));
        }

        [Fact]
        public void PosTest3()
        {
            string name = "AssemblyEmit3Pos3";
            AssemblyName asmname = new AssemblyName(name);

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);


            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");

            TypeBuilder tb = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder mbuild = tb.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static,
                                                    typeof(bool), new Type[] { });

            ILGenerator ilgen = mbuild.GetILGenerator();

            LocalBuilder lb0 = ilgen.DeclareLocal(typeof(int));

            // emit the locals

            ilgen.Emit(OpCodes.Nop, lb0);
            ilgen.Emit(OpCodes.Ldc_I4, 1);
            ilgen.Emit(OpCodes.Ret);

            // create the type where this method is in
            Type tp = tb.CreateTypeInfo().AsType();
            MethodInfo mi = tp.GetMethod("meth1");

            Assert.True((bool)mi.Invoke(null, null));
        }

        [Fact]
        public void PosTest4()
        {
            string name = "AssemblyEmit3Pos4";
            AssemblyName asmname = new AssemblyName(name);

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tb = modbuild.DefineType("C1", TypeAttributes.Public);
            MethodBuilder mbuild = tb.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static,
                                                    typeof(bool), new Type[] { });

            ILGenerator ilgen = mbuild.GetILGenerator();

            LocalBuilder lb0 = ilgen.DeclareLocal(typeof(int));
            LocalBuilder lb1 = ilgen.DeclareLocal(typeof(byte));
            LocalBuilder lb2 = ilgen.DeclareLocal(typeof(double));
            LocalBuilder lb3 = ilgen.DeclareLocal(typeof(bool));
            LocalBuilder lb4 = ilgen.DeclareLocal(typeof(bool));

            Label label1 = ilgen.DefineLabel();
            Label label2 = ilgen.DefineLabel();
            Label label3 = ilgen.DefineLabel();
            Label label4 = ilgen.DefineLabel();



            // emit the locals using Stloc_0, Stloc_1, Stloc_2, Stloc_3, Stloc_S, 
            // Ldloc_0, Ldloc_1, Ldloc_2, Ldloc_3, Ldloc_S,

            ilgen.Emit(OpCodes.Ldc_I4, 5);
            ilgen.Emit(OpCodes.Stloc_0, lb0);
            ilgen.Emit(OpCodes.Ldloc_0, lb0);
            ilgen.Emit(OpCodes.Ldc_I4, 5);
            ilgen.Emit(OpCodes.Ceq);
            ilgen.Emit(OpCodes.Stloc_S, lb4);
            ilgen.Emit(OpCodes.Ldloc_S, lb4);
            ilgen.Emit(OpCodes.Brtrue_S, label1);

            ilgen.Emit(OpCodes.Ldc_I4, 0);
            ilgen.Emit(OpCodes.Stloc_3, lb3);
            ilgen.Emit(OpCodes.Br_S, label4);


            ilgen.MarkLabel(label1);
            ilgen.Emit(OpCodes.Ldc_I4, 1);
            ilgen.Emit(OpCodes.Stloc_1, lb1);
            ilgen.Emit(OpCodes.Ldloc_1, lb1);
            ilgen.Emit(OpCodes.Ldc_I4, 1);
            ilgen.Emit(OpCodes.Ceq);
            ilgen.Emit(OpCodes.Stloc_S, lb4);
            ilgen.Emit(OpCodes.Ldloc_S, lb4);
            ilgen.Emit(OpCodes.Brtrue_S, label2);

            ilgen.Emit(OpCodes.Ldc_I4, 0);
            ilgen.Emit(OpCodes.Stloc_3, lb3);
            ilgen.Emit(OpCodes.Br_S, label4);

            ilgen.MarkLabel(label2);
            ilgen.Emit(OpCodes.Ldc_R8, 2.5);
            ilgen.Emit(OpCodes.Stloc_2, lb2);
            ilgen.Emit(OpCodes.Ldloc_2, lb2);
            ilgen.Emit(OpCodes.Ldc_R8, 2.5);
            ilgen.Emit(OpCodes.Ceq);
            ilgen.Emit(OpCodes.Stloc_S, lb4);
            ilgen.Emit(OpCodes.Ldloc_S, lb4);
            ilgen.Emit(OpCodes.Brtrue_S, label3);

            ilgen.Emit(OpCodes.Ldc_I4, 0);
            ilgen.Emit(OpCodes.Stloc_3, lb3);
            ilgen.Emit(OpCodes.Br_S, label4);

            ilgen.MarkLabel(label3);
            ilgen.Emit(OpCodes.Ldc_I4, 1);
            ilgen.Emit(OpCodes.Stloc_3, lb3);
            ilgen.Emit(OpCodes.Br_S, label4);

            ilgen.MarkLabel(label4);
            ilgen.Emit(OpCodes.Ldloc_3, lb3);
            ilgen.Emit(OpCodes.Ret);

            // create the type where this method is in
            Type tp = tb.CreateTypeInfo().AsType();
            MethodInfo mi = tp.GetMethod("meth1");

            Assert.True((bool)mi.Invoke(null, null));
        }

        [Fact]
        public void NegTest1()
        {
            string name = "AssemblyEmit31";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            MethodBuilder methbuild = modbuild.DefineGlobalMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[] { });

            ILGenerator ilgen = methbuild.GetILGenerator();

            LocalBuilder lb = null;

            Assert.Throws<ArgumentNullException>(() => { ilgen.Emit(OpCodes.Ldloc_0, lb); });
        }

        [Fact]
        public void NegTest2()
        {
            string name = "AssemblyEmit32";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            MethodBuilder methbuild = modbuild.DefineGlobalMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[] { });
            MethodBuilder methbuild2 = modbuild.DefineGlobalMethod("method2", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[] { });

            ILGenerator ilgen = methbuild.GetILGenerator();
            ILGenerator ilgen2 = methbuild2.GetILGenerator();

            LocalBuilder lb = ilgen2.DeclareLocal(typeof(string));

            Assert.Throws<ArgumentException>(() => { ilgen.Emit(OpCodes.Ldloc_0, lb); });
        }

        [Fact]
        public void NegTest3()
        {
            string name = "AssemblyEmit33";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            MethodBuilder methbuild = modbuild.DefineGlobalMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[] { });
            MethodBuilder methbuild2 = modbuild.DefineGlobalMethod("method2", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[] { });

            ILGenerator ilgen = methbuild.GetILGenerator();

            for (int i = 0; i <= byte.MaxValue; ++i)
            {
                ilgen.DeclareLocal(typeof(string));
            }

            LocalBuilder lb = ilgen.DeclareLocal(typeof(string));

            Assert.Throws<InvalidOperationException>(() => { ilgen.Emit(OpCodes.Ldloc_S, lb); });
        }
    }
}

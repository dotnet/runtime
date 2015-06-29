// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public interface TBI1
    {
        int M();
    }

    public class TBA1 : TBI1
    {
        public virtual int M() { return 1; }
    }

    public class MethodBuilderDefineMethodOverride
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
            MethodBuilder methbuild = tpbuild.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            ILGenerator ilgen = methbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ldc_I4, 2);
            ilgen.Emit(OpCodes.Ret);

            // C1 implements interface TBI1
            tpbuild.AddInterfaceImplementation(typeof(TBI1));

            MethodInfo md2 = typeof(TBI1).GetMethod("M");

            tpbuild.DefineMethodOverride(methbuild, md2);

            Type tp = tpbuild.CreateTypeInfo().AsType();

            MethodInfo mdInvoke = typeof(TBI1).GetMethod("M");
            int ret = (int)mdInvoke.Invoke(Activator.CreateInstance(tp), null);

            Assert.Equal(2, ret);
        }

        [Fact]
        public void PosTest2()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public, typeof(TBA1));
            MethodBuilder methbuild = tpbuild.DefineMethod("M2", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            ILGenerator ilgen = methbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ldc_I4, 2);
            ilgen.Emit(OpCodes.Ret);

            // C1 implements interface TBI1 and extends class TBA1
            tpbuild.AddInterfaceImplementation(typeof(TBI1));

            MethodInfo md2 = typeof(TBI1).GetMethod("M");

            tpbuild.DefineMethodOverride(methbuild, md2);

            Type tp = tpbuild.CreateTypeInfo().AsType();

            MethodInfo mdInvoke = typeof(TBI1).GetMethod("M");

            //         
            ConstructorInfo tpCtor = tp.GetConstructor(new Type[] { });
            Console.WriteLine(tpCtor.ToString());
            object instOfTp = tpCtor.Invoke(new object[] { });
            int ret = (int)mdInvoke.Invoke(instOfTp, null);
            int retParent = (int)mdInvoke.Invoke(Activator.CreateInstance(typeof(TBA1)), null);

            Assert.True(ret == 2 && retParent == 1);
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

            MethodInfo md = typeof(TBA1).GetMethod("M");

            Assert.Throws<ArgumentNullException>(() => { tpbuild.DefineMethodOverride(null, md); });
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

            MethodInfo md = typeof(TBI1).GetMethod("M");

            Assert.Throws<ArgumentNullException>(() => { tpbuild.DefineMethodOverride(md, null); });
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


            MethodInfo md = typeof(TBI1).GetMethod("M");
            MethodInfo md2 = typeof(TBA1).GetMethod("M");

            Assert.Throws<ArgumentException>(() => { tpbuild.DefineMethodOverride(md, md2); });
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
            MethodBuilder methbuild = tpbuild.DefineMethod("M", MethodAttributes.Public | MethodAttributes.Virtual, typeof(int), null);
            ILGenerator ilgen = methbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);

            // C1 implements interface TBI1
            tpbuild.AddInterfaceImplementation(typeof(TBI1));

            Type tp = tpbuild.CreateTypeInfo().AsType();
            MethodInfo md = tp.GetMethod("M");
            MethodInfo md2 = typeof(TBI1).GetMethod("M");

            Assert.Throws<InvalidOperationException>(() => { tpbuild.DefineMethodOverride(md, md2); });
        }
    }
}

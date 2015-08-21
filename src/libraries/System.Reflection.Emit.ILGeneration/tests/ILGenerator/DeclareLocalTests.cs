// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class ILGeneratorDeclareLocal
    {
        private ModuleBuilder modHelper(AssemblyBuilder asmbuild)
        {
            return (ModuleBuilder)asmbuild.ManifestModule;
        }

        [Fact]
        public void NegTest1()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            MethodBuilder methbuild = modbuild.DefineGlobalMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[] { });
            ILGenerator ilgen = methbuild.GetILGenerator();
            Assert.Throws<ArgumentNullException>(() => { ilgen.DeclareLocal(null); });
        }

        [Fact]
        public void NegTest2()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;
            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1");
            MethodBuilder methbuild = tpbuild.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[] { });
            ILGenerator ilgen = methbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);
            tpbuild.CreateTypeInfo();
            Assert.Throws<InvalidOperationException>(() => { ilgen.DeclareLocal(typeof(int)); });
        }

        [Fact]
        public void NegTest3()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;
            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            MethodBuilder methbuild = modbuild.DefineGlobalMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(Type), new Type[] { });
            ILGenerator ilgen = methbuild.GetILGenerator();
            ilgen.Emit(OpCodes.Ret);
            modbuild.CreateGlobalFunctions();
            Assert.Throws<InvalidOperationException>(() => { ilgen.DeclareLocal(typeof(int)); });
        }
    }
}

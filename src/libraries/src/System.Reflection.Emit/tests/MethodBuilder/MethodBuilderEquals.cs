// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using TestLibrary;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderEquals
    {
        [Fact]
        public void TestRandomObject()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            MethodBuilder methbuild = tpbuild.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static,
                                                            typeof(int), new Type[] { });

            Assert.False(methbuild.Equals(tpbuild));
        }

        [Fact]
        public void TestWithDifferentNames()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;

            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            MethodBuilder methbuild1 = tpbuild.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static,
                                                            typeof(int), new Type[] { });

            MethodBuilder methbuild2 = tpbuild.DefineMethod("meth11", MethodAttributes.Public | MethodAttributes.Static,
                                                            typeof(int), new Type[] { });

            Assert.False(methbuild1.Equals(methbuild2));
        }

        [Fact]
        public void TestWithDifferentAttributes()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;


            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            MethodBuilder methbuild1 = tpbuild.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static,
                                                            typeof(int), new Type[] { });

            MethodBuilder methbuild2 = tpbuild.DefineMethod("meth1", MethodAttributes.Public,
                                                            typeof(int), new Type[] { });


            Assert.False(methbuild1.Equals(methbuild2));
        }

        [Fact]
        public void TestWithDifferentSignatures()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;


            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1"); TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            MethodBuilder methbuild1 = tpbuild.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static);
            methbuild1.SetSignature(typeof(int), new Type[] { typeof(string) }, null, null, null, null);

            MethodBuilder methbuild2 = tpbuild.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static);
            methbuild2.SetSignature(typeof(int), new Type[] { typeof(object) }, null, null, null, null);

            Assert.False(methbuild1.Equals(methbuild2));
        }

        [Fact]
        public void TestWithEqualObjects()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;


            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1"); TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            MethodBuilder methbuild1 = tpbuild.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static);
            methbuild1.SetSignature(typeof(int), new Type[] { typeof(string) }, null, null, null, null);

            MethodBuilder methbuild2 = tpbuild.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static);
            methbuild2.SetSignature(typeof(int), new Type[] { typeof(string) }, null, null, null, null);

            Assert.True(methbuild1.Equals(methbuild2));
        }

        [Fact]
        public void TestWithDifferentGenericArguments()
        {
            string name = "Assembly1";
            AssemblyName asmname = new AssemblyName();
            asmname.Name = name;


            AssemblyBuilder asmbuild = AssemblyBuilder.DefineDynamicAssembly(asmname, AssemblyBuilderAccess.Run);
            ModuleBuilder modbuild = TestLibrary.Utilities.GetModuleBuilder(asmbuild, "Module1");
            TypeBuilder tpbuild = modbuild.DefineType("C1", TypeAttributes.Public);

            MethodBuilder methbuild1 = tpbuild.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static);
            methbuild1.SetSignature(typeof(int), new Type[] { typeof(string) }, null, null, null, null);
            methbuild1.DefineGenericParameters(new string[] { "T", "U", "V" });

            MethodBuilder methbuild2 = tpbuild.DefineMethod("meth1", MethodAttributes.Public | MethodAttributes.Static);
            methbuild2.SetSignature(typeof(int), new Type[] { typeof(string) }, null, null, null, null);
            methbuild2.DefineGenericParameters(new string[] { "T", "U" });

            Assert.False(methbuild1.Equals(methbuild2));
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using Xunit;

namespace System.Refleciton.Emit.ILGeneration.Tests
{
    public class LabelEquals
    {
        private const string c_TEST_DYNAMIC_ASSEMBLY_NAME = "TestDynamicAssembly";
        private const string c_TEST_MODULE_NAME = "TestModuleName";
        private const string c_TEST_TYPE_NAME = "TestTypeName";
        private const string c_TEST_METHOD_NAME = "TestMethodName";

        private AssemblyBuilder CreateDynamicAssembly(string name, AssemblyBuilderAccess access)
        {
            AssemblyName myAsmName = new AssemblyName();
            myAsmName.Name = name;
            AssemblyBuilder myAsmBuilder = AssemblyBuilder.DefineDynamicAssembly(myAsmName, access);
            return myAsmBuilder;
        }

        [Fact]
        public void PosTest1()
        {
            Label label = new Label();
            object ob = label;
            Assert.False(!label.Equals(ob));
        }

        [Fact]
        public void PosTest2()
        {
            AssemblyBuilder assemblyBuilder = this.CreateDynamicAssembly(c_TEST_DYNAMIC_ASSEMBLY_NAME, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, c_TEST_MODULE_NAME);
            TypeBuilder typeBuilder = moduleBuilder.DefineType(c_TEST_TYPE_NAME);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(c_TEST_METHOD_NAME, MethodAttributes.Public);
            ILGenerator iLGenerator = methodBuilder.GetILGenerator();
            Label label1 = iLGenerator.DefineLabel();
            object ob = iLGenerator.DefineLabel();
            Assert.False(label1.Equals(ob));
        }

        [Fact]
        public void PosTest3()
        {
            object ob = TestLibrary.Generator.GetInt32(-55);
            Label label = new Label();
            Assert.False(label.Equals(ob));
        }

        [Fact]
        public void PosTest4()
        {
            object ob = "label";
            Label label = new Label();
            Assert.False(label.Equals(ob));
        }

        [Fact]
        public void PosTest5()
        {
            object ob = null;
            Label label = new Label();
            Assert.False(label.Equals(ob));
        }

        [Fact]
        public void PosTest6()
        {
            object ob = " ";
            Label label = new Label();
            Assert.False(label.Equals(ob));
        }
    }
}
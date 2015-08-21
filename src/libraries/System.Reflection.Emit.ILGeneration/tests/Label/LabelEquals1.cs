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
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestModuleName = "TestModuleName";
        private const string TestTypeName = "TestTypeName";
        private const string TestMethodName = "TestMethodName";
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

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
            Assert.True(label.Equals(ob));
        }

        [Fact]
        public void PosTest2()
        {
            AssemblyBuilder assemblyBuilder = this.CreateDynamicAssembly(TestDynamicAssemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestModuleName);
            TypeBuilder typeBuilder = moduleBuilder.DefineType(TestTypeName);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(TestMethodName, MethodAttributes.Public);
            ILGenerator iLGenerator = methodBuilder.GetILGenerator();
            Label label1 = iLGenerator.DefineLabel();
            object ob = iLGenerator.DefineLabel();
            Assert.False(label1.Equals(ob));
        }

        [Fact]
        public void PosTest3()
        {
            object ob = _generator.GetInt32();
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

        [Fact]
        public void TestLabelEqualityOperators()
        {
            Label lb1 = new Label();
            Label lb2 = new Label();

            Assert.True(lb1 == lb2);
            Assert.False(lb1 != lb2);
        }
    }
}

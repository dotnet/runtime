// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class LabelGetHashCode
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestModuleName = "TestModuleName";
        private const string TestTypeName = "TestTypeName";
        private const string TestMethodName = "TestMethodName";

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
            Label label1 = new Label();
            Label label2 = new Label();
            int la1hash = label1.GetHashCode();
            int la2hash = label2.GetHashCode();
            Assert.Equal(la1hash, 0);

            Assert.Equal(la1hash, la2hash);
        }

        [Fact]
        public void PosTest2()
        {
            AssemblyBuilder assemblyBuilder = this.CreateDynamicAssembly(TestDynamicAssemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(assemblyBuilder, TestModuleName);
            TypeBuilder typeBuilder = moduleBuilder.DefineType(TestTypeName);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(TestMethodName, MethodAttributes.Public);
            ILGenerator iLGenerator = methodBuilder.GetILGenerator();
            for (int i = 0; i < 1000; i++)
            {
                Label label = iLGenerator.DefineLabel();
                int hash = label.GetHashCode();

                Assert.Equal(hash, i);
            }
        }
    }
}

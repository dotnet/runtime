// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class LocalBuilderIsPinned
    {
        private const string TestDynamicAssemblyName = "TestDynamicAssembly";
        private const string TestModuleName = "TestModuleName";
        private const string TestTypeName = "TestTypeName";
        private const string TestMethodName = "TestMethodName";

        private TypeBuilder GetCustomType(string name, AssemblyBuilderAccess access)
        {
            AssemblyName myAsmName = new AssemblyName();
            myAsmName.Name = name;
            AssemblyBuilder myAsmBuilder = AssemblyBuilder.DefineDynamicAssembly(myAsmName, access);
            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAsmBuilder, TestModuleName);
            return moduleBuilder.DefineType(TestTypeName);
        }

        [Fact]
        public void PosTest1()
        {
            TypeBuilder typeBuilder = this.GetCustomType(TestDynamicAssemblyName, AssemblyBuilderAccess.Run);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(TestMethodName, MethodAttributes.Public);
            ILGenerator iLGenerator = methodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ret);
            LocalBuilder localBuilder = iLGenerator.DeclareLocal(typeof(string), true);
            Assert.True(localBuilder.IsPinned);
        }

        [Fact]
        public void PosTest2()
        {
            TypeBuilder typeBuilder = this.GetCustomType(TestDynamicAssemblyName, AssemblyBuilderAccess.Run);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(TestMethodName, MethodAttributes.Public);
            ILGenerator iLGenerator = methodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ret);
            LocalBuilder localBuilder = iLGenerator.DeclareLocal(typeof(string), false);
            Assert.False(localBuilder.IsPinned);
        }
    }
}

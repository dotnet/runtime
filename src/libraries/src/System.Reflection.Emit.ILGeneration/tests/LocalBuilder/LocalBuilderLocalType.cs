// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class LocalBuilderLocalType
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
            LocalBuilder localBuilder = iLGenerator.DeclareLocal(typeof(string));
            Type type = localBuilder.LocalType;
            Assert.Equal(type, typeof(string));
        }

        [Fact]
        public void PosTest2()
        {
            TypeBuilder typeBuilder = this.GetCustomType(TestDynamicAssemblyName, AssemblyBuilderAccess.Run);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(TestMethodName, MethodAttributes.Public);
            ILGenerator iLGenerator = methodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ret);
            LocalBuilder localBuilder = iLGenerator.DeclareLocal(typeof(int));
            Type type = localBuilder.LocalType;
            Assert.Equal(type, typeof(int));
        }

        [Fact]
        public void PosTest3()
        {
            TypeBuilder typeBuilder = this.GetCustomType(TestDynamicAssemblyName, AssemblyBuilderAccess.Run);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(TestMethodName, MethodAttributes.Public);
            ILGenerator iLGenerator = methodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ret);
            LocalBuilder localBuilder = iLGenerator.DeclareLocal(typeof(MyClassLocalType));
            Type type = localBuilder.LocalType;
            Assert.Equal(type, typeof(MyClassLocalType));
        }
    }

    public class MyClassLocalType
    {
    }
}

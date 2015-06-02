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
        private const string c_TEST_DYNAMIC_ASSEMBLY_NAME = "TestDynamicAssembly";
        private const string c_TEST_MODULE_NAME = "TestModuleName";
        private const string c_TEST_TYPE_NAME = "TestTypeName";
        private const string c_TEST_METHOD_NAME = "TestMethodName";

        private TypeBuilder GetCustomType(string name, AssemblyBuilderAccess access)
        {
            AssemblyName myAsmName = new AssemblyName();
            myAsmName.Name = name;
            AssemblyBuilder myAsmBuilder = AssemblyBuilder.DefineDynamicAssembly(myAsmName, access);
            ModuleBuilder moduleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAsmBuilder, c_TEST_MODULE_NAME);
            return moduleBuilder.DefineType(c_TEST_TYPE_NAME);
        }

        [Fact]
        public void PosTest1()
        {
            TypeBuilder typeBuilder = this.GetCustomType(c_TEST_DYNAMIC_ASSEMBLY_NAME, AssemblyBuilderAccess.Run);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(c_TEST_METHOD_NAME, MethodAttributes.Public);
            ILGenerator iLGenerator = methodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ret);
            LocalBuilder localBuilder = iLGenerator.DeclareLocal(typeof(string));
            Type type = localBuilder.LocalType;
            Assert.Equal(type, typeof(string));
        }

        [Fact]
        public void PosTest2()
        {
            TypeBuilder typeBuilder = this.GetCustomType(c_TEST_DYNAMIC_ASSEMBLY_NAME, AssemblyBuilderAccess.Run);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(c_TEST_METHOD_NAME, MethodAttributes.Public);
            ILGenerator iLGenerator = methodBuilder.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ret);
            LocalBuilder localBuilder = iLGenerator.DeclareLocal(typeof(int));
            Type type = localBuilder.LocalType;
            Assert.Equal(type, typeof(int));
        }

        [Fact]
        public void PosTest3()
        {
            TypeBuilder typeBuilder = this.GetCustomType(c_TEST_DYNAMIC_ASSEMBLY_NAME, AssemblyBuilderAccess.Run);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(c_TEST_METHOD_NAME, MethodAttributes.Public);
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
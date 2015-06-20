// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderMakePointerType
    {
        [Fact]
        public void PosTest1()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            Type pointerType = testTyBuilder.MakePointerType();
            Assert.False(pointerType.GetTypeInfo().BaseType != typeof(Array) || pointerType.Name != "testType*");
        }

        [Fact]
        public void PosTest2()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType", TypeAttributes.Public | TypeAttributes.Abstract);
            Type pointerType = testTyBuilder.MakePointerType();
            Assert.False(pointerType.GetTypeInfo().BaseType != typeof(Array) || pointerType.Name != "testType*");
        }

        private ModuleBuilder CreateModuleBuilder()
        {
            AssemblyName assemblyName = new AssemblyName();
            assemblyName.Name = "myAssembly.dll";
            AssemblyBuilder myAssemBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssemBuilder, "Module1");
            return myModBuilder;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderMakeArrayType1
    {
        [Fact]
        public void TestForAbstractType()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType", TypeAttributes.Public | TypeAttributes.Abstract);
            Type arrayType = testTyBuilder.MakeArrayType();
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("testType[]", arrayType.Name);
        }

        [Fact]
        public void TestForInstanceType()
        {
            ModuleBuilder testModBuilder = CreateModuleBuilder();
            TypeBuilder testTyBuilder = testModBuilder.DefineType("testType");
            Type arrayType = testTyBuilder.MakeArrayType();
            Assert.Equal(typeof(Array), arrayType.GetTypeInfo().BaseType);
            Assert.Equal("testType[]", arrayType.Name);
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

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineType1
    {
        [Fact]
        public void PosTest1()
        {
            string typeName = "testType";
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateTypeBuilder();
            testTypeBuilder = testModuleBuilder.DefineType(typeName);
            Type tpA = testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Equal(typeName, tpA.Name);
        }

        [Fact]
        public void NegTest1()
        {
            string typeName = null;
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateTypeBuilder();
            Assert.Throws<ArgumentNullException>(() => { testTypeBuilder = testModuleBuilder.DefineType(typeName); });
        }

        [Fact]
        public void NegTest2()
        {
            string typeName = "testType";
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateTypeBuilder();
            testTypeBuilder = testModuleBuilder.DefineType(typeName);
            Type tpA = testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<ArgumentException>(() => { TypeBuilder testTypeBuilder2 = testModuleBuilder.DefineType(typeName); });
        }

        private ModuleBuilder CreateTypeBuilder()
        {
            AssemblyName assemName = new AssemblyName();
            assemName.Name = "testAssembly.exe";
            AssemblyBuilder myAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssemblyBuilder, "Module1");

            return myModuleBuilder;
        }
    }
}

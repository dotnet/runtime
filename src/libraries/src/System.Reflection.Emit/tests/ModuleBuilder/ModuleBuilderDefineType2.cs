// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineType2
    {
        [Fact]
        public void TestWithNonPublicAttributeInModule()
        {
            string typeName = "testType";
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateModuleBuilder();
            testTypeBuilder = testModuleBuilder.DefineType(typeName, TypeAttributes.NotPublic);

            Type tpA = testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Equal(typeName, tpA.Name);
            Assert.Equal(TypeAttributes.NotPublic, tpA.GetTypeInfo().Attributes);
        }

        [Fact]
        public void TestWithInterfaceAttributeInModule()
        {
            string typeName = "testType";
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateModuleBuilder();
            TypeAttributes typeAttr = TypeAttributes.Interface;
            testTypeBuilder = testModuleBuilder.DefineType(typeName, typeAttr | TypeAttributes.Abstract);
            Type tpA = testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Equal(typeName, tpA.Name);
            Assert.Equal((TypeAttributes.Interface | TypeAttributes.Abstract), tpA.GetTypeInfo().Attributes);
        }

        [Fact]
        public void TestWithClassAttributeInModule()
        {
            string typeName = "testType";
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateModuleBuilder();
            testTypeBuilder = testModuleBuilder.DefineType(typeName, TypeAttributes.Class);
            Type tpA = testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Equal(typeName, tpA.Name);
            Assert.Equal(TypeAttributes.Class, tpA.GetTypeInfo().Attributes);
        }

        [Fact]
        public void TestWithNullName()
        {
            string typeName = null;
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateModuleBuilder();
            Assert.Throws<ArgumentNullException>(() => { testTypeBuilder = testModuleBuilder.DefineType(typeName, TypeAttributes.NotPublic); });
        }

        [Fact]
        public void TestWithSameNameTypeAlreadyExists()
        {
            string typeName = "testType";
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateModuleBuilder();
            testTypeBuilder = testModuleBuilder.DefineType(typeName);
            Type tpA = testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<ArgumentException>(() => { TypeBuilder testTypeBuilder2 = testModuleBuilder.DefineType(typeName, TypeAttributes.NotPublic); });
        }

        private ModuleBuilder CreateModuleBuilder()
        {
            AssemblyName assemName = new AssemblyName();
            assemName.Name = "testAssembly.dll";
            AssemblyBuilder myAssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssemblyBuilder, "Module1");
            return myModuleBuilder;
        }
    }
}

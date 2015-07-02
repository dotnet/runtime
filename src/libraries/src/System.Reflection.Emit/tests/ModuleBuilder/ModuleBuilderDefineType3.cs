// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ModuleBuilderDefineType3
    {
        [Fact]
        public void TestTypeWithNonPublicAttributeAndBaseTypeClassInModule()
        {
            string typeName = "testType";
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateModuleBuilder();
            Type parent = typeof(MBTestClass);
            testTypeBuilder = testModuleBuilder.DefineType(typeName, TypeAttributes.NotPublic, parent);
            Type tpA = testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Equal(typeName, tpA.Name);
            Assert.Equal(TypeAttributes.NotPublic, tpA.GetTypeInfo().Attributes);
            Assert.Equal(parent, tpA.GetTypeInfo().BaseType);
        }

        [Fact]
        public void TestTypeWithClassAttributeAndBaseTypeClassInModule()
        {
            string typeName = "testType";
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateModuleBuilder();
            Type parent = typeof(MBTestClass);
            testTypeBuilder = testModuleBuilder.DefineType(typeName, TypeAttributes.Class, parent);
            Type tpA = testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Equal(typeName, tpA.Name);
            Assert.Equal(TypeAttributes.Class, tpA.GetTypeInfo().Attributes);
            Assert.Equal(parent, tpA.GetTypeInfo().BaseType);
        }

        [Fact]
        public void TestTypeWithNonPublicAttributeAndBaseTypeCreateTypeInModule()
        {
            string typeName = "testType";
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateModuleBuilder();
            TypeBuilder myTypeBuilder = testModuleBuilder.DefineType("myType");
            Type parent = myTypeBuilder.CreateTypeInfo().AsType();
            testTypeBuilder = testModuleBuilder.DefineType(typeName, TypeAttributes.NotPublic, parent);
            Type tpA = testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Equal(typeName, tpA.Name);
            Assert.Equal(TypeAttributes.NotPublic, tpA.GetTypeInfo().Attributes);
            Assert.Equal(parent, tpA.GetTypeInfo().BaseType);
        }

        [Fact]
        public void NegTest1()
        {
            string typeName = null;
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateModuleBuilder();
            Assert.Throws<ArgumentNullException>(() => { testTypeBuilder = testModuleBuilder.DefineType(typeName, TypeAttributes.NotPublic, typeof(MBTestClass)); });
        }

        [Fact]
        public void NegTest2()
        {
            string typeName = "testType";
            ModuleBuilder testModuleBuilder;
            TypeBuilder testTypeBuilder;
            testModuleBuilder = CreateModuleBuilder();
            testTypeBuilder = testModuleBuilder.DefineType(typeName);
            Type tpA = testTypeBuilder.CreateTypeInfo().AsType();
            Assert.Throws<ArgumentException>(() => { TypeBuilder testTypeBuilder2 = testModuleBuilder.DefineType(typeName, TypeAttributes.NotPublic, typeof(MBTestClass)); });
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

    public class MBTestClass { }
    public interface MBTestInterface { }
}

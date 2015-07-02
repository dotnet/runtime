// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderSetParent
    {
        public const string ModuleName = "ModuleName";
        public const string TypeName = "TypeName";


        private TypeBuilder GetTypeBuilder()
        {
            AssemblyName assemblyname = new AssemblyName("assemblyname");

            AssemblyBuilder assemblybuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.Run);
            ModuleBuilder modulebuilder = TestLibrary.Utilities.GetModuleBuilder(assemblybuilder, "Module1");
            return modulebuilder.DefineType(TypeName);
        }

        [Fact]
        public void TestWithStringParent()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            typebuilder.SetParent(typeof(string));
        }

        [Fact]
        public void TestWithCustomClassParent()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            typebuilder.SetParent(typeof(TBSetParentTestClass));
        }

        [Fact]
        public void TestWithCustomGenericClassParent()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            typebuilder.SetParent(typeof(TBSetParentGenClass<>));
        }

        [Fact]
        public void TestWithNullableValueTypeParent()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            typebuilder.SetParent(typeof(int?));
        }

        [Fact]
        public void TestWithNullParent()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            typebuilder.SetParent(null);
        }

        [Fact]
        public void TestThrowsExceptionForCreateTypeCalled()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            typebuilder.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => { typebuilder.SetParent(typeof(string)); });
        }

        [Fact]
        public void TestThrowsExceptionForNullParentAndInstanceIsInterfaceWithoutAbstractAttribute()
        {
            AssemblyName assemblyname = new AssemblyName("assemblyname");

            AssemblyBuilder assemblybuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.Run);
            ModuleBuilder modulebuilder = TestLibrary.Utilities.GetModuleBuilder(assemblybuilder, "Module1");

            Assert.Throws<InvalidOperationException>(() =>
            {
                TypeBuilder typebuilder = modulebuilder.DefineType(TypeName, TypeAttributes.Interface);
                typebuilder.SetParent(null);
            });
        }
    }

    public class TBSetParentTestClass
    {
    }
    public class TBSetParentGenClass<T>
    {
    }
}

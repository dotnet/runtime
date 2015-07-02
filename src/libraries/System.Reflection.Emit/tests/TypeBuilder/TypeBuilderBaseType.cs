// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderBaseType
    {
        public const string ModuleName = "ModuleName";
        public const string TypeName = "TypeName";


        private TypeBuilder GetTypeBuilder()
        {
            AssemblyName assemblyname = new AssemblyName("assemblyname");

            AssemblyBuilder assemblybuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.Run);
            ModuleBuilder modulebuilder = TestLibrary.Utilities.GetModuleBuilder(assemblybuilder, ModuleName);
            return modulebuilder.DefineType(TypeName);
        }

        [Fact]
        public void TestWithInt32()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            typebuilder.SetParent(typeof(int));
            Type type = typebuilder.BaseType;
            Assert.Equal(typeof(int), type);
        }

        [Fact]
        public void TestWithCustomClass()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            typebuilder.SetParent(typeof(TBTestClass1));
            Type type = typebuilder.BaseType;
            Assert.Equal(typeof(TBTestClass1), type);
        }

        [Fact]
        public void TestWithGenericClass()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            typebuilder.SetParent(typeof(TBGenClass1<>));
            Type type = typebuilder.BaseType;
            Assert.Equal(typeof(TBGenClass1<>), type);
        }

        [Fact]
        public void TestWithNullableType()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            typebuilder.SetParent(typeof(int?));
            Type type = typebuilder.BaseType;
            Assert.Equal(typeof(int?), type);
        }

        [Fact]
        public void TestWithInterfaceType()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            Assert.Throws<ArgumentException>(() => { typebuilder.SetParent(typeof(TBInterfaceClass1)); });
        }

        [Fact]
        public void TestWithDefaultType()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            Type type = typebuilder.BaseType;
            Assert.Equal(typeof(object), type);
        }
    }

    public class TBTestClass1
    {
    }
    public class TBGenClass1<T>
    {
    }
    public interface TBInterfaceClass1
    {
    }
}

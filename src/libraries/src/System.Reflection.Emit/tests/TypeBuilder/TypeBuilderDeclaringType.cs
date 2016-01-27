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
    public class TypeBuilderDeclaringType
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
        public void TestOnRootType()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            Type type = typebuilder.DeclaringType;
            Assert.Null(type);
        }

        [Fact]
        public void TestOnNestedType()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            TypeBuilder typebuilder2 = typebuilder.DefineNestedType("typename");
            Type type = typebuilder2.DeclaringType;
            Assert.NotNull(type);
            Assert.Equal("TypeName", type.Name);
        }
    }
}

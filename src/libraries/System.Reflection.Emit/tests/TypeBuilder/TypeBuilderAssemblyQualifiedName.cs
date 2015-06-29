// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderAssemblyQualifiedName
    {
        public const string ModuleName = "ModuleName";
        public const string TypeName = "TypeName";

        private TypeBuilder GetTypeBuilder(string ass_name, string type_name)
        {
            AssemblyName assemblyname = new AssemblyName(ass_name);

            AssemblyBuilder assemblybuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.Run);
            ModuleBuilder modulebuilder = TestLibrary.Utilities.GetModuleBuilder(assemblybuilder, ModuleName);
            return modulebuilder.DefineType(type_name);
        }

        [Fact]
        public void PosTest1()
        {
            string assename = "assemblyname";
            TypeBuilder typebuilder = GetTypeBuilder(assename, TypeName);
            string assembly = typebuilder.AssemblyQualifiedName;
            Assert.NotNull(assembly);
        }

        [Fact]
        public void PosTest2()
        {
            string assename = "ad df df";
            TypeBuilder typebuilder = GetTypeBuilder(assename, TypeName);
            string assembly = typebuilder.AssemblyQualifiedName;
            Assert.NotNull(assembly);
        }

        [Fact]
        public void PosTest3()
        {
            string assename = "assebly name  ";
            string typename = "type name  ";
            TypeBuilder typebuilder = GetTypeBuilder(assename, typename);
            string assembly = typebuilder.AssemblyQualifiedName;
            Assert.NotNull(assembly);
        }

        [Fact]
        public void PosTest4()
        {
            string assename = "assebly name  ";
            string typename = "-";
            TypeBuilder typebuilder = GetTypeBuilder(assename, typename);
            string assembly = typebuilder.AssemblyQualifiedName;
            Assert.NotNull(assembly);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGUID
    {
        public const string ModuleName = "ModuleName";
        public const string TypeName = "TypeName";
        private TypeBuilder GetTypeBuilder(string typename)
        {
            AssemblyName assemblyname = new AssemblyName("assemblyname");

            AssemblyBuilder assemblybuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.Run);
            ModuleBuilder modulebuilder = TestLibrary.Utilities.GetModuleBuilder(assemblybuilder, ModuleName);
            return modulebuilder.DefineType(typename);
        }

        [Fact]
        public void PosTest1()
        {
            TypeBuilder typebuilder = GetTypeBuilder(TypeName);
            typebuilder.CreateTypeInfo().AsType();
            Guid guid = typebuilder.GUID;
        }

        [Fact]
        public void NegTest1()
        {
            TypeBuilder typebuilder = GetTypeBuilder(TypeName);
            Assert.Throws<NotSupportedException>(() => { Guid guid = typebuilder.GUID; });
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderAssembly
    {
        public const string ModuleName = "ModuleName";
        public const string TypeName = "TypeName";

        private TypeBuilder GetTypeBuilder(string asmName, string type_name)
        {
            AssemblyName assemblyname = new AssemblyName(asmName);
            AssemblyBuilder assemblybuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.Run);
            ModuleBuilder modulebuilder = TestLibrary.Utilities.GetModuleBuilder(assemblybuilder, ModuleName);
            return modulebuilder.DefineType(type_name);
        }

        [Fact]
        public void TestForAssemblyProperty()
        {
            string asmName = "assemblyname";
            TypeBuilder typebuilder = GetTypeBuilder(asmName, TypeName);
            Assembly assembly = typebuilder.Assembly;
            Assert.NotNull(assembly);
            Assert.False(assembly.GetName().Name != asmName);
        }
    }
}

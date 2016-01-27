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

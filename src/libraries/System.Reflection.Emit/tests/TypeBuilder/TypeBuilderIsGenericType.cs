// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderIsGenericType
    {
        public const string ModuleName = "ModuleName";
        public const string TypeName = "TypeName";

        private TypeBuilder GetTypeBuilder(string typename)
        {
            AssemblyName assemblyname = new AssemblyName("assemblyname");

            AssemblyBuilder assemblybuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyname, AssemblyBuilderAccess.Run);
            ModuleBuilder modulebuilder = TestLibrary.Utilities.GetModuleBuilder(assemblybuilder, "Module1");
            return modulebuilder.DefineType(typename);
        }

        [Fact]
        public void PosTest1()
        {
            TypeBuilder typebuilder = GetTypeBuilder(TypeName);
            string[] argu = { "argu0", "argu1", "argu2" };
            GenericTypeParameterBuilder[] gtpb = typebuilder.DefineGenericParameters(argu);
            bool boolvalue = typebuilder.IsGenericType;
            Assert.True(boolvalue);
        }

        [Fact]
        public void PosTest2()
        {
            TypeBuilder typebuilder = GetTypeBuilder(TypeName);
            bool boolvalue = typebuilder.IsGenericType;
            Assert.False(boolvalue);
        }
    }
}

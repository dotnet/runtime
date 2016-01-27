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
    public class TypeBuilderGenericParameterPosition
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
        public void TestGenericParameterPosition()
        {
            TypeBuilder typebuilder = GetTypeBuilder(TypeName);
            string[] argu = { "argu0", "argu1" };
            GenericTypeParameterBuilder[] gtpb = typebuilder.DefineGenericParameters(argu);
            TypeInfo typebuilder2 = gtpb[1].DeclaringType.GetTypeInfo();
            int position = typebuilder2.GenericParameterPosition;
            Assert.Equal(0, position);
        }
    }
}

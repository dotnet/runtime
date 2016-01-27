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
    public class TypeBuilderDeclaringMethod
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
        public void TestOnGenericTypeParameter()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            string[] argu = { "argu0", "argu1", "argu2" };
            MethodBuilder methodbuilder = typebuilder.DefineMethod("method", MethodAttributes.Public);
            GenericTypeParameterBuilder[] gt = methodbuilder.DefineGenericParameters(argu);
            for (int i = 0; i < 3; i++)
            {
                MethodBase mb = gt[i].DeclaringMethod;
                Assert.NotNull(mb);
            }
        }

        [Fact]
        public void TestOnTypeBuilder()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            string[] argu = { "argu0", "argu1" };
            typebuilder.DefineGenericParameters(argu);
            MethodBase mb = typebuilder.DeclaringMethod;
            Assert.Null(mb);
        }
    }
}

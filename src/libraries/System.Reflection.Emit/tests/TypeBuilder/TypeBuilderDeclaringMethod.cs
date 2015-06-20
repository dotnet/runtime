// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public void PosTest1()
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
        public void PosTest2()
        {
            TypeBuilder typebuilder = GetTypeBuilder();
            string[] argu = { "argu0", "argu1" };
            typebuilder.DefineGenericParameters(argu);
            MethodBase mb = typebuilder.DeclaringMethod;
            Assert.Null(mb);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderGenericParameterAttributes
    {
        public const string ModuleName = "ModuleName";
        public const string TypeName = "TypeName";
        public const string Method = "Method";

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
            string[] argu = { "argu0" };
            GenericTypeParameterBuilder[] gtpb = typebuilder.DefineGenericParameters(argu);
            gtpb[0].SetGenericParameterAttributes(GenericParameterAttributes.SpecialConstraintMask);
            GenericParameterAttributes gpa = gtpb[0].DeclaringType.GetTypeInfo().GenericParameterAttributes;
            Assert.Equal(GenericParameterAttributes.None, gpa);
        }
    }
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderToString
    {
        [Fact]
        public void PosTest1()
        {
            AssemblyName an = new AssemblyName();
            an.Name = "DynamicRandomAssembly";
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            ModuleBuilder mb = TestLibrary.Utilities.GetModuleBuilder(ab, "Module1");
            TypeBuilder tb = mb.DefineType("DynamicRandomClass", TypeAttributes.Public);

            Type[] parameterTypes = { typeof(int), typeof(double) };

            System.Reflection.Emit.ConstructorBuilder cb =
                tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes, null, null);

            Assert.StartsWith("Name: .ctor", cb.ToString());
        }

        [Fact]
        public void PosTest2()
        {
            AssemblyName an = new AssemblyName();
            an.Name = "DynamicRandomAssembly";
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            ModuleBuilder mb = TestLibrary.Utilities.GetModuleBuilder(ab, "Module1");
            TypeBuilder tb = mb.DefineType("DynamicRandomClass", TypeAttributes.Public);

            Type[] parameterTypes = { typeof(int), typeof(double) };

            System.Reflection.Emit.ConstructorBuilder cb =
                tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);

            Assert.StartsWith("Name: .ctor", cb.ToString());
        }
    }
}

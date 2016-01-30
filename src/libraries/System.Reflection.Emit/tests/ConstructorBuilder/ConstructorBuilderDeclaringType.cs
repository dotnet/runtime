// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderDeclaringType
    {
        [Fact]
        public void TestDeclaringType()
        {
            AssemblyName an = new AssemblyName();
            an.Name = "DynamicRandomAssembly";
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            ModuleBuilder mb = TestLibrary.Utilities.GetModuleBuilder(ab, "Module1");


            TypeBuilder tb = mb.DefineType("DynamicRandomClass", TypeAttributes.Public);

            Type[] parameterTypes = { typeof(int), typeof(double) };

            ConstructorBuilder cb = tb.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                parameterTypes,
                null,
                null);

            Assert.Equal(tb.AsType(), cb.DeclaringType);
        }

        [Fact]
        public void TestDeclaringTypeWithDifferentOverload()
        {
            AssemblyName an = new AssemblyName();
            an.Name = "DynamicRandomAssembly";
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            ModuleBuilder mb = TestLibrary.Utilities.GetModuleBuilder(ab, "Module1");
            TypeBuilder tb = mb.DefineType("DynamicRandomClass", TypeAttributes.Public);

            Type[] parameterTypes = { typeof(int), typeof(double) };

            ConstructorBuilder cb =
                tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, parameterTypes);

            Assert.Equal(tb.AsType(), cb.DeclaringType);
        }
    }
}

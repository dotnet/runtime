// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class ConstructorBuilderCallingConvention
    {
        private static CallingConventions[] s_supportedCConvs = new CallingConventions[] { CallingConventions.Any,
        CallingConventions.ExplicitThis, CallingConventions.HasThis, CallingConventions.Standard,
        CallingConventions.VarArgs };

        [Fact]
        public void TestCallingConventions()
        {
            AssemblyName an = new AssemblyName();
            an.Name = "DynamicRandomAssembly";
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            ModuleBuilder mb = TestLibrary.Utilities.GetModuleBuilder(ab, "Module1");
            for (int i = 0; i < s_supportedCConvs.Length; i++)
            {
                TypeBuilder tb = mb.DefineType("DynamicRandomClass" + i.ToString(), TypeAttributes.Public);

                Type[] parameterTypes = { typeof(int), typeof(double) };

                ConstructorBuilder cb =
                    tb.DefineConstructor(MethodAttributes.Public, s_supportedCConvs[i], parameterTypes, null, null);

                Assert.Equal(CallingConventions.Standard, cb.CallingConvention);
            }
        }

        [Fact]
        public void TestCallingConventionWithDifferentOverload()
        {
            AssemblyName an = new AssemblyName();
            an.Name = "DynamicRandomAssembly";
            AssemblyBuilder ab = AssemblyBuilder.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);

            ModuleBuilder mb = TestLibrary.Utilities.GetModuleBuilder(ab, "Module1");

            for (int i = 0; i < s_supportedCConvs.Length; i++)
            {
                TypeBuilder tb = mb.DefineType("DynamicRandomClass" + i.ToString(), TypeAttributes.Public);
                tb.DefineGenericParameters("T");
                Type[] parameterTypes = { typeof(int), typeof(double) };

                ConstructorBuilder cb =
                    tb.DefineConstructor(MethodAttributes.Public, s_supportedCConvs[i], parameterTypes);

                Assert.Equal(CallingConventions.HasThis, cb.CallingConvention);
            }
        }
    }
}

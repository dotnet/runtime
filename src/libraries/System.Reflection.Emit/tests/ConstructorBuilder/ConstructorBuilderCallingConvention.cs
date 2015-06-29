// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        public void PosTest1()
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
        public void PosTest2()
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

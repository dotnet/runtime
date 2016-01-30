// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Reflection.Emit;

namespace System.Reflection.Emit.ILGeneration.Tests
{
    public class TestSetConstant
    {
        [Fact]
        public void TestNullableValueTypes()
        {
            AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("TestAssembly_SetConstant"), AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("ModuleName");
            TypeBuilder typeBuilder = moduleBuilder.DefineType("TypeName", TypeAttributes.Public);
            MethodBuilder methodBuilder = typeBuilder.DefineMethod("TestMethod", MethodAttributes.Public, typeof(void), new Type[] { typeof(int?) });
            ILGenerator generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ret);
            ParameterBuilder pBuilder = methodBuilder.DefineParameter(1, ParameterAttributes.HasDefault, "paramName");
            // should not throw
            pBuilder.SetConstant(null);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class GenericTypeParameterBuilderIsGenericTypeDefinition
    {
        [Fact]
        public void TestIsGenericTypeDefinition()
        {
            AssemblyName myAsmName = new AssemblyName("GenericEmitExample1");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAsmName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModule = TestLibrary.Utilities.GetModuleBuilder(myAssembly, myAsmName.Name);

            Type baseType = typeof(ExampleBase);

            TypeBuilder myType = myModule.DefineType("Sample", TypeAttributes.Public);

            string[] typeParamNames = { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = myType.DefineGenericParameters(typeParamNames);

            GenericTypeParameterBuilder TFirst = typeParams[0];


            bool expectedValue = false;
            bool actualValue;

            actualValue = TFirst.IsGenericTypeDefinition;

            Assert.Equal(expectedValue, actualValue);
        }
    }
}

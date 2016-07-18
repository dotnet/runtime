// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class GenericTypeParameterBuilderAssembly
    {
        [Fact]
        public void Assembly()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ModuleBuilder module = assembly.DefineDynamicModule("Module");            
            TypeBuilder type = module.DefineType("Sample", TypeAttributes.Public);

            string[] typeParamNames = new string[] { "TFirst" };
            GenericTypeParameterBuilder[] typeParams = type.DefineGenericParameters(typeParamNames);
            Assert.Equal(assembly, typeParams[0].Assembly);
        }
    }
}

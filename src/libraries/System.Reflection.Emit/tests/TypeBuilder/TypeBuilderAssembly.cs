// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderAssembly
    {
        [Fact]
        public void Assembly()
        {
            AssemblyBuilder assembly = Helpers.DynamicAssembly();
            ModuleBuilder module = assembly.DefineDynamicModule("TestModule");
            TypeBuilder type = module.DefineType("TestType");

            Assert.Same(assembly, type.Assembly);
        }
    }
}

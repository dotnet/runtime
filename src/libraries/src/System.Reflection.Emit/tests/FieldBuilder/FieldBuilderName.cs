// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderName
    {
        private readonly RandomDataGenerator _generator = new RandomDataGenerator();

        [Fact]
        public void TestNameProperty()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("FieldBuilderName_Assembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderName_Module");
            TypeBuilder type = module.DefineType("FieldBuilderName_Type", TypeAttributes.Abstract);

            string expected = _generator.GetString(false, 1, 30);
            FieldBuilder field = type.DefineField(expected, typeof(object), FieldAttributes.Public);
            string actual = field.Name;

            Assert.Equal(expected, actual);
        }
    }
}

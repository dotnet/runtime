// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderDeclaringType
    {
        [Fact]
        public void TestDeclaringTypeProperty()
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("FieldBuilderDeclaringType_Assembly"), AssemblyBuilderAccess.Run);
            ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderDeclaringType_Module");
            TypeBuilder type = module.DefineType("FieldBuilderDeclaringType_Type", TypeAttributes.Abstract);

            VerificationHelper(type.DefineField("Field_PosTest1_1", typeof(object), FieldAttributes.Public), type.AsType());
            VerificationHelper(type.DefineField("Field_PosTest1_2", typeof(int), FieldAttributes.Public), type.AsType());
            VerificationHelper(type.DefineField("Field_PosTest1_3", typeof(string), FieldAttributes.Public), type.AsType());
            VerificationHelper(type.DefineField("Field_PosTest1_4", typeof(FieldBuilderDeclaringType), FieldAttributes.Public), type.AsType());
            VerificationHelper(type.DefineField("Field_PosTest1_5", type.AsType(), FieldAttributes.Public), type.AsType());

            // Create a new type and try again
            type = module.DefineType("FieldBuilderDeclaringType_Type1", TypeAttributes.Abstract);
            VerificationHelper(type.DefineField("Field_PosTest1_6", typeof(object), FieldAttributes.Public), type.AsType());
            VerificationHelper(type.DefineField("Field_PosTest1_7", typeof(int), FieldAttributes.Public), type.AsType());
            VerificationHelper(type.DefineField("Field_PosTest1_8", typeof(string), FieldAttributes.Public), type.AsType());
            VerificationHelper(type.DefineField("Field_PosTest1_9", typeof(FieldBuilderDeclaringType), FieldAttributes.Public), type.AsType());
            VerificationHelper(type.DefineField("Field_PosTest1_10", type.AsType(), FieldAttributes.Public), type.AsType());
        }

        private void VerificationHelper(FieldBuilder field, Type expected)
        {
            Type actual = field.DeclaringType;

            Assert.Equal(expected, actual);
        }
    }
}

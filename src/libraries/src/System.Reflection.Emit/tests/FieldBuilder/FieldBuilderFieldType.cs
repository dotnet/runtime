// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderFieldType
    {
        private TypeBuilder TypeBuilder
        {
            get
            {
                if (null == _typeBuilder)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("FieldBuilderDeclaringType_Assembly"), AssemblyBuilderAccess.Run);
                    ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderDeclaringType_Module");
                    _typeBuilder = module.DefineType("FieldBuilderDeclaringType_Type", TypeAttributes.Abstract);
                }

                return _typeBuilder;
            }
        }

        private TypeBuilder _typeBuilder;

        [Fact]
        public void TestFieldTypeProperty()
        {
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_1", typeof(object), FieldAttributes.Public), typeof(object));
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_2", typeof(int), FieldAttributes.Public), typeof(int));
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_3", typeof(string), FieldAttributes.Public), typeof(string));
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_4", typeof(FieldBuilderFieldType), FieldAttributes.Public), typeof(FieldBuilderFieldType));
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_5", TypeBuilder.AsType(), FieldAttributes.Public), TypeBuilder.AsType());
        }

        private void VerificationHelper(FieldBuilder field, Type expected)
        {
            Type actual = field.FieldType;
            Assert.Equal(expected, actual);
        }
    }
}

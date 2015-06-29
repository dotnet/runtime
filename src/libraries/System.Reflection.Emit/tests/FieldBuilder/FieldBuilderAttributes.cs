// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class FieldBuilderAttributes
    {
        private TypeBuilder TypeBuilder
        {
            get
            {
                if (null == _typeBuilder)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("FieldBuilderAttributes_Assembly"), AssemblyBuilderAccess.Run);
                    ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderAttributes_Module");
                    _typeBuilder = module.DefineType("FieldBuilderAttributes_Type", TypeAttributes.Abstract);
                }

                return _typeBuilder;
            }
        }

        private TypeBuilder _typeBuilder;

        [Fact]
        public void PosTest1()
        {
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_1", typeof(object), FieldAttributes.Assembly), FieldAttributes.Assembly);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_2", typeof(object), FieldAttributes.FamANDAssem), FieldAttributes.FamANDAssem);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_3", typeof(object), FieldAttributes.Family), FieldAttributes.Family);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_4", typeof(object), FieldAttributes.FamORAssem), FieldAttributes.FamORAssem);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_5", typeof(object), FieldAttributes.FieldAccessMask), FieldAttributes.FieldAccessMask);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_6", typeof(object), FieldAttributes.HasDefault), FieldAttributes.PrivateScope);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_7", typeof(object), FieldAttributes.HasFieldMarshal), FieldAttributes.PrivateScope);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_8", typeof(object), FieldAttributes.HasFieldRVA), FieldAttributes.PrivateScope);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_9", typeof(object), FieldAttributes.InitOnly), FieldAttributes.InitOnly);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_10", typeof(object), FieldAttributes.Literal), FieldAttributes.Literal);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_11", typeof(object), FieldAttributes.NotSerialized), FieldAttributes.NotSerialized);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_12", typeof(object), FieldAttributes.PinvokeImpl), FieldAttributes.PinvokeImpl);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_13", typeof(object), FieldAttributes.Private), FieldAttributes.Private);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_14", typeof(object), FieldAttributes.PrivateScope), FieldAttributes.PrivateScope);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_15", typeof(object), FieldAttributes.Public), FieldAttributes.Public);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_17", typeof(object), FieldAttributes.RTSpecialName), FieldAttributes.PrivateScope);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_18", typeof(object), FieldAttributes.SpecialName), FieldAttributes.SpecialName);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_19", typeof(object), FieldAttributes.Static), FieldAttributes.Static);
            VerificationHelper(TypeBuilder.DefineField("Field_PosTest1_20", typeof(object), FieldAttributes.Public | FieldAttributes.Static), FieldAttributes.Public | FieldAttributes.Static);
        }


        private void VerificationHelper(FieldBuilder field, FieldAttributes expected)
        {
            FieldAttributes actual = field.Attributes;
            Assert.Equal(expected, actual);
        }
    }
}

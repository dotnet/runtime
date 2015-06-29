// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public enum FBTestEnum
    {
        VALUE_0,
        VALUE_1
    }

    public struct FBTestStruct
    {
        public int m_Int;
        public FBTestEnum m_FBTestEnum;
    }

    public class FieldBuilderSetConstant
    {
        private TypeBuilder TypeBuilder
        {
            get
            {
                if (null == _typeBuilder)
                {
                    AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                        new AssemblyName("FieldBuilderSetConstant_Assembly"), AssemblyBuilderAccess.Run);
                    ModuleBuilder module = TestLibrary.Utilities.GetModuleBuilder(assembly, "FieldBuilderSetConstant_Module");
                    _typeBuilder = module.DefineType("FieldBuilderSetConstant_Type", TypeAttributes.Abstract);
                }

                return _typeBuilder;
            }
        }

        private TypeBuilder _typeBuilder;

        [Fact]
        public void PosTest1()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest1", typeof(Boolean), FieldAttributes.Public);

            // set default value
            field.SetConstant(false);

            // change default value
            field.SetConstant(true);
        }

        [Fact]
        public void PosTest2()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest2", typeof(SByte), FieldAttributes.Public);

            // set default value
            field.SetConstant(SByte.MinValue);

            // change default value
            field.SetConstant(SByte.MaxValue);
        }

        [Fact]
        public void PosTest3()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest3", typeof(Int16), FieldAttributes.Public);

            // set default value
            field.SetConstant(Int16.MaxValue);

            // change default value
            field.SetConstant(Int16.MinValue);
        }

        [Fact]
        public void PosTest4()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest4", typeof(int), FieldAttributes.Public);

            // set default value
            field.SetConstant(int.MinValue);

            // change default value
            field.SetConstant(int.MaxValue);
        }

        [Fact]
        public void PosTest5()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest5", typeof(long), FieldAttributes.Public);

            // set default value
            field.SetConstant(long.MaxValue);

            // change default value
            field.SetConstant(long.MinValue);
        }

        [Fact]
        public void PosTest6()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest6", typeof(byte), FieldAttributes.Public);

            // set default value
            field.SetConstant(byte.MinValue);

            // change default value
            field.SetConstant(byte.MaxValue);
        }

        [Fact]
        public void PosTest7()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest7", typeof(ushort), FieldAttributes.Public);

            // set default value
            field.SetConstant(ushort.MaxValue);

            // change default value
            field.SetConstant(ushort.MinValue);
        }

        [Fact]
        public void PosTest8()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest8", typeof(uint), FieldAttributes.Public);

            // set default value
            field.SetConstant(uint.MaxValue);

            // change default value
            field.SetConstant(uint.MinValue);
        }

        [Fact]
        public void PosTest9()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest9", typeof(ulong), FieldAttributes.Public);

            // set default value
            field.SetConstant(ulong.MaxValue);

            // change default value
            field.SetConstant(ulong.MinValue);
        }

        [Fact]
        public void PosTest10()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest10", typeof(float), FieldAttributes.Public);

            // set default value
            field.SetConstant(float.MaxValue);

            // change default value
            field.SetConstant(float.NaN);
        }

        [Fact]
        public void PosTest11()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest11", typeof(double), FieldAttributes.Public);

            // set default value
            field.SetConstant(double.PositiveInfinity);

            // change default value
            field.SetConstant(double.NegativeInfinity);
        }

        [Fact]
        public void PosTest12()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest12", typeof(DateTime), FieldAttributes.Public);

            // set default value
            field.SetConstant(DateTime.MinValue);

            // change default value
            field.SetConstant(DateTime.MaxValue);
        }

        [Fact]
        public void PosTest13()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest13", typeof(Char), FieldAttributes.Public);

            // set default value
            field.SetConstant(Char.MaxValue);

            // change default value
            field.SetConstant(Char.MinValue);
        }

        [Fact]
        public void PosTest14()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest14", typeof(string), FieldAttributes.Public);

            // set default value
            field.SetConstant(null);

            // change default value
            field.SetConstant(TestLibrary.Generator.GetString(false, 1, 30));
        }

        [Fact]
        public void PosTest15()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest15", typeof(FBTestEnum), FieldAttributes.Public);

            // set default value
            field.SetConstant(FBTestEnum.VALUE_0);

            // change default value
            field.SetConstant(FBTestEnum.VALUE_1);
        }

        [Fact]
        public void PosTest17()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest17", typeof(object), FieldAttributes.Public);

            // set default value
            field.SetConstant(null);
        }

        [Fact]
        public void PosTest18()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest18", typeof(FieldBuilderSetConstant), FieldAttributes.Public);

            // set default value
            field.SetConstant(null);
        }

        [Fact]
        public void NegTest1()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_NegTest1", typeof(int), FieldAttributes.Public);

            TypeBuilder.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => { field.SetConstant(0); });
        }

        [Fact]
        public void NegTest2()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_NegTest2_Boolean", typeof(Boolean), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_int", typeof(int), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_SByte", typeof(SByte), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_Int16", typeof(Int16), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_Int64", typeof(long), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_Byte", typeof(byte), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_UInt16", typeof(ushort), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_UInt32", typeof(uint), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_UInt64", typeof(ulong), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_Single", typeof(float), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_Double", typeof(double), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_DateTime", typeof(DateTime), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_Char", typeof(Char), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_String", typeof(string), FieldAttributes.Public);
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_FBTestEnum", typeof(FBTestEnum), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_Decimal", typeof(Decimal), FieldAttributes.Public);
            VerificationHelper(field, new object(), typeof(ArgumentException));

            field = TypeBuilder.DefineField("Field_NegTest2_Object", typeof(object), FieldAttributes.Public);
            VerificationHelper(field, new object(), typeof(ArgumentException));
        }

        [Fact]
        public void NegTest3()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_NegTest3", typeof(FBTestStruct), FieldAttributes.Public);
            VerificationHelper(field, null, typeof(ArgumentException));
        }

        [Fact]
        public void NegTest4()
        {
            FieldBuilder field = TypeBuilder.DefineField("Field_PosTest16", typeof(Decimal), FieldAttributes.Public);
            VerificationHelper(field, Decimal.One, typeof(ArgumentException));
        }

        private void VerificationHelper(FieldBuilder field, object value, Type expected)
        {
            Assert.Throws(expected, () => { field.SetConstant(value); });
        }
    }
}

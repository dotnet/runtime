// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public enum CustomEnum
    {
        Value0,
        Value1
    }

    public struct FBTestStruct
    {
        public int m_Int;
        public CustomEnum m_FBTestEnum;
    }

    public class FieldBuilderSetConstant
    {
        private static TypeBuilder s_type = Helpers.DynamicType(TypeAttributes.Abstract);

        [Fact]
        public void SetConstant_Bool()
        {
            FieldBuilder field = s_type.DefineField("BoolField", typeof(bool), FieldAttributes.Public);

            field.SetConstant(false);
            field.SetConstant(true);
        }

        [Fact]
        public void SetConstant_SByte()
        {
            FieldBuilder field = s_type.DefineField("SByteField", typeof(sbyte), FieldAttributes.Public);

            field.SetConstant(sbyte.MinValue);
            field.SetConstant(sbyte.MaxValue);
        }

        [Fact]
        public void SetConstant_Short()
        {
            FieldBuilder field = s_type.DefineField("ShortField", typeof(short), FieldAttributes.Public);

            field.SetConstant(short.MaxValue);
            field.SetConstant(short.MinValue);
        }

        [Fact]
        public void SetConstant_Int()
        {
            FieldBuilder field = s_type.DefineField("IntField", typeof(int), FieldAttributes.Public);

            field.SetConstant(int.MinValue);
            field.SetConstant(int.MaxValue);
        }

        [Fact]
        public void SetConstant_Long()
        {
            FieldBuilder field = s_type.DefineField("LongField", typeof(long), FieldAttributes.Public);

            field.SetConstant(long.MaxValue);
            field.SetConstant(long.MinValue);
        }

        [Fact]
        public void SetConstant_Byte()
        {
            FieldBuilder field = s_type.DefineField("ByteField", typeof(byte), FieldAttributes.Public);

            field.SetConstant(byte.MinValue);
            field.SetConstant(byte.MaxValue);
        }

        [Fact]
        public void SetConstant_UShort()
        {
            FieldBuilder field = s_type.DefineField("UShortField", typeof(ushort), FieldAttributes.Public);

            field.SetConstant(ushort.MaxValue);
            field.SetConstant(ushort.MinValue);
        }

        [Fact]
        public void SetConstant_UInt()
        {
            FieldBuilder field = s_type.DefineField("UIntField", typeof(uint), FieldAttributes.Public);

            field.SetConstant(uint.MaxValue);
            field.SetConstant(uint.MinValue);
        }

        [Fact]
        public void SetConstant_ULong()
        {
            FieldBuilder field = s_type.DefineField("ULongField", typeof(ulong), FieldAttributes.Public);

            field.SetConstant(ulong.MaxValue);
            field.SetConstant(ulong.MinValue);
        }

        [Fact]
        public void SetConstant_Float()
        {
            FieldBuilder field = s_type.DefineField("FloatField", typeof(float), FieldAttributes.Public);

            field.SetConstant(float.MaxValue);
            field.SetConstant(float.NaN);
        }

        [Fact]
        public void SetConstant_Double()
        {
            FieldBuilder field = s_type.DefineField("DoubleField", typeof(double), FieldAttributes.Public);

            field.SetConstant(double.PositiveInfinity);
            field.SetConstant(double.NegativeInfinity);
        }

        [Fact]
        public void SetConstant_DateTime()
        {
            FieldBuilder field = s_type.DefineField("DateTimeField", typeof(DateTime), FieldAttributes.Public);

            field.SetConstant(DateTime.MinValue);
            field.SetConstant(DateTime.MaxValue);
        }

        [Fact]
        public void SetConstant_Char()
        {
            FieldBuilder field = s_type.DefineField("CharField", typeof(char), FieldAttributes.Public);

            field.SetConstant(char.MaxValue);
            field.SetConstant(char.MinValue);
        }

        [Fact]
        public void SetConstant_String()
        {
            FieldBuilder field = s_type.DefineField("StringField", typeof(string), FieldAttributes.Public);

            field.SetConstant(null);
            field.SetConstant("TestString");
        }

        [Fact]
        public void SetConstant_Enum()
        {
            FieldBuilder field = s_type.DefineField("EnumField", typeof(CustomEnum), FieldAttributes.Public);

            field.SetConstant(CustomEnum.Value0);
            field.SetConstant(CustomEnum.Value1);
        }

        [Fact]
        public void SetConstant_Object()
        {
            FieldBuilder field = s_type.DefineField("ObjectField", typeof(object), FieldAttributes.Public);

            field.SetConstant(null);
        }

        [Fact]
        public void SetConstant_CustomObject()
        {
            FieldBuilder field = s_type.DefineField("CustomObjectField", typeof(FieldBuilderSetConstant), FieldAttributes.Public);

            field.SetConstant(null);
        }

        [Fact]
        public void SetConstant_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            FieldBuilder field = type.DefineField("Field_NegTest1", typeof(int), FieldAttributes.Public);

            type.CreateType();

            Assert.Throws<InvalidOperationException>(() => field.SetConstant(0));
        }

        public static IEnumerable<object[]> SetConstant_Invalid_TestData()
        {
            FieldBuilder boolField = s_type.DefineField("BoolField2", typeof(bool), FieldAttributes.Public);
            yield return new object[] { boolField, null };
            yield return new object[] { boolField, new object() };

            FieldBuilder intField = s_type.DefineField("IntField2", typeof(int), FieldAttributes.Public);
            yield return new object[] { intField, null };
            yield return new object[] { intField, new object() };

            FieldBuilder sbyteField = s_type.DefineField("SByteField2", typeof(sbyte), FieldAttributes.Public);
            yield return new object[] { sbyteField, null };
            yield return new object[] { sbyteField, new object() };

            FieldBuilder shortField = s_type.DefineField("ShortField2", typeof(short), FieldAttributes.Public);
            yield return new object[] { shortField, null };
            yield return new object[] { shortField, new object() };

            FieldBuilder longField = s_type.DefineField("LongField2", typeof(long), FieldAttributes.Public);
            yield return new object[] { longField, null };
            yield return new object[] { longField, new object() };

            FieldBuilder byteField = s_type.DefineField("ByteField2", typeof(byte), FieldAttributes.Public);
            yield return new object[] { byteField, null };
            yield return new object[] { byteField, new object() };

            FieldBuilder ushortField = s_type.DefineField("UShortField2", typeof(ushort), FieldAttributes.Public);
            yield return new object[] { ushortField, null };
            yield return new object[] { ushortField, new object() };

            FieldBuilder uintField = s_type.DefineField("UIntField2", typeof(uint), FieldAttributes.Public);
            yield return new object[] { uintField, null };
            yield return new object[] { uintField, new object() };

            FieldBuilder ulongField = s_type.DefineField("ULongField2", typeof(ulong), FieldAttributes.Public);
            yield return new object[] { ulongField, null };
            yield return new object[] { ulongField, new object() };

            FieldBuilder floatField = s_type.DefineField("FloatField2", typeof(float), FieldAttributes.Public);
            yield return new object[] { floatField, null };
            yield return new object[] { floatField, new object() };

            FieldBuilder doubleField = s_type.DefineField("DoubleField2", typeof(double), FieldAttributes.Public);
            yield return new object[] { doubleField, null };
            yield return new object[] { doubleField, new object() };

            FieldBuilder dateTimeField = s_type.DefineField("DateTimeField2", typeof(DateTime), FieldAttributes.Public);
            yield return new object[] { dateTimeField, null };
            yield return new object[] { dateTimeField, new object() };

            FieldBuilder charField = s_type.DefineField("CharField2", typeof(char), FieldAttributes.Public);
            yield return new object[] { charField, null };
            yield return new object[] { charField, new object() };

            FieldBuilder stringField = s_type.DefineField("StringField2", typeof(string), FieldAttributes.Public);
            yield return new object[] { stringField, new object() };

            FieldBuilder enumField = s_type.DefineField("EnumField2", typeof(CustomEnum), FieldAttributes.Public);
            yield return new object[] { enumField, null };
            yield return new object[] { enumField, new object() };

            FieldBuilder decimalField = s_type.DefineField("DecimalField2", typeof(decimal), FieldAttributes.Public);
            yield return new object[] { decimalField, new object() };
            yield return new object[] { decimalField, decimal.One };

            FieldBuilder objectField = s_type.DefineField("ObjectField2", typeof(object), FieldAttributes.Public);
            yield return new object[] { objectField, new object() };

            FieldBuilder structField = s_type.DefineField("LongField2", typeof(FBTestStruct), FieldAttributes.Public);
            yield return new object[] { structField, null };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(SetConstant_Invalid_TestData))]
        public void SetConstant_InvalidType_ThrowsArgumentException(FieldBuilder field, object defaultValue)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => field.SetConstant(defaultValue));
        }
    }
}

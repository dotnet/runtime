// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;
using static VariantNative;

// Class used to validate the IClassX generation path doesn't fail.
// Support for this scenario is extremely limited so we simply validate
// it continues be generated and can be marshalled to native.
//
// This class must be marked public in order for the IDispatch path for
// field access to be validated.
public class GenerateIClassX
{
    public int FieldPrimitive;
    public System.Collections.Generic.List<int> FieldWithGeneric;
    public object FieldRefType;
    public DateTime FieldValueType;
}

[ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
partial class Test_VariantTest
{
    private const byte NumericValue = 15;

    private const char CharValue = 'z';

    private const string StringValue = "Abcdefg";

    private const decimal DecimalValue = 74.25M;

    private static readonly DateTime DateValue = new DateTime(2018, 11, 6);

    private unsafe static void TestByValue(bool hasComSupport)
    {
        Assert.True(Marshal_ByValue_Byte((byte)NumericValue, NumericValue));
        Assert.True(Marshal_ByValue_SByte((sbyte)NumericValue, (sbyte)NumericValue));
        Assert.True(Marshal_ByValue_Int16((short)NumericValue, NumericValue));
        Assert.True(Marshal_ByValue_UInt16((ushort)NumericValue, NumericValue));
        Assert.True(Marshal_ByValue_Int32((int)NumericValue, NumericValue));
        Assert.True(Marshal_ByValue_UInt32((uint)NumericValue, NumericValue));
        Assert.True(Marshal_ByValue_Int64((long)NumericValue, NumericValue));
        Assert.True(Marshal_ByValue_UInt64((ulong)NumericValue, NumericValue));
        Assert.True(Marshal_ByValue_Single((float)NumericValue, NumericValue));
        Assert.True(Marshal_ByValue_Double((double)NumericValue, NumericValue));
        Assert.True(Marshal_ByValue_String(StringValue, StringValue));
        Assert.True(Marshal_ByValue_String(new BStrWrapper(null), null));
        Assert.True(Marshal_ByValue_Char(CharValue, CharValue));
        Assert.True(Marshal_ByValue_Boolean(true, true));
        Assert.True(Marshal_ByValue_DateTime(DateValue, DateValue));
        Assert.True(Marshal_ByValue_Decimal(DecimalValue, DecimalValue));
        Assert.True(Marshal_ByValue_Currency(new CurrencyWrapper(DecimalValue), DecimalValue));
        Assert.True(Marshal_ByValue_Null(DBNull.Value));
        Assert.True(Marshal_ByValue_Missing(System.Reflection.Missing.Value));
        Assert.True(Marshal_ByValue_Empty(null));

        if (hasComSupport)
        {
            Assert.True(Marshal_ByValue_Object(new object()));
            Assert.True(Marshal_ByValue_Object_IUnknown(new UnknownWrapper(new object())));
            Assert.True(Marshal_ByValue_Object(new GenerateIClassX()));
            Assert.True(Marshal_ByValue_Object_IUnknown(new UnknownWrapper(new GenerateIClassX())));
        }
        else
        {
            Assert.Throws<NotSupportedException>(
                    () =>
                    {
                        Marshal_ByValue_Object(new object());
                    });
            Assert.Throws<NotSupportedException>(
                    () =>
                    {
                        Marshal_ByValue_Object_IUnknown(new UnknownWrapper(new object()));
                    });
        }

        Assert.Throws<ArgumentException>(() => Marshal_ByValue_Invalid(TimeSpan.Zero));
        Assert.Throws<NotSupportedException>(() => Marshal_ByValue_Invalid(new CustomStruct()));
        Assert.Throws<ArgumentException>(() => Marshal_ByValue_Invalid(new VariantWrapper(CharValue)));
    }

    private unsafe static void TestByRef(bool hasComSupport)
    {
        object obj;

        obj = (byte)NumericValue;
        Assert.True(Marshal_ByRef_Byte(ref obj, NumericValue));

        obj = (sbyte)NumericValue;
        Assert.True(Marshal_ByRef_SByte(ref obj, (sbyte)NumericValue));

        obj = (short)NumericValue;
        Assert.True(Marshal_ByRef_Int16(ref obj, NumericValue));

        obj = (ushort)NumericValue;
        Assert.True(Marshal_ByRef_UInt16(ref obj, NumericValue));

        obj = (int)NumericValue;
        Assert.True(Marshal_ByRef_Int32(ref obj, NumericValue));

        obj = (uint)NumericValue;
        Assert.True(Marshal_ByRef_UInt32(ref obj, NumericValue));

        obj = (long)NumericValue;
        Assert.True(Marshal_ByRef_Int64(ref obj, NumericValue));

        obj = (ulong)NumericValue;
        Assert.True(Marshal_ByRef_UInt64(ref obj, NumericValue));

        obj = (float)NumericValue;
        Assert.True(Marshal_ByRef_Single(ref obj, NumericValue));

        obj = (double)NumericValue;
        Assert.True(Marshal_ByRef_Double(ref obj, NumericValue));

        obj = StringValue;
        Assert.True(Marshal_ByRef_String(ref obj, StringValue));

        obj = new BStrWrapper(null);
        Assert.True(Marshal_ByRef_String(ref obj, null));

        obj = CharValue;
        Assert.True(Marshal_ByRef_Char(ref obj, CharValue));

        obj = true;
        Assert.True(Marshal_ByRef_Boolean(ref obj, true));

        obj = DateValue;
        Assert.True(Marshal_ByRef_DateTime(ref obj, DateValue));

        obj = DecimalValue;
        Assert.True(Marshal_ByRef_Decimal(ref obj, DecimalValue));

        obj = new CurrencyWrapper(DecimalValue);
        Assert.True(Marshal_ByRef_Currency(ref obj, DecimalValue));

        obj = DBNull.Value;
        Assert.True(Marshal_ByRef_Null(ref obj));

        obj = System.Reflection.Missing.Value;
        Assert.True(Marshal_ByRef_Missing(ref obj));

        obj = null;
        Assert.True(Marshal_ByRef_Empty(ref obj));

        if (hasComSupport)
        {
            obj = new object();
            Assert.True(Marshal_ByRef_Object(ref obj));

            obj = new UnknownWrapper(new object());
            Assert.True(Marshal_ByRef_Object_IUnknown(ref obj));

            obj = new GenerateIClassX();
            Assert.True(Marshal_ByRef_Object(ref obj));

            obj = new UnknownWrapper(new GenerateIClassX());
            Assert.True(Marshal_ByRef_Object_IUnknown(ref obj));
        }
        else
        {
            Assert.Throws<NotSupportedException>(
                    () =>
                    {
                        obj = new object();
                        Marshal_ByRef_Object(ref obj);
                    });
            Assert.Throws<NotSupportedException>(
                    () =>
                    {
                        obj = new UnknownWrapper(new object());
                        Marshal_ByRef_Object_IUnknown(ref obj);
                    });
        }

        obj = DecimalValue;
        Assert.True(Marshal_ChangeVariantType(ref obj, NumericValue));
        Assert.True(obj is int);
        Assert.Equal(NumericValue, (int)obj);
    }

    private unsafe static void TestOut()
    {
        Assert.True(Marshal_Out(out object obj, NumericValue));
        Assert.True(obj is int);
        Assert.Equal(NumericValue, (int)obj);
    }

    private unsafe static void TestFieldByValue(bool hasComSupport)
    {
        ObjectWrapper wrapper = new ObjectWrapper();

        wrapper.value = (byte)NumericValue;
        Assert.True(Marshal_Struct_ByValue_Byte(wrapper, NumericValue));

        wrapper.value = (sbyte)NumericValue;
        Assert.True(Marshal_Struct_ByValue_SByte(wrapper, (sbyte)NumericValue));

        wrapper.value = (short)NumericValue;
        Assert.True(Marshal_Struct_ByValue_Int16(wrapper, NumericValue));

        wrapper.value = (ushort)NumericValue;
        Assert.True(Marshal_Struct_ByValue_UInt16(wrapper, NumericValue));

        wrapper.value = (int)NumericValue;
        Assert.True(Marshal_Struct_ByValue_Int32(wrapper, NumericValue));

        wrapper.value = (uint)NumericValue;
        Assert.True(Marshal_Struct_ByValue_UInt32(wrapper, NumericValue));

        wrapper.value = (long)NumericValue;
        Assert.True(Marshal_Struct_ByValue_Int64(wrapper, NumericValue));

        wrapper.value = (ulong)NumericValue;
        Assert.True(Marshal_Struct_ByValue_UInt64(wrapper, NumericValue));

        wrapper.value = (float)NumericValue;
        Assert.True(Marshal_Struct_ByValue_Single(wrapper, NumericValue));

        wrapper.value = (double)NumericValue;
        Assert.True(Marshal_Struct_ByValue_Double(wrapper, NumericValue));

        wrapper.value = StringValue;
        Assert.True(Marshal_Struct_ByValue_String(wrapper, StringValue));

        wrapper.value = new BStrWrapper(null);
        Assert.True(Marshal_Struct_ByValue_String(wrapper, null));

        wrapper.value = CharValue;
        Assert.True(Marshal_Struct_ByValue_Char(wrapper, CharValue));

        wrapper.value = true;
        Assert.True(Marshal_Struct_ByValue_Boolean(wrapper, true));

        wrapper.value = DateValue;
        Assert.True(Marshal_Struct_ByValue_DateTime(wrapper, DateValue));

        wrapper.value = DecimalValue;
        Assert.True(Marshal_Struct_ByValue_Decimal(wrapper, DecimalValue));

        wrapper.value = new CurrencyWrapper(DecimalValue);
        Assert.True(Marshal_Struct_ByValue_Currency(wrapper, DecimalValue));

        wrapper.value = DBNull.Value;
        Assert.True(Marshal_Struct_ByValue_Null(wrapper));

        wrapper.value = System.Reflection.Missing.Value;
        Assert.True(Marshal_Struct_ByValue_Missing(wrapper));

        wrapper.value = null;
        Assert.True(Marshal_Struct_ByValue_Empty(wrapper));

        if (hasComSupport)
        {
            wrapper.value = new object();
            Assert.True(Marshal_Struct_ByValue_Object(wrapper));

            wrapper.value = new UnknownWrapper(new object());
            Assert.True(Marshal_Struct_ByValue_Object_IUnknown(wrapper));

            wrapper.value = new GenerateIClassX();
            Assert.True(Marshal_Struct_ByValue_Object(wrapper));

            wrapper.value = new UnknownWrapper(new GenerateIClassX());
            Assert.True(Marshal_Struct_ByValue_Object_IUnknown(wrapper));
        }
        else
        {
            Assert.Throws<NotSupportedException>(
                () =>
                {
                    wrapper.value = new object();
                    Marshal_Struct_ByValue_Object(wrapper);
                });
            Assert.Throws<NotSupportedException>(
                () =>
                {
                    wrapper.value = new UnknownWrapper(new object());
                    Marshal_Struct_ByValue_Object_IUnknown(wrapper);
                });
        }
    }

    private unsafe static void TestFieldByRef(bool hasComSupport)
    {
        ObjectWrapper wrapper = new ObjectWrapper();

        wrapper.value = (byte)NumericValue;
        Assert.True(Marshal_Struct_ByRef_Byte(ref wrapper, NumericValue));

        wrapper.value = (sbyte)NumericValue;
        Assert.True(Marshal_Struct_ByRef_SByte(ref wrapper, (sbyte)NumericValue));

        wrapper.value = (short)NumericValue;
        Assert.True(Marshal_Struct_ByRef_Int16(ref wrapper, NumericValue));

        wrapper.value = (ushort)NumericValue;
        Assert.True(Marshal_Struct_ByRef_UInt16(ref wrapper, NumericValue));

        wrapper.value = (int)NumericValue;
        Assert.True(Marshal_Struct_ByRef_Int32(ref wrapper, NumericValue));

        wrapper.value = (uint)NumericValue;
        Assert.True(Marshal_Struct_ByRef_UInt32(ref wrapper, NumericValue));

        wrapper.value = (long)NumericValue;
        Assert.True(Marshal_Struct_ByRef_Int64(ref wrapper, NumericValue));

        wrapper.value = (ulong)NumericValue;
        Assert.True(Marshal_Struct_ByRef_UInt64(ref wrapper, NumericValue));

        wrapper.value = (float)NumericValue;
        Assert.True(Marshal_Struct_ByRef_Single(ref wrapper, NumericValue));

        wrapper.value = (double)NumericValue;
        Assert.True(Marshal_Struct_ByRef_Double(ref wrapper, NumericValue));

        wrapper.value = StringValue;
        Assert.True(Marshal_Struct_ByRef_String(ref wrapper, StringValue));

        wrapper.value = new BStrWrapper(null);
        Assert.True(Marshal_Struct_ByRef_String(ref wrapper, null));

        wrapper.value = CharValue;
        Assert.True(Marshal_Struct_ByRef_Char(ref wrapper, CharValue));

        wrapper.value = true;
        Assert.True(Marshal_Struct_ByRef_Boolean(ref wrapper, true));

        wrapper.value = DateValue;
        Assert.True(Marshal_Struct_ByRef_DateTime(ref wrapper, DateValue));

        wrapper.value = DecimalValue;
        Assert.True(Marshal_Struct_ByRef_Decimal(ref wrapper, DecimalValue));

        wrapper.value = new CurrencyWrapper(DecimalValue);
        Assert.True(Marshal_Struct_ByRef_Currency(ref wrapper, DecimalValue));

        wrapper.value = DBNull.Value;
        Assert.True(Marshal_Struct_ByRef_Null(ref wrapper));

        wrapper.value = System.Reflection.Missing.Value;
        Assert.True(Marshal_Struct_ByRef_Missing(ref wrapper));

        wrapper.value = null;
        Assert.True(Marshal_Struct_ByRef_Empty(ref wrapper));

        if (hasComSupport)
        {
            wrapper.value = new object();
            Assert.True(Marshal_Struct_ByRef_Object(ref wrapper));

            wrapper.value = new UnknownWrapper(new object());
            Assert.True(Marshal_Struct_ByRef_Object_IUnknown(ref wrapper));

            wrapper.value = new GenerateIClassX();
            Assert.True(Marshal_Struct_ByRef_Object(ref wrapper));

            wrapper.value = new UnknownWrapper(new GenerateIClassX());
            Assert.True(Marshal_Struct_ByRef_Object_IUnknown(ref wrapper));
        }
        else
        {
            Assert.Throws<NotSupportedException>(
                    () =>
                    {
                        wrapper.value = new object();
                        Marshal_Struct_ByRef_Object(ref wrapper);
                    });
            Assert.Throws<NotSupportedException>(
                    () =>
                    {
                        wrapper.value = new UnknownWrapper(new object());
                        Marshal_Struct_ByRef_Object_IUnknown(ref wrapper);
                    });
        }
    }
}

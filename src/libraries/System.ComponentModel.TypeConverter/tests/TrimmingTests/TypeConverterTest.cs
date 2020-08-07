// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;

/// <summary>
/// Tests that the relevant constructors of intrinsic TypeConverter types are preserved when needed in a trimmed application.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        if (!RunTest(targetType: typeof(bool), expectedConverterType: typeof(BooleanConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(byte), expectedConverterType: typeof(ByteConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(sbyte), expectedConverterType: typeof(SByteConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(char), expectedConverterType: typeof(CharConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(double), expectedConverterType: typeof(DoubleConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(string), expectedConverterType: typeof(StringConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(short), expectedConverterType: typeof(Int16Converter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(int), expectedConverterType: typeof(Int32Converter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(long), expectedConverterType: typeof(Int64Converter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(float), expectedConverterType: typeof(SingleConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(ushort), expectedConverterType: typeof(UInt16Converter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(uint), expectedConverterType: typeof(UInt32Converter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(ulong), expectedConverterType: typeof(UInt64Converter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(object), expectedConverterType: typeof(TypeConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(DateTime), expectedConverterType: typeof(DateTimeConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(DateTimeOffset), expectedConverterType: typeof(DateTimeOffsetConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(decimal), expectedConverterType: typeof(DecimalConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(TimeSpan), expectedConverterType: typeof(TimeSpanConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(Guid), expectedConverterType: typeof(GuidConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(Array), expectedConverterType: typeof(ArrayConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(ICollection), expectedConverterType: typeof(CollectionConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(Enum), expectedConverterType: typeof(EnumConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(DayOfWeek), expectedConverterType: typeof(EnumConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(SomeValueType?), expectedConverterType: typeof(NullableConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(ClassWithNoConverter), expectedConverterType: typeof(TypeConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(Uri), expectedConverterType: typeof(UriTypeConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(CultureInfo), expectedConverterType: typeof(CultureInfoConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(Version), expectedConverterType: typeof(VersionConverter)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(IFooComponent), expectedConverterType: typeof(ReferenceConverter)))
        {
            return -1;
        }

        return 100;
    }

    private static bool RunTest(Type targetType, Type expectedConverterType)
    {
        TypeConverter retrievedConverter = TypeDescriptor.GetConverter(targetType);
        return retrievedConverter.GetType() == expectedConverterType && retrievedConverter.CanConvertTo(typeof(string));
    }

    private struct SomeValueType
    {
    }

    // TypeDescriptor should default to the TypeConverter in this case.
    private class ClassWithNoConverter
    {
    }

    private interface IFooComponent
    {
    }
}

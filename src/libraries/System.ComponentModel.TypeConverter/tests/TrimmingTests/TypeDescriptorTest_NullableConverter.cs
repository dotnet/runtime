// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

/// <summary>
/// Tests that the Nullable converter works with trimming.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        TypeDescriptor.RegisterType<ClassWithGenericProperty>();

        // Intrinsic type (internally registered).
        NullableConverter nullableConverter = (NullableConverter)TypeDescriptor.GetConverter(GetType_nullableByte);
        if (nullableConverter.UnderlyingType != typeof(byte))
        {
            return -1;
        }

        // Custom type (registered).
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(GetType_ClassWithGenericProperty);
        if (properties.Count != 1)
        {
            return -2;
        }

        if (properties[0].Name != "NullableStructWithCustomConverter")
        {
            return -3;
        }

        // Ensure the converter for the property is not trimmed.
        TypeConverter typeConverter = properties[0].Converter;
        if (!typeConverter.CanConvertTo(typeof(sbyte)))
        {
            return -4;
        }

        // Target members of the [TypeConverter] are trimmed since they are not referenced.
        // The linker will preserve the target type, but not its members.
        properties = TypeDescriptor.GetProperties(GetType_MyStructWithCustomConverter);
        if (properties.Count != 0)
        {
            return -5;
        }

        return 100;
    }

    // Helpers so that we don't we don't use 'typeof' in the method above and interfere with the linker.
    static Type GetType_nullableByte => typeof(byte?);
    static Type GetType_ClassWithGenericProperty => typeof(ClassWithGenericProperty);
    static Type GetType_MyStructWithCustomConverter => typeof(MyStructWithCustomConverter);
}

[TypeConverter(typeof(MyStructConverter))] 
struct MyStructWithCustomConverter
{
    public int Value { get; set; }
}

internal class MyStructConverter : TypeConverter
{
    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) => destinationType == typeof(sbyte);
}

class ClassWithGenericProperty
{
    public MyStructWithCustomConverter? NullableStructWithCustomConverter { get; set; }
}

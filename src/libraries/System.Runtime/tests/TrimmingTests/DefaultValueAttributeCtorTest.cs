// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Globalization;

/// <summary>
/// Tests that System.ComponentModel.TypeConverter.ConvertFromInvariantString
/// is not trimmed out when needed by DefaultValueAttribute in a trimmed application.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        TypeDescriptor.AddAttributes(typeof(string), new TypeConverterAttribute(typeof(MyStringConverter)));

        var attribute = new DefaultValueAttribute(typeof(string), "Hello, world!");
        return (string)attribute.Value == "Hello, world!trivia" ? 100 : -1;
    }

    private class MyStringConverter : StringConverter
    {
        /// <summary>
        /// Converts the specified value object to a string object.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string)
            {
                return (string)value + "trivia";
            }

            throw new NotSupportedException();
        }
    }
}

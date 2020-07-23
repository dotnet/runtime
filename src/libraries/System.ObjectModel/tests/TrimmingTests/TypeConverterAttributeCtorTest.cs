// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Globalization;

namespace TypeConverterAttributeTest
{
    /// <summary>
    /// Tests that the public constructors of types passed into System.ComponentModel.TypeConverterAttribute
    /// are not trimmed out when needed in a trimmed application.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            // Type-based TypeConverterAttribute ctor overload, ensure public parameterized ctor of TypeConverter type is preserved.
            TypeDescriptor.AddAttributes(typeof(DayOfWeek), new TypeConverterAttribute(typeof(EnumConverter)));
            var attribute = new DefaultValueAttribute(typeof(DayOfWeek), "Friday");
            if ((DayOfWeek)attribute.Value != DayOfWeek.Friday)
            {
                return -1;
            }

            // String-based TypeConverterAttribute ctor overload, ensure public parameterless ctor of TypeConverter type is preserved.
            TypeDescriptor.AddAttributes(typeof(string), new TypeConverterAttribute("TypeConverterAttributeTest.MyStringConverter, project, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"));
            attribute = new DefaultValueAttribute(typeof(string), "Hello, world!");
            if ((string)attribute.Value != "Hello, world!trivia")
            {
                return -1;
            }

            return 100;
        }
    }

    internal class MyStringConverter : StringConverter
    {
        /// <summary>
        /// Converts the specified value object to a string object.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string str)
            {
                return str + "trivia";
            }

            throw new NotSupportedException();
        }
    }
}

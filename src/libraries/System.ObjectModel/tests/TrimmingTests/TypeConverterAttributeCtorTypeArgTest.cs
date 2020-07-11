// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Globalization;

namespace TypeConverterAttributeTest
{
    /// <summary>
    /// Tests that the public parameterless ctor of a type passed into System.ComponentModel.TypeConverterAttribute
    /// is not trimmed out when needed in a trimmed application.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            TypeDescriptor.AddAttributes(typeof(string), new TypeConverterAttribute(typeof(MyStringConverter)));
            var attribute = new DefaultValueAttribute(typeof(string), "Hello, world!");
            return (string)attribute.Value == "Hello, world!trivia" ? 100 : -1;
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

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
            TypeDescriptor.AddAttributes(typeof(DayOfWeek), new TypeConverterAttribute(typeof(MyDayOfWeekConverter)));
            var attribute = new DefaultValueAttribute(typeof(DayOfWeek), "Friday");
            return (DayOfWeek)attribute.Value == DayOfWeek.Monday ? 100 : -1;
        }
    }

    internal class MyDayOfWeekConverter : TypeConverter
    {
        private readonly Type _type;

        public MyDayOfWeekConverter(Type type)
        {
            _type = type;
        }

        /// <summary>
        /// Converts the specified value object to a DayOfWeek value.
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (_type == typeof(DayOfWeek) && value is string str && str == "Friday")
            {
                return DayOfWeek.Monday;
            }

            throw new NotSupportedException();
        }
    }
}

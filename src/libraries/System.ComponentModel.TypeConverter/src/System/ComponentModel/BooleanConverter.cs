// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert Boolean objects to and from various other representations.
    /// </summary>
    public class BooleanConverter : TypeConverter
    {
        private static volatile StandardValuesCollection? s_values;

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object
        /// in the given source type to a Boolean object using the specified context.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Converts the given value
        /// object to a Boolean object.
        /// </summary>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string text)
            {
                text = text.Trim();
                try
                {
                    return bool.Parse(text);
                }
                catch (FormatException e)
                {
                    throw new FormatException(SR.Format(SR.ConvertInvalidPrimitive, (string)value, nameof(Boolean)), e);
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Gets a collection of standard values for the Boolean data type.
        /// </summary>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        {
            return s_values ?? (s_values = new StandardValuesCollection(new object[] { true, false }));
        }

        /// <summary>
        ///
        /// Gets a value indicating whether the list of standard values returned from
        /// <see cref='System.ComponentModel.BooleanConverter.GetStandardValues'/> is an exclusive list.
        ///
        /// </summary>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context) => true;

        /// <summary>
        ///
        /// Gets a value indicating whether this object supports a standard set of values that can
        /// be picked from a list.
        ///
        /// </summary>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;
    }
}

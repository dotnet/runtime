// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Provides a type converter to convert
    ///       Boolean objects to and from various other representations.</para>
    /// </summary>
    public class BooleanConverter : TypeConverter
    {
        private static volatile StandardValuesCollection s_values;

        /// <summary>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object in the given source type to a Boolean object using the
        ///       specified context.</para>
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        ///    <para>Converts the given value
        ///       object to a Boolean object.</para>
        /// </summary>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string text = value as string;
            if (text != null)
            {
                text = text.Trim();
                try
                {
                    return Boolean.Parse(text);
                }
                catch (FormatException e)
                {
                    throw new FormatException(SR.Format(SR.ConvertInvalidPrimitive, (string)value, nameof(Boolean)), e);
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        ///    <para>Gets a collection of standard values for the Boolean data type.</para>
        /// </summary>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return s_values ?? (s_values = new StandardValuesCollection(new object[] {true, false}));
        }

        /// <summary>
        ///    <para>
        ///        Gets a value indicating whether the list of standard values returned from
        ///        <see cref='System.ComponentModel.BooleanConverter.GetStandardValues'/> is an exclusive list.
        ///    </para>
        /// </summary>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <summary>
        ///    <para>
        ///        Gets a value indicating whether this object supports a standard set of values that can
        ///        be picked from a list.
        ///    </para>
        /// </summary>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}

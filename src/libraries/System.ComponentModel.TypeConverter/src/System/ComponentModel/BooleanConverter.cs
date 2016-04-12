// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Provides a type converter to convert
    ///       Boolean objects to and from various other representations.</para>
    /// </devdoc>
    public class BooleanConverter : TypeConverter
    {
        private static volatile StandardValuesCollection s_values;

        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object in the given source type to a Boolean object using the
        ///       specified context.</para>
        /// </devdoc>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <devdoc>
        ///    <para>Converts the given value
        ///       object to a Boolean object.</para>
        /// </devdoc>
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

        /// <devdoc>
        ///    <para>Gets a collection of standard values for the Boolean data type.</para>
        /// </devdoc>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (s_values == null)
            {
                s_values = new StandardValuesCollection(new object[] { true, false });
            }
            return s_values;
        }

        /// <devdoc>
        ///    <para>
        ///        Gets a value indicating whether the list of standard values returned from
        ///        <see cref='System.ComponentModel.BooleanConverter.GetStandardValues'/> is an exclusive list.
        ///    </para>
        /// </devdoc>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <devdoc>
        ///    <para>
        ///        Gets a value indicating whether this object supports a standard set of values that can
        ///        be picked from a list.
        ///    </para>
        /// </devdoc>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}

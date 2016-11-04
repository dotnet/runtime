// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;

namespace System.Data
{
    /// <summary>
    /// Provides a type converter that can be used to populate a list box with available types.
    /// </summary>
    internal sealed class DefaultValueTypeConverter : StringConverter
    {
        private const string NullString = "<null>";
        private const string DbNullString = "<DBNull>";

        // converter classes should have public ctor
        public DefaultValueTypeConverter()
        {
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(string))
            {
                if (value == null)
                {
                    return NullString;
                }
                else if (value == DBNull.Value)
                {
                    return DbNullString;
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value != null && value.GetType() == typeof(string))
            {
                string strValue = (string)value;
                if (string.Equals(strValue, NullString, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                else if (string.Equals(strValue, DbNullString, StringComparison.OrdinalIgnoreCase))
                {
                    return DBNull.Value;
                }
            }

            return base.ConvertFrom(context, culture, value);
        }
    }
}


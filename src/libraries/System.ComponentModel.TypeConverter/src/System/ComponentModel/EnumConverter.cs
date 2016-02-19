// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <devdoc>
    /// <para>Provides a type converter to convert <see cref='System.Enum'/>
    /// objects to and from various
    /// other representations.</para>
    /// </devdoc>
    public class EnumConverter : TypeConverter
    {
        /// <devdoc>
        ///    <para>
        ///       Specifies
        ///       the
        ///       type of the enumerator this converter is
        ///       associated with.
        ///    </para>
        /// </devdoc>
        private Type _type;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.EnumConverter'/> class for the given
        ///       type.
        ///    </para>
        /// </devdoc>
        public EnumConverter(Type type)
        {
            _type = type;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        protected Type EnumType
        {
            get
            {
                return _type;
            }
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter
        ///       can convert an object in the given source type to an enumeration object using
        ///       the specified context.</para>
        /// </devdoc>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string) || sourceType == typeof(Enum[]))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <devdoc>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object to the given destination type using the context.</para>
        /// </devdoc>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(Enum[]))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>Converts the specified value object to an enumeration object.</para>
        /// </devdoc>
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string strValue = value as string;
            if (strValue != null)
            {
                try
                {
                    if (strValue.IndexOf(',') != -1)
                    {
                        long convertedValue = 0;
                        string[] values = strValue.Split(new char[] { ',' });
                        foreach (string v in values)
                        {
                            convertedValue |= Convert.ToInt64((Enum)Enum.Parse(_type, v, true), culture);
                        }
                        return Enum.ToObject(_type, convertedValue);
                    }
                    else
                    {
                        return Enum.Parse(_type, strValue, true);
                    }
                }
                catch (Exception e)
                {
                    throw new FormatException(SR.Format(SR.ConvertInvalidPrimitive, (string)value, _type.Name), e);
                }
            }
            else if (value is Enum[])
            {
                long finalValue = 0;
                foreach (Enum e in (Enum[])value)
                {
                    finalValue |= Convert.ToInt64(e, culture);
                }
                return Enum.ToObject(_type, finalValue);
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <internalonly/>
        /// <devdoc>
        ///    <para>Converts the given
        ///       value object to the
        ///       specified destination type.</para>
        /// </devdoc>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1803:AvoidCostlyCallsWherePossible")]  // Keep call to Enum.IsDefined
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(string) && value != null)
            {
                // Raise an argument exception if the value isn't defined and if
                // the enum isn't a flags style.
                //
                if (!_type.GetTypeInfo().IsDefined(typeof(FlagsAttribute), false) && !Enum.IsDefined(_type, value))
                {
                    throw new ArgumentException(SR.Format(SR.EnumConverterInvalidValue, value.ToString(), _type.Name));
                }

                return Enum.Format(_type, value, "G");
            }

            if (destinationType == typeof(Enum[]) && value != null)
            {
                if (_type.GetTypeInfo().IsDefined(typeof(FlagsAttribute), false))
                {
                    List<Enum> flagValues = new List<Enum>();

                    Array objValues = Enum.GetValues(_type);
                    long[] ulValues = new long[objValues.Length];
                    for (int idx = 0; idx < objValues.Length; idx++)
                    {
                        ulValues[idx] = Convert.ToInt64((Enum)objValues.GetValue(idx), culture);
                    }

                    long longValue = Convert.ToInt64((Enum)value, culture);
                    bool valueFound = true;
                    while (valueFound)
                    {
                        valueFound = false;
                        foreach (long ul in ulValues)
                        {
                            if ((ul != 0 && (ul & longValue) == ul) || ul == longValue)
                            {
                                flagValues.Add((Enum)Enum.ToObject(_type, ul));
                                valueFound = true;
                                longValue &= ~ul;
                                break;
                            }
                        }

                        if (longValue == 0)
                        {
                            break;
                        }
                    }

                    if (!valueFound && longValue != 0)
                    {
                        flagValues.Add((Enum)Enum.ToObject(_type, longValue));
                    }

                    return flagValues.ToArray();
                }
                else
                {
                    return new Enum[] { (Enum)Enum.ToObject(_type, value) };
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}

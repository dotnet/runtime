// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    /// <para>Provides a type converter to convert <see cref='System.Enum'/>
    /// objects to and from various
    /// other representations.</para>
    /// </summary>
    public class EnumConverter : TypeConverter
    {
        private static readonly char[] s_separators = {','};
        /// <summary>
        ///    <para>
        ///       Provides a <see cref='System.ComponentModel.TypeConverter.StandardValuesCollection'/> that specifies the
        ///       possible values for the enumeration.
        ///    </para>
        /// </summary>
        private StandardValuesCollection _values;

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.EnumConverter'/> class for the given
        ///       type.
        ///    </para>
        /// </summary>
        public EnumConverter(Type type)
        {
            EnumType = type;
        }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        protected Type EnumType { get; }

        /// <summary>
        ///    <para>[To be supplied.]</para>
        /// </summary>
        protected StandardValuesCollection Values
        {
            get
            {
                return _values;
            }
            set
            {
                _values = value;
            }
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>Gets a value indicating whether this converter
        ///       can convert an object in the given source type to an enumeration object using
        ///       the specified context.</para>
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string) || sourceType == typeof(Enum[]))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        ///    <para>Gets a value indicating whether this converter can
        ///       convert an object to the given destination type using the context.</para>
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(Enum[]))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        ///     <para>
        ///         Gets an <see cref='System.Collections.IComparer'/> interface that can
        ///         be used to sort the values of the enumerator.
        ///     </para>
        /// </summary>
        protected virtual IComparer Comparer => InvariantComparer.Default;

        /// <internalonly/>
        /// <summary>
        ///    <para>Converts the specified value object to an enumeration object.</para>
        /// </summary>
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
                        string[] values = strValue.Split(s_separators);
                        foreach (string v in values)
                        {
                            convertedValue |= Convert.ToInt64((Enum)Enum.Parse(EnumType, v, true), culture);
                        }
                        return Enum.ToObject(EnumType, convertedValue);
                    }
                    else
                    {
                        return Enum.Parse(EnumType, strValue, true);
                    }
                }
                catch (Exception e)
                {
                    throw new FormatException(SR.Format(SR.ConvertInvalidPrimitive, (string)value, EnumType.Name), e);
                }
            }
            else if (value is Enum[])
            {
                long finalValue = 0;
                foreach (Enum e in (Enum[])value)
                {
                    finalValue |= Convert.ToInt64(e, culture);
                }
                return Enum.ToObject(EnumType, finalValue);
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>Converts the given
        ///       value object to the
        ///       specified destination type.</para>
        /// </summary>
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
                if (!EnumType.IsDefined(typeof(FlagsAttribute), false) && !Enum.IsDefined(EnumType, value))
                {
                    throw new ArgumentException(SR.Format(SR.EnumConverterInvalidValue, value.ToString(), EnumType.Name));
                }

                return Enum.Format(EnumType, value, "G");
            }

            if (destinationType == typeof(Enum[]) && value != null)
            {
                if (EnumType.IsDefined(typeof(FlagsAttribute), false))
                {
                    List<Enum> flagValues = new List<Enum>();

                    Array objValues = Enum.GetValues(EnumType);
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
                                flagValues.Add((Enum)Enum.ToObject(EnumType, ul));
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
                        flagValues.Add((Enum)Enum.ToObject(EnumType, longValue));
                    }

                    return flagValues.ToArray();
                }
                else
                {
                    return new Enum[] { (Enum)Enum.ToObject(EnumType, value) };
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>Gets a collection of standard values for the data type this validator is
        ///       designed for.</para>
        /// </summary>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            if (_values == null)
            {
                // We need to get the enum values in this rather round-about way so we can filter
                // out fields marked Browsable(false). Note that if multiple fields have the same value,
                // the behavior is undefined, since what we return are just enum values, not names.

                Type reflectType = TypeDescriptor.GetReflectionType(EnumType) ?? EnumType;

                FieldInfo[] fields = reflectType.GetFields(BindingFlags.Public | BindingFlags.Static);
                ArrayList objValues = null;

                if (fields != null && fields.Length > 0)
                {
                    objValues = new ArrayList(fields.Length);
                }

                if (objValues != null)
                {
                    foreach (FieldInfo field in fields)
                    {
                        BrowsableAttribute browsableAttr = null;
                        foreach (Attribute attr in field.GetCustomAttributes(typeof(BrowsableAttribute), false))
                        {
                            browsableAttr = attr as BrowsableAttribute;
                        }

                        if (browsableAttr == null || browsableAttr.Browsable)
                        {
                            object value = null;

                            try
                            {
                                if (field.Name != null)
                                {
                                    value = Enum.Parse(EnumType, field.Name);
                                }
                            }
                            catch (ArgumentException)
                            {
                                // Hmm, for some reason, the parse threw. Let us ignore this value.
                            }

                            if (value != null)
                            {
                                objValues.Add(value);
                            }
                        }
                    }

                    IComparer comparer = Comparer;
                    if (comparer != null)
                    {
                        objValues.Sort(comparer);
                    }
                }

                Array arr = objValues?.ToArray();
                _values = new StandardValuesCollection(arr);
            }
            return _values;
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>Gets a value indicating whether the list of standard values returned from
        ///    <see cref='System.ComponentModel.TypeConverter.GetStandardValues'/> 
        ///    is an exclusive list using the specified context.</para>
        /// </summary>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return !EnumType.IsDefined(typeof(FlagsAttribute), false);
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>Gets a value indicating
        ///       whether this object
        ///       supports a standard set of values that can be picked
        ///       from a list using the specified context.</para>
        /// </summary>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>Gets a value indicating whether the given object value is valid for this type.</para>
        /// </summary>
        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            return Enum.IsDefined(EnumType, value);
        }
    }
}

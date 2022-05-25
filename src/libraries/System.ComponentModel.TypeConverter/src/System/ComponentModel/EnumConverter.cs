// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.ComponentModel
{
    /// <summary>
    /// Provides a type converter to convert <see cref='System.Enum'/> objects to and
    /// from various other representations
    /// </summary>
    public class EnumConverter : TypeConverter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.EnumConverter'/> class for the given
        /// type.
        /// </summary>
        public EnumConverter([DynamicallyAccessedMembers(TypeDescriptor.ReflectTypesDynamicallyAccessedMembers)] Type type)
        {
            EnumType = type;
        }

        [DynamicallyAccessedMembers(TypeDescriptor.ReflectTypesDynamicallyAccessedMembers)]
        protected Type EnumType { get; }

        protected StandardValuesCollection? Values { get; set; }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object in the given
        /// source type to an enumeration object using the specified context.
        /// </summary>
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            if (sourceType == typeof(string) || sourceType == typeof(Enum[]))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        /// <summary>
        /// Gets a value indicating whether this converter can convert an object to the
        /// given destination type using the context.
        /// </summary>
        public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
        {
            if (destinationType == typeof(Enum[]) || destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        /// <summary>
        /// Gets an <see cref='System.Collections.IComparer'/> interface that can
        /// be used to sort the values of the enumerator.
        /// </summary>
        protected virtual IComparer Comparer => InvariantComparer.Default;

        private static long GetEnumValue(bool isUnderlyingTypeUInt64, Enum enumVal, CultureInfo? culture)
        {
            return isUnderlyingTypeUInt64 ?
                unchecked((long)Convert.ToUInt64(enumVal, culture)) :
                Convert.ToInt64(enumVal, culture);
        }

        /// <summary>
        /// Converts the specified value object to an enumeration object.
        /// </summary>
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string strValue)
            {
                try
                {
                    if (strValue.Contains(','))
                    {
                        bool isUnderlyingTypeUInt64 = Enum.GetUnderlyingType(EnumType) == typeof(ulong);
                        long convertedValue = 0;
                        string[] values = strValue.Split(',');
                        foreach (string v in values)
                        {
                            convertedValue |= GetEnumValue(isUnderlyingTypeUInt64, (Enum)Enum.Parse(EnumType, v, true), culture);
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
                bool isUnderlyingTypeUInt64 = Enum.GetUnderlyingType(EnumType) == typeof(ulong);
                long finalValue = 0;
                foreach (Enum e in (Enum[])value)
                {
                    finalValue |= GetEnumValue(isUnderlyingTypeUInt64, e, culture);
                }
                return Enum.ToObject(EnumType, finalValue);
            }
            return base.ConvertFrom(context, culture, value);
        }

        /// <summary>
        /// Converts the given value object to the specified destination type.
        /// </summary>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (destinationType == typeof(string) && value != null)
            {
                // Raise an argument exception if the value isn't defined and if
                // the enum isn't a flags style.
                if (!EnumType.IsDefined(typeof(FlagsAttribute), false) && !Enum.IsDefined(EnumType, value))
                {
                    throw new ArgumentException(SR.Format(SR.EnumConverterInvalidValue, value, EnumType.Name));
                }

                return Enum.Format(EnumType, value, "G");
            }

            if (destinationType == typeof(InstanceDescriptor) && value != null)
            {
                string enumName = ConvertToInvariantString(context, value)!;

                if (EnumType.IsDefined(typeof(FlagsAttribute), false) && enumName.Contains(','))
                {
                    // This is a flags enum, and there is no one flag
                    // that covers the value.  Instead, convert the
                    // value to the underlying type and invoke
                    // a ToObject call on enum.
                    //
                    Type underlyingType = Enum.GetUnderlyingType(EnumType);
                    if (value is IConvertible)
                    {
                        object convertedValue = ((IConvertible)value).ToType(underlyingType, culture);

                        MethodInfo? method = typeof(Enum).GetMethod("ToObject", new Type[] { typeof(Type), underlyingType });
                        if (method != null)
                        {
                            return new InstanceDescriptor(method, new object[] { EnumType, convertedValue });
                        }
                    }
                }
                else
                {
                    FieldInfo? info = EnumType.GetField(enumName);
                    if (info != null)
                    {
                        return new InstanceDescriptor(info, null);
                    }
                }
            }

            if (destinationType == typeof(Enum[]) && value != null)
            {
                if (EnumType.IsDefined(typeof(FlagsAttribute), false))
                {
                    bool isUnderlyingTypeUInt64 = Enum.GetUnderlyingType(EnumType) == typeof(ulong);
                    List<Enum> flagValues = new List<Enum>();

                    Array objValues = Enum.GetValues(EnumType);
                    long[] ulValues = new long[objValues.Length];
                    for (int idx = 0; idx < objValues.Length; idx++)
                    {
                        ulValues[idx] = GetEnumValue(isUnderlyingTypeUInt64, (Enum)objValues.GetValue(idx)!, culture);
                    }

                    long longValue = GetEnumValue(isUnderlyingTypeUInt64, (Enum)value, culture);
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

        /// <summary>
        /// Gets a collection of standard values for the data type this validator is
        /// designed for.
        /// </summary>
        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext? context)
        {
            if (Values == null)
            {
                // We need to get the enum values in this rather round-about way so we can filter
                // out fields marked Browsable(false). Note that if multiple fields have the same value,
                // the behavior is undefined, since what we return are just enum values, not names.
                Type reflectType = TypeDescriptor.GetReflectionType(EnumType) ?? EnumType;

                FieldInfo[]? fields = reflectType.GetFields(BindingFlags.Public | BindingFlags.Static);
                ArrayList? objValues = null;

                if (fields != null && fields.Length > 0)
                {
                    objValues = new ArrayList(fields.Length);
                }

                if (objValues != null)
                {
                    foreach (FieldInfo field in fields!)
                    {
                        BrowsableAttribute? browsableAttr = null;
                        foreach (Attribute attr in field.GetCustomAttributes(typeof(BrowsableAttribute), false))
                        {
                            browsableAttr = attr as BrowsableAttribute;
                        }

                        if (browsableAttr == null || browsableAttr.Browsable)
                        {
                            object? value = null;

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

                    IComparer? comparer = Comparer;
                    if (comparer != null)
                    {
                        objValues.Sort(comparer);
                    }
                }

                Array? arr = objValues?.ToArray();
                Values = new StandardValuesCollection(arr);
            }
            return Values;
        }

        /// <summary>
        /// Gets a value indicating whether the list of standard values returned from
        /// <see cref='System.ComponentModel.TypeConverter.GetStandardValues()'/>
        /// is an exclusive list using the specified context.
        /// </summary>
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext? context)
        {
            return !EnumType.IsDefined(typeof(FlagsAttribute), false);
        }

        /// <summary>
        /// Gets a value indicating whether this object supports a standard set of values
        /// that can be picked from a list using the specified context.
        /// </summary>
        public override bool GetStandardValuesSupported(ITypeDescriptorContext? context) => true;

        /// <summary>
        /// Gets a value indicating whether the given object value is valid for this type.
        /// </summary>
        public override bool IsValid(ITypeDescriptorContext? context, object? value) => Enum.IsDefined(EnumType, value!);
    }
}

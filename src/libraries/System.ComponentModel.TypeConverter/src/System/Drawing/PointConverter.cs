// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace System.Drawing
{
    public class PointConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType)
        {
            return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string strValue)
            {
                string text = strValue.Trim();
                if (text.Length == 0)
                {
                    return null;
                }

                // Parse 2 integer values.
                if (culture == null)
                {
                    culture = CultureInfo.CurrentCulture;
                }

                char sep = culture.TextInfo.ListSeparator[0];
                string[] tokens = text.Split(sep);
                int[] values = new int[tokens.Length];
                TypeConverter intConverter = TypeDescriptor.GetConverterTrimUnsafe(typeof(int));
                for (int i = 0; i < values.Length; i++)
                {
                    // Note: ConvertFromString will raise exception if value cannot be converted.
                    values[i] = (int)intConverter.ConvertFromString(context, culture, tokens[i])!;
                }

                if (values.Length == 2)
                {
                    return new Point(values[0], values[1]);
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.TextParseFailedFormat, text, "x, y"));
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (value is Point pt)
            {
                if (destinationType == typeof(string))
                {
                    if (culture == null)
                    {
                        culture = CultureInfo.CurrentCulture;
                    }

                    string sep = culture.TextInfo.ListSeparator + " ";
                    TypeConverter intConverter = TypeDescriptor.GetConverterTrimUnsafe(typeof(int));

                    // Note: ConvertFromString will raise exception if value cannot be converted.
                    var args = new string?[]
                    {
                        intConverter.ConvertToString(context, culture, pt.X),
                        intConverter.ConvertToString(context, culture, pt.Y)
                    };
                    return string.Join(sep, args);
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo? ctor = typeof(Point).GetConstructor(new Type[] { typeof(int), typeof(int) });
                    if (ctor != null)
                    {
                        return new InstanceDescriptor(ctor, new object[] { pt.X, pt.Y });
                    }
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues)
        {
            ArgumentNullException.ThrowIfNull(propertyValues);

            object? x = propertyValues["X"];
            object? y = propertyValues["Y"];

            if (x == null || y == null || !(x is int) || !(y is int))
            {
                throw new ArgumentException(SR.PropertyValueInvalidEntry);
            }

            return new Point((int)x, (int)y);
        }

        public override bool GetCreateInstanceSupported(ITypeDescriptorContext? context) => true;

        private static readonly string[] s_propertySort = { "X", "Y" };

        [RequiresUnreferencedCode("The Type of value cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext? context, object? value, Attribute[]? attributes)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(Point), attributes);
            return props.Sort(s_propertySort);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;
    }
}

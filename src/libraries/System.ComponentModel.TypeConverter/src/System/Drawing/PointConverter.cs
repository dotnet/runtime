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
                ReadOnlySpan<char> text = strValue.AsSpan().Trim();
                if (text.Length == 0)
                {
                    return null;
                }

                // Parse 2 integer values.
                culture ??= CultureInfo.CurrentCulture;

                string sep = culture.TextInfo.ListSeparator;
                Span<Range> ranges = stackalloc Range[3];
                int rangesCount = text.Split(ranges, sep);
                if (rangesCount != 2)
                {
                    throw new ArgumentException(SR.Format(SR.TextParseFailedFormat, text.ToString(), $"x{sep} y"));
                }

                TypeConverter converter = TypeDescriptor.GetConverterTrimUnsafe(typeof(int));
                int x = (int)converter.ConvertFromString(context, culture, strValue[ranges[0]])!;
                int y = (int)converter.ConvertFromString(context, culture, strValue[ranges[1]])!;

                return new Point(x, y);
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
                    culture ??= CultureInfo.CurrentCulture;

                    string sep = culture.TextInfo.ListSeparator;
                    TypeConverter intConverter = TypeDescriptor.GetConverterTrimUnsafe(typeof(int));

                    // Note: ConvertFromString will raise exception if value cannot be converted.
                    string? x = intConverter.ConvertToString(context, culture, pt.X);
                    string? y = intConverter.ConvertToString(context, culture, pt.Y);

                    return $"{x}{sep} {y}";
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo? ctor = typeof(Point).GetConstructor([typeof(int), typeof(int)]);
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

        private static readonly string[] s_propertySort = ["X", "Y"];

        [RequiresUnreferencedCode("The Type of value cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext? context, object? value, Attribute[]? attributes)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(Point), attributes);
            return props.Sort(s_propertySort);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;
    }
}

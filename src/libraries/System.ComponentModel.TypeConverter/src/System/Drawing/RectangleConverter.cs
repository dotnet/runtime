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
    public class RectangleConverter : TypeConverter
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

                // Parse 4 integer values.
                culture ??= CultureInfo.CurrentCulture;

                string sep = culture.TextInfo.ListSeparator;
                Span<Range> ranges = stackalloc Range[5];
                int rangesCount = text.Split(ranges, sep);
                if (rangesCount != 4)
                {
                    throw new ArgumentException(SR.Format(SR.TextParseFailedFormat, text.ToString(), $"x{sep} y{sep} width{sep} height"));
                }

                TypeConverter converter = TypeDescriptor.GetConverterTrimUnsafe(typeof(int));
                int x = (int)converter.ConvertFromString(context, culture, strValue[ranges[0]])!;
                int y = (int)converter.ConvertFromString(context, culture, strValue[ranges[1]])!;
                int width = (int)converter.ConvertFromString(context, culture, strValue[ranges[2]])!;
                int height = (int)converter.ConvertFromString(context, culture, strValue[ranges[3]])!;

                return new Rectangle(x, y, width, height);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (value is Rectangle rect)
            {
                if (destinationType == typeof(string))
                {
                    culture ??= CultureInfo.CurrentCulture;

                    string sep = culture.TextInfo.ListSeparator;
                    TypeConverter intConverter = TypeDescriptor.GetConverterTrimUnsafe(typeof(int));

                    // Note: ConvertToString will raise exception if value cannot be converted.
                    string? x = intConverter.ConvertToString(context, culture, rect.X);
                    string? y = intConverter.ConvertToString(context, culture, rect.Y);
                    string? width = intConverter.ConvertToString(context, culture, rect.Width);
                    string? height = intConverter.ConvertToString(context, culture, rect.Height);

                    return $"{x}{sep} {y}{sep} {width}{sep} {height}";
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo? ctor = typeof(Rectangle).GetConstructor(
                        [typeof(int), typeof(int), typeof(int), typeof(int)]);

                    if (ctor != null)
                    {
                        return new InstanceDescriptor(ctor, new object[] {
                            rect.X, rect.Y, rect.Width, rect.Height});
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
            object? width = propertyValues["Width"];
            object? height = propertyValues["Height"];

            if (x == null || y == null || width == null || height == null ||
                !(x is int) || !(y is int) || !(width is int) || !(height is int))
            {
                throw new ArgumentException(SR.PropertyValueInvalidEntry);
            }

            return new Rectangle((int)x, (int)y, (int)width, (int)height);
        }

        public override bool GetCreateInstanceSupported(ITypeDescriptorContext? context) => true;

        private static readonly string[] s_propertySort = ["X", "Y", "Width", "Height"];

        [RequiresUnreferencedCode("The Type of value cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext? context, object? value, Attribute[]? attributes)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(Rectangle), attributes);
            return props.Sort(s_propertySort);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;
    }
}

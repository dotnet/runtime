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
    public class SizeFConverter : TypeConverter
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
                    throw new ArgumentException(SR.Format(SR.TextParseFailedFormat, text.ToString(), $"Width{sep} Height"));
                }

                TypeConverter converter = TypeDescriptor.GetConverterTrimUnsafe(typeof(float));
                float width = (float)converter.ConvertFromString(context, culture, strValue[ranges[0]])!;
                float height = (float)converter.ConvertFromString(context, culture, strValue[ranges[1]])!;

                return new SizeF(width, height);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            ArgumentNullException.ThrowIfNull(destinationType);

            if (value is SizeF size)
            {
                if (destinationType == typeof(string))
                {
                    culture ??= CultureInfo.CurrentCulture;

                    string sep = culture.TextInfo.ListSeparator;
                    TypeConverter floatConverter = TypeDescriptor.GetConverterTrimUnsafe(typeof(float));
                    string? width = floatConverter.ConvertToString(context, culture, size.Width);
                    string? height = floatConverter.ConvertToString(context, culture, size.Height);

                    return $"{width}{sep} {height}";
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo? ctor = typeof(SizeF).GetConstructor([typeof(float), typeof(float)]);
                    if (ctor != null)
                    {
                        return new InstanceDescriptor(ctor, new object[] { size.Width, size.Height });
                    }
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override object CreateInstance(ITypeDescriptorContext? context, IDictionary propertyValues)
        {
            ArgumentNullException.ThrowIfNull(propertyValues);

            object? width = propertyValues["Width"];
            object? height = propertyValues["Height"];

            if (width == null || height == null || !(width is float) || !(height is float))
            {
                throw new ArgumentException(SR.PropertyValueInvalidEntry);
            }

            return new SizeF((float)width, (float)height);
        }

        public override bool GetCreateInstanceSupported(ITypeDescriptorContext? context) => true;

        private static readonly string[] s_propertySort = ["Width", "Height"];

        [RequiresUnreferencedCode("The Type of value cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(SizeF), attributes);
            return props.Sort(s_propertySort);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;
    }
}

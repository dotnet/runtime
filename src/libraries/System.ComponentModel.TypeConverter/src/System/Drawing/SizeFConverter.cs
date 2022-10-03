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
                string text = strValue.Trim();
                if (text.Length == 0)
                {
                    return null;
                }

                // Parse 2 integer values.
                culture ??= CultureInfo.CurrentCulture;

                char sep = culture.TextInfo.ListSeparator[0];
                string[] tokens = text.Split(sep);
                float[] values = new float[tokens.Length];
                TypeConverter floatConverter = TypeDescriptor.GetConverterTrimUnsafe(typeof(float));
                for (int i = 0; i < values.Length; i++)
                {
                    values[i] = (float)floatConverter.ConvertFromString(context, culture, tokens[i])!;
                }

                if (values.Length != 2)
                {
                    throw new ArgumentException(SR.Format(SR.TextParseFailedFormat, text, "Width,Height"));
                }

                return new SizeF(values[0], values[1]);
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

                    string sep = culture.TextInfo.ListSeparator + " ";
                    TypeConverter floatConverter = TypeDescriptor.GetConverterTrimUnsafe(typeof(float));
                    var args = new string?[]
                    {
                        floatConverter.ConvertToString(context, culture, size.Width),
                        floatConverter.ConvertToString(context, culture, size.Height)
                    };
                    return string.Join(sep, args);
                }
                else if (destinationType == typeof(InstanceDescriptor))
                {
                    ConstructorInfo? ctor = typeof(SizeF).GetConstructor(new Type[] { typeof(float), typeof(float) });
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

        private static readonly string[] s_propertySort = { "Width", "Height" };

        [RequiresUnreferencedCode("The Type of value cannot be statically discovered. " + AttributeCollection.FilterRequiresUnreferencedCodeMessage)]
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext? context, object value, Attribute[]? attributes)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(SizeF), attributes);
            return props.Sort(s_propertySort);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;
    }
}

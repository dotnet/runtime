// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(InstanceDescriptor))
            {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string strValue = value as string;

            if (strValue != null)
            {
                string text = strValue.Trim();

                if (text.Length == 0)
                {
                    return null;
                }
                else
                {
                    // Parse 2 integer values.
                    //
                    if (culture == null)
                    {
                        culture = CultureInfo.CurrentCulture;
                    }
                    char sep = culture.TextInfo.ListSeparator[0];
                    string[] tokens = text.Split(sep);
                    float[] values = new float[tokens.Length];
                    TypeConverter floatConverter = TypeDescriptor.GetConverter(typeof(float));
                    for (int i = 0; i < values.Length; i++)
                    {
                        values[i] = (float)floatConverter.ConvertFromString(context, culture, tokens[i]);
                    }

                    if (values.Length == 2)
                    {
                        return new SizeF(values[0], values[1]);
                    }
                    else
                    {
                        throw new ArgumentException(SR.Format(SR.TextParseFailedFormat, text, "Width,Height"));
                    }
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (destinationType == typeof(string) && value is SizeF)
            {
                SizeF size = (SizeF)value;

                if (culture == null)
                {
                    culture = CultureInfo.CurrentCulture;
                }
                string sep = culture.TextInfo.ListSeparator + " ";
                TypeConverter floatConverter = TypeDescriptor.GetConverter(typeof(float));
                string[] args = new string[2];
                int nArg = 0;

                args[nArg++] = floatConverter.ConvertToString(context, culture, size.Width);
                args[nArg++] = floatConverter.ConvertToString(context, culture, size.Height);

                return string.Join(sep, args);
            }
            
            if (destinationType == typeof(InstanceDescriptor) && value is SizeF)
            {
                SizeF size = (SizeF)value;

                ConstructorInfo ctor = typeof(SizeF).GetConstructor(new Type[] { typeof(float), typeof(float) });
                if (ctor != null)
                {
                    return new InstanceDescriptor(ctor, new object[] { size.Width, size.Height });
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (propertyValues == null)
            {
                throw new ArgumentNullException(nameof(propertyValues));
            }


            object width = propertyValues["Width"];
            object height = propertyValues["Height"];

            if (width == null || height == null ||
                !(width is float) || !(height is float))
            {
                throw new ArgumentException(SR.Format(SR.PropertyValueInvalidEntry));
            }
            return new SizeF((float)width, (float)height);
        }

        public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        private static readonly string[] s_propertySort = { "Width", "Height" };

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(SizeF), attributes);
            return props.Sort(s_propertySort);
        }


        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}
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
    public class SizeConverter : TypeConverter
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
                    int[] values = new int[tokens.Length];
                    TypeConverter intConverter = TypeDescriptor.GetConverter(typeof(int));
                    for (int i = 0; i < values.Length; i++)
                    {
                        // Note: ConvertFromString will raise exception if value cannot be converted.
                        values[i] = (int)intConverter.ConvertFromString(context, culture, tokens[i]);
                    }

                    if (values.Length == 2)
                    {
                        return new Size(values[0], values[1]);
                    }
                    else
                    {
                        throw new ArgumentException(SR.Format(SR.TextParseFailedFormat,
                                                                  text,
                                                                  "Width,Height"));
                    }
                }
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (value is Size)
            {
                if (destinationType == typeof(string))
                {
                    Size size = (Size)value;

                    if (culture == null)
                    {
                        culture = CultureInfo.CurrentCulture;
                    }
                    string sep = culture.TextInfo.ListSeparator + " ";
                    TypeConverter intConverter = TypeDescriptor.GetConverter(typeof(int));
                    string[] args = new string[2];
                    int nArg = 0;

                    // Note: ConvertToString will raise exception if value cannot be converted.
                    args[nArg++] = intConverter.ConvertToString(context, culture, size.Width);
                    args[nArg++] = intConverter.ConvertToString(context, culture, size.Height);

                    return string.Join(sep, args);
                }
                
                if (destinationType == typeof(InstanceDescriptor))
                {
                    Size size = (Size)value;

                    ConstructorInfo ctor = typeof(Size).GetConstructor(new Type[] { typeof(int), typeof(int) });
                    if (ctor != null)
                    {
                        return new InstanceDescriptor(ctor, new object[] { size.Width, size.Height });
                    }
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        [SuppressMessage("Microsoft.Performance", "CA1808:AvoidCallsThatBoxValueTypes")]
        [SuppressMessage("Microsoft.Security", "CA2102:CatchNonClsCompliantExceptionsInGeneralHandlers")]
        public override object CreateInstance(ITypeDescriptorContext context, IDictionary propertyValues)
        {
            if (propertyValues == null)
            {
                throw new ArgumentNullException(nameof(propertyValues));
            }


            object width = propertyValues["Width"];
            object height = propertyValues["Height"];

            if (width == null || height == null ||
                !(width is int) || !(height is int))
            {
                throw new ArgumentException(SR.Format(SR.PropertyValueInvalidEntry));
            }
            return new Size((int)width, (int)height);
        }

        public override bool GetCreateInstanceSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        private static readonly string[] s_propertySort = { "Width", "Height" };

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            PropertyDescriptorCollection props = TypeDescriptor.GetProperties(typeof(Size), attributes);
            return props.Sort(s_propertySort);
        }


        public override bool GetPropertiesSupported(ITypeDescriptorContext context)
        {
            return true;
        }
    }
}
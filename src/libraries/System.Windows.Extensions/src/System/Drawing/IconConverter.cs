// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Globalization;
using System.IO;

namespace System.Drawing
{
    public class IconConverter : ExpandableObjectConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return (sourceType == typeof(byte[]));
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(byte[]) || destinationType == typeof(string)
                || destinationType == typeof(Image) || destinationType == typeof(Bitmap);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return value is byte[] bytes ? new Icon(new MemoryStream(bytes)) : base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value == null)
                {
                    return SR.none;
                }
                else if (value is Icon)
                {
                    return value.ToString();
                }
            }
            else if (destinationType == typeof(byte[]))
            {
                if (value is Icon icon)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        icon.Save(ms);
                        return ms.ToArray();
                    }
                }
            }
            else if (destinationType == typeof(Image) || destinationType == typeof(Bitmap))
            {
                if (value is Icon icon)
                {
                    return icon.ToBitmap();
                }
            }

            throw GetConvertFromException(value);
        }
    }
}

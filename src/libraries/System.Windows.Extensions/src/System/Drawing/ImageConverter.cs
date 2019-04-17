// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;

namespace System.Drawing
{
    public class ImageConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof(byte[]);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof(byte[]) || destinationType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return value is byte[] bytes ? Image.FromStream(new MemoryStream(bytes)) : base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value == null)
                {
                    return SR.none;
                }
                else if (value is Image)
                {
                    return value.ToString();
                }
            }
            else if (destinationType == typeof(byte[]))
            {
                if (value == null)
                {
                    return Array.Empty<byte>();
                }
                else if (value is Image image)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ImageFormat dest = image.RawFormat;
                        // Jpeg loses data, so we don't want to use it to serialize.
                        if (dest == ImageFormat.Jpeg)
                        {
                            dest = ImageFormat.Png;
                        }

                        // If we don't find an Encoder (for things like Icon), we
                        // just switch back to PNG.
                        ImageCodecInfo codec = FindEncoder(dest) ?? FindEncoder(ImageFormat.Png);
                        image.Save(ms, codec, null);
                        return ms.ToArray();
                    }
                }
            }

            throw GetConvertFromException(value);
        }

        // Find any random encoder which supports this format.
        private static ImageCodecInfo FindEncoder(ImageFormat imageformat)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID.Equals(imageformat.Guid))
                    return codec;
            }
            return null;
        }

        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
        {
            return TypeDescriptor.GetProperties(typeof(Image), attributes);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext context) => true;
    }
}

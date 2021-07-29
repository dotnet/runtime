// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Drawing
{
    public class ImageConverter : TypeConverter
    {
        private static ReadOnlySpan<byte> PBrush => new byte[] { (byte)'P', (byte)'B', (byte)'r', (byte)'u', (byte)'s', (byte)'h' };

        private static ReadOnlySpan<byte> BMBytes => new byte[] { (byte)'B', (byte)'M' };

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type? sourceType)
        {
            return sourceType == typeof(byte[]) || sourceType == typeof(Icon);
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return destinationType == typeof(byte[]) || destinationType == typeof(string);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is Icon icon)
            {
                return icon.ToBitmap();
            }

            if (value is byte[] bytes)
            {
                Debug.Assert(value != null, "value is null.");
                // Try to get memory stream for images with ole header.
                Stream memStream = GetBitmapStream(bytes) ?? new MemoryStream(bytes);
                return Image.FromStream(memStream);
            }
            else
            {
                return base.ConvertFrom(context, culture, value);
            }
        }

        public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value == null)
                {
                    return SR.none;
                }
                else if (value is Image)
                {
                    return value.ToString()!;
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
                        ImageCodecInfo codec = FindEncoder(dest) ?? FindEncoder(ImageFormat.Png)!;
                        image.Save(ms, codec, null);
                        return ms.ToArray();
                    }
                }
            }

            throw GetConvertFromException(value);
        }

        // Find any random encoder which supports this format.
        private static ImageCodecInfo? FindEncoder(ImageFormat imageformat)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID.Equals(imageformat.Guid))
                    return codec;
            }
            return null;
        }

        [RequiresUnreferencedCode("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext? context, object? value, Attribute[]? attributes)
        {
            return TypeDescriptor.GetProperties(typeof(Image), attributes);
        }

        public override bool GetPropertiesSupported(ITypeDescriptorContext? context) => true;

        private unsafe Stream? GetBitmapStream(ReadOnlySpan<byte> rawData)
        {
            try
            {
                short signature = BinaryPrimitives.ReadInt16LittleEndian(rawData);

                if (signature != 0x1c15)
                {
                    return null;
                }

                // The data is in the form of OBJECTHEADER. It's an encoded format that Access uses to push images into the DB.
                //
                // The layout of OBJECTHEADER is as follows - we only need the signature
                // and headersize fields, which need to be read as little-endian data:
                //
                //   [StructLayout(LayoutKind.Sequential)]
                //   private struct OBJECTHEADER
                //   {
                //       public short signature; // it's always 0x1c15
                //       public short headersize;
                //       public short objectType;
                //       public short nameLen;
                //       public short classLen;
                //       public short nameOffset;
                //       public short classOffset;
                //       public short width;
                //       public short height;
                //       public IntPtr pInfo;
                //   }
                short headersize = BinaryPrimitives.ReadInt16LittleEndian(rawData.Slice(2, 2));

                // pHeader.signature will always be 0x1c15.
                // "PBrush" should be the 6 chars after position 12 as well.
                if (rawData.Length <= headersize + 18 ||
                    !rawData.Slice(headersize + 12, 6).SequenceEqual(PBrush))
                {
                    return null;
                }

                // We can safely trust that we've got a bitmap.
                // The start of our bitmap data in the rawdata is always 78.
                return new MemoryStream(rawData.Slice(78).ToArray());
            }
            catch (OutOfMemoryException) // This exception may be caused by creating a new MemoryStream.
            {
            }
            catch (ArgumentOutOfRangeException) // This exception may get thrown by MemoryMarshal when input array size is less than the size of the output type.
            {
            }

            return null;
        }
    }
}

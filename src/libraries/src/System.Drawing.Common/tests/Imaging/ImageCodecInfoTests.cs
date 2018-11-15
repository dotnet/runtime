// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Authors:
// 	Jordi Mas i Hernàndez (jordi@ximian.com)
//	Sebastien Pouliot  <sebastien@ximian.com>
//
// (C) 2004 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004-2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Xunit;

namespace System.Drawing.Imaging.Tests
{
    public class ImageCodecInfoTests
    {
        private const string GIF_CSID = "557cf402-1a04-11d3-9a73-0000f81ef32e";
        private const string EMF_CSID = "557cf403-1a04-11d3-9a73-0000f81ef32e";
        private const string BMP_DIB_RLE_CSID = "557cf400-1a04-11d3-9a73-0000f81ef32e";
        private const string JPG_JPEG_JPE_JFIF_CSID = "557cf401-1a04-11d3-9a73-0000f81ef32e";
        private const string PNG_CSID = "557cf406-1a04-11d3-9a73-0000f81ef32e";
        private const string ICO_CSID = "557cf407-1a04-11d3-9a73-0000f81ef32e";
        private const string WMF_CSID = "557cf404-1a04-11d3-9a73-0000f81ef32e";
        private const string TIF_CSID = "557cf405-1a04-11d3-9a73-0000f81ef32e";

        private Hashtable decoders;
        private Hashtable encoders;

        public ImageCodecInfoTests()
        {
            decoders = new Hashtable();
            encoders = new Hashtable();

            foreach (ImageCodecInfo decoder in ImageCodecInfo.GetImageDecoders())
                decoders[decoder.Clsid] = decoder;

            foreach (ImageCodecInfo encoder in ImageCodecInfo.GetImageEncoders())
                encoders[encoder.Clsid] = encoder;
        }

        private ImageCodecInfo GetEncoder(string clsid)
        {
            return (ImageCodecInfo)encoders[new Guid(clsid)];
        }

        private ImageCodecInfo GetDecoder(string clsid)
        {
            return (ImageCodecInfo)decoders[new Guid(clsid)];
        }

        private void CheckDecoderAndEncoder(string clsid, ImageFormat format, string CodecName, string DllName,
            string FilenameExtension, ImageCodecFlags Flags, string FormatDescription,
            string MimeType, int Version, int signatureLength, string mask, string pattern, string pattern2)
        {
            ImageCodecInfo encoder = GetEncoder(clsid);
            ImageCodecInfo decoder = GetDecoder(clsid);

            if (encoder != null)
            {
                CheckImageCodecInfo(format, CodecName, DllName, FilenameExtension, Flags, FormatDescription, MimeType, signatureLength, mask, pattern, pattern2, encoder);
            }
            if (decoder != null)
            {
                CheckImageCodecInfo(format, CodecName, DllName, FilenameExtension, Flags, FormatDescription, MimeType, signatureLength, mask, pattern, pattern2, decoder);
            }
        }

        private void CheckImageCodecInfo(ImageFormat format, string CodecName, string DllName, string FilenameExtension, ImageCodecFlags Flags, string FormatDescription, string MimeType, int signatureLength, string mask, string pattern, string pattern2, ImageCodecInfo codecInfo)
        {
            Regex extRegex = new Regex(@"^(\*\.\w+(;(\*\.\w+))*;)?" +
                Regex.Escape(FilenameExtension) + @"(;\*\.\w+(;(\*\.\w+))*)?$",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            Assert.Equal(format.Guid, codecInfo.FormatID);
            Assert.Contains(CodecName, codecInfo.CodecName);
            Assert.Equal(DllName, codecInfo.DllName);
            Assert.True(extRegex.IsMatch(codecInfo.FilenameExtension));
            Assert.Equal(Flags, codecInfo.Flags);
            Assert.Contains(FormatDescription, codecInfo.FormatDescription);
            Assert.Contains(MimeType, codecInfo.MimeType);
            Assert.Equal(signatureLength, codecInfo.SignatureMasks.Length);

            for (int i = 0; i < signatureLength; i++)
            {
                Assert.Equal(mask, BitConverter.ToString(codecInfo.SignatureMasks[i]));
            }

            Assert.Equal(signatureLength, codecInfo.SignaturePatterns.Length);
            Assert.Equal(pattern, BitConverter.ToString(codecInfo.SignaturePatterns[0]));
            if (pattern2 != null)
                Assert.Equal(pattern2, BitConverter.ToString(codecInfo.SignaturePatterns[1]));
        }

        public static IEnumerable<object[]> CodecInfoTestData
        {
            get
            {
                yield return new object[] { WMF_CSID, ImageFormat.Wmf,
                "WMF", null, "*.WMF",
                ImageCodecFlags.Builtin | ImageCodecFlags.Decoder | ImageCodecFlags.SupportBitmap,
                "WMF", "image/x-wmf", 1, 1, "FF-FF-FF-FF", "D7-CD-C6-9A", null};

                yield return new object[] { EMF_CSID, ImageFormat.Emf,
                "EMF", null, "*.EMF",
                ImageCodecFlags.Builtin | ImageCodecFlags.Decoder | ImageCodecFlags.SupportBitmap,
                "EMF", "image/x-emf", 1, 1, "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-FF-FF-FF-FF",
                "00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-00-20-45-4D-46", null};

                yield return new object[] { ICO_CSID, ImageFormat.Icon,
                "ICO", null, "*.ICO",
                ImageCodecFlags.Builtin | ImageCodecFlags.Decoder | ImageCodecFlags.SupportBitmap,
                "ICO", "image/x-icon", 1, 1, "FF-FF-FF-FF", "00-00-01-00", null};

                yield return new object[] { TIF_CSID, ImageFormat.Tiff,
                "TIFF", null, "*.TIF;*.TIFF",
                ImageCodecFlags.Builtin | ImageCodecFlags.Encoder | ImageCodecFlags.Decoder | ImageCodecFlags.SupportBitmap,
                "TIFF", "image/tiff", 1, 2, "FF-FF", "49-49", "4D-4D" };

                yield return new object[] { PNG_CSID, ImageFormat.Png,
                "PNG", null, "*.PNG",
                ImageCodecFlags.Builtin | ImageCodecFlags.Encoder | ImageCodecFlags.Decoder | ImageCodecFlags.SupportBitmap,
                "PNG", "image/png", 1, 1, "FF-FF-FF-FF-FF-FF-FF-FF", "89-50-4E-47-0D-0A-1A-0A", null };

                yield return new object[] { JPG_JPEG_JPE_JFIF_CSID, ImageFormat.Jpeg,
                "JPEG", null, "*.JPG",
                ImageCodecFlags.Builtin | ImageCodecFlags.Encoder | ImageCodecFlags.Decoder | ImageCodecFlags.SupportBitmap,
                "JPEG", "image/jpeg", 1, 1, "FF-FF", "FF-D8", null};

                yield return new object[] { GIF_CSID, ImageFormat.Gif,
                "GIF", null, "*.GIF",
                ImageCodecFlags.Builtin | ImageCodecFlags.Encoder | ImageCodecFlags.Decoder | ImageCodecFlags.SupportBitmap,
                "GIF", "image/gif", 1, 2, "FF-FF-FF-FF-FF-FF", "47-49-46-38-39-61", "47-49-46-38-37-61"};

                yield return new object[] { BMP_DIB_RLE_CSID, ImageFormat.Bmp,
                "BMP", null, "*.BMP",
                ImageCodecFlags.Builtin | ImageCodecFlags.Encoder | ImageCodecFlags.Decoder | ImageCodecFlags.SupportBitmap,
                "BMP", "image/bmp", 1, 1, "FF-FF", "42-4D", null };
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(GIF_CSID)]
        [InlineData(EMF_CSID)]
        [InlineData(BMP_DIB_RLE_CSID)]
        [InlineData(JPG_JPEG_JPE_JFIF_CSID)]
        [InlineData(PNG_CSID)]
        [InlineData(ICO_CSID)]
        [InlineData(WMF_CSID)]
        [InlineData(TIF_CSID)]
        public void GetDecoder_Success(string csid)
        {
            Assert.NotNull(GetDecoder(csid));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(GIF_CSID)]
        [InlineData(BMP_DIB_RLE_CSID)]
        [InlineData(JPG_JPEG_JPE_JFIF_CSID)]
        [InlineData(PNG_CSID)]
        [InlineData(TIF_CSID)]
        public void GetEncoder_Success(string csid)
        {
            Assert.NotNull(GetEncoder(csid));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void CountEncoders_ReturnsExcpected()
        {
            Assert.Equal(5, encoders.Count);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void CountDecoders_ReturnsExcpected()
        {
            Assert.Equal(8, decoders.Count);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(CodecInfoTestData))]
        public void CheckDecoderAndEncoder_ReturnsExpecetd(string clsid, ImageFormat format, string codecName, string dllName,
            string fileNameExtension, ImageCodecFlags flags, string formatDescription,
            string mimeType, int version, int signatureLength, string mask, string pattern, string pattern2)
        {
            CheckDecoderAndEncoder(clsid, format, codecName, dllName, fileNameExtension, flags, formatDescription, mimeType, version, signatureLength, mask, pattern, pattern2);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(WMF_CSID)]
        [InlineData(EMF_CSID)]
        [InlineData(ICO_CSID)]
        public void GetEncoder_NoSuchEncoding_ReturnsNull(string clsid)
        {
            Assert.Null(GetEncoder(clsid));
        }
    }
}

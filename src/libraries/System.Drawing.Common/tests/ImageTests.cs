// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Drawing.Tests
{
    public class ImageTests
    {
        public static IEnumerable<object[]> InvalidBytes_TestData()
        {
            // IconTests.Ctor_InvalidBytesInStream_TestData an array of 2 objects, but this test only uses the
            // 1st object.
            foreach (object[] data in IconTests.Ctor_InvalidBytesInStream_TestData())
            {
                yield return new object[] { data[0] };
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(InvalidBytes_TestData))]
        public void FromFile_InvalidBytes_ThrowsOutOfMemoryException(byte[] bytes)
        {
            using (var file = TempFile.Create(bytes))
            {
                Assert.Throws<OutOfMemoryException>(() => Image.FromFile(file.Path));
                Assert.Throws<OutOfMemoryException>(() => Image.FromFile(file.Path, useEmbeddedColorManagement: true));
            }
        }

        [Fact]
        public void FromFile_NullFileName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => Image.FromFile(null));
            AssertExtensions.Throws<ArgumentNullException>("path", () => Image.FromFile(null, useEmbeddedColorManagement: true));
        }

        [Fact]
        public void FromFile_EmptyFileName_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () => Image.FromFile(string.Empty));
            AssertExtensions.Throws<ArgumentException>("path", null, () => Image.FromFile(string.Empty, useEmbeddedColorManagement: true));
        }

        [Fact]
        public void FromFile_LongSegment_ThrowsException()
        {
            // Throws PathTooLongException on Desktop and FileNotFoundException elsewhere.
            if (PlatformDetection.IsFullFramework)
            {
                string fileName = new string('a', 261);

                Assert.Throws<PathTooLongException>(() => Image.FromFile(fileName));
                Assert.Throws<PathTooLongException>(() => Image.FromFile(fileName,
                    useEmbeddedColorManagement: true));
            }
            else
            {
                string fileName = new string('a', 261);

                Assert.Throws<FileNotFoundException>(() => Image.FromFile(fileName));
                Assert.Throws<FileNotFoundException>(() => Image.FromFile(fileName,
                    useEmbeddedColorManagement: true));
            }
        }

        [Fact]
        public void FromFile_NoSuchFile_ThrowsFileNotFoundException()
        {
            Assert.Throws<FileNotFoundException>(() => Image.FromFile("NoSuchFile"));
            Assert.Throws<FileNotFoundException>(() => Image.FromFile("NoSuchFile", useEmbeddedColorManagement: true));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(InvalidBytes_TestData))]
        public void FromStream_InvalidBytes_ThrowsArgumentException(byte[] bytes)
        {
            using (var stream = new MemoryStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Position = 0;

                AssertExtensions.Throws<ArgumentException>(null, () => Image.FromStream(stream));
                Assert.Equal(0, stream.Position);

                AssertExtensions.Throws<ArgumentException>(null, () => Image.FromStream(stream, useEmbeddedColorManagement: true));
                AssertExtensions.Throws<ArgumentException>(null, () => Image.FromStream(stream, useEmbeddedColorManagement: true, validateImageData: true));
                Assert.Equal(0, stream.Position);
            }
        }

        [Fact]
        public void FromStream_NullStream_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException, ArgumentException>("stream", null, () => Image.FromStream(null));
            AssertExtensions.Throws<ArgumentNullException, ArgumentException>("stream", null, () => Image.FromStream(null, useEmbeddedColorManagement: true));
            AssertExtensions.Throws<ArgumentNullException, ArgumentException>("stream", null, () => Image.FromStream(null, useEmbeddedColorManagement: true, validateImageData: true));
        }

        [Theory]
        [InlineData(PixelFormat.Format1bppIndexed, 1)]
        [InlineData(PixelFormat.Format4bppIndexed, 4)]
        [InlineData(PixelFormat.Format8bppIndexed, 8)]
        [InlineData(PixelFormat.Format16bppArgb1555, 16)]
        [InlineData(PixelFormat.Format16bppGrayScale, 16)]
        [InlineData(PixelFormat.Format16bppRgb555, 16)]
        [InlineData(PixelFormat.Format16bppRgb565, 16)]
        [InlineData(PixelFormat.Format24bppRgb, 24)]
        [InlineData(PixelFormat.Format32bppArgb, 32)]
        [InlineData(PixelFormat.Format32bppPArgb, 32)]
        [InlineData(PixelFormat.Format32bppRgb, 32)]
        [InlineData(PixelFormat.Format48bppRgb, 48)]
        [InlineData(PixelFormat.Format64bppArgb, 64)]
        [InlineData(PixelFormat.Format64bppPArgb, 64)]
        public void GetPixelFormatSize_ReturnsExpected(PixelFormat format, int expectedSize)
        {
            Assert.Equal(expectedSize, Image.GetPixelFormatSize(format));
        }

        [Theory]
        [InlineData(PixelFormat.Format16bppArgb1555, true)]
        [InlineData(PixelFormat.Format32bppArgb, true)]
        [InlineData(PixelFormat.Format32bppPArgb, true)]
        [InlineData(PixelFormat.Format64bppArgb, true)]
        [InlineData(PixelFormat.Format64bppPArgb, true)]
        [InlineData(PixelFormat.Format16bppGrayScale, false)]
        [InlineData(PixelFormat.Format16bppRgb555, false)]
        [InlineData(PixelFormat.Format16bppRgb565, false)]
        [InlineData(PixelFormat.Format1bppIndexed, false)]
        [InlineData(PixelFormat.Format24bppRgb, false)]
        [InlineData(PixelFormat.Format32bppRgb, false)]
        [InlineData(PixelFormat.Format48bppRgb, false)]
        [InlineData(PixelFormat.Format4bppIndexed, false)]
        [InlineData(PixelFormat.Format8bppIndexed, false)]
        public void IsAlphaPixelFormat_ReturnsExpected(PixelFormat format, bool expected)
        {
            Assert.Equal(expected, Image.IsAlphaPixelFormat(format));
        }

        public static IEnumerable<object[]> GetEncoderParameterList_ReturnsExpected_TestData()
        {
            yield return new object[]
            {
                ImageFormat.Tiff,
                new Guid[]
                {
                    Encoder.Compression.Guid,
                    Encoder.ColorDepth.Guid,
                    Encoder.SaveFlag.Guid,
                    new Guid(unchecked((int)0xa219bbc9), unchecked((short)0x0a9d), unchecked((short)0x4005), new byte[] { 0xa3, 0xee, 0x3a, 0x42, 0x1b, 0x8b, 0xb0, 0x6c }) /* Encoder.SaveAsCmyk.Guid */
                }
            };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(GetEncoderParameterList_ReturnsExpected_TestData))]
        public void GetEncoderParameterList_ReturnsExpected(ImageFormat format, Guid[] expectedParameters)
        {
            if (PlatformDetection.IsFullFramework)
            {
                throw new SkipTestException("This is a known bug for .NET Framework");
            }

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
            ImageCodecInfo codec = codecs.Single(c => c.FormatID == format.Guid);

            using (var bitmap = new Bitmap(1, 1))
            {
                EncoderParameters paramList = bitmap.GetEncoderParameterList(codec.Clsid);

                Assert.Equal(
                    expectedParameters,
                    paramList.Param.Select(p => p.Encoder.Guid));
            }
        }
    }
}

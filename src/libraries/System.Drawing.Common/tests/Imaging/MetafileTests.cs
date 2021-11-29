// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Copyright (C) 2005-2006 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Drawing.Imaging.Tests
{
    public class MetafileTests
    {
        private const string WmfFile = "telescope_01.wmf";
        private const string BmpFile = "bitmap_173x183_indexed_8bit.bmp";
        private readonly Rectangle _rectangle = new Rectangle(0, 0, 64, 64);
        private readonly RectangleF _rectangleF = new RectangleF(0, 0, 64, 64);

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_IntPtrZero_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(IntPtr.Zero, false));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_IntPtrToWmf_ThrowsExternalException()
        {
            using (var metafile = new Metafile(GetPath(WmfFile)))
            {
                Assert.Throws<ExternalException>(() => new Metafile(metafile.GetHenhmetafile(), false));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_String_Success()
        {
            using (var metafile = new Metafile(GetPath(WmfFile)))
            {
                AssertMetafile(metafile);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Bitmap_ThrowsExternalException()
        {
            Assert.Throws<ExternalException>(() => new Metafile(GetPath(BmpFile)));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_NullString_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => new Metafile((string)null));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_InvalidPath_ThrowsExternalException()
        {
            Assert.Throws<ExternalException>(() => new Metafile("fileNotExist"));
        }

        public static IEnumerable<object[]> InvalidPath_TestData()
        {
            yield return new object[] { new string('a', 261) };
            yield return new object[] { @"fileNo*-//\\#@(found" };
            yield return new object[] { string.Empty };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData("bad\0name")]
        [InlineData("")]
        public void Ctor_InvalidPath_ThrowsArgumentException(string path)
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () => new Metafile(path));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34591", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void Ctor_Stream_Success()
        {
            using (FileStream stream = File.OpenRead(GetPath(WmfFile)))
            using (var metafile = new Metafile(stream))
            {
                AssertMetafile(metafile);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_NullStream_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentNullException, ArgumentException>("stream", null, () => new Metafile((Stream)null));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34591", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void Ctor_EmptyStream_ThrowsExternalException()
        {
            using (var stream = new MemoryStream())
            {
                Assert.Throws<ExternalException>(() => new Metafile(stream));
            }
        }

        public static IEnumerable<object[]> EmfType_TestData()
        {
            yield return new object[] { EmfType.EmfOnly };
            yield return new object[] { EmfType.EmfPlusDual };
            yield return new object[] { EmfType.EmfPlusOnly };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(EmfType_TestData))]
        public void Ctor_IntPtrEmfType_Success(EmfType emfType)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(g.GetHdc(), emfType))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), emfType);
            }
        }

        public static IEnumerable<object[]> EmfType_Invalid_TestData()
        {
            yield return new object[] { (EmfType.EmfOnly - 1) };
            yield return new object[] { (EmfType.EmfPlusDual + 1) };
            yield return new object[] { (EmfType)int.MaxValue };
            yield return new object[] { (EmfType)int.MinValue };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(EmfType_Invalid_TestData))]
        public void Ctor_IntPtrInvalidEmfType_ThrowsArgumentException(EmfType emfType)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(g.GetHdc(), emfType));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_NullEmfType_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile((IntPtr)null, EmfType.EmfOnly));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_ZeroPointerEmfType_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(IntPtr.Zero, EmfType.EmfOnly));
        }

        public static IEnumerable<object[]> Description_TestData()
        {
            yield return new object[] { null };
            yield return new object[] { "description" };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Description_TestData))]
        public void Ctor_IntPtrEmfTypeString_Success(string description)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(g.GetHdc(), EmfType.EmfOnly, description))
            {
                AssertMetafileIsBlank(metafile);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(InvalidPath_TestData))]
        public void Ctor_ZeroPointerEmfTypeInvalidString_ThrowsArgumentException(string description)
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(IntPtr.Zero, EmfType.EmfOnly, description));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_IntPtrRectangleF_Success()
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(g.GetHdc(), _rectangleF))
            {
                AssertMetafileIsBlank(metafile);
            }
        }

        public static IEnumerable<object[]> MetafileFrameUnit_TestData()
        {
            yield return new object[] { MetafileFrameUnit.Pixel };
            yield return new object[] { MetafileFrameUnit.Point };
            yield return new object[] { MetafileFrameUnit.Inch };
            yield return new object[] { MetafileFrameUnit.Document };
            yield return new object[] { MetafileFrameUnit.Millimeter };
            yield return new object[] { MetafileFrameUnit.GdiCompatible };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(MetafileFrameUnit_TestData))]
        public void Ctor_IntPtrRectangleFMetafileFrameUnit_Success(MetafileFrameUnit frameUnit)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(g.GetHdc(), _rectangleF, frameUnit))
            {
                AssertMetafileIsBlank(metafile);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(EmfType_TestData))]
        public void Ctor_IntPtrRectangleFMetafileFrameUnitEmfType_Success(EmfType emfType)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(g.GetHdc(), _rectangleF, MetafileFrameUnit.Pixel, emfType))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), emfType);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Description_TestData))]
        public void Ctor_IntPtrRectangleFMetafileFrameUnitEmfTypeString_Success(string description)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(g.GetHdc(), _rectangleF, MetafileFrameUnit.Pixel, EmfType.EmfOnly, description))
            {
                AssertMetafileIsBlank(metafile);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_IntPtrRectangle_Success()
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(g.GetHdc(), _rectangle))
            {
                AssertMetafileIsBlank(metafile);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(MetafileFrameUnit_TestData))]
        public void Ctor_IntPtrRectangleMetafileFrameUnit_Success(MetafileFrameUnit frameUnit)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(g.GetHdc(), _rectangle, frameUnit))
            {
                AssertMetafileIsBlank(metafile);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(EmfType_TestData))]
        public void Ctor_IntPtrRectangleMetafileFrameUnitEmfType_Success(EmfType emfType)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(g.GetHdc(), _rectangle, MetafileFrameUnit.Pixel, emfType))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), emfType);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Description_TestData))]
        public void Ctor_IntPtrRectangleMetafileFrameUnitEmfTypeString_Success(string description)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(g.GetHdc(), _rectangle, MetafileFrameUnit.Pixel, EmfType.EmfOnly, description))
            {
                AssertMetafileIsBlank(metafile);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_IntPtrZeroI_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(IntPtr.Zero, _rectangleF));
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(IntPtr.Zero, _rectangleF, MetafileFrameUnit.Pixel));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                new Metafile(IntPtr.Zero, _rectangleF, MetafileFrameUnit.Pixel, EmfType.EmfOnly));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                new Metafile(IntPtr.Zero, _rectangleF, MetafileFrameUnit.Pixel, EmfType.EmfOnly, "description"));

            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(IntPtr.Zero, _rectangle));
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(IntPtr.Zero, _rectangle, MetafileFrameUnit.Pixel));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                new Metafile(IntPtr.Zero, _rectangle, MetafileFrameUnit.Pixel, EmfType.EmfOnly));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                new Metafile(IntPtr.Zero, _rectangle, MetafileFrameUnit.Pixel, EmfType.EmfOnly, "description"));
        }

        public static IEnumerable<object[]> MetafileFrameUnit_Invalid_TestData()
        {
            yield return new object[] { (MetafileFrameUnit.Pixel - 1) };
            yield return new object[] { (MetafileFrameUnit.GdiCompatible + 1) };
            yield return new object[] { (MetafileFrameUnit)int.MaxValue };
            yield return new object[] { (MetafileFrameUnit)int.MinValue };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(MetafileFrameUnit_Invalid_TestData))]
        public void Ctor_InvalidMetafileFrameUnit_ThrowsArgumentException(MetafileFrameUnit farameUnit)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(referenceHdc, _rectangleF, farameUnit));
                AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(referenceHdc, _rectangleF, farameUnit, EmfType.EmfOnly));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(referenceHdc, _rectangleF, farameUnit, EmfType.EmfOnly, "description"));

                AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(referenceHdc, _rectangle, farameUnit));
                AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(referenceHdc, _rectangle, farameUnit, EmfType.EmfOnly));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(referenceHdc, _rectangle, farameUnit, EmfType.EmfOnly, "description"));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(EmfType_Invalid_TestData))]
        public void Ctor_InvalidEmfType_ThrowsArgumentException(EmfType emfType)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible, emfType));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible, emfType, "description"));

                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(referenceHdc, _rectangle, MetafileFrameUnit.GdiCompatible, emfType));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(referenceHdc, _rectangle, MetafileFrameUnit.GdiCompatible, emfType, "description"));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_StringIntPtr_Success()
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(fileName, g.GetHdc()))
            {
                AssertMetafileIsBlank(metafile);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(EmfType_TestData))]
        public void Ctor_StringIntPtrEmfType_Success(EmfType emfType)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(fileName, g.GetHdc(), emfType))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), emfType);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Description_TestData))]
        public void Ctor_StringIntPtrEmfTypeDescription_Success(string description)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(fileName, g.GetHdc(), EmfType.EmfPlusDual, description))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), EmfType.EmfPlusDual);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_IntPtrZeroII_ThrowsArgumentException()
        {
            string fileName = GetPath("newTestImage.wmf");
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(fileName, IntPtr.Zero));
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(fileName, IntPtr.Zero, EmfType.EmfOnly));
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(fileName, IntPtr.Zero, EmfType.EmfOnly, "description"));
            DeleteFile(fileName);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(EmfType_Invalid_TestData))]
        public void Ctor_InvalidEmfTypeI_ThrowsArgumentException(EmfType emfType)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(fileName, referenceHdc, emfType));
                AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(fileName, referenceHdc, emfType, "description"));
                DeleteFile(fileName);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_NullPath_ThrowsArgumentNullException()
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                AssertExtensions.Throws<ArgumentNullException>("path", () => new Metafile((string)null, referenceHdc));
                AssertExtensions.Throws<ArgumentNullException>("path", () => new Metafile((string)null, referenceHdc, EmfType.EmfOnly));
                AssertExtensions.Throws<ArgumentNullException>("path", () => new Metafile((string)null, referenceHdc, EmfType.EmfOnly, "description"));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData("bad\0path")]
        [InlineData("")]
        public void Ctor_InvalidPathI_ThrowsArgumentException(string fileName)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                AssertExtensions.Throws<ArgumentException>("path", null, () => new Metafile(fileName, referenceHdc));
                AssertExtensions.Throws<ArgumentException>("path", null, () => new Metafile(fileName, referenceHdc, EmfType.EmfOnly));
                AssertExtensions.Throws<ArgumentException>("path", null, () => new Metafile(fileName, referenceHdc, EmfType.EmfOnly, "description"));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_PathTooLong_ThrowsPathTooLongException()
        {
            string fileName = GetPath(new string('a', short.MaxValue));
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                Assert.Throws<PathTooLongException>(() => new Metafile(fileName, referenceHdc));
                Assert.Throws<PathTooLongException>(() => new Metafile(fileName, referenceHdc, EmfType.EmfOnly));
                Assert.Throws<PathTooLongException>(() => new Metafile(fileName, referenceHdc, EmfType.EmfOnly, "description"));
                DeleteFile(fileName);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_StringIntPtrRectangleF_Success()
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(fileName, g.GetHdc(), _rectangleF))
            {
                AssertMetafileIsBlank(metafile);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(MetafileFrameUnit_TestData))]
        public void Ctor_StringIntPtrRectangleFMetafileFrameUnit_Success(MetafileFrameUnit frameUnit)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(fileName, g.GetHdc(), _rectangleF, frameUnit))
            {
                AssertMetafileIsBlank(metafile);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(EmfType_TestData))]
        public void Ctor_StringIntPtrRectangleFMetafileFrameUnitEmfType_Success(EmfType emfType)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(fileName, g.GetHdc(), _rectangleF, MetafileFrameUnit.GdiCompatible, emfType))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), emfType);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Description_TestData))]
        public void Ctor_StringIntPtrRectangleFMetafileFrameUnitEmfTypeString_Success(string description)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(
                fileName, g.GetHdc(), _rectangleF, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, description))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), EmfType.EmfOnly);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Description_TestData))]
        public void Ctor_RectangleFEmpty_Success(string description)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(
                fileName, g.GetHdc(), new RectangleF(), MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, description))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), EmfType.EmfOnly);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_StringIntPtrRectangle_Success()
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(fileName, g.GetHdc(), _rectangle))
            {
                AssertMetafileIsBlank(metafile);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(MetafileFrameUnit_TestData))]
        public void Ctor_StringIntPtrRectangleMetafileFrameUnit_Success(MetafileFrameUnit frameUnit)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(fileName, g.GetHdc(), _rectangle, frameUnit))
            {
                AssertMetafileIsBlank(metafile);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(EmfType_TestData))]
        public void Ctor_StringIntPtrRectangleMetafileFrameUnitEmfType_Success(EmfType emfType)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(fileName, g.GetHdc(), _rectangle, MetafileFrameUnit.GdiCompatible, emfType))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), emfType);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Description_TestData))]
        public void Ctor_StringIntPtrRectangleMetafileFrameUnitEmfTypeString_Success(string description)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(
                fileName, g.GetHdc(), _rectangle, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, description))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), EmfType.EmfOnly);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Description_TestData))]
        public void Ctor_RectangleEmpty_Success(string description)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var metafile = new Metafile(
                fileName, g.GetHdc(), new Rectangle(), MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, description))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), EmfType.EmfOnly);
                Assert.True(File.Exists(fileName));
            }

            File.Delete(fileName);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_IntPtrZeroIII_ThrowsArgumentException()
        {
            string fileName = GetPath("newTestImage.wmf");
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(fileName, IntPtr.Zero, _rectangleF));
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(fileName, IntPtr.Zero, _rectangleF, MetafileFrameUnit.GdiCompatible));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                new Metafile(fileName, IntPtr.Zero, _rectangleF, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                new Metafile(fileName, IntPtr.Zero, _rectangleF, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, "description"));

            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(fileName, IntPtr.Zero, _rectangle));
            AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(fileName, IntPtr.Zero, _rectangle, MetafileFrameUnit.GdiCompatible));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                new Metafile(fileName, IntPtr.Zero, _rectangle, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly));
            AssertExtensions.Throws<ArgumentException>(null, () =>
                new Metafile(fileName, IntPtr.Zero, _rectangle, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, "description"));
            DeleteFile(fileName);
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(MetafileFrameUnit_Invalid_TestData))]
        public void Ctor_InvalidFrameUnit_ThrowsArgumentException(MetafileFrameUnit frameUnit)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(fileName, referenceHdc, _rectangleF, frameUnit));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(fileName, referenceHdc, _rectangleF, frameUnit, EmfType.EmfOnly));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(fileName, referenceHdc, _rectangleF, frameUnit, EmfType.EmfOnly, "description"));

                AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(fileName, referenceHdc, _rectangle, frameUnit));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(fileName, referenceHdc, _rectangle, frameUnit, EmfType.EmfOnly));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(fileName, referenceHdc, _rectangle, frameUnit, EmfType.EmfOnly, "description"));
                DeleteFile(fileName);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(EmfType_Invalid_TestData))]
        public void Ctor_InvalidEmfTypeII_ThrowsArgumentException(EmfType emfType)
        {
            string fileName = GetPath("newTestImage.wmf");
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(fileName, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible, emfType));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(fileName, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible, emfType, "description"));

                AssertExtensions.Throws<ArgumentException>(null, () =>
                   new Metafile(fileName, referenceHdc, _rectangle, MetafileFrameUnit.GdiCompatible, emfType));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(fileName, referenceHdc, _rectangle, MetafileFrameUnit.GdiCompatible, emfType, "description"));
                DeleteFile(fileName);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_NullPathI_ThrowsArgumentNullException()
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                AssertExtensions.Throws<ArgumentNullException>("path", () => new Metafile((string)null, referenceHdc, _rectangleF));
                AssertExtensions.Throws<ArgumentNullException>("path", () =>
                    new Metafile((string)null, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible));
                AssertExtensions.Throws<ArgumentNullException>("path", () =>
                    new Metafile((string)null, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly));
                AssertExtensions.Throws<ArgumentNullException>("path", () =>
                    new Metafile((string)null, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, "description"));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData("bad\0path")]
        [InlineData("")]
        public void Ctor_InvalidPathII_ThrowsArgumentException(string fileName)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                AssertExtensions.Throws<ArgumentException, ArgumentException>("path", null, () => new Metafile(fileName, referenceHdc, _rectangleF));
                AssertExtensions.Throws<ArgumentException, ArgumentException>("path", null, () =>
                    new Metafile(fileName, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible));
                AssertExtensions.Throws<ArgumentException, ArgumentException>("path", null, () =>
                    new Metafile(fileName, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly));
                AssertExtensions.Throws<ArgumentException, ArgumentException>("path", null, () =>
                    new Metafile(fileName, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, "description"));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_PathTooLongI_ThrowsPathTooLongException()
        {
            string fileName = GetPath(new string('a', 261));
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                Assert.Throws<PathTooLongException>(() => new Metafile(fileName, referenceHdc, _rectangleF));
                Assert.Throws<PathTooLongException>(() =>
                    new Metafile(fileName, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible));
                Assert.Throws<PathTooLongException>(() =>
                    new Metafile(fileName, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly));
                Assert.Throws<PathTooLongException>(() =>
                    new Metafile(fileName, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, "description"));
                DeleteFile(fileName);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34591", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void Ctor_StreamIntPtrRectangle_Success()
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var stream = new MemoryStream())
            using (var metafile = new Metafile(stream, g.GetHdc(), _rectangle))
            {
                AssertMetafileIsBlank(metafile);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34591", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(MetafileFrameUnit_TestData))]
        public void Ctor_StreamIntPtrRectangleMetafileFrameUnit_Success(MetafileFrameUnit frameUnit)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var stream = new MemoryStream())
            using (var metafile = new Metafile(stream, g.GetHdc(), _rectangle, frameUnit))
            {
                AssertMetafileIsBlank(metafile);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34591", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(EmfType_TestData))]
        public void Ctor_StreamIntPtrRectangleMetafileFrameUnitEmfType_Success(EmfType emfType)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var stream = new MemoryStream())
            using (var metafile = new Metafile(stream, g.GetHdc(), _rectangle, MetafileFrameUnit.GdiCompatible, emfType))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), emfType);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34591", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(Description_TestData))]
        public void Ctor_StreamIntPtrRectangleMetafileFrameUnitEmfTypeString_Success(string description)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var stream = new MemoryStream())
            using (var metafile = new Metafile(
                stream, g.GetHdc(), _rectangle, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, description))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), EmfType.EmfOnly);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34591", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [MemberData(nameof(Description_TestData))]
        public void Ctor_RectangleEmptyI_Success(string description)
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            using (var stream = new MemoryStream())
            using (var metafile = new Metafile(
                stream, g.GetHdc(), new Rectangle(), MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, description))
            {
                AssertMetafileIsBlank(metafile);
                AssertEmfType(metafile.GetMetafileHeader(), EmfType.EmfOnly);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/55655", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_IntPtrZeroIV_ThrowsArgumentException()
        {
            using (var stream = new MemoryStream())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(stream, IntPtr.Zero, _rectangle));
                AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(stream, IntPtr.Zero, _rectangle, MetafileFrameUnit.GdiCompatible));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(stream, IntPtr.Zero, _rectangle, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(stream, IntPtr.Zero, _rectangle, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, "description"));
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/55655", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(MetafileFrameUnit_Invalid_TestData))]
        public void Ctor_InvalidFrameUnitIII_ThrowsArgumentException(MetafileFrameUnit frameUnit)
        {
            using (var stream = new MemoryStream())
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                AssertExtensions.Throws<ArgumentException>(null, () => new Metafile(stream, referenceHdc, _rectangle, frameUnit));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(stream, referenceHdc, _rectangle, frameUnit, EmfType.EmfOnly));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(stream, referenceHdc, _rectangle, frameUnit, EmfType.EmfOnly, "description"));
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/55655", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(EmfType_Invalid_TestData))]
        public void Ctor_InvalidEmfTypeIII_ThrowsArgumentException(EmfType emfType)
        {
            using (var stream = new MemoryStream())
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                AssertExtensions.Throws<ArgumentException>(null, () =>
                   new Metafile(stream, referenceHdc, _rectangle, MetafileFrameUnit.GdiCompatible, emfType));
                AssertExtensions.Throws<ArgumentException>(null, () =>
                    new Metafile(stream, referenceHdc, _rectangle, MetafileFrameUnit.GdiCompatible, emfType, "description"));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_NullStream_ThrowsNullReferenceException()
        {
            using (Bitmap bmp = new Bitmap(10, 10, PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr referenceHdc = g.GetHdc();
                Assert.Throws<NullReferenceException>(() => new Metafile((Stream)null, referenceHdc, _rectangleF));
                Assert.Throws<NullReferenceException>(() => new Metafile((Stream)null, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible));
                Assert.Throws<NullReferenceException>(() =>
                    new Metafile((Stream)null, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly));
                Assert.Throws<NullReferenceException>(() =>
                    new Metafile((Stream)null, referenceHdc, _rectangleF, MetafileFrameUnit.GdiCompatible, EmfType.EmfOnly, "description"));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Static_GetMetafileHeader_String_ReturnsExpected()
        {
            MetafileHeader header = Metafile.GetMetafileHeader(GetPath(WmfFile));
            AssertMetafileHeader(header);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Static_GetMetafileHeader_IntPtr_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Metafile.GetMetafileHeader(IntPtr.Zero));
            using (var metafile = new Metafile(GetPath(WmfFile)))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => Metafile.GetMetafileHeader(metafile.GetHenhmetafile()));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData("bad\0path")]
        [InlineData("")]
        public void Static_GetMetafileHeader_InvalidPath_ThrowsArgumentException(string fileName)
        {
            AssertExtensions.Throws<ArgumentException>("path", null, () => Metafile.GetMetafileHeader(fileName));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Static_GetMetafileHeader_NullString_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => Metafile.GetMetafileHeader((string)null));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34591", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void Static_GetMetafileHeader_Stream_ReturnsExpected()
        {
            using (FileStream stream = File.OpenRead(GetPath(WmfFile)))
            {
                MetafileHeader header = Metafile.GetMetafileHeader(stream);
                AssertMetafileHeader(header);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Static_GetMetafileHeader_NullStream_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => Metafile.GetMetafileHeader((Stream)null));
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34591", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
        public void Static_GetMetafileHeader_EmptyStream_ArgumentException()
        {
            using (var stream = new MemoryStream())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => Metafile.GetMetafileHeader(stream));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetMetafileHeader_ReturnsExpected()
        {
            using (var metafile = new Metafile(GetPath(WmfFile)))
            {
                MetafileHeader headerA = metafile.GetMetafileHeader();
                MetafileHeader headerB = metafile.GetMetafileHeader();
                AssertMetafileHeader(headerA);
                Assert.NotSame(headerA, headerB);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetMetafileHeader_Disposed_ThrowsArgumentException()
        {
            var metafile = new Metafile(GetPath(WmfFile));
            metafile.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => metafile.GetMetafileHeader());
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetHenhmetafile_ReturnsExpected()
        {
            using (var metafile = new Metafile(GetPath(WmfFile)))
            {
                Assert.NotEqual(IntPtr.Zero, metafile.GetHenhmetafile());
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetHenhmetafile_Disposed_ThrowsArgumentException()
        {
            var metafile = new Metafile(GetPath(WmfFile));
            metafile.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => metafile.GetHenhmetafile());
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void PlayRecord_Disposed_ThrowsArgumentException()
        {
            var metafile = new Metafile(GetPath(WmfFile));
            metafile.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () =>
                metafile.PlayRecord(EmfPlusRecordType.BeginContainer, 0, 1, new byte[1]));
        }

        private void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private string GetPath(string fileName)
        {
            return Helpers.GetTestBitmapPath(fileName);
        }

        private void AssertEmfType(MetafileHeader metafileHeader, EmfType emfType)
        {
            switch (emfType)
            {
                case EmfType.EmfOnly:
                    Assert.True(metafileHeader.IsEmf());
                    break;
                case EmfType.EmfPlusDual:
                    Assert.True(metafileHeader.IsEmfPlusDual());
                    break;
                case EmfType.EmfPlusOnly:
                    Assert.True(metafileHeader.IsEmfPlusOnly());
                    break;
            }
        }

        private void AssertMetafileIsBlank(Metafile metafile)
        {
            GraphicsUnit graphicsUnit = (GraphicsUnit)int.MaxValue;

            AssertMetafileHeaderIsBlank(metafile.GetMetafileHeader());

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // This values are incorrect on libgdiplus.
                Assert.Equal(new Rectangle(0, 0, 1, 1), metafile.GetBounds(ref graphicsUnit));
                Assert.Equal(GraphicsUnit.Pixel, graphicsUnit);
            }
        }

        private void AssertMetafileHeaderIsBlank(MetafileHeader metafileHeader)
        {
            Assert.Equal(new Rectangle(0, 0, 0, 0), metafileHeader.Bounds);
            Assert.Equal(0, metafileHeader.MetafileSize);
        }

        private void AssertMetafile(Metafile metafile)
        {
            GraphicsUnit graphicsUnit = (GraphicsUnit)int.MaxValue;

            AssertMetafileHeader(metafile.GetMetafileHeader());
            Assert.Equal(new Rectangle(-30, -40, 3096, 4127), metafile.GetBounds(ref graphicsUnit));
            Assert.Equal(GraphicsUnit.Pixel, graphicsUnit);
        }

        private void AssertMetafileHeader(MetafileHeader header)
        {
            Assert.Equal(MetafileType.WmfPlaceable, header.Type);
            Assert.Equal(0x300, header.Version);
            Assert.Equal(new Rectangle(-30, -40, 3096, 4127), header.Bounds);
            Assert.Equal(606, header.DpiX);
            Assert.Equal(606, header.DpiY);
            Assert.Equal(0, header.EmfPlusHeaderSize);
            Assert.Equal(0, header.LogicalDpiX);
            Assert.Equal(0, header.LogicalDpiY);
            Assert.Equal(3474, header.MetafileSize);
            Assert.NotNull(header.WmfHeader);
            Assert.False(header.IsDisplay());
            Assert.False(header.IsEmf());
            Assert.False(header.IsEmfOrEmfPlus());
            Assert.False(header.IsEmfPlus());
            Assert.False(header.IsEmfPlusDual());
            Assert.False(header.IsEmfPlusOnly());
            Assert.True(header.IsWmf());
            Assert.True(header.IsWmfPlaceable());

            Assert.Equal(9, header.WmfHeader.HeaderSize);
            Assert.Equal(98, header.WmfHeader.MaxRecord);
            Assert.Equal(3, header.WmfHeader.NoObjects);
            Assert.Equal(0, header.WmfHeader.NoParameters);
            Assert.Equal(1737, header.WmfHeader.Size);
            Assert.Equal((int)MetafileType.Wmf, header.WmfHeader.Type);
            Assert.Equal(0x300, header.WmfHeader.Version);
        }
    }
}

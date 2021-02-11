// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Graphics class testing unit
//
// Authors:
//   Jordi Mas, jordi@ximian.com
//   Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2005-2008 Novell, Inc (http://www.novell.com)
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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace MonoTests.System.Drawing
{
    public class GraphicsTest : IDisposable
    {
        private RectangleF[] rects;
        private Font font;

        public GraphicsTest()
        {
            try
            {
                font = new Font("Arial", 12);
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            if (font != null)
                font.Dispose();
        }

        private bool IsEmptyBitmap(Bitmap bitmap, out int x, out int y)
        {
            bool result = true;
            int empty = Color.Empty.ToArgb();
            for (y = 0; y < bitmap.Height; y++)
            {
                for (x = 0; x < bitmap.Width; x++)
                {
                    if (bitmap.GetPixel(x, y).ToArgb() != empty)
                        return false;
                }
            }

            x = -1;
            y = -1;
            return result;
        }

        private void CheckForEmptyBitmap(Bitmap bitmap)
        {
            int x, y;
            if (!IsEmptyBitmap(bitmap, out x, out y))
                Assert.True(false, string.Format("Position {0},{1}", x, y));
        }

        private void CheckForNonEmptyBitmap(Bitmap bitmap)
        {
            int x, y;
            if (IsEmptyBitmap(bitmap, out x, out y))
                Assert.True(false);
        }

        private void AssertEquals(string msg, object expected, object actual)
        {
            Assert.Equal(expected, actual);
        }

        private void AssertEquals(string msg, double expected, double actual, int precision)
        {
            Assert.Equal(expected, actual, precision);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DefaultProperties()
        {
            using (Bitmap bmp = new Bitmap(200, 200))
            using (Graphics g = Graphics.FromImage(bmp))
            using (Region r = new Region())
            {
                Assert.Equal(r.GetBounds(g), g.ClipBounds);
                Assert.Equal(CompositingMode.SourceOver, g.CompositingMode);
                Assert.Equal(CompositingQuality.Default, g.CompositingQuality);
                Assert.Equal(InterpolationMode.Bilinear, g.InterpolationMode);
                Assert.Equal(1, g.PageScale);
                Assert.Equal(GraphicsUnit.Display, g.PageUnit);
                Assert.Equal(PixelOffsetMode.Default, g.PixelOffsetMode);
                Assert.Equal(new Point(0, 0), g.RenderingOrigin);
                Assert.Equal(SmoothingMode.None, g.SmoothingMode);
                Assert.Equal(TextRenderingHint.SystemDefault, g.TextRenderingHint);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetGetProperties()
        {
            using (Bitmap bmp = new Bitmap(200, 200))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.GammaCorrected;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PageScale = 2;
                g.PageUnit = GraphicsUnit.Inch;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.RenderingOrigin = new Point(10, 20);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.SystemDefault;

                //Clipping set/get tested in clipping functions
                Assert.Equal(CompositingMode.SourceCopy, g.CompositingMode);
                Assert.Equal(CompositingQuality.GammaCorrected, g.CompositingQuality);
                Assert.Equal(InterpolationMode.HighQualityBilinear, g.InterpolationMode);
                Assert.Equal(2, g.PageScale);
                Assert.Equal(GraphicsUnit.Inch, g.PageUnit);
                Assert.Equal(PixelOffsetMode.Half, g.PixelOffsetMode);
                Assert.Equal(new Point(10, 20), g.RenderingOrigin);
                Assert.Equal(SmoothingMode.AntiAlias, g.SmoothingMode);
                Assert.Equal(TextRenderingHint.SystemDefault, g.TextRenderingHint);
            }
        }

        // Properties
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clip()
        {
            RectangleF[] rects;
            using (Bitmap bmp = new Bitmap(200, 200))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clip = new Region(new Rectangle(50, 40, 210, 220));
                rects = g.Clip.GetRegionScans(new Matrix());

                Assert.Equal(1, rects.Length);
                Assert.Equal(50, rects[0].X);
                Assert.Equal(40, rects[0].Y);
                Assert.Equal(210, rects[0].Width);
                Assert.Equal(220, rects[0].Height);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clip_NotAReference()
        {
            using (Bitmap bmp = new Bitmap(200, 200))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.True(g.Clip.IsInfinite(g));
                g.Clip.IsEmpty(g);
                Assert.False(g.Clip.IsEmpty(g));
                Assert.True(g.Clip.IsInfinite(g));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ExcludeClip()
        {
            using (Bitmap bmp = new Bitmap(200, 200))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clip = new Region(new RectangleF(10, 10, 100, 100));
                g.ExcludeClip(new Rectangle(40, 60, 100, 20));
                rects = g.Clip.GetRegionScans(new Matrix());

                Assert.Equal(3, rects.Length);

                Assert.Equal(10, rects[0].X);
                Assert.Equal(10, rects[0].Y);
                Assert.Equal(100, rects[0].Width);
                Assert.Equal(50, rects[0].Height);

                Assert.Equal(10, rects[1].X);
                Assert.Equal(60, rects[1].Y);
                Assert.Equal(30, rects[1].Width);
                Assert.Equal(20, rects[1].Height);

                Assert.Equal(10, rects[2].X);
                Assert.Equal(80, rects[2].Y);
                Assert.Equal(100, rects[2].Width);
                Assert.Equal(30, rects[2].Height);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void IntersectClip()
        {
            using (Bitmap bmp = new Bitmap(200, 200))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clip = new Region(new RectangleF(260, 30, 60, 80));
                g.IntersectClip(new Rectangle(290, 40, 60, 80));
                rects = g.Clip.GetRegionScans(new Matrix());

                Assert.Equal(1, rects.Length);

                Assert.Equal(290, rects[0].X);
                Assert.Equal(40, rects[0].Y);
                Assert.Equal(30, rects[0].Width);
                Assert.Equal(70, rects[0].Height);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ResetClip()
        {
            using (Bitmap bmp = new Bitmap(200, 200))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clip = new Region(new RectangleF(260, 30, 60, 80));
                g.IntersectClip(new Rectangle(290, 40, 60, 80));
                g.ResetClip();
                rects = g.Clip.GetRegionScans(new Matrix());

                Assert.Equal(1, rects.Length);

                Assert.Equal(-4194304, rects[0].X);
                Assert.Equal(-4194304, rects[0].Y);
                Assert.Equal(8388608, rects[0].Width);
                Assert.Equal(8388608, rects[0].Height);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetClip()
        {
            RectangleF[] rects;
            using (Bitmap bmp = new Bitmap(200, 200))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    // Region
                    g.SetClip(new Region(new Rectangle(50, 40, 210, 220)), CombineMode.Replace);
                    rects = g.Clip.GetRegionScans(new Matrix());
                    Assert.Equal(1, rects.Length);
                    Assert.Equal(50, rects[0].X);
                    Assert.Equal(40, rects[0].Y);
                    Assert.Equal(210, rects[0].Width);
                    Assert.Equal(220, rects[0].Height);
                }

                // RectangleF
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SetClip(new RectangleF(50, 40, 210, 220));
                    rects = g.Clip.GetRegionScans(new Matrix());
                    Assert.Equal(1, rects.Length);
                    Assert.Equal(50, rects[0].X);
                    Assert.Equal(40, rects[0].Y);
                    Assert.Equal(210, rects[0].Width);
                    Assert.Equal(220, rects[0].Height);
                }

                // Rectangle
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SetClip(new Rectangle(50, 40, 210, 220));
                    rects = g.Clip.GetRegionScans(new Matrix());
                    Assert.Equal(1, rects.Length);
                    Assert.Equal(50, rects[0].X);
                    Assert.Equal(40, rects[0].Y);
                    Assert.Equal(210, rects[0].Width);
                    Assert.Equal(220, rects[0].Height);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SetSaveReset()
        {
            using (Bitmap bmp = new Bitmap(200, 200))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                GraphicsState state_default, state_modified;

                state_default = g.Save(); // Default

                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.GammaCorrected;
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;
                g.PageScale = 2;
                g.PageUnit = GraphicsUnit.Inch;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.Clip = new Region(new Rectangle(0, 0, 100, 100));
                g.RenderingOrigin = new Point(10, 20);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;


                state_modified = g.Save(); // Modified

                g.CompositingMode = CompositingMode.SourceOver;
                g.CompositingQuality = CompositingQuality.Default;
                g.InterpolationMode = InterpolationMode.Bilinear;
                g.PageScale = 5;
                g.PageUnit = GraphicsUnit.Display;
                g.PixelOffsetMode = PixelOffsetMode.Default;
                g.Clip = new Region(new Rectangle(1, 2, 20, 25));
                g.RenderingOrigin = new Point(5, 6);
                g.SmoothingMode = SmoothingMode.None;
                g.TextRenderingHint = TextRenderingHint.SystemDefault;

                g.Restore(state_modified);

                Assert.Equal(CompositingMode.SourceCopy, g.CompositingMode);
                Assert.Equal(CompositingQuality.GammaCorrected, g.CompositingQuality);
                Assert.Equal(InterpolationMode.HighQualityBilinear, g.InterpolationMode);
                Assert.Equal(2, g.PageScale);
                Assert.Equal(GraphicsUnit.Inch, g.PageUnit);
                Assert.Equal(PixelOffsetMode.Half, g.PixelOffsetMode);
                Assert.Equal(new Point(10, 20), g.RenderingOrigin);
                Assert.Equal(SmoothingMode.AntiAlias, g.SmoothingMode);
                Assert.Equal(TextRenderingHint.ClearTypeGridFit, g.TextRenderingHint);
                Assert.Equal(0, (int)g.ClipBounds.X);
                Assert.Equal(0, (int)g.ClipBounds.Y);

                g.Restore(state_default);

                Assert.Equal(CompositingMode.SourceOver, g.CompositingMode);
                Assert.Equal(CompositingQuality.Default, g.CompositingQuality);
                Assert.Equal(InterpolationMode.Bilinear, g.InterpolationMode);
                Assert.Equal(1, g.PageScale);
                Assert.Equal(GraphicsUnit.Display, g.PageUnit);
                Assert.Equal(PixelOffsetMode.Default, g.PixelOffsetMode);
                Assert.Equal(new Point(0, 0), g.RenderingOrigin);
                Assert.Equal(SmoothingMode.None, g.SmoothingMode);
                Assert.Equal(TextRenderingHint.SystemDefault, g.TextRenderingHint);

                Region r = new Region();
                Assert.Equal(r.GetBounds(g), g.ClipBounds);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void LoadIndexed_BmpFile()
        {
            // Tests that we can load an indexed file, but...
            string sInFile = Helpers.GetTestBitmapPath("almogaver1bit.bmp");
            // note: file is misnamed (it's a 4bpp bitmap)
            using (Image img = Image.FromFile(sInFile))
            {
                Assert.Equal(PixelFormat.Format4bppIndexed, img.PixelFormat);
                Exception exception = AssertExtensions.Throws<ArgumentException, Exception>(() => Graphics.FromImage(img));
                if (exception is ArgumentException argumentException)
                    Assert.Equal("image", argumentException.ParamName);
            }
        }

        class BitmapAndGraphics : IDisposable
        {
            private readonly Bitmap _bitmap;
            public Graphics Graphics { get; }
            public BitmapAndGraphics(int width, int height)
            {
                _bitmap = new Bitmap(width, height);
                Graphics = Graphics.FromImage(_bitmap);
                Graphics.Clip = new Region(new Rectangle(0, 0, width, height));
            }
            public void Dispose() { Graphics.Dispose(); _bitmap.Dispose(); }
        }

        private void Compare(string msg, RectangleF b1, RectangleF b2)
        {
            AssertEquals(msg + ".compare.X", b1.X, b2.X);
            AssertEquals(msg + ".compare.Y", b1.Y, b2.Y);
            AssertEquals(msg + ".compare.Width", b1.Width, b2.Width);
            AssertEquals(msg + ".compare.Height", b1.Height, b2.Height);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clip_GetBounds()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                RectangleF bounds = g.Clip.GetBounds(g);
                Assert.Equal(0, bounds.X);
                Assert.Equal(0, bounds.Y);
                Assert.Equal(16, bounds.Width);
                Assert.Equal(16, bounds.Height);
                Assert.True(g.Transform.IsIdentity);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clip_TranslateTransform()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                g.TranslateTransform(12.22f, 10.10f);
                RectangleF bounds = g.Clip.GetBounds(g);
                Compare("translate", bounds, g.ClipBounds);
                Assert.Equal(-12.2200003f, bounds.X);
                Assert.Equal(-10.1000004f, bounds.Y);
                Assert.Equal(16, bounds.Width);
                Assert.Equal(16, bounds.Height);
                float[] elements = g.Transform.Elements;
                Assert.Equal(1, elements[0]);
                Assert.Equal(0, elements[1]);
                Assert.Equal(0, elements[2]);
                Assert.Equal(1, elements[3]);
                Assert.Equal(12.2200003f, elements[4]);
                Assert.Equal(10.1000004f, elements[5]);

                g.ResetTransform();
                bounds = g.Clip.GetBounds(g);
                Compare("reset", bounds, g.ClipBounds);
                Assert.Equal(0, bounds.X);
                Assert.Equal(0, bounds.Y);
                Assert.Equal(16, bounds.Width);
                Assert.Equal(16, bounds.Height);
                Assert.True(g.Transform.IsIdentity);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_NonInvertibleMatrix()
        {
            using (Matrix matrix = new Matrix(123, 24, 82, 16, 47, 30))
            using (var b = new BitmapAndGraphics(16, 16))
            {
                Assert.False(matrix.IsInvertible);

                var g = b.Graphics;
                Assert.Throws<ArgumentException>(() => g.Transform = matrix);
            }
        }


        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Multiply_NonInvertibleMatrix()
        {
            using (Matrix matrix = new Matrix(123, 24, 82, 16, 47, 30))
            using (var b = new BitmapAndGraphics(16, 16))
            {
                Assert.False(matrix.IsInvertible);

                var g = b.Graphics;
                Assert.Throws<ArgumentException>(() => g.MultiplyTransform(matrix));
            }
        }

        private void CheckBounds(string msg, RectangleF bounds, float x, float y, float w, float h)
        {
            AssertEquals(msg + ".X", x, bounds.X, 1);
            AssertEquals(msg + ".Y", y, bounds.Y, 1);
            AssertEquals(msg + ".Width", w, bounds.Width, 1);
            AssertEquals(msg + ".Height", h, bounds.Height, 1);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClipBounds()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                CheckBounds("graphics.ClipBounds", g.ClipBounds, 0, 0, 16, 16);
                CheckBounds("graphics.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 16, 16);

                g.Clip = new Region(new Rectangle(0, 0, 8, 8));
                CheckBounds("clip.ClipBounds", g.ClipBounds, 0, 0, 8, 8);
                CheckBounds("clip.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClipBounds_Rotate()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                g.Clip = new Region(new Rectangle(0, 0, 8, 8));
                g.RotateTransform(90);
                CheckBounds("rotate.ClipBounds", g.ClipBounds, 0, -8, 8, 8);
                CheckBounds("rotate.Clip.GetBounds", g.Clip.GetBounds(g), 0, -8, 8, 8);

                g.Transform = new Matrix();
                CheckBounds("identity.ClipBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);
                CheckBounds("identity.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClipBounds_Scale()
        {
            RectangleF clip = new Rectangle(0, 0, 8, 8);
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                g.Clip = new Region(clip);
                g.ScaleTransform(0.25f, 0.5f);
                CheckBounds("scale.ClipBounds", g.ClipBounds, 0, 0, 32, 16);
                CheckBounds("scale.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 32, 16);

                g.SetClip(clip);
                CheckBounds("setclip.ClipBounds", g.ClipBounds, 0, 0, 8, 8);
                CheckBounds("setclip.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClipBounds_Translate()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                g.Clip = new Region(new Rectangle(0, 0, 8, 8));
                using (Region clone = g.Clip.Clone())
                {
                    g.TranslateTransform(8, 8);
                    CheckBounds("translate.ClipBounds", g.ClipBounds, -8, -8, 8, 8);
                    CheckBounds("translate.Clip.GetBounds", g.Clip.GetBounds(g), -8, -8, 8, 8);

                    g.SetClip(clone, CombineMode.Replace);
                    CheckBounds("setclip.ClipBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);
                    CheckBounds("setclip.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClipBounds_Transform_Translation()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                g.Clip = new Region(new Rectangle(0, 0, 8, 8));
                g.Transform = new Matrix(1, 0, 0, 1, 8, 8);
                CheckBounds("transform.ClipBounds", g.ClipBounds, -8, -8, 8, 8);
                CheckBounds("transform.Clip.GetBounds", g.Clip.GetBounds(g), -8, -8, 8, 8);

                g.ResetTransform();
                CheckBounds("reset.ClipBounds", g.ClipBounds, 0, 0, 8, 8);
                CheckBounds("reset.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClipBounds_Transform_Scale()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                g.Clip = new Region(new Rectangle(0, 0, 8, 8));
                g.Transform = new Matrix(0.5f, 0, 0, 0.25f, 0, 0);
                CheckBounds("scale.ClipBounds", g.ClipBounds, 0, 0, 16, 32);
                CheckBounds("scale.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 16, 32);

                g.ResetClip();
                // see next test for ClipBounds
                CheckBounds("resetclip.Clip.GetBounds", g.Clip.GetBounds(g), -4194304, -4194304, 8388608, 8388608);
                Assert.True(g.Clip.IsInfinite(g));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClipBounds_Multiply()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                g.Clip = new Region(new Rectangle(0, 0, 8, 8));
                g.Transform = new Matrix(1, 0, 0, 1, 8, 8);
                g.MultiplyTransform(g.Transform);
                CheckBounds("multiply.ClipBounds", g.ClipBounds, -16, -16, 8, 8);
                CheckBounds("multiply.Clip.GetBounds", g.Clip.GetBounds(g), -16, -16, 8, 8);

                g.ResetTransform();
                CheckBounds("reset.ClipBounds", g.ClipBounds, 0, 0, 8, 8);
                CheckBounds("reset.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ClipBounds_Cumulative_Effects()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                CheckBounds("graphics.ClipBounds", g.ClipBounds, 0, 0, 16, 16);
                CheckBounds("graphics.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 16, 16);

                g.Clip = new Region(new Rectangle(0, 0, 8, 8));
                CheckBounds("clip.ClipBounds", g.ClipBounds, 0, 0, 8, 8);
                CheckBounds("clip.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);

                g.RotateTransform(90);
                CheckBounds("rotate.ClipBounds", g.ClipBounds, 0, -8, 8, 8);
                CheckBounds("rotate.Clip.GetBounds", g.Clip.GetBounds(g), 0, -8, 8, 8);

                g.ScaleTransform(0.25f, 0.5f);
                CheckBounds("scale.ClipBounds", g.ClipBounds, 0, -16, 32, 16);
                CheckBounds("scale.Clip.GetBounds", g.Clip.GetBounds(g), 0, -16, 32, 16);

                g.TranslateTransform(8, 8);
                CheckBounds("translate.ClipBounds", g.ClipBounds, -8, -24, 32, 16);
                CheckBounds("translate.Clip.GetBounds", g.Clip.GetBounds(g), -8, -24, 32, 16);

                g.MultiplyTransform(g.Transform);
                CheckBounds("multiply.ClipBounds", g.ClipBounds, -104, -56, 64, 64);
                CheckBounds("multiply.Clip.GetBounds", g.Clip.GetBounds(g), -104, -56, 64, 64);

                g.ResetTransform();
                CheckBounds("reset.ClipBounds", g.ClipBounds, 0, 0, 8, 8);
                CheckBounds("reset.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clip_TranslateTransform_BoundsChange()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                CheckBounds("graphics.ClipBounds", g.ClipBounds, 0, 0, 16, 16);
                CheckBounds("graphics.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 16, 16);
                g.TranslateTransform(-16, -16);
                CheckBounds("translated.ClipBounds", g.ClipBounds, 16, 16, 16, 16);
                CheckBounds("translated.Clip.GetBounds", g.Clip.GetBounds(g), 16, 16, 16, 16);

                g.Clip = new Region(new Rectangle(0, 0, 8, 8));
                // ClipBounds isn't affected by a previous translation
                CheckBounds("rectangle.ClipBounds", g.ClipBounds, 0, 0, 8, 8);
                // Clip.GetBounds isn't affected by a previous translation
                CheckBounds("rectangle.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);

                g.ResetTransform();
                CheckBounds("reseted.ClipBounds", g.ClipBounds, -16, -16, 8, 8);
                CheckBounds("reseted.Clip.GetBounds", g.Clip.GetBounds(g), -16, -16, 8, 8);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clip_RotateTransform_BoundsChange()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                CheckBounds("graphics.ClipBounds", g.ClipBounds, 0, 0, 16, 16);
                CheckBounds("graphics.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 16, 16);
                // we select a "simple" angle because the region will be converted into
                // a bitmap (well for libgdiplus) and we would lose precision after that
                g.RotateTransform(90);
                CheckBounds("rotated.ClipBounds", g.ClipBounds, 0, -16, 16, 16);
                CheckBounds("rotated.Clip.GetBounds", g.Clip.GetBounds(g), 0, -16, 16, 16);
                g.Clip = new Region(new Rectangle(0, 0, 8, 8));
                // ClipBounds isn't affected by a previous rotation (90)
                CheckBounds("rectangle.ClipBounds", g.ClipBounds, 0, 0, 8, 8);
                // Clip.GetBounds isn't affected by a previous rotation
                CheckBounds("rectangle.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);

                g.ResetTransform();
                CheckBounds("reseted.ClipBounds", g.ClipBounds, -8, 0, 8, 8);
                CheckBounds("reseted.Clip.GetBounds", g.Clip.GetBounds(g), -8, 0, 8, 8);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clip_ScaleTransform_NoBoundsChange()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                CheckBounds("graphics.ClipBounds", g.ClipBounds, 0, 0, 16, 16);
                CheckBounds("graphics.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 16, 16);
                g.ScaleTransform(2, 0.5f);
                CheckBounds("scaled.ClipBounds", g.ClipBounds, 0, 0, 8, 32);
                CheckBounds("scaled.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 8, 32);
                g.Clip = new Region(new Rectangle(0, 0, 8, 8));
                // ClipBounds isn't affected by a previous scaling
                CheckBounds("rectangle.ClipBounds", g.ClipBounds, 0, 0, 8, 8);
                // Clip.GetBounds isn't affected by a previous scaling
                CheckBounds("rectangle.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 8, 8);

                g.ResetTransform();
                CheckBounds("reseted.ClipBounds", g.ClipBounds, 0, 0, 16, 4);
                CheckBounds("reseted.Clip.GetBounds", g.Clip.GetBounds(g), 0, 0, 16, 4);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ScaleTransform_X0()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                Assert.Throws<ArgumentException>(() => g.ScaleTransform(0, 1));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ScaleTransform_Y0()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                Assert.Throws<ArgumentException>(() => g.ScaleTransform(1, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TranslateTransform_Order()
        {
            using (var b = new BitmapAndGraphics(16, 16))
            {
                var g = b.Graphics;
                g.Transform = new Matrix(1, 2, 3, 4, 5, 6);
                g.TranslateTransform(3, -3);
                float[] elements = g.Transform.Elements;
                Assert.Equal(1, elements[0]);
                Assert.Equal(2, elements[1]);
                Assert.Equal(3, elements[2]);
                Assert.Equal(4, elements[3]);
                Assert.Equal(-1, elements[4]);
                Assert.Equal(0, elements[5]);

                g.Transform = new Matrix(1, 2, 3, 4, 5, 6);
                g.TranslateTransform(3, -3, MatrixOrder.Prepend);
                elements = g.Transform.Elements;
                Assert.Equal(1, elements[0]);
                Assert.Equal(2, elements[1]);
                Assert.Equal(3, elements[2]);
                Assert.Equal(4, elements[3]);
                Assert.Equal(-1, elements[4]);
                Assert.Equal(0, elements[5]);

                g.Transform = new Matrix(1, 2, 3, 4, 5, 6);
                g.TranslateTransform(3, -3, MatrixOrder.Append);
                elements = g.Transform.Elements;
                Assert.Equal(1, elements[0]);
                Assert.Equal(2, elements[1]);
                Assert.Equal(3, elements[2]);
                Assert.Equal(4, elements[3]);
                Assert.Equal(8, elements[4]);
                Assert.Equal(3, elements[5]);
            }
        }

        static PointF[] SmallCurveF = new PointF[3] { new PointF(0, 0), new PointF(15, 5), new PointF(5, 15) };

        static Point[] TooSmallCurve = new Point[2] { new Point(0, 0), new Point(15, 5) };
        static PointF[] LargeCurveF = new PointF[4] { new PointF(0, 0), new PointF(15, 5), new PointF(5, 15), new PointF(0, 20) };

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawCurve_NotEnoughPoints()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                CheckForEmptyBitmap(bitmap);
                g.DrawCurve(Pens.Black, TooSmallCurve, 0.5f);
                CheckForNonEmptyBitmap(bitmap);
                // so a "curve" can be drawn with less than 3 points!
                // actually I used to call that a line... (and it's not related to tension)
                g.Dispose();
                bitmap.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawCurve_SinglePoint()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentException>(() => g.DrawCurve(Pens.Black, new Point[1] { new Point(10, 10) }, 0.5f));
                // a single point isn't enough
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawCurve3_NotEnoughPoints()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentException>(() => g.DrawCurve(Pens.Black, TooSmallCurve, 0, 2, 0.5f));
                // aha, this is API dependent
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawCurve_NegativeTension()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // documented as bigger (or equals) to 0
                g.DrawCurve(Pens.Black, SmallCurveF, -0.9f);
                CheckForNonEmptyBitmap(bitmap);
                g.Dispose();
                bitmap.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawCurve_PositiveTension()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawCurve(Pens.Black, SmallCurveF, 0.9f);
                // this is not the same as -1
                CheckForNonEmptyBitmap(bitmap);
                g.Dispose();
                bitmap.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawCurve_ZeroSegments()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentException>(() => g.DrawCurve(Pens.Black, SmallCurveF, 0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawCurve_NegativeSegments()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentException>(() => g.DrawCurve(Pens.Black, SmallCurveF, 0, -1));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawCurve_OffsetTooLarge()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // starting offset 1 doesn't give 3 points to make a curve
                Assert.Throws<ArgumentException>(() => g.DrawCurve(Pens.Black, SmallCurveF, 1, 2));
                // and in this case 2 points aren't enough to draw something
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawCurve_Offset_0()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawCurve(Pens.Black, LargeCurveF, 0, 2, 0.5f);
                CheckForNonEmptyBitmap(bitmap);
                g.Dispose();
                bitmap.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawCurve_Offset_1()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.DrawCurve(Pens.Black, LargeCurveF, 1, 2, 0.5f);
                CheckForNonEmptyBitmap(bitmap);
                g.Dispose();
                bitmap.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawCurve_Offset_2()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // it works even with two points because we know the previous ones
                g.DrawCurve(Pens.Black, LargeCurveF, 2, 1, 0.5f);
                CheckForNonEmptyBitmap(bitmap);
                g.Dispose();
                bitmap.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawRectangle_Negative()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            using (Pen pen = new Pen(Color.Red))
            {
                g.DrawRectangle(pen, 5, 5, -10, -10);
                g.DrawRectangle(pen, 0.0f, 0.0f, 5.0f, -10.0f);
                g.DrawRectangle(pen, new Rectangle(15, 0, -10, 5));
                CheckForEmptyBitmap(bitmap);
                pen.Dispose();
                g.Dispose();
                bitmap.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawRectangles_Negative()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            using (Pen pen = new Pen(Color.Red))
            {
                Rectangle[] rects = new Rectangle[2]
                {
                    new Rectangle (5, 5, -10, -10),
                    new Rectangle (0, 0, 5, -10)
                };
                RectangleF[] rectf = new RectangleF[2]
                {
                    new RectangleF (0.0f, 5.0f, -10.0f, -10.0f),
                    new RectangleF (15.0f, 0.0f, -10.0f, 5.0f)
                };
                g.DrawRectangles(pen, rects);
                g.DrawRectangles(pen, rectf);
                CheckForEmptyBitmap(bitmap);
                pen.Dispose();
                g.Dispose();
                bitmap.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillRectangle_Negative()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            using (SolidBrush brush = new SolidBrush(Color.Red))
            {
                g.FillRectangle(brush, 5, 5, -10, -10);
                g.FillRectangle(brush, 0.0f, 0.0f, 5.0f, -10.0f);
                g.FillRectangle(brush, new Rectangle(15, 0, -10, 5));
                CheckForEmptyBitmap(bitmap);
                brush.Dispose();
                g.Dispose();
                bitmap.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillRectangles_Negative()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            using (SolidBrush brush = new SolidBrush(Color.Red))
            {
                Rectangle[] rects = new Rectangle[2]
                {
                    new Rectangle (5, 5, -10, -10),
                    new Rectangle (0, 0, 5, -10)
                };

                RectangleF[] rectf = new RectangleF[2]
                {
                    new RectangleF (0.0f, 5.0f, -10.0f, -10.0f),
                    new RectangleF (15.0f, 0.0f, -10.0f, 5.0f)
                };

                g.FillRectangles(brush, rects);
                g.FillRectangles(brush, rectf);
                CheckForEmptyBitmap(bitmap);
                brush.Dispose();
                g.Dispose();
                bitmap.Dispose();
            }
        }

        private void CheckDefaultProperties(string message, Graphics g)
        {
            Assert.True(g.Clip.IsInfinite(g), message + ".Clip.IsInfinite");
            AssertEquals(message + ".CompositingMode", CompositingMode.SourceOver, g.CompositingMode);
            AssertEquals(message + ".CompositingQuality", CompositingQuality.Default, g.CompositingQuality);
            AssertEquals(message + ".InterpolationMode", InterpolationMode.Bilinear, g.InterpolationMode);
            AssertEquals(message + ".PageScale", 1.0f, g.PageScale);
            AssertEquals(message + ".PageUnit", GraphicsUnit.Display, g.PageUnit);
            AssertEquals(message + ".PixelOffsetMode", PixelOffsetMode.Default, g.PixelOffsetMode);
            AssertEquals(message + ".SmoothingMode", SmoothingMode.None, g.SmoothingMode);
            AssertEquals(message + ".TextContrast", 4, g.TextContrast);
            AssertEquals(message + ".TextRenderingHint", TextRenderingHint.SystemDefault, g.TextRenderingHint);
            Assert.True(g.Transform.IsIdentity, message + ".Transform.IsIdentity");
        }

        private void CheckCustomProperties(string message, Graphics g)
        {
            Assert.False(g.Clip.IsInfinite(g), message + ".Clip.IsInfinite");
            AssertEquals(message + ".CompositingMode", CompositingMode.SourceCopy, g.CompositingMode);
            AssertEquals(message + ".CompositingQuality", CompositingQuality.HighQuality, g.CompositingQuality);
            AssertEquals(message + ".InterpolationMode", InterpolationMode.HighQualityBicubic, g.InterpolationMode);
            AssertEquals(message + ".PageScale", 0.5f, g.PageScale);
            AssertEquals(message + ".PageUnit", GraphicsUnit.Inch, g.PageUnit);
            AssertEquals(message + ".PixelOffsetMode", PixelOffsetMode.Half, g.PixelOffsetMode);
            AssertEquals(message + ".RenderingOrigin", new Point(-1, -1), g.RenderingOrigin);
            AssertEquals(message + ".SmoothingMode", SmoothingMode.AntiAlias, g.SmoothingMode);
            AssertEquals(message + ".TextContrast", 0, g.TextContrast);
            AssertEquals(message + ".TextRenderingHint", TextRenderingHint.AntiAlias, g.TextRenderingHint);
            Assert.False(g.Transform.IsIdentity, message + ".Transform.IsIdentity");
        }

        private void CheckMatrix(string message, Matrix m, float xx, float yx, float xy, float yy, float x0, float y0)
        {
            float[] elements = m.Elements;
            AssertEquals(message + ".Matrix.xx", xx, elements[0], 2);
            AssertEquals(message + ".Matrix.yx", yx, elements[1], 2);
            AssertEquals(message + ".Matrix.xy", xy, elements[2], 2);
            AssertEquals(message + ".Matrix.yy", yy, elements[3], 2);
            AssertEquals(message + ".Matrix.x0", x0, elements[4], 2);
            AssertEquals(message + ".Matrix.y0", y0, elements[5], 2);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void BeginContainer()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                CheckDefaultProperties("default", g);
                Assert.Equal(new Point(0, 0), g.RenderingOrigin);

                g.Clip = new Region(new Rectangle(10, 10, 10, 10));
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PageScale = 0.5f;
                g.PageUnit = GraphicsUnit.Inch;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.RenderingOrigin = new Point(-1, -1);
                g.RotateTransform(45);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextContrast = 0;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                CheckCustomProperties("modified", g);
                CheckMatrix("modified.Transform", g.Transform, 0.707f, 0.707f, -0.707f, 0.707f, 0, 0);

                GraphicsContainer gc = g.BeginContainer();
                // things gets reseted after calling BeginContainer
                CheckDefaultProperties("BeginContainer", g);
                // but not everything
                Assert.Equal(new Point(-1, -1), g.RenderingOrigin);

                g.EndContainer(gc);
                CheckCustomProperties("EndContainer", g);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void BeginContainer_Rect()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                CheckDefaultProperties("default", g);
                Assert.Equal(new Point(0, 0), g.RenderingOrigin);

                g.Clip = new Region(new Rectangle(10, 10, 10, 10));
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PageScale = 0.5f;
                g.PageUnit = GraphicsUnit.Inch;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.RenderingOrigin = new Point(-1, -1);
                g.RotateTransform(45);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextContrast = 0;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                CheckCustomProperties("modified", g);
                CheckMatrix("modified.Transform", g.Transform, 0.707f, 0.707f, -0.707f, 0.707f, 0, 0);

                GraphicsContainer gc = g.BeginContainer(new Rectangle(10, 20, 30, 40), new Rectangle(10, 20, 300, 400), GraphicsUnit.Millimeter);
                // things gets reseted after calling BeginContainer
                CheckDefaultProperties("BeginContainer", g);
                // but not everything
                Assert.Equal(new Point(-1, -1), g.RenderingOrigin);

                g.EndContainer(gc);
                CheckCustomProperties("EndContainer", g);
                CheckMatrix("EndContainer.Transform", g.Transform, 0.707f, 0.707f, -0.707f, 0.707f, 0, 0);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void BeginContainer_RectF()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                CheckDefaultProperties("default", g);
                Assert.Equal(new Point(0, 0), g.RenderingOrigin);

                g.Clip = new Region(new Rectangle(10, 10, 10, 10));
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PageScale = 0.5f;
                g.PageUnit = GraphicsUnit.Inch;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.RenderingOrigin = new Point(-1, -1);
                g.RotateTransform(45);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextContrast = 0;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                CheckCustomProperties("modified", g);
                CheckMatrix("modified.Transform", g.Transform, 0.707f, 0.707f, -0.707f, 0.707f, 0, 0);

                GraphicsContainer gc = g.BeginContainer(new RectangleF(40, 30, 20, 10), new RectangleF(10, 20, 30, 40), GraphicsUnit.Inch);
                // things gets reseted after calling BeginContainer
                CheckDefaultProperties("BeginContainer", g);
                // but not everything
                Assert.Equal(new Point(-1, -1), g.RenderingOrigin);

                g.EndContainer(gc);
                CheckCustomProperties("EndContainer", g);
            }
        }

        private void BeginContainer_GraphicsUnit(GraphicsUnit unit)
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.BeginContainer(new RectangleF(40, 30, 20, 10), new RectangleF(10, 20, 30, 40), unit);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void BeginContainer_GraphicsUnit_Display()
        {
            Assert.Throws<ArgumentException>(() => BeginContainer_GraphicsUnit(GraphicsUnit.Display));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void BeginContainer_GraphicsUnit_Valid()
        {
            BeginContainer_GraphicsUnit(GraphicsUnit.Document);
            BeginContainer_GraphicsUnit(GraphicsUnit.Inch);
            BeginContainer_GraphicsUnit(GraphicsUnit.Millimeter);
            BeginContainer_GraphicsUnit(GraphicsUnit.Pixel);
            BeginContainer_GraphicsUnit(GraphicsUnit.Point);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void BeginContainer_GraphicsUnit_World()
        {
            Assert.Throws<ArgumentException>(() => BeginContainer_GraphicsUnit(GraphicsUnit.World));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void BeginContainer_GraphicsUnit_Bad()
        {
            Assert.Throws<ArgumentException>(() => BeginContainer_GraphicsUnit((GraphicsUnit)int.MinValue));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void EndContainer_Null()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentNullException>(() => g.EndContainer(null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Save()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                CheckDefaultProperties("default", g);
                Assert.Equal(new Point(0, 0), g.RenderingOrigin);

                GraphicsState gs1 = g.Save();
                // nothing is changed after a save
                CheckDefaultProperties("save1", g);
                Assert.Equal(new Point(0, 0), g.RenderingOrigin);

                g.Clip = new Region(new Rectangle(10, 10, 10, 10));
                g.CompositingMode = CompositingMode.SourceCopy;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PageScale = 0.5f;
                g.PageUnit = GraphicsUnit.Inch;
                g.PixelOffsetMode = PixelOffsetMode.Half;
                g.RenderingOrigin = new Point(-1, -1);
                g.RotateTransform(45);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextContrast = 0;
                g.TextRenderingHint = TextRenderingHint.AntiAlias;
                CheckCustomProperties("modified", g);
                CheckMatrix("modified.Transform", g.Transform, 0.707f, 0.707f, -0.707f, 0.707f, 0, 0);

                GraphicsState gs2 = g.Save();
                CheckCustomProperties("save2", g);

                g.Restore(gs2);
                CheckCustomProperties("restored1", g);
                CheckMatrix("restored1.Transform", g.Transform, 0.707f, 0.707f, -0.707f, 0.707f, 0, 0);

                g.Restore(gs1);
                CheckDefaultProperties("restored2", g);
                Assert.Equal(new Point(0, 0), g.RenderingOrigin);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Restore_Null()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<NullReferenceException>(() => g.Restore(null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillRectangles_BrushNull_Rectangle()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentNullException>(() => g.FillRectangles(null, new Rectangle[1]));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillRectangles_Rectangle_Null()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentNullException>(() => g.FillRectangles(Brushes.Red, (Rectangle[])null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillRectanglesZeroRectangle()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentException>(() => g.FillRectangles(Brushes.Red, new Rectangle[0]));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillRectangles_BrushNull_RectangleF()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentNullException>(() => g.FillRectangles(null, new RectangleF[1]));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillRectangles_RectangleF_Null()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentNullException>(() => g.FillRectangles(Brushes.Red, (RectangleF[])null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillRectanglesZeroRectangleF()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentException>(() => g.FillRectangles(Brushes.Red, new RectangleF[0]));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillRectangles_NormalBehavior()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Fuchsia);
                    Rectangle rect = new Rectangle(5, 5, 10, 10);
                    g.Clip = new Region(rect);
                    g.FillRectangle(Brushes.Red, rect);
                }
                Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(5, 5).ToArgb());
                Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(14, 5).ToArgb());
                Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(5, 14).ToArgb());
                Assert.Equal(Color.Red.ToArgb(), bitmap.GetPixel(14, 14).ToArgb());

                Assert.Equal(Color.Fuchsia.ToArgb(), bitmap.GetPixel(15, 5).ToArgb());
                Assert.Equal(Color.Fuchsia.ToArgb(), bitmap.GetPixel(5, 15).ToArgb());
                Assert.Equal(Color.Fuchsia.ToArgb(), bitmap.GetPixel(15, 15).ToArgb());
            }
        }

        private Bitmap FillDrawRectangle(float width)
        {
            Bitmap bitmap = new Bitmap(20, 20);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Red);
                Rectangle rect = new Rectangle(5, 5, 10, 10);
                g.FillRectangle(Brushes.Green, rect);
                if (width >= 0)
                {
                    using (Pen pen = new Pen(Color.Blue, width))
                    {
                        g.DrawRectangle(pen, rect);
                    }
                }
                else
                {
                    g.DrawRectangle(Pens.Blue, rect);
                }
            }
            return bitmap;
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillDrawRectangle_Width_Default()
        {
            // default pen size
            using (Bitmap bitmap = FillDrawRectangle(float.MinValue))
            {
                // NW
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(4, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(5, 5).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(6, 6).ToArgb());
                // N
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(9, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 5).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 6).ToArgb());
                // NE
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 5).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(14, 6).ToArgb());
                // E
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(14, 9).ToArgb());
                // SE
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 15).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(14, 14).ToArgb());
                // S
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(9, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 15).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 14).ToArgb());
                // SW
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(4, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(5, 15).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(6, 14).ToArgb());
                // W
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(4, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(5, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(6, 9).ToArgb());
            }
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void FillDrawRectangle_Width_2()
        {
            // even pen size
            using (Bitmap bitmap = FillDrawRectangle(2.0f))
            {
                // NW
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(3, 3).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(4, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(5, 5).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(6, 6).ToArgb());
                // N
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(9, 3).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 5).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 6).ToArgb());
                // NE
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 3).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(14, 5).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(13, 6).ToArgb());
                // E
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(14, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(13, 9).ToArgb());
                // SE
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 15).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(14, 14).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(13, 13).ToArgb());
                // S
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(9, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 15).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 14).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 13).ToArgb());
                // SW
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(3, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(4, 15).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(5, 14).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(6, 13).ToArgb());
                // W
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(3, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(4, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(5, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(6, 9).ToArgb());
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillDrawRectangle_Width_3()
        {
            // odd pen size
            using (Bitmap bitmap = FillDrawRectangle(3.0f))
            {
                // NW
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(3, 3).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(4, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(5, 5).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(6, 6).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(7, 7).ToArgb());
                // N
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(9, 3).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 5).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 6).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 7).ToArgb());
                // NE
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(17, 3).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(16, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 5).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(14, 6).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(13, 7).ToArgb());
                // E
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(17, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(16, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(14, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(13, 9).ToArgb());
                // SE
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(17, 17).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(16, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 15).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(14, 14).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(13, 13).ToArgb());
                // S
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(9, 17).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 15).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 14).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 13).ToArgb());
                // SW
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(3, 17).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(4, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(5, 15).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(6, 14).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(7, 13).ToArgb());
                // W
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(3, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(4, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(5, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(6, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(7, 9).ToArgb());
            }
        }

        // reverse, draw the fill over
        private Bitmap DrawFillRectangle(float width)
        {
            Bitmap bitmap = new Bitmap(20, 20);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Red);
                Rectangle rect = new Rectangle(5, 5, 10, 10);
                if (width >= 0)
                {
                    using (Pen pen = new Pen(Color.Blue, width))
                    {
                        g.DrawRectangle(pen, rect);
                    }
                }
                else
                {
                    g.DrawRectangle(Pens.Blue, rect);
                }
                g.FillRectangle(Brushes.Green, rect);
            }
            return bitmap;
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawFillRectangle_Width_Default()
        {
            // default pen size
            using (Bitmap bitmap = DrawFillRectangle(float.MinValue))
            {
                // NW - no blue border
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(4, 4).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(5, 5).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(6, 6).ToArgb());
                // N - no blue border
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(9, 4).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 5).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 6).ToArgb());
                // NE
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 5).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(14, 6).ToArgb());
                // E
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(14, 9).ToArgb());
                // SE
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 15).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(14, 14).ToArgb());
                // S
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(9, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 15).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 14).ToArgb());
                // SW
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(4, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(5, 15).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(6, 14).ToArgb());
                // W - no blue border
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(4, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(5, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(6, 9).ToArgb());
            }
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void DrawFillRectangle_Width_2()
        {
            // even pen size
            using (Bitmap bitmap = DrawFillRectangle(2.0f))
            {
                // looks like a one pixel border - but enlarged
                // NW
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(3, 3).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(4, 4).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(5, 5).ToArgb());
                // N
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(9, 3).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 4).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 5).ToArgb());
                // NE
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 3).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 4).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(14, 5).ToArgb());
                // E
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(14, 9).ToArgb());
                // SE
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 15).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(14, 14).ToArgb());
                // S
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(9, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 15).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 14).ToArgb());
                // SW
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(3, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(4, 15).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(5, 14).ToArgb());
                // W
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(3, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(4, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(5, 9).ToArgb());
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawFillRectangle_Width_3()
        {
            // odd pen size
            using (Bitmap bitmap = DrawFillRectangle(3.0f))
            {
                // NW
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(3, 3).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(4, 4).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(5, 5).ToArgb());
                // N
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(9, 3).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 4).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 5).ToArgb());
                // NE
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(17, 3).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(16, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 4).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(14, 5).ToArgb());
                // E
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(17, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(16, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(14, 9).ToArgb());
                // SE
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(17, 17).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(16, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 15).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(14, 14).ToArgb());
                // S
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(9, 17).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(9, 15).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(9, 14).ToArgb());
                // SW
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(3, 17).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(4, 16).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(5, 15).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(6, 14).ToArgb());
                // W
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(3, 9).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(4, 9).ToArgb());
                Assert.Equal(0xFF008000, (uint)bitmap.GetPixel(5, 9).ToArgb());
            }
        }

        private Bitmap DrawLines(float width)
        {
            Bitmap bitmap = new Bitmap(20, 20);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Red);
                Point[] pts = new Point[3] { new Point(5, 5), new Point(15, 5), new Point(15, 15) };
                if (width >= 0)
                {
                    using (Pen pen = new Pen(Color.Blue, width))
                    {
                        g.DrawLines(pen, pts);
                    }
                }
                else
                {
                    g.DrawLines(Pens.Blue, pts);
                }
            }
            return bitmap;
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawLines_Width_Default()
        {
            // default pen size
            using (Bitmap bitmap = DrawLines(float.MinValue))
            {
                // start
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(4, 4).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(4, 5).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(4, 6).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(5, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(5, 5).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(5, 6).ToArgb());
                // middle
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(14, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(14, 5).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(14, 6).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(15, 4).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 5).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 6).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 4).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 5).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 6).ToArgb());
                //end
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(14, 15).ToArgb());
                Assert.Equal(0xFF0000FF, (uint)bitmap.GetPixel(15, 15).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 15).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(14, 16).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(15, 16).ToArgb());
                Assert.Equal(0xFFFF0000, (uint)bitmap.GetPixel(16, 16).ToArgb());
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureString_StringFont()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    SizeF size = g.MeasureString(null, font);
                    Assert.True(size.IsEmpty);
                    size = g.MeasureString(string.Empty, font);
                    Assert.True(size.IsEmpty);
                    // null font
                    size = g.MeasureString(null, null);
                    Assert.True(size.IsEmpty);
                    size = g.MeasureString(string.Empty, null);
                    Assert.True(size.IsEmpty);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureString_StringFont_Null()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentNullException>(() => g.MeasureString("a", null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureString_StringFontSizeF()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                SizeF size = g.MeasureString("a", font, SizeF.Empty);
                Assert.False(size.IsEmpty);

                size = g.MeasureString(string.Empty, font, SizeF.Empty);
                Assert.True(size.IsEmpty);
            }
        }

        private void MeasureString_StringFontInt(string s)
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                SizeF size0 = g.MeasureString(s, font, 0);
                SizeF sizeN = g.MeasureString(s, font, int.MinValue);
                SizeF sizeP = g.MeasureString(s, font, int.MaxValue);
                Assert.Equal(size0, sizeN);
                Assert.Equal(size0, sizeP);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureString_StringFontInt_ShortString()
        {
            MeasureString_StringFontInt("a");
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureString_StringFontInt_LongString()
        {
            MeasureString_StringFontInt("A very long string...");
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureString_StringFormat_Alignment()
        {
            string text = "Hello Mono::";

            using (StringFormat string_format = new StringFormat())
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                string_format.Alignment = StringAlignment.Near;
                SizeF near = g.MeasureString(text, font, int.MaxValue, string_format);

                string_format.Alignment = StringAlignment.Center;
                SizeF center = g.MeasureString(text, font, int.MaxValue, string_format);

                string_format.Alignment = StringAlignment.Far;
                SizeF far = g.MeasureString(text, font, int.MaxValue, string_format);

                Assert.Equal(near.Width, center.Width, 1);
                Assert.Equal(near.Height, center.Height, 1);

                Assert.Equal(center.Width, far.Width, 1);
                Assert.Equal(center.Height, far.Height, 1);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureString_StringFormat_Alignment_DirectionVertical()
        {
            string text = "Hello Mono::";
            using (StringFormat string_format = new StringFormat())
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                string_format.FormatFlags = StringFormatFlags.DirectionVertical;

                string_format.Alignment = StringAlignment.Near;
                SizeF near = g.MeasureString(text, font, int.MaxValue, string_format);

                string_format.Alignment = StringAlignment.Center;
                SizeF center = g.MeasureString(text, font, int.MaxValue, string_format);

                string_format.Alignment = StringAlignment.Far;
                SizeF far = g.MeasureString(text, font, int.MaxValue, string_format);

                Assert.Equal(near.Width, center.Width, 0);
                Assert.Equal(near.Height, center.Height, 0);

                Assert.Equal(center.Width, far.Width, 0);
                Assert.Equal(center.Height, far.Height, 0);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureString_StringFormat_LineAlignment()
        {
            string text = "Hello Mono::";
            using (StringFormat string_format = new StringFormat())
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                string_format.LineAlignment = StringAlignment.Near;
                SizeF near = g.MeasureString(text, font, int.MaxValue, string_format);

                string_format.LineAlignment = StringAlignment.Center;
                SizeF center = g.MeasureString(text, font, int.MaxValue, string_format);

                string_format.LineAlignment = StringAlignment.Far;
                SizeF far = g.MeasureString(text, font, int.MaxValue, string_format);

                Assert.Equal(near.Width, center.Width, 1);
                Assert.Equal(near.Height, center.Height, 1);

                Assert.Equal(center.Width, far.Width, 1);
                Assert.Equal(center.Height, far.Height, 1);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureString_StringFormat_LineAlignment_DirectionVertical()
        {
            string text = "Hello Mono::";
            using (StringFormat string_format = new StringFormat())
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                string_format.FormatFlags = StringFormatFlags.DirectionVertical;

                string_format.LineAlignment = StringAlignment.Near;
                SizeF near = g.MeasureString(text, font, int.MaxValue, string_format);

                string_format.LineAlignment = StringAlignment.Center;
                SizeF center = g.MeasureString(text, font, int.MaxValue, string_format);

                string_format.LineAlignment = StringAlignment.Far;
                SizeF far = g.MeasureString(text, font, int.MaxValue, string_format);

                Assert.Equal(near.Width, center.Width, 1);
                Assert.Equal(near.Height, center.Height, 1);

                Assert.Equal(center.Width, far.Width, 1);
                Assert.Equal(center.Height, far.Height, 1);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureString_CharactersFitted()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                string s = "aaa aa aaaa a aaa";
                SizeF size = g.MeasureString(s, font);

                int chars, lines;
                SizeF size2 = g.MeasureString(s, font, new SizeF(80, size.Height), null, out chars, out lines);

                // in pixels
                Assert.True(size2.Width < size.Width);
                Assert.Equal(size2.Height, size.Height);

                Assert.Equal(1, lines);
                // documentation seems to suggest chars is total length
                Assert.True(chars < s.Length);
            }
        }

        [ConditionalFact(Helpers.GdiPlusIsAvailableNotRedhat73)]
        public void MeasureString_Whitespace()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                string s = string.Empty;
                SizeF size = g.MeasureString(s, font);
                Assert.Equal(0, size.Height);
                Assert.Equal(0, size.Width);

                s += " ";
                SizeF expected = g.MeasureString(s, font);
                for (int i = 1; i < 10; i++)
                {
                    s += " ";
                    size = g.MeasureString(s, font);
                    Assert.Equal(expected.Height, size.Height, 1);
                    Assert.Equal(expected.Width, size.Width, 1);
                }

                s = "a";
                expected = g.MeasureString(s, font);
                s = " " + s;
                size = g.MeasureString(s, font);
                float space_width = size.Width - expected.Width;
                for (int i = 1; i < 10; i++)
                {
                    size = g.MeasureString(s, font);
                    Assert.Equal(expected.Height, size.Height, 1);
                    Assert.Equal(expected.Width + i * space_width, size.Width, 1);
                    s = " " + s;
                }

                s = "a";
                expected = g.MeasureString(s, font);
                for (int i = 1; i < 10; i++)
                {
                    s = s + " ";
                    size = g.MeasureString(s, font);
                    Assert.Equal(expected.Height, size.Height, 1);
                    Assert.Equal(expected.Width, size.Width, 1);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureCharacterRanges_NullOrEmptyText()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Region[] regions = g.MeasureCharacterRanges(null, font, new RectangleF(), null);
                Assert.Equal(0, regions.Length);
                regions = g.MeasureCharacterRanges(string.Empty, font, new RectangleF(), null);
                Assert.Equal(0, regions.Length);
                // null font is ok with null or empty string
                regions = g.MeasureCharacterRanges(null, null, new RectangleF(), null);
                Assert.Equal(0, regions.Length);
                regions = g.MeasureCharacterRanges(string.Empty, null, new RectangleF(), null);
                Assert.Equal(0, regions.Length);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureCharacterRanges_EmptyStringFormat()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                // string format without character ranges
                Region[] regions = g.MeasureCharacterRanges("Mono", font, new RectangleF(), new StringFormat());
                Assert.Equal(0, regions.Length);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureCharacterRanges_FontNull()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentNullException>(() => g.MeasureCharacterRanges("a", null, new RectangleF(), null));
            }
        }

        [ConditionalFact(Helpers.GdiPlusIsAvailableNotRedhat73)]
        public void MeasureCharacterRanges_TwoLines()
        {
            string text = "this\nis a test";
            CharacterRange[] ranges = new CharacterRange[2];
            ranges[0] = new CharacterRange(0, 5);
            ranges[1] = new CharacterRange(5, 9);

            using (StringFormat string_format = new StringFormat())
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                string_format.FormatFlags = StringFormatFlags.NoClip;
                string_format.SetMeasurableCharacterRanges(ranges);

                SizeF size = g.MeasureString(text, font, new Point(0, 0), string_format);
                RectangleF layout_rect = new RectangleF(0.0f, 0.0f, size.Width, size.Height);
                Region[] regions = g.MeasureCharacterRanges(text, font, layout_rect, string_format);

                Assert.Equal(2, regions.Length);
                Assert.Equal(regions[0].GetBounds(g).Height, regions[1].GetBounds(g).Height);
            }
        }

        private void MeasureCharacterRanges(string text, int first, int length)
        {
            CharacterRange[] ranges = new CharacterRange[1];
            ranges[0] = new CharacterRange(first, length);

            using (StringFormat string_format = new StringFormat())
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                string_format.FormatFlags = StringFormatFlags.NoClip;
                string_format.SetMeasurableCharacterRanges(ranges);

                SizeF size = g.MeasureString(text, font, new Point(0, 0), string_format);
                RectangleF layout_rect = new RectangleF(0.0f, 0.0f, size.Width, size.Height);
                g.MeasureCharacterRanges(text, font, layout_rect, string_format);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureCharacterRanges_FirstTooFar()
        {
            string text = "this\nis a test";
            Assert.Throws<ArgumentException>(() => MeasureCharacterRanges(text, text.Length, 1));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureCharacterRanges_LengthTooLong()
        {
            string text = "this\nis a test";
            Assert.Throws<ArgumentException>(() => MeasureCharacterRanges(text, 0, text.Length + 1));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureCharacterRanges_Prefix()
        {
            string text = "Hello &Mono::";
            CharacterRange[] ranges = new CharacterRange[1];
            ranges[0] = new CharacterRange(5, 4);

            using (StringFormat string_format = new StringFormat())
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                string_format.SetMeasurableCharacterRanges(ranges);

                SizeF size = g.MeasureString(text, font, new Point(0, 0), string_format);
                RectangleF layout_rect = new RectangleF(0.0f, 0.0f, size.Width, size.Height);

                // here & is part of the measure and visible
                string_format.HotkeyPrefix = HotkeyPrefix.None;
                Region[] regions = g.MeasureCharacterRanges(text, font, layout_rect, string_format);
                RectangleF bounds_none = regions[0].GetBounds(g);

                // here & is part of the measure (range) but visible as an underline
                string_format.HotkeyPrefix = HotkeyPrefix.Show;
                regions = g.MeasureCharacterRanges(text, font, layout_rect, string_format);
                RectangleF bounds_show = regions[0].GetBounds(g);
                Assert.True(bounds_show.Width < bounds_none.Width);

                // here & is part of the measure (range) but invisible
                string_format.HotkeyPrefix = HotkeyPrefix.Hide;
                regions = g.MeasureCharacterRanges(text, font, layout_rect, string_format);
                RectangleF bounds_hide = regions[0].GetBounds(g);
                Assert.Equal(bounds_hide.Width, bounds_show.Width);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureCharacterRanges_NullStringFormat()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                Assert.Throws<ArgumentException>(() => g.MeasureCharacterRanges("Mono", font, new RectangleF(), null));
            }
        }

        static CharacterRange[] ranges = new CharacterRange[] {
                    new CharacterRange (0, 1),
                    new CharacterRange (1, 1),
                    new CharacterRange (2, 1)
                };

        Region[] Measure_Helper(Graphics gfx, RectangleF rect)
        {
            using (StringFormat format = StringFormat.GenericTypographic)
            {
                format.SetMeasurableCharacterRanges(ranges);

                using (Font font = new Font(FontFamily.GenericSerif, 11.0f))
                {
                    return gfx.MeasureCharacterRanges("abc", font, rect, format);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Measure()
        {
            using (Graphics gfx = Graphics.FromImage(new Bitmap(1, 1)))
            {
                Region[] zero = Measure_Helper(gfx, new RectangleF(0, 0, 0, 0));
                Assert.Equal(3, zero.Length);

                Region[] small = Measure_Helper(gfx, new RectangleF(0, 0, 100, 100));
                Assert.Equal(3, small.Length);
                for (int i = 0; i < 3; i++)
                {
                    RectangleF zb = zero[i].GetBounds(gfx);
                    RectangleF sb = small[i].GetBounds(gfx);
                    Assert.Equal(sb.X, zb.X);
                    Assert.Equal(sb.Y, zb.Y);
                    Assert.Equal(sb.Width, zb.Width);
                    Assert.Equal(sb.Height, zb.Height);
                }

                Region[] max = Measure_Helper(gfx, new RectangleF(0, 0, float.MaxValue, float.MaxValue));
                Assert.Equal(3, max.Length);
                for (int i = 0; i < 3; i++)
                {
                    RectangleF zb = zero[i].GetBounds(gfx);
                    RectangleF mb = max[i].GetBounds(gfx);
                    Assert.Equal(mb.X, zb.X);
                    Assert.Equal(mb.Y, zb.Y);
                    Assert.Equal(mb.Width, zb.Width);
                    Assert.Equal(mb.Height, zb.Height);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MeasureLimits()
        {
            using (Graphics gfx = Graphics.FromImage(new Bitmap(1, 1)))
            {
                Region[] min = Measure_Helper(gfx, new RectangleF(0, 0, float.MinValue, float.MinValue));
                Assert.Equal(3, min.Length);
                for (int i = 0; i < 3; i++)
                {
                    RectangleF mb = min[i].GetBounds(gfx);
                    Assert.Equal(-4194304.0f, mb.X);
                    Assert.Equal(-4194304.0f, mb.Y);
                    Assert.Equal(8388608.0f, mb.Width);
                    Assert.Equal(8388608.0f, mb.Height);
                }

                Region[] neg = Measure_Helper(gfx, new RectangleF(0, 0, -20, -20));
                Assert.Equal(3, neg.Length);
                for (int i = 0; i < 3; i++)
                {
                    RectangleF mb = neg[i].GetBounds(gfx);
                    Assert.Equal(-4194304.0f, mb.X);
                    Assert.Equal(-4194304.0f, mb.Y);
                    Assert.Equal(8388608.0f, mb.Width);
                    Assert.Equal(8388608.0f, mb.Height);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawString_EndlessLoop()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            using (StringFormat fmt = new StringFormat())
            {
                Rectangle rect = Rectangle.Empty;
                rect.Location = new Point(10, 10);
                rect.Size = new Size(1, 20);
                fmt.Alignment = StringAlignment.Center;
                fmt.LineAlignment = StringAlignment.Center;
                fmt.FormatFlags = StringFormatFlags.NoWrap;
                fmt.Trimming = StringTrimming.EllipsisWord;
                g.DrawString("Test String", font, Brushes.Black, rect, fmt);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawString_EndlessLoop_Wrapping()
        {
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            using (StringFormat fmt = new StringFormat())
            {
                Rectangle rect = Rectangle.Empty;
                rect.Location = new Point(10, 10);
                rect.Size = new Size(1, 20);
                fmt.Alignment = StringAlignment.Center;
                fmt.LineAlignment = StringAlignment.Center;
                fmt.Trimming = StringTrimming.EllipsisWord;
                g.DrawString("Test String", font, Brushes.Black, rect, fmt);
            }
        }

        [ConditionalFact(Helpers.RecentGdiplusIsAvailable)]
        public void MeasureString_Wrapping_Dots()
        {
            string text = "this is really long text........................................... with a lot o periods.";
            using (Bitmap bitmap = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bitmap))
            using (StringFormat format = new StringFormat())
            {
                format.Alignment = StringAlignment.Center;
                SizeF sz = g.MeasureString(text, font, 80, format);
                Assert.True(sz.Width <= 80);
                Assert.True(sz.Height > font.Height * 2);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetReleaseHdcInternal()
        {
            using (Bitmap b = new Bitmap(10, 10))
            using (Graphics g = Graphics.FromImage(b))
            {
                IntPtr hdc1 = g.GetHdc();
                g.ReleaseHdcInternal(hdc1);
                IntPtr hdc2 = g.GetHdc();
                g.ReleaseHdcInternal(hdc2);
                Assert.Equal(hdc1, hdc2);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ReleaseHdcInternal_IntPtrZero()
        {
            using (Bitmap b = new Bitmap(10, 10))
            using (Graphics g = Graphics.FromImage(b))
            {
                Assert.Throws<ArgumentException>(() => g.ReleaseHdcInternal(IntPtr.Zero));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ReleaseHdcInternal_TwoTimes()
        {
            using (Bitmap b = new Bitmap(10, 10))
            using (Graphics g = Graphics.FromImage(b))
            {
                IntPtr hdc = g.GetHdc();
                g.ReleaseHdcInternal(hdc);
                Assert.Throws<ArgumentException>(() => g.ReleaseHdcInternal(hdc));
            }
        }
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TestReleaseHdc()
        {
            using (Bitmap b = new Bitmap(10, 10))
            using (Graphics g = Graphics.FromImage(b))
            {
                IntPtr hdc1 = g.GetHdc();
                g.ReleaseHdc();
                IntPtr hdc2 = g.GetHdc();
                g.ReleaseHdc();
                Assert.Equal(hdc1, hdc2);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TestReleaseHdcException()
        {
            using (Bitmap b = new Bitmap(10, 10))
            using (Graphics g = Graphics.FromImage(b))
            {
                Assert.Throws<ArgumentException>(() => g.ReleaseHdc());
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TestReleaseHdcException2()
        {
            using (Bitmap b = new Bitmap(10, 10))
            using (Graphics g = Graphics.FromImage(b))
            {
                g.GetHdc();
                g.ReleaseHdc();
                Assert.Throws<ArgumentException>(() => g.ReleaseHdc());
            }
        }
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void VisibleClipBound()
        {
            if (PlatformDetection.IsArmOrArm64Process)
            {
                // [ActiveIssue("https://github.com/dotnet/runtime/issues/28859")]
                throw new SkipTestException("Precision on float numbers");
            }

            // see #78958
            using (Bitmap bmp = new Bitmap(100, 100))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                RectangleF noclip = g.VisibleClipBounds;
                Assert.Equal(0, noclip.X);
                Assert.Equal(0, noclip.Y);
                Assert.Equal(100, noclip.Width);
                Assert.Equal(100, noclip.Height);

                // note: libgdiplus regions are precise to multiple of multiple of 8
                g.Clip = new Region(new RectangleF(0, 0, 32, 32));
                RectangleF clip = g.VisibleClipBounds;
                Assert.Equal(0, clip.X);
                Assert.Equal(0, clip.Y);
                Assert.Equal(32, clip.Width, 4);
                Assert.Equal(32, clip.Height, 4);

                g.RotateTransform(90);
                RectangleF rotclip = g.VisibleClipBounds;
                Assert.Equal(0, rotclip.X);
                Assert.Equal(-32, rotclip.Y, 4);
                Assert.Equal(32, rotclip.Width, 4);
                Assert.Equal(32, rotclip.Height, 4);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void VisibleClipBound_BigClip()
        {
            if (PlatformDetection.IsArmOrArm64Process)
            {
                //ActiveIssue: 35744
                throw new SkipTestException("Precision on float numbers");
            }

            using (Bitmap bmp = new Bitmap(100, 100))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                RectangleF noclip = g.VisibleClipBounds;
                Assert.Equal(0, noclip.X);
                Assert.Equal(0, noclip.Y);
                Assert.Equal(100, noclip.Width);
                Assert.Equal(100, noclip.Height);

                // clip is larger than bitmap
                g.Clip = new Region(new RectangleF(0, 0, 200, 200));
                RectangleF clipbound = g.ClipBounds;
                Assert.Equal(0, clipbound.X);
                Assert.Equal(0, clipbound.Y);
                Assert.Equal(200, clipbound.Width);
                Assert.Equal(200, clipbound.Height);

                RectangleF clip = g.VisibleClipBounds;
                Assert.Equal(0, clip.X);
                Assert.Equal(0, clip.Y);
                Assert.Equal(100, clip.Width);
                Assert.Equal(100, clip.Height);

                g.RotateTransform(90);
                RectangleF rotclipbound = g.ClipBounds;
                Assert.Equal(0, rotclipbound.X);
                Assert.Equal(-200, rotclipbound.Y, 4);
                Assert.Equal(200, rotclipbound.Width, 4);
                Assert.Equal(200, rotclipbound.Height, 4);

                RectangleF rotclip = g.VisibleClipBounds;
                Assert.Equal(0, rotclip.X);
                Assert.Equal(-100, rotclip.Y, 4);
                Assert.Equal(100, rotclip.Width, 4);
                Assert.Equal(100, rotclip.Height, 4);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Rotate()
        {
            if (PlatformDetection.IsArmOrArm64Process)
            {
                //ActiveIssue: 35744
                throw new SkipTestException("Precision on float numbers");
            }

            using (Bitmap bmp = new Bitmap(100, 50))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                RectangleF vcb = g.VisibleClipBounds;
                Assert.Equal(0, vcb.X);
                Assert.Equal(0, vcb.Y);
                Assert.Equal(100, vcb.Width, 4);
                Assert.Equal(50, vcb.Height, 4);

                g.RotateTransform(90);
                RectangleF rvcb = g.VisibleClipBounds;
                Assert.Equal(0, rvcb.X);
                Assert.Equal(-100, rvcb.Y, 4);
                Assert.Equal(50.0f, rvcb.Width, 4);
                Assert.Equal(100, rvcb.Height, 4);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Scale()
        {
            using (Bitmap bmp = new Bitmap(100, 50))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                RectangleF vcb = g.VisibleClipBounds;
                Assert.Equal(0, vcb.X);
                Assert.Equal(0, vcb.Y);
                Assert.Equal(100, vcb.Width);
                Assert.Equal(50, vcb.Height);

                g.ScaleTransform(2, 0.5f);
                RectangleF svcb = g.VisibleClipBounds;
                Assert.Equal(0, svcb.X);
                Assert.Equal(0, svcb.Y);
                Assert.Equal(50, svcb.Width);
                Assert.Equal(100, svcb.Height);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Translate()
        {
            using (Bitmap bmp = new Bitmap(100, 50))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                RectangleF vcb = g.VisibleClipBounds;
                Assert.Equal(0, vcb.X);
                Assert.Equal(0, vcb.Y);
                Assert.Equal(100, vcb.Width);
                Assert.Equal(50, vcb.Height);

                g.TranslateTransform(-25, 25);
                RectangleF tvcb = g.VisibleClipBounds;
                Assert.Equal(25, tvcb.X);
                Assert.Equal(-25, tvcb.Y);
                Assert.Equal(100, tvcb.Width);
                Assert.Equal(50, tvcb.Height);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawIcon_NullRectangle()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawIcon(null, new Rectangle(0, 0, 32, 32)));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawIcon_IconRectangle()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawIcon(SystemIcons.Application, new Rectangle(0, 0, 40, 20));
                // Rectangle is empty when X, Y, Width and Height == 0
                // (yep X and Y too, RectangleF only checks for Width and Height)
                g.DrawIcon(SystemIcons.Asterisk, new Rectangle(0, 0, 0, 0));
                // so this one is half-empty ;-)
                g.DrawIcon(SystemIcons.Error, new Rectangle(20, 40, 0, 0));
                // negative width or height isn't empty (for Rectangle)
                g.DrawIconUnstretched(SystemIcons.WinLogo, new Rectangle(10, 20, -1, 0));
                g.DrawIconUnstretched(SystemIcons.WinLogo, new Rectangle(20, 10, 0, -1));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawIcon_NullIntInt()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawIcon(null, 4, 2));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawIcon_IconIntInt()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawIcon(SystemIcons.Exclamation, 4, 2);
                g.DrawIcon(SystemIcons.Hand, 0, 0);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawIconUnstretched_NullRectangle()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawIconUnstretched(null, new Rectangle(0, 0, 40, 20)));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawIconUnstretched_IconRectangle()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawIconUnstretched(SystemIcons.Information, new Rectangle(0, 0, 40, 20));
                // Rectangle is empty when X, Y, Width and Height == 0
                // (yep X and Y too, RectangleF only checks for Width and Height)
                g.DrawIconUnstretched(SystemIcons.Question, new Rectangle(0, 0, 0, 0));
                // so this one is half-empty ;-)
                g.DrawIconUnstretched(SystemIcons.Warning, new Rectangle(20, 40, 0, 0));
                // negative width or height isn't empty (for Rectangle)
                g.DrawIconUnstretched(SystemIcons.WinLogo, new Rectangle(10, 20, -1, 0));
                g.DrawIconUnstretched(SystemIcons.WinLogo, new Rectangle(20, 10, 0, -1));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_NullRectangleF()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImage(null, new RectangleF(0, 0, 0, 0)));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImageRectangleF()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(bmp, new RectangleF(0, 0, 0, 0));
                g.DrawImage(bmp, new RectangleF(20, 40, 0, 0));
                g.DrawImage(bmp, new RectangleF(10, 20, -1, 0));
                g.DrawImage(bmp, new RectangleF(20, 10, 0, -1));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_NullPointF()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImage(null, new PointF(0, 0)));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointF()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(bmp, new PointF(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_NullPointFArray()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImage(null, new PointF[0]));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointFArrayNull()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    Assert.Throws<ArgumentNullException>(() => g.DrawImage(bmp, (PointF[])null));
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointFArrayEmpty()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentException>(() => g.DrawImage(bmp, new PointF[0]));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointFArray()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(bmp, new PointF[] {
                        new PointF (0, 0), new PointF (1, 1), new PointF (2, 2) });
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_NullRectangle()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImage(null, new Rectangle(0, 0, 0, 0)));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImageRectangle()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // Rectangle is empty when X, Y, Width and Height == 0
                // (yep X and Y too, RectangleF only checks for Width and Height)
                g.DrawImage(bmp, new Rectangle(0, 0, 0, 0));
                // so this one is half-empty ;-)
                g.DrawImage(bmp, new Rectangle(20, 40, 0, 0));
                // negative width or height isn't empty (for Rectangle)
                g.DrawImage(bmp, new Rectangle(10, 20, -1, 0));
                g.DrawImage(bmp, new Rectangle(20, 10, 0, -1));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_NullPoint()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImage(null, new Point(0, 0)));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePoint()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(bmp, new Point(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_NullPointArray()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImage(null, new Point[0]));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointArrayNull()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImage(bmp, (Point[])null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointArrayEmpty()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentException>(() => g.DrawImage(bmp, new Point[0]));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointArray()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(bmp, new Point[] {
                        new Point (0, 0), new Point (1, 1), new Point (2, 2) });
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_NullIntInt()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImage(null, int.MaxValue, int.MinValue));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImageIntInt_Overflow()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<OverflowException>(() => g.DrawImage(bmp, int.MaxValue, int.MinValue));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImageIntInt()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(bmp, -40, -40);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_NullFloat()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImage(null, float.MaxValue, float.MinValue));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImageFloatFloat_Overflow()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<OverflowException>(() => g.DrawImage(bmp, float.MaxValue, float.MinValue));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImageFloatFloat()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(bmp, -40.0f, -40.0f);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_NullRectangleRectangleGraphicsUnit()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImage(null, new Rectangle(), new Rectangle(), GraphicsUnit.Display));
            }
        }

        private void DrawImage_ImageRectangleRectangleGraphicsUnit(GraphicsUnit unit)
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Rectangle r = new Rectangle(0, 0, 40, 40);
                g.DrawImage(bmp, r, r, unit);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImageRectangleRectangleGraphicsUnit_Display()
        {
            Assert.Throws<ArgumentException>(() => DrawImage_ImageRectangleRectangleGraphicsUnit(GraphicsUnit.Display));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImageRectangleRectangleGraphicsUnit_Pixel()
        {
            // this unit works
            DrawImage_ImageRectangleRectangleGraphicsUnit(GraphicsUnit.Pixel);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImageRectangleRectangleGraphicsUnit_World()
        {
            Assert.Throws<ArgumentException>(() => DrawImage_ImageRectangleRectangleGraphicsUnit(GraphicsUnit.World));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_NullPointRectangleGraphicsUnit()
        {
            Rectangle r = new Rectangle(1, 2, 3, 4);
            Point[] pts = new Point[3] { new Point(1, 1), new Point(2, 2), new Point(3, 3) };
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImage(null, pts, r, GraphicsUnit.Pixel));
            }
        }

        private void DrawImage_ImagePointRectangleGraphicsUnit(Point[] pts)
        {
            Rectangle r = new Rectangle(1, 2, 3, 4);
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(bmp, pts, r, GraphicsUnit.Pixel);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImageNullRectangleGraphicsUnit()
        {
            Assert.Throws<ArgumentNullException>(() => DrawImage_ImagePointRectangleGraphicsUnit(null));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePoint0RectangleGraphicsUnit()
        {
            Assert.Throws<ArgumentException>(() => DrawImage_ImagePointRectangleGraphicsUnit(new Point[0]));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePoint1RectangleGraphicsUnit()
        {
            Point p = new Point(1, 1);
            Assert.Throws<ArgumentException>(() => DrawImage_ImagePointRectangleGraphicsUnit(new Point[1] { p }));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePoint2RectangleGraphicsUnit()
        {
            Point p = new Point(1, 1);
            Assert.Throws<ArgumentException>(() => DrawImage_ImagePointRectangleGraphicsUnit(new Point[2] { p, p }));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePoint3RectangleGraphicsUnit()
        {
            Point p = new Point(1, 1);
            DrawImage_ImagePointRectangleGraphicsUnit(new Point[3] { p, p, p });
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePoint4RectangleGraphicsUnit()
        {
            Point p = new Point(1, 1);
            Assert.Throws<NotImplementedException>(() => DrawImage_ImagePointRectangleGraphicsUnit(new Point[4] { p, p, p, p }));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_NullPointFRectangleGraphicsUnit()
        {
            Rectangle r = new Rectangle(1, 2, 3, 4);
            PointF[] pts = new PointF[3] { new PointF(1, 1), new PointF(2, 2), new PointF(3, 3) };
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImage(null, pts, r, GraphicsUnit.Pixel));
            }
        }

        private void DrawImage_ImagePointFRectangleGraphicsUnit(PointF[] pts)
        {
            Rectangle r = new Rectangle(1, 2, 3, 4);
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(bmp, pts, r, GraphicsUnit.Pixel);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImageNullFRectangleGraphicsUnit()
        {
            Assert.Throws<ArgumentNullException>(() => DrawImage_ImagePointFRectangleGraphicsUnit(null));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointF0RectangleGraphicsUnit()
        {
            Assert.Throws<ArgumentException>(() => DrawImage_ImagePointFRectangleGraphicsUnit(new PointF[0]));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointF1RectangleGraphicsUnit()
        {
            PointF p = new PointF(1, 1);
            Assert.Throws<ArgumentException>(() => DrawImage_ImagePointFRectangleGraphicsUnit(new PointF[1] { p }));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointF2RectangleGraphicsUnit()
        {
            PointF p = new PointF(1, 1);
            Assert.Throws<ArgumentException>(() => DrawImage_ImagePointFRectangleGraphicsUnit(new PointF[2] { p, p }));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointF3RectangleGraphicsUnit()
        {
            PointF p = new PointF(1, 1);
            DrawImage_ImagePointFRectangleGraphicsUnit(new PointF[3] { p, p, p });
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointF4RectangleGraphicsUnit()
        {
            PointF p = new PointF(1, 1);
            Assert.Throws<NotImplementedException>(() => DrawImage_ImagePointFRectangleGraphicsUnit(new PointF[4] { p, p, p, p }));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointRectangleGraphicsUnitNull()
        {
            Point p = new Point(1, 1);
            Point[] pts = new Point[3] { p, p, p };
            Rectangle r = new Rectangle(1, 2, 3, 4);
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImage(bmp, pts, r, GraphicsUnit.Pixel, null);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImage_ImagePointRectangleGraphicsUnitAttributes()
        {
            Point p = new Point(1, 1);
            Point[] pts = new Point[3] { p, p, p };
            Rectangle r = new Rectangle(1, 2, 3, 4);
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            using (ImageAttributes ia = new ImageAttributes())
            {
                g.DrawImage(bmp, pts, r, GraphicsUnit.Pixel, ia);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImageUnscaled_NullPoint()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImageUnscaled(null, new Point(0, 0)));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImageUnscaled_ImagePoint()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImageUnscaled(bmp, new Point(0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImageUnscaled_NullRectangle()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImageUnscaled(null, new Rectangle(0, 0, -1, -1)));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImageUnscaled_ImageRectangle()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImageUnscaled(bmp, new Rectangle(0, 0, -1, -1));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImageUnscaled_NullIntInt()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImageUnscaled(null, 0, 0));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImageUnscaled_ImageIntInt()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImageUnscaled(bmp, 0, 0);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImageUnscaled_NullIntIntIntInt()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImageUnscaled(null, 0, 0, -1, -1));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImageUnscaled_ImageIntIntIntInt()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.DrawImageUnscaled(bmp, 0, 0, -1, -1);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImageUnscaledAndClipped_Null()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawImageUnscaledAndClipped(null, new Rectangle(0, 0, 0, 0)));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawImageUnscaledAndClipped()
        {
            using (Bitmap bmp = new Bitmap(40, 40))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                // Rectangle is empty when X, Y, Width and Height == 0
                // (yep X and Y too, RectangleF only checks for Width and Height)
                g.DrawImageUnscaledAndClipped(bmp, new Rectangle(0, 0, 0, 0));
                // so this one is half-empty ;-)
                g.DrawImageUnscaledAndClipped(bmp, new Rectangle(20, 40, 0, 0));
                // negative width or height isn't empty (for Rectangle)
                g.DrawImageUnscaledAndClipped(bmp, new Rectangle(10, 20, -1, 0));
                g.DrawImageUnscaledAndClipped(bmp, new Rectangle(20, 10, 0, -1));
                // smaller
                g.DrawImageUnscaledAndClipped(bmp, new Rectangle(0, 0, 10, 20));
                g.DrawImageUnscaledAndClipped(bmp, new Rectangle(0, 0, 40, 10));
                g.DrawImageUnscaledAndClipped(bmp, new Rectangle(0, 0, 80, 20));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawPath_Pen_Null()
        {
            using (Bitmap bmp = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bmp))
            using (GraphicsPath path = new GraphicsPath())
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawPath(null, path));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawPath_Path_Null()
        {
            using (Bitmap bmp = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.DrawPath(Pens.Black, null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void DrawPath_Arcs()
        {
            using (Bitmap bmp = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bmp))
            using (GraphicsPath path = new GraphicsPath())
            {
                int d = 5;
                Rectangle baserect = new Rectangle(0, 0, 19, 19);
                Rectangle arcrect = new Rectangle(baserect.Location, new Size(d, d));

                path.AddArc(arcrect, 180, 90);
                arcrect.X = baserect.Right - d;
                path.AddArc(arcrect, 270, 90);
                arcrect.Y = baserect.Bottom - d;
                path.AddArc(arcrect, 0, 90);
                arcrect.X = baserect.Left;
                path.AddArc(arcrect, 90, 90);
                path.CloseFigure();
                g.Clear(Color.White);
                g.DrawPath(Pens.SteelBlue, path);

                Assert.Equal(-12156236, bmp.GetPixel(0, 9).ToArgb());
                Assert.Equal(-1, bmp.GetPixel(1, 9).ToArgb());
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillPath_Brush_Null()
        {
            using (Bitmap bmp = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bmp))
            using (GraphicsPath path = new GraphicsPath())
            {
                Assert.Throws<ArgumentNullException>(() => g.FillPath(null, path));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillPath_Path_Null()
        {
            using (Bitmap bmp = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Assert.Throws<ArgumentNullException>(() => g.FillPath(Brushes.Black, null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FillPath_Arcs()
        {
            using (Bitmap bmp = new Bitmap(20, 20))
            using (Graphics g = Graphics.FromImage(bmp))
            using (GraphicsPath path = new GraphicsPath())
            {
                int d = 5;
                Rectangle baserect = new Rectangle(0, 0, 19, 19);
                Rectangle arcrect = new Rectangle(baserect.Location, new Size(d, d));

                path.AddArc(arcrect, 180, 90);
                arcrect.X = baserect.Right - d;
                path.AddArc(arcrect, 270, 90);
                arcrect.Y = baserect.Bottom - d;
                path.AddArc(arcrect, 0, 90);
                arcrect.X = baserect.Left;
                path.AddArc(arcrect, 90, 90);
                path.CloseFigure();
                g.Clear(Color.White);
                g.FillPath(Brushes.SteelBlue, path);

                Assert.Equal(-12156236, bmp.GetPixel(0, 9).ToArgb());
                Assert.Equal(-12156236, bmp.GetPixel(1, 9).ToArgb());
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/22221", TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void TransformPoints()
        {
            using (Bitmap bmp = new Bitmap(10, 10))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                Point[] pts = new Point[5];
                PointF[] ptf = new PointF[5];
                for (int i = 0; i < 5; i++)
                {
                    pts[i] = new Point(i, i);
                    ptf[i] = new PointF(i, i);
                }

                g.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Device, pts);
                g.TransformPoints(CoordinateSpace.Page, CoordinateSpace.Device, ptf);

                for (int i = 0; i < 5; i++)
                {
                    Assert.Equal(i, pts[i].X);
                    Assert.Equal(i, pts[i].Y);
                    Assert.Equal(i, ptf[i].X);
                    Assert.Equal(i, ptf[i].Y);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Dpi()
        {
            float x, y;
            using (Bitmap bmp = new Bitmap(10, 10))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    x = g.DpiX - 10;
                    y = g.DpiY + 10;
                }
                bmp.SetResolution(x, y);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    Assert.Equal(x, g.DpiX);
                    Assert.Equal(y, g.DpiY);
                }
            }
        }
    }

    public class GraphicsFullTrustTest
    {
        // note: this test would fail, on ReleaseHdc, without fulltrust
        // i.e. it's a demand and not a linkdemand
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetReleaseHdc()
        {
            using (Bitmap b = new Bitmap(100, 100))
            {
                using (Graphics g = Graphics.FromImage(b))
                {
                    IntPtr hdc1 = g.GetHdc();
                    g.ReleaseHdc(hdc1);
                    IntPtr hdc2 = g.GetHdc();
                    g.ReleaseHdc(hdc2);
                    Assert.Equal(hdc1, hdc2);
                }
            }
        }
    }
}

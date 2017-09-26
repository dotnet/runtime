// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Drawing.Tests
{
    public class BufferedGraphicsTests
    {
        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Dispose_TempMultipleTimes_Success()
        {
            using (var context = new BufferedGraphicsContext())
            using (var image = new Bitmap(3, 3))
            using (Graphics targetGraphics = Graphics.FromImage(image))
            {
                BufferedGraphics graphics = context.Allocate(targetGraphics, Rectangle.Empty);
                Assert.NotNull(graphics.Graphics);

                graphics.Dispose();
                Assert.Null(graphics.Graphics);

                graphics.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Dispose_ActualMultipleTimes_Success()
        {
            using (var context = new BufferedGraphicsContext())
            using (var image = new Bitmap(3, 3))
            using (Graphics targetGraphics = Graphics.FromImage(image))
            {
                BufferedGraphics graphics = context.Allocate(targetGraphics, new Rectangle(0, 0, context.MaximumBuffer.Width + 1, context.MaximumBuffer.Height + 1));
                Assert.NotNull(graphics.Graphics);

                graphics.Dispose();
                Assert.Null(graphics.Graphics);

                graphics.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Render_ParameterlessWithTargetGraphics_Success()
        {
            Color color = Color.FromArgb(255, 0, 0, 0);

            using (var context = new BufferedGraphicsContext())
            using (var image = new Bitmap(3, 3))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var brush = new SolidBrush(Color.Red))
            {
                graphics.FillRectangle(brush, new Rectangle(0, 0, 3, 3));

                using (BufferedGraphics bufferedGraphics = context.Allocate(graphics, new Rectangle(0, 0, 3, 3)))
                {
                    bufferedGraphics.Render();

                    Helpers.VerifyBitmap(image, new Color[][]
                    {
                        new Color[] { color, color, color },
                        new Color[] { color, color, color },
                        new Color[] { color, color, color }
                    });
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Render_ParameterlessWithNullTargetGraphics_Success()
        {
            Color color = Color.FromArgb(255, 0, 0, 0);

            using (var context = new BufferedGraphicsContext())
            using (var image = new Bitmap(3, 3))
            using (Graphics graphics = Graphics.FromImage(image))
            using (var brush = new SolidBrush(Color.Red))
            {
                graphics.FillRectangle(brush, new Rectangle(0, 0, 3, 3));
                try
                {
                    IntPtr hdc = graphics.GetHdc();

                    using (BufferedGraphics bufferedGraphics = context.Allocate(hdc, new Rectangle(0, 0, 3, 3)))
                    {
                        bufferedGraphics.Render();
                    }
                }
                finally
                {
                    graphics.ReleaseHdc();
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Render_TargetGraphics_Success()
        {
            Color color = Color.FromArgb(255, 0, 0, 0);

            using (var context = new BufferedGraphicsContext())
            using (var originalImage = new Bitmap(3, 3))
            using (var targetImage = new Bitmap(3, 3))
            using (Graphics originalGraphics = Graphics.FromImage(originalImage))
            using (Graphics targetGraphics = Graphics.FromImage(targetImage))
            using (var brush = new SolidBrush(Color.Red))
            {
                originalGraphics.FillRectangle(brush, new Rectangle(0, 0, 3, 3));

                using (BufferedGraphics graphics = context.Allocate(originalGraphics, new Rectangle(0, 0, 3, 3)))
                {
                    graphics.Render(targetGraphics);

                    Helpers.VerifyBitmap(targetImage, new Color[][]
                    {
                        new Color[] { color, color, color },
                        new Color[] { color, color, color },
                        new Color[] { color, color, color }
                    });
                }
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Render_NullGraphics_Nop()
        {
            using (var context = new BufferedGraphicsContext())
            using (BufferedGraphics graphics = context.Allocate(null, Rectangle.Empty))
            {
                graphics.Render(null);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.GdiplusIsAvailable)]
        public void Render_InvalidTargetDC_Nop()
        {
            using (var context = new BufferedGraphicsContext())
            using (BufferedGraphics graphics = context.Allocate(null, Rectangle.Empty))
            {
                graphics.Render(IntPtr.Zero);
                graphics.Render((IntPtr)(-1));
            }
        }
    }
}

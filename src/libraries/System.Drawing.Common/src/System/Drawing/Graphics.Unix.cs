// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Graphics.cs
//
// Authors:
//    Gonzalo Paniagua Javier (gonzalo@ximian.com) (stubbed out)
//      Alexandre Pigolkine(pigolkine@gmx.de)
//    Jordi Mas i Hernandez (jordi@ximian.com)
//    Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2003 Ximian, Inc. (http://www.ximian.com)
// Copyright (C) 2004-2006 Novell, Inc. (http://www.novell.com)
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

using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.Drawing.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing
{
    public sealed partial class Graphics : MarshalByRefObject, IDisposable
    , IDeviceContext
    {
        internal IMacContext maccontext;
        private bool disposed = false;
        private static float defDpiX = 0;
        private static float defDpiY = 0;

        public delegate bool EnumerateMetafileProc(EmfPlusRecordType recordType,
                                int flags,
                                int dataSize,
                                IntPtr data,
                                PlayRecordCallback callbackData);

        public delegate bool DrawImageAbort(IntPtr callbackdata);

        internal Graphics(IntPtr nativeGraphics) => NativeGraphics = nativeGraphics;

        ~Graphics()
        {
            Dispose();
        }

        static internal float systemDpiX
        {
            get
            {
                if (defDpiX == 0)
                {
                    Bitmap bmp = new Bitmap(1, 1);
                    Graphics g = Graphics.FromImage(bmp);
                    defDpiX = g.DpiX;
                    defDpiY = g.DpiY;
                }
                return defDpiX;
            }
        }

        static internal float systemDpiY
        {
            get
            {
                if (defDpiY == 0)
                {
                    Bitmap bmp = new Bitmap(1, 1);
                    Graphics g = Graphics.FromImage(bmp);
                    defDpiX = g.DpiX;
                    defDpiY = g.DpiY;
                }
                return defDpiY;
            }
        }

        public void AddMetafileComment(byte[] data)
        {
            throw new NotImplementedException();
        }

        public GraphicsContainer BeginContainer()
        {
            int state;
            int status = Gdip.GdipBeginContainer2(new HandleRef(this, NativeGraphics), out state);
            Gdip.CheckStatus(status);

            return new GraphicsContainer(state);
        }

        public GraphicsContainer BeginContainer(Rectangle dstrect, Rectangle srcrect, GraphicsUnit unit)
        {
            int state;

            int status = Gdip.GdipBeginContainerI(new HandleRef(this, NativeGraphics), ref dstrect, ref srcrect, unit, out state);
            Gdip.CheckStatus(status);

            return new GraphicsContainer(state);
        }

        public GraphicsContainer BeginContainer(RectangleF dstrect, RectangleF srcrect, GraphicsUnit unit)
        {
            int state;

            int status = Gdip.GdipBeginContainer(new HandleRef(this, NativeGraphics), ref dstrect, ref srcrect, unit, out state);
            Gdip.CheckStatus(status);

            return new GraphicsContainer(state);
        }

        public void Clear(Color color)
        {
            int status;
            status = Gdip.GdipGraphicsClear(NativeGraphics, color.ToArgb());
            Gdip.CheckStatus(status);
        }

        public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize)
        {
            CopyFromScreen(upperLeftSource.X, upperLeftSource.Y, upperLeftDestination.X, upperLeftDestination.Y,
                blockRegionSize, CopyPixelOperation.SourceCopy);
        }

        public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            CopyFromScreen(upperLeftSource.X, upperLeftSource.Y, upperLeftDestination.X, upperLeftDestination.Y,
                blockRegionSize, copyPixelOperation);
        }

        public void CopyFromScreen(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize)
        {
            CopyFromScreen(sourceX, sourceY, destinationX, destinationY, blockRegionSize,
                CopyPixelOperation.SourceCopy);
        }

        public void CopyFromScreen(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            if (!Enum.IsDefined(typeof(CopyPixelOperation), copyPixelOperation))
                throw new InvalidEnumArgumentException(string.Format("Enum argument value '{0}' is not valid for CopyPixelOperation", copyPixelOperation));

            if (Gdip.UseX11Drawable)
            {
                CopyFromScreenX11(sourceX, sourceY, destinationX, destinationY, blockRegionSize, copyPixelOperation);
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        private void CopyFromScreenX11(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            IntPtr window, image, defvisual, vPtr;
            int AllPlanes = ~0, nitems = 0, pixel;

            if (copyPixelOperation != CopyPixelOperation.SourceCopy)
                throw new NotImplementedException("Operation not implemented under X11");

            if (Gdip.Display == IntPtr.Zero)
            {
                Gdip.Display = LibX11Functions.XOpenDisplay(IntPtr.Zero);
            }

            window = LibX11Functions.XRootWindow(Gdip.Display, 0);
            defvisual = LibX11Functions.XDefaultVisual(Gdip.Display, 0);
            XVisualInfo visual = new XVisualInfo();

            /* Get XVisualInfo for this visual */
            visual.visualid = LibX11Functions.XVisualIDFromVisual(defvisual);
            vPtr = LibX11Functions.XGetVisualInfo(Gdip.Display, 0x1 /* VisualIDMask */, ref visual, ref nitems);
            visual = (XVisualInfo)Marshal.PtrToStructure(vPtr, typeof(XVisualInfo));
            image = LibX11Functions.XGetImage(Gdip.Display, window, sourceX, sourceY, blockRegionSize.Width,
                blockRegionSize.Height, AllPlanes, 2 /* ZPixmap*/);
            if (image == IntPtr.Zero)
            {
                string s = string.Format("XGetImage returned NULL when asked to for a {0}x{1} region block",
                    blockRegionSize.Width, blockRegionSize.Height);
                throw new InvalidOperationException(s);
            }

            Bitmap bmp = new Bitmap(blockRegionSize.Width, blockRegionSize.Height);
            int red, blue, green;
            int red_mask = (int)visual.red_mask;
            int blue_mask = (int)visual.blue_mask;
            int green_mask = (int)visual.green_mask;
            for (int y = 0; y < blockRegionSize.Height; y++)
            {
                for (int x = 0; x < blockRegionSize.Width; x++)
                {
                    pixel = LibX11Functions.XGetPixel(image, x, y);

                    switch (visual.depth)
                    {
                        case 16: /* 16bbp pixel transformation */
                            red = (int)((pixel & red_mask) >> 8) & 0xff;
                            green = (int)(((pixel & green_mask) >> 3)) & 0xff;
                            blue = (int)((pixel & blue_mask) << 3) & 0xff;
                            break;
                        case 24:
                        case 32:
                            red = (int)((pixel & red_mask) >> 16) & 0xff;
                            green = (int)(((pixel & green_mask) >> 8)) & 0xff;
                            blue = (int)((pixel & blue_mask)) & 0xff;
                            break;
                        default:
                            string text = string.Format("{0}bbp depth not supported.", visual.depth);
                            throw new NotImplementedException(text);
                    }

                    bmp.SetPixel(x, y, Color.FromArgb(255, red, green, blue));
                }
            }

            DrawImage(bmp, destinationX, destinationY);
            bmp.Dispose();
            LibX11Functions.XDestroyImage(image);
            LibX11Functions.XFree(vPtr);
        }

        public void Dispose()
        {
            int status;
            if (!disposed)
            {
                if (_nativeHdc != IntPtr.Zero) // avoid a handle leak.
                {
                    ReleaseHdc();
                }

                if (!Gdip.UseX11Drawable)
                {
                    Flush();
                    if (maccontext != null)
                        maccontext.Release();
                }

                status = Gdip.GdipDeleteGraphics(NativeGraphics);
                NativeGraphics = IntPtr.Zero;
                Gdip.CheckStatus(status);
                disposed = true;
            }

            GC.SuppressFinalize(this);
        }


        public void DrawArc(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }


        public void DrawArc(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
        {
            DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }


        public void DrawArc(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            int status;
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            status = Gdip.GdipDrawArc(NativeGraphics, pen.NativePen,
                                        x, y, width, height, startAngle, sweepAngle);
            Gdip.CheckStatus(status);
        }

        // Microsoft documentation states that the signature for this member should be
        // public void DrawArc( Pen pen,  int x,  int y,  int width,  int height,   int startAngle,
        // int sweepAngle. However, GdipDrawArcI uses also float for the startAngle and sweepAngle params
        public void DrawArc(Pen pen, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            int status;
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            status = Gdip.GdipDrawArcI(NativeGraphics, pen.NativePen,
                        x, y, width, height, startAngle, sweepAngle);
            Gdip.CheckStatus(status);
        }

        public void DrawBezier(Pen pen, PointF pt1, PointF pt2, PointF pt3, PointF pt4)
        {
            int status;
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            status = Gdip.GdipDrawBezier(NativeGraphics, pen.NativePen,
                            pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X,
                            pt3.Y, pt4.X, pt4.Y);
            Gdip.CheckStatus(status);
        }

        public void DrawBezier(Pen pen, Point pt1, Point pt2, Point pt3, Point pt4)
        {
            int status;
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            status = Gdip.GdipDrawBezierI(NativeGraphics, pen.NativePen,
                            pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X,
                            pt3.Y, pt4.X, pt4.Y);
            Gdip.CheckStatus(status);
        }

        public void DrawBezier(Pen pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            int status;
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            status = Gdip.GdipDrawBezier(NativeGraphics, pen.NativePen, x1,
                            y1, x2, y2, x3, y3, x4, y4);
            Gdip.CheckStatus(status);
        }

        public void DrawBeziers(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int length = points.Length;
            int status;

            if (length < 4)
                return;

            for (int i = 0; i < length - 1; i += 3)
            {
                Point p1 = points[i];
                Point p2 = points[i + 1];
                Point p3 = points[i + 2];
                Point p4 = points[i + 3];

                status = Gdip.GdipDrawBezier(NativeGraphics,
            pen.NativePen,
                                        p1.X, p1.Y, p2.X, p2.Y,
                                        p3.X, p3.Y, p4.X, p4.Y);
                Gdip.CheckStatus(status);
            }
        }

        public void DrawBeziers(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int length = points.Length;
            int status;

            if (length < 4)
                return;

            for (int i = 0; i < length - 1; i += 3)
            {
                PointF p1 = points[i];
                PointF p2 = points[i + 1];
                PointF p3 = points[i + 2];
                PointF p4 = points[i + 3];

                status = Gdip.GdipDrawBezier(NativeGraphics,
            pen.NativePen,
                                        p1.X, p1.Y, p2.X, p2.Y,
                                        p3.X, p3.Y, p4.X, p4.Y);
                Gdip.CheckStatus(status);
            }
        }


        public void DrawClosedCurve(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int status;
            status = Gdip.GdipDrawClosedCurve(NativeGraphics, pen.NativePen, points, points.Length);
            Gdip.CheckStatus(status);
        }

        public void DrawClosedCurve(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int status;
            status = Gdip.GdipDrawClosedCurveI(NativeGraphics, pen.NativePen, points, points.Length);
            Gdip.CheckStatus(status);
        }

        // according to MSDN fillmode "is required but ignored" which makes _some_ sense since the unmanaged 
        // GDI+ call doesn't support it (issue spotted using Gendarme's AvoidUnusedParametersRule)
        public void DrawClosedCurve(Pen pen, Point[] points, float tension, FillMode fillmode)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int status;
            status = Gdip.GdipDrawClosedCurve2I(NativeGraphics, pen.NativePen, points, points.Length, tension);
            Gdip.CheckStatus(status);
        }

        // according to MSDN fillmode "is required but ignored" which makes _some_ sense since the unmanaged 
        // GDI+ call doesn't support it (issue spotted using Gendarme's AvoidUnusedParametersRule)
        public void DrawClosedCurve(Pen pen, PointF[] points, float tension, FillMode fillmode)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int status;
            status = Gdip.GdipDrawClosedCurve2(NativeGraphics, pen.NativePen, points, points.Length, tension);
            Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int status;
            status = Gdip.GdipDrawCurveI(NativeGraphics, pen.NativePen, points, points.Length);
            Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int status;
            status = Gdip.GdipDrawCurve(NativeGraphics, pen.NativePen, points, points.Length);
            Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, PointF[] points, float tension)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int status;
            status = Gdip.GdipDrawCurve2(NativeGraphics, pen.NativePen, points, points.Length, tension);
            Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, Point[] points, float tension)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int status;
            status = Gdip.GdipDrawCurve2I(NativeGraphics, pen.NativePen, points, points.Length, tension);
            Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int status;
            status = Gdip.GdipDrawCurve3(NativeGraphics, pen.NativePen,
                            points, points.Length, offset,
                            numberOfSegments, 0.5f);
            Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, Point[] points, int offset, int numberOfSegments, float tension)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int status;
            status = Gdip.GdipDrawCurve3I(NativeGraphics, pen.NativePen,
                            points, points.Length, offset,
                            numberOfSegments, tension);
            Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments, float tension)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));

            int status;
            status = Gdip.GdipDrawCurve3(NativeGraphics, pen.NativePen,
                            points, points.Length, offset,
                            numberOfSegments, tension);
            Gdip.CheckStatus(status);
        }

        public void DrawEllipse(Pen pen, Rectangle rect)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawEllipse(Pen pen, RectangleF rect)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawEllipse(Pen pen, int x, int y, int width, int height)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            int status;
            status = Gdip.GdipDrawEllipseI(NativeGraphics, pen.NativePen, x, y, width, height);
            Gdip.CheckStatus(status);
        }

        public void DrawEllipse(Pen pen, float x, float y, float width, float height)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            int status = Gdip.GdipDrawEllipse(NativeGraphics, pen.NativePen, x, y, width, height);
            Gdip.CheckStatus(status);
        }

        public void DrawIcon(Icon icon, Rectangle targetRect)
        {
            if (icon == null)
                throw new ArgumentNullException(nameof(icon));

            DrawImage(icon.GetInternalBitmap(), targetRect);
        }

        public void DrawIcon(Icon icon, int x, int y)
        {
            if (icon == null)
                throw new ArgumentNullException(nameof(icon));

            DrawImage(icon.GetInternalBitmap(), x, y);
        }

        public void DrawIconUnstretched(Icon icon, Rectangle targetRect)
        {
            if (icon == null)
                throw new ArgumentNullException(nameof(icon));

            DrawImageUnscaled(icon.GetInternalBitmap(), targetRect);
        }

        public void DrawImage(Image image, RectangleF rect)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int status = Gdip.GdipDrawImageRect(NativeGraphics, image.nativeImage, rect.X, rect.Y, rect.Width, rect.Height);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, PointF point)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int status = Gdip.GdipDrawImage(NativeGraphics, image.nativeImage, point.X, point.Y);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Point[] destPoints)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));

            int status = Gdip.GdipDrawImagePointsI(NativeGraphics, image.nativeImage, destPoints, destPoints.Length);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Point point)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            DrawImage(image, point.X, point.Y);
        }

        public void DrawImage(Image image, Rectangle rect)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            DrawImage(image, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawImage(Image image, PointF[] destPoints)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            int status = Gdip.GdipDrawImagePoints(NativeGraphics, image.nativeImage, destPoints, destPoints.Length);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, int x, int y)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageI(NativeGraphics, image.nativeImage, x, y);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, float x, float y)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImage(NativeGraphics, image.nativeImage, x, y);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageRectRectI(NativeGraphics, image.nativeImage,
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                srcUnit, IntPtr.Zero, null, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageRectRect(NativeGraphics, image.nativeImage,
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                srcUnit, IntPtr.Zero, null, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));

            int status = Gdip.GdipDrawImagePointsRectI(NativeGraphics, image.nativeImage,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit, IntPtr.Zero,
                null, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));

            int status = Gdip.GdipDrawImagePointsRect(NativeGraphics, image.nativeImage,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit, IntPtr.Zero,
                null, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit,
                                ImageAttributes imageAttr)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            int status = Gdip.GdipDrawImagePointsRectI(NativeGraphics, image.nativeImage,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit,
                imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, null, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, float x, float y, float width, float height)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageRect(NativeGraphics, image.nativeImage, x, y,
                           width, height);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit,
                                ImageAttributes imageAttr)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            int status = Gdip.GdipDrawImagePointsRect(NativeGraphics, image.nativeImage,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit,
                imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, null, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, int x, int y, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImagePointRectI(NativeGraphics, image.nativeImage, x, y, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, srcUnit);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, int x, int y, int width, int height)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageRectI(NativeGraphics, image.nativeImage, x, y, width, height);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, float x, float y, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImagePointRect(NativeGraphics, image.nativeImage, x, y, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, srcUnit);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, ImageAttributes imageAttr, DrawImageAbort callback)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));
            int status = Gdip.GdipDrawImagePointsRect(NativeGraphics, image.nativeImage,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit,
                imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, callback, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit, ImageAttributes imageAttr, DrawImageAbort callback)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));

            int status = Gdip.GdipDrawImagePointsRectI(NativeGraphics, image.nativeImage,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit,
                imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, callback, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit, ImageAttributes imageAttr, DrawImageAbort callback, int callbackData)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            if (destPoints == null)
                throw new ArgumentNullException(nameof(destPoints));

            int status = Gdip.GdipDrawImagePointsRectI(NativeGraphics, image.nativeImage,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit,
                imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, callback, (IntPtr)callbackData);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageRectRect(NativeGraphics, image.nativeImage,
                                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                               srcX, srcY, srcWidth, srcHeight, srcUnit, IntPtr.Zero,
                               null, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, ImageAttributes imageAttr, DrawImageAbort callback, int callbackData)
        {
            int status = Gdip.GdipDrawImagePointsRect(NativeGraphics, image.nativeImage,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit,
                imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, callback, (IntPtr)callbackData);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageRectRectI(NativeGraphics, image.nativeImage,
                                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                               srcX, srcY, srcWidth, srcHeight, srcUnit, IntPtr.Zero,
                               null, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttrs)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageRectRect(NativeGraphics, image.nativeImage,
                                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                               srcX, srcY, srcWidth, srcHeight, srcUnit,
                imageAttrs != null ? imageAttrs.nativeImageAttributes : IntPtr.Zero, null, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttr)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageRectRectI(NativeGraphics, image.nativeImage,
                                        destRect.X, destRect.Y, destRect.Width,
                    destRect.Height, srcX, srcY, srcWidth, srcHeight,
                    srcUnit, imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, null, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttr, DrawImageAbort callback)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageRectRectI(NativeGraphics, image.nativeImage,
                                        destRect.X, destRect.Y, destRect.Width,
                    destRect.Height, srcX, srcY, srcWidth, srcHeight,
                    srcUnit, imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, callback,
                    IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttrs, DrawImageAbort callback)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageRectRect(NativeGraphics, image.nativeImage,
                                        destRect.X, destRect.Y, destRect.Width,
                    destRect.Height, srcX, srcY, srcWidth, srcHeight,
                    srcUnit, imageAttrs != null ? imageAttrs.nativeImageAttributes : IntPtr.Zero,
                    callback, IntPtr.Zero);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttrs, DrawImageAbort callback, IntPtr callbackData)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageRectRect(NativeGraphics, image.nativeImage,
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcX, srcY, srcWidth, srcHeight, srcUnit,
                imageAttrs != null ? imageAttrs.nativeImageAttributes : IntPtr.Zero, callback, callbackData);
            Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttrs, DrawImageAbort callback, IntPtr callbackData)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            int status = Gdip.GdipDrawImageRectRect(NativeGraphics, image.nativeImage,
                               destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcX, srcY, srcWidth, srcHeight, srcUnit,
                imageAttrs != null ? imageAttrs.nativeImageAttributes : IntPtr.Zero, callback, callbackData);
            Gdip.CheckStatus(status);
        }

        public void DrawImageUnscaled(Image image, Point point)
        {
            DrawImageUnscaled(image, point.X, point.Y);
        }

        public void DrawImageUnscaled(Image image, Rectangle rect)
        {
            DrawImageUnscaled(image, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawImageUnscaled(Image image, int x, int y)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));
            DrawImage(image, x, y, image.Width, image.Height);
        }

        public void DrawImageUnscaled(Image image, int x, int y, int width, int height)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            // avoid creating an empty, or negative w/h, bitmap...
            if ((width <= 0) || (height <= 0))
                return;

            using (Image tmpImg = new Bitmap(width, height))
            {
                using (Graphics g = FromImage(tmpImg))
                {
                    g.DrawImage(image, 0, 0, image.Width, image.Height);
                    DrawImage(tmpImg, x, y, width, height);
                }
            }
        }

        public void DrawImageUnscaledAndClipped(Image image, Rectangle rect)
        {
            if (image == null)
                throw new ArgumentNullException(nameof(image));

            int width = (image.Width > rect.Width) ? rect.Width : image.Width;
            int height = (image.Height > rect.Height) ? rect.Height : image.Height;

            DrawImageUnscaled(image, rect.X, rect.Y, width, height);
        }

        public void DrawLine(Pen pen, PointF pt1, PointF pt2)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            int status = Gdip.GdipDrawLine(NativeGraphics, pen.NativePen,
                            pt1.X, pt1.Y, pt2.X, pt2.Y);
            Gdip.CheckStatus(status);
        }

        public void DrawLine(Pen pen, Point pt1, Point pt2)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            int status = Gdip.GdipDrawLineI(NativeGraphics, pen.NativePen,
                            pt1.X, pt1.Y, pt2.X, pt2.Y);
            Gdip.CheckStatus(status);
        }

        public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            int status = Gdip.GdipDrawLineI(NativeGraphics, pen.NativePen, x1, y1, x2, y2);
            Gdip.CheckStatus(status);
        }

        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (!float.IsNaN(x1) && !float.IsNaN(y1) &&
                !float.IsNaN(x2) && !float.IsNaN(y2))
            {
                int status = Gdip.GdipDrawLine(NativeGraphics, pen.NativePen, x1, y1, x2, y2);
                Gdip.CheckStatus(status);
            }
        }

        public void DrawLines(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            int status = Gdip.GdipDrawLines(NativeGraphics, pen.NativePen, points, points.Length);
            Gdip.CheckStatus(status);
        }

        public void DrawLines(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            int status = Gdip.GdipDrawLinesI(NativeGraphics, pen.NativePen, points, points.Length);
            Gdip.CheckStatus(status);
        }

        public void DrawPath(Pen pen, GraphicsPath path)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            int status = Gdip.GdipDrawPath(NativeGraphics, pen.NativePen, path._nativePath);
            Gdip.CheckStatus(status);
        }

        public void DrawPie(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void DrawPie(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void DrawPie(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            int status = Gdip.GdipDrawPie(NativeGraphics, pen.NativePen, x, y, width, height, startAngle, sweepAngle);
            Gdip.CheckStatus(status);
        }

        // Microsoft documentation states that the signature for this member should be
        // public void DrawPie(Pen pen, int x,  int y,  int width,   int height,   int startAngle
        // int sweepAngle. However, GdipDrawPieI uses also float for the startAngle and sweepAngle params
        public void DrawPie(Pen pen, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            int status = Gdip.GdipDrawPieI(NativeGraphics, pen.NativePen, x, y, width, height, startAngle, sweepAngle);
            Gdip.CheckStatus(status);
        }

        public void DrawPolygon(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            int status = Gdip.GdipDrawPolygonI(NativeGraphics, pen.NativePen, points, points.Length);
            Gdip.CheckStatus(status);
        }

        public void DrawPolygon(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            int status = Gdip.GdipDrawPolygon(NativeGraphics, pen.NativePen, points, points.Length);
            Gdip.CheckStatus(status);
        }

        public void DrawRectangle(Pen pen, Rectangle rect)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            DrawRectangle(pen, rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            int status = Gdip.GdipDrawRectangle(NativeGraphics, pen.NativePen, x, y, width, height);
            Gdip.CheckStatus(status);
        }

        public void DrawRectangle(Pen pen, int x, int y, int width, int height)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            int status = Gdip.GdipDrawRectangleI(NativeGraphics, pen.NativePen, x, y, width, height);
            Gdip.CheckStatus(status);
        }

        public void DrawRectangles(Pen pen, RectangleF[] rects)
        {
            if (pen == null)
                throw new ArgumentNullException("image");
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));
            int status = Gdip.GdipDrawRectangles(NativeGraphics, pen.NativePen, rects, rects.Length);
            Gdip.CheckStatus(status);
        }

        public void DrawRectangles(Pen pen, Rectangle[] rects)
        {
            if (pen == null)
                throw new ArgumentNullException("image");
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));
            int status = Gdip.GdipDrawRectanglesI(NativeGraphics, pen.NativePen, rects, rects.Length);
            Gdip.CheckStatus(status);
        }

        public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle)
        {
            DrawString(s, font, brush, layoutRectangle, null);
        }

        public void DrawString(string s, Font font, Brush brush, PointF point)
        {
            DrawString(s, font, brush, new RectangleF(point.X, point.Y, 0, 0), null);
        }

        public void DrawString(string s, Font font, Brush brush, PointF point, StringFormat format)
        {
            DrawString(s, font, brush, new RectangleF(point.X, point.Y, 0, 0), format);
        }

        public void DrawString(string s, Font font, Brush brush, float x, float y)
        {
            DrawString(s, font, brush, new RectangleF(x, y, 0, 0), null);
        }

        public void DrawString(string s, Font font, Brush brush, float x, float y, StringFormat format)
        {
            DrawString(s, font, brush, new RectangleF(x, y, 0, 0), format);
        }

        public void DrawString(string s, Font font, Brush brush, RectangleF layoutRectangle, StringFormat format)
        {
            if (font == null)
                throw new ArgumentNullException(nameof(font));
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (s == null || s.Length == 0)
                return;

            int status = Gdip.GdipDrawString(NativeGraphics, s, s.Length, font.NativeFont, ref layoutRectangle, format != null ? format.nativeFormat : IntPtr.Zero, brush.NativeBrush);
            Gdip.CheckStatus(status);
        }

        public void EndContainer(GraphicsContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            int status = Gdip.GdipEndContainer(new HandleRef(this, NativeGraphics), container.nativeGraphicsContainer);
            Gdip.CheckStatus(status);
        }

        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF destPoint, RectangleF srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point destPoint, Rectangle srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF destPoint, RectangleF srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point destPoint, Rectangle srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, Rectangle srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, Rectangle srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point destPoint, Rectangle srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, RectangleF srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, RectangleF srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF destPoint, RectangleF srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void FillClosedCurve(Brush brush, PointF[] points)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            int status = Gdip.GdipFillClosedCurve(NativeGraphics, brush.NativeBrush, points, points.Length);
            Gdip.CheckStatus(status);
        }

        public void FillClosedCurve(Brush brush, Point[] points)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            int status = Gdip.GdipFillClosedCurveI(NativeGraphics, brush.NativeBrush, points, points.Length);
            Gdip.CheckStatus(status);
        }


        public void FillClosedCurve(Brush brush, PointF[] points, FillMode fillmode)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            FillClosedCurve(brush, points, fillmode, 0.5f);
        }

        public void FillClosedCurve(Brush brush, Point[] points, FillMode fillmode)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            FillClosedCurve(brush, points, fillmode, 0.5f);
        }

        public void FillClosedCurve(Brush brush, PointF[] points, FillMode fillmode, float tension)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            int status = Gdip.GdipFillClosedCurve2(NativeGraphics, brush.NativeBrush, points, points.Length, tension, fillmode);
            Gdip.CheckStatus(status);
        }

        public void FillClosedCurve(Brush brush, Point[] points, FillMode fillmode, float tension)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            int status = Gdip.GdipFillClosedCurve2I(NativeGraphics, brush.NativeBrush, points, points.Length, tension, fillmode);
            Gdip.CheckStatus(status);
        }

        public void FillEllipse(Brush brush, Rectangle rect)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillEllipse(Brush brush, RectangleF rect)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillEllipse(Brush brush, float x, float y, float width, float height)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            int status = Gdip.GdipFillEllipse(NativeGraphics, brush.NativeBrush, x, y, width, height);
            Gdip.CheckStatus(status);
        }

        public void FillEllipse(Brush brush, int x, int y, int width, int height)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            int status = Gdip.GdipFillEllipseI(NativeGraphics, brush.NativeBrush, x, y, width, height);
            Gdip.CheckStatus(status);
        }

        public void FillPath(Brush brush, GraphicsPath path)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            int status = Gdip.GdipFillPath(NativeGraphics, brush.NativeBrush, path._nativePath);
            Gdip.CheckStatus(status);
        }

        public void FillPie(Brush brush, Rectangle rect, float startAngle, float sweepAngle)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            int status = Gdip.GdipFillPie(NativeGraphics, brush.NativeBrush, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
            Gdip.CheckStatus(status);
        }

        public void FillPie(Brush brush, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            int status = Gdip.GdipFillPieI(NativeGraphics, brush.NativeBrush, x, y, width, height, startAngle, sweepAngle);
            Gdip.CheckStatus(status);
        }

        public void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            int status = Gdip.GdipFillPie(NativeGraphics, brush.NativeBrush, x, y, width, height, startAngle, sweepAngle);
            Gdip.CheckStatus(status);
        }

        public void FillPolygon(Brush brush, PointF[] points)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            int status = Gdip.GdipFillPolygon2(NativeGraphics, brush.NativeBrush, points, points.Length);
            Gdip.CheckStatus(status);
        }

        public void FillPolygon(Brush brush, Point[] points)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            int status = Gdip.GdipFillPolygon2I(NativeGraphics, brush.NativeBrush, points, points.Length);
            Gdip.CheckStatus(status);
        }

        public void FillPolygon(Brush brush, Point[] points, FillMode fillMode)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            int status = Gdip.GdipFillPolygonI(NativeGraphics, brush.NativeBrush, points, points.Length, fillMode);
            Gdip.CheckStatus(status);
        }

        public void FillPolygon(Brush brush, PointF[] points, FillMode fillMode)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            int status = Gdip.GdipFillPolygon(NativeGraphics, brush.NativeBrush, points, points.Length, fillMode);
            Gdip.CheckStatus(status);
        }

        public void FillRectangle(Brush brush, RectangleF rect)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            FillRectangle(brush, rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public void FillRectangle(Brush brush, Rectangle rect)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            FillRectangle(brush, rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public void FillRectangle(Brush brush, int x, int y, int width, int height)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            int status = Gdip.GdipFillRectangleI(NativeGraphics, brush.NativeBrush, x, y, width, height);
            Gdip.CheckStatus(status);
        }

        public void FillRectangle(Brush brush, float x, float y, float width, float height)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            int status = Gdip.GdipFillRectangle(NativeGraphics, brush.NativeBrush, x, y, width, height);
            Gdip.CheckStatus(status);
        }

        public void FillRectangles(Brush brush, Rectangle[] rects)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));

            int status = Gdip.GdipFillRectanglesI(NativeGraphics, brush.NativeBrush, rects, rects.Length);
            Gdip.CheckStatus(status);
        }

        public void FillRectangles(Brush brush, RectangleF[] rects)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (rects == null)
                throw new ArgumentNullException(nameof(rects));

            int status = Gdip.GdipFillRectangles(NativeGraphics, brush.NativeBrush, rects, rects.Length);
            Gdip.CheckStatus(status);
        }


        public void FillRegion(Brush brush, Region region)
        {
            if (brush == null)
                throw new ArgumentNullException(nameof(brush));
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            int status = (int)Gdip.GdipFillRegion(new HandleRef(this, NativeGraphics), new HandleRef(brush, brush.NativeBrush), new HandleRef(region, region.NativeRegion));
            Gdip.CheckStatus(status);
        }

        private void FlushCore() => maccontext?.Synchronize();

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdc(IntPtr hdc)
        {
            IntPtr graphics;
            int status = Gdip.GdipCreateFromHDC(hdc, out graphics);
            Gdip.CheckStatus(status);
            return new Graphics(graphics);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdc(IntPtr hdc, IntPtr hdevice)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdcInternal(IntPtr hdc)
        {
            Gdip.Display = hdc;
            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHwnd(IntPtr hwnd)
        {
            IntPtr graphics;

            if (!Gdip.UseX11Drawable)
            {
                CarbonContext context = MacSupport.GetCGContextForView(hwnd);
                Gdip.GdipCreateFromContext_macosx(context.ctx, context.width, context.height, out graphics);

                Graphics g = new Graphics(graphics);
                g.maccontext = context;

                return g;
            }
            else
            {
                if (Gdip.Display == IntPtr.Zero)
                {
                    Gdip.Display = LibX11Functions.XOpenDisplay(IntPtr.Zero);
                    if (Gdip.Display == IntPtr.Zero)
                        throw new NotSupportedException("Could not open display (X-Server required. Check your DISPLAY environment variable)");
                }
                if (hwnd == IntPtr.Zero)
                {
                    hwnd = LibX11Functions.XRootWindow(Gdip.Display, LibX11Functions.XDefaultScreen(Gdip.Display));
                }

                return FromXDrawable(hwnd, Gdip.Display);
            }
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHwndInternal(IntPtr hwnd)
        {
            return FromHwnd(hwnd);
        }

        public static Graphics FromImage(Image image)
        {
            IntPtr graphics;

            if (image == null)
                throw new ArgumentNullException(nameof(image));

            if ((image.PixelFormat & PixelFormat.Indexed) != 0)
                throw new ArgumentException("Cannot create Graphics from an indexed bitmap.", nameof(image));            

            int status = Gdip.GdipGetImageGraphicsContext(image.nativeImage, out graphics);
            Gdip.CheckStatus(status);
            Graphics result = new Graphics(graphics);

            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            Gdip.GdipSetVisibleClip_linux(result.NativeGraphics, ref rect);

            return result;
        }

        internal static Graphics FromXDrawable(IntPtr drawable, IntPtr display)
        {
            IntPtr graphics;

            int s = Gdip.GdipCreateFromXDrawable_linux(drawable, display, out graphics);
            Gdip.CheckStatus(s);
            return new Graphics(graphics);
        }

        public static IntPtr GetHalftonePalette()
        {
            throw new NotImplementedException();
        }

        public Color GetNearestColor(Color color)
        {
            int argb;

            int status = Gdip.GdipGetNearestColor(NativeGraphics, out argb);
            Gdip.CheckStatus(status);

            return Color.FromArgb(argb);
        }

        public Region[] MeasureCharacterRanges(string text, Font font, RectangleF layoutRect, StringFormat stringFormat)
        {
            if ((text == null) || (text.Length == 0))
                return Array.Empty<Region>();

            if (font == null)
                throw new ArgumentNullException(nameof(font));

            if (stringFormat == null)
                throw new ArgumentException(nameof(stringFormat));

            int regcount = stringFormat.GetMeasurableCharacterRangeCount();
            if (regcount == 0)
                return Array.Empty<Region>();

            IntPtr[] native_regions = new IntPtr[regcount];
            Region[] regions = new Region[regcount];

            for (int i = 0; i < regcount; i++)
            {
                regions[i] = new Region();
                native_regions[i] = regions[i].NativeRegion;
            }

            int status = Gdip.GdipMeasureCharacterRanges(NativeGraphics, text, text.Length,
                font.NativeFont, ref layoutRect, stringFormat.nativeFormat, regcount, out native_regions[0]);
            Gdip.CheckStatus(status);

            return regions;
        }

        private unsafe SizeF GdipMeasureString(IntPtr graphics, string text, Font font, ref RectangleF layoutRect,
            IntPtr stringFormat)
        {
            if ((text == null) || (text.Length == 0))
                return SizeF.Empty;

            if (font == null)
                throw new ArgumentNullException(nameof(font));

            RectangleF boundingBox = new RectangleF();

            int status = Gdip.GdipMeasureString(NativeGraphics, text, text.Length, font.NativeFont,
                ref layoutRect, stringFormat, out boundingBox, null, null);
            Gdip.CheckStatus(status);

            return new SizeF(boundingBox.Width, boundingBox.Height);
        }

        public SizeF MeasureString(string text, Font font)
        {
            return MeasureString(text, font, SizeF.Empty);
        }

        public SizeF MeasureString(string text, Font font, SizeF layoutArea)
        {
            RectangleF rect = new RectangleF(0, 0, layoutArea.Width, layoutArea.Height);
            return GdipMeasureString(NativeGraphics, text, font, ref rect, IntPtr.Zero);
        }

        public SizeF MeasureString(string text, Font font, int width)
        {
            RectangleF rect = new RectangleF(0, 0, width, int.MaxValue);
            return GdipMeasureString(NativeGraphics, text, font, ref rect, IntPtr.Zero);
        }

        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat)
        {
            RectangleF rect = new RectangleF(0, 0, layoutArea.Width, layoutArea.Height);
            IntPtr format = (stringFormat == null) ? IntPtr.Zero : stringFormat.nativeFormat;
            return GdipMeasureString(NativeGraphics, text, font, ref rect, format);
        }

        public SizeF MeasureString(string text, Font font, int width, StringFormat format)
        {
            RectangleF rect = new RectangleF(0, 0, width, int.MaxValue);
            IntPtr stringFormat = (format == null) ? IntPtr.Zero : format.nativeFormat;
            return GdipMeasureString(NativeGraphics, text, font, ref rect, stringFormat);
        }

        public SizeF MeasureString(string text, Font font, PointF origin, StringFormat stringFormat)
        {
            RectangleF rect = new RectangleF(origin.X, origin.Y, 0, 0);
            IntPtr format = (stringFormat == null) ? IntPtr.Zero : stringFormat.nativeFormat;
            return GdipMeasureString(NativeGraphics, text, font, ref rect, format);
        }

        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat,
            out int charactersFitted, out int linesFilled)
        {
            charactersFitted = 0;
            linesFilled = 0;

            if ((text == null) || (text.Length == 0))
                return SizeF.Empty;

            if (font == null)
                throw new ArgumentNullException(nameof(font));

            RectangleF boundingBox = new RectangleF();
            RectangleF rect = new RectangleF(0, 0, layoutArea.Width, layoutArea.Height);

            IntPtr format = (stringFormat == null) ? IntPtr.Zero : stringFormat.nativeFormat;

            unsafe
            {
                fixed (int* pc = &charactersFitted, pl = &linesFilled)
                {
                    int status = Gdip.GdipMeasureString(NativeGraphics, text, text.Length,
                    font.NativeFont, ref rect, format, out boundingBox, pc, pl);
                    Gdip.CheckStatus(status);
                }
            }
            return new SizeF(boundingBox.Width, boundingBox.Height);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ReleaseHdcInternal(IntPtr hdc)
        {
            int status = Gdip.InvalidParameter;
            if (hdc == _nativeHdc)
            {
                status = Gdip.GdipReleaseDC(NativeGraphics, _nativeHdc);
                _nativeHdc = IntPtr.Zero;
            }
            Gdip.CheckStatus(status);
        }

        public void Restore(GraphicsState gstate)
        {
            // the possible NRE thrown by gstate.nativeState match MS behaviour
            int status = Gdip.GdipRestoreGraphics(NativeGraphics, (uint)gstate.nativeState);
            Gdip.CheckStatus(status);
        }

        public GraphicsState Save()
        {
            uint saveState;
            int status = Gdip.GdipSaveGraphics(NativeGraphics, out saveState);
            Gdip.CheckStatus(status);

            GraphicsState state = new GraphicsState((int)saveState);
            return state;
        }


        public void TransformPoints(CoordinateSpace destSpace, CoordinateSpace srcSpace, PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            IntPtr ptrPt = MarshallingHelpers.FromPointToUnManagedMemory(pts);

            int status = Gdip.GdipTransformPoints(NativeGraphics, destSpace, srcSpace, ptrPt, pts.Length);
            Gdip.CheckStatus(status);

            MarshallingHelpers.FromUnManagedMemoryToPoint(ptrPt, pts);
        }


        public void TransformPoints(CoordinateSpace destSpace, CoordinateSpace srcSpace, Point[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));
            IntPtr ptrPt = MarshallingHelpers.FromPointToUnManagedMemoryI(pts);

            int status = Gdip.GdipTransformPointsI(NativeGraphics, destSpace, srcSpace, ptrPt, pts.Length);
            Gdip.CheckStatus(status);

            MarshallingHelpers.FromUnManagedMemoryToPointI(ptrPt, pts);
        }

        public RectangleF VisibleClipBounds
        {
            get
            {
                int status = Gdip.GdipGetVisibleClipBounds(new HandleRef(this, NativeGraphics), out RectangleF rect);
                Gdip.CheckStatus(status);

                return rect;
            }
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public object GetContextInfo()
        {
            // only known source of information @ http://blogs.wdevs.com/jdunlap/Default.aspx
            throw new NotImplementedException();
        }
    }
}

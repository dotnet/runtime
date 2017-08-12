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

namespace System.Drawing
{
    public sealed class Graphics : MarshalByRefObject, IDisposable
    , IDeviceContext
    {
        internal IntPtr nativeObject = IntPtr.Zero;
        internal IMacContext maccontext;
        private bool disposed = false;
        private static float defDpiX = 0;
        private static float defDpiY = 0;
        private IntPtr deviceContextHdc;

        public delegate bool EnumerateMetafileProc(EmfPlusRecordType recordType,
                                int flags,
                                int dataSize,
                                IntPtr data,
                                PlayRecordCallback callbackData);

        public delegate bool DrawImageAbort(IntPtr callbackdata);

        internal Graphics(IntPtr nativeGraphics)
        {
            nativeObject = nativeGraphics;
        }

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

        // For CoreFX compatibility
        internal IntPtr NativeGraphics => nativeObject;

        internal IntPtr NativeObject
        {
            get
            {
                return nativeObject;
            }

            set
            {
                nativeObject = value;
            }
        }

        [MonoTODO("Metafiles, both WMF and EMF formats, aren't supported.")]
        public void AddMetafileComment(byte[] data)
        {
            throw new NotImplementedException();
        }

        public GraphicsContainer BeginContainer()
        {
            int state;
            int status = SafeNativeMethods.Gdip.GdipBeginContainer2(new HandleRef(this, nativeObject), out state);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new GraphicsContainer(state);
        }

        [MonoTODO("The rectangles and unit parameters aren't supported in libgdiplus")]
        public GraphicsContainer BeginContainer(Rectangle dstrect, Rectangle srcrect, GraphicsUnit unit)
        {
            int state;

            var dstf = new GPRECT(dstrect);
            var srcf = new GPRECT(srcrect);

            int status = SafeNativeMethods.Gdip.GdipBeginContainerI(new HandleRef(this, nativeObject), ref dstf, ref srcf, unchecked((int)unit), out state);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new GraphicsContainer(state);
        }

        [MonoTODO("The rectangles and unit parameters aren't supported in libgdiplus")]
        public GraphicsContainer BeginContainer(RectangleF dstrect, RectangleF srcrect, GraphicsUnit unit)
        {
            int state;

            var dstf = new GPRECTF(dstrect);
            var srcf = new GPRECTF(srcrect);

            int status = SafeNativeMethods.Gdip.GdipBeginContainer(new HandleRef(this, nativeObject), ref dstf, ref srcf, unchecked((int)unit), out state);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new GraphicsContainer(state);
        }


        public void Clear(Color color)
        {
            Status status;
            status = SafeNativeMethods.Gdip.GdipGraphicsClear(nativeObject, color.ToArgb());
            SafeNativeMethods.Gdip.CheckStatus(status);
        }
        [MonoLimitation("Works on Win32 and on X11 (but not on Cocoa and Quartz)")]
        public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize)
        {
            CopyFromScreen(upperLeftSource.X, upperLeftSource.Y, upperLeftDestination.X, upperLeftDestination.Y,
                blockRegionSize, CopyPixelOperation.SourceCopy);
        }

        [MonoLimitation("Works on Win32 and (for CopyPixelOperation.SourceCopy only) on X11 but not on Cocoa and Quartz")]
        public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            CopyFromScreen(upperLeftSource.X, upperLeftSource.Y, upperLeftDestination.X, upperLeftDestination.Y,
                blockRegionSize, copyPixelOperation);
        }

        [MonoLimitation("Works on Win32 and on X11 (but not on Cocoa and Quartz)")]
        public void CopyFromScreen(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize)
        {
            CopyFromScreen(sourceX, sourceY, destinationX, destinationY, blockRegionSize,
                CopyPixelOperation.SourceCopy);
        }

        [MonoLimitation("Works on Win32 and (for CopyPixelOperation.SourceCopy only) on X11 but not on Cocoa and Quartz")]
        public void CopyFromScreen(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            if (!Enum.IsDefined(typeof(CopyPixelOperation), copyPixelOperation))
                throw new InvalidEnumArgumentException(string.Format("Enum argument value '{0}' is not valid for CopyPixelOperation", copyPixelOperation));

            if (GDIPlus.UseX11Drawable)
            {
                CopyFromScreenX11(sourceX, sourceY, destinationX, destinationY, blockRegionSize, copyPixelOperation);
            }
            else if (GDIPlus.UseCarbonDrawable)
            {
                CopyFromScreenMac(sourceX, sourceY, destinationX, destinationY, blockRegionSize, copyPixelOperation);
            }
            else if (GDIPlus.UseCocoaDrawable)
            {
                CopyFromScreenMac(sourceX, sourceY, destinationX, destinationY, blockRegionSize, copyPixelOperation);
            }
            else
            {
                CopyFromScreenWin32(sourceX, sourceY, destinationX, destinationY, blockRegionSize, copyPixelOperation);
            }
        }

        private void CopyFromScreenWin32(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            IntPtr window = GDIPlus.GetDesktopWindow();
            IntPtr srcDC = GDIPlus.GetDC(window);
            IntPtr dstDC = GetHdc();
            GDIPlus.BitBlt(dstDC, destinationX, destinationY, blockRegionSize.Width,
                blockRegionSize.Height, srcDC, sourceX, sourceY, (int)copyPixelOperation);

            GDIPlus.ReleaseDC(IntPtr.Zero, srcDC);
            ReleaseHdc(dstDC);
        }

        private void CopyFromScreenMac(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            throw new NotImplementedException();
        }

        private void CopyFromScreenX11(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            IntPtr window, image, defvisual, vPtr;
            int AllPlanes = ~0, nitems = 0, pixel;

            if (copyPixelOperation != CopyPixelOperation.SourceCopy)
                throw new NotImplementedException("Operation not implemented under X11");

            if (GDIPlus.Display == IntPtr.Zero)
            {
                GDIPlus.Display = GDIPlus.XOpenDisplay(IntPtr.Zero);
            }

            window = GDIPlus.XRootWindow(GDIPlus.Display, 0);
            defvisual = GDIPlus.XDefaultVisual(GDIPlus.Display, 0);
            XVisualInfo visual = new XVisualInfo();

            /* Get XVisualInfo for this visual */
            visual.visualid = GDIPlus.XVisualIDFromVisual(defvisual);
            vPtr = GDIPlus.XGetVisualInfo(GDIPlus.Display, 0x1 /* VisualIDMask */, ref visual, ref nitems);
            visual = (XVisualInfo)Marshal.PtrToStructure(vPtr, typeof(XVisualInfo));
#if false
            Console.WriteLine ("visual\t{0}", visual.visual);
            Console.WriteLine ("visualid\t{0}", visual.visualid);
            Console.WriteLine ("screen\t{0}", visual.screen);
            Console.WriteLine ("depth\t{0}", visual.depth);
            Console.WriteLine ("klass\t{0}", visual.klass);
            Console.WriteLine ("red_mask\t{0:X}", visual.red_mask);
            Console.WriteLine ("green_mask\t{0:X}", visual.green_mask);
            Console.WriteLine ("blue_mask\t{0:X}", visual.blue_mask);
            Console.WriteLine ("colormap_size\t{0}", visual.colormap_size);
            Console.WriteLine ("bits_per_rgb\t{0}", visual.bits_per_rgb);
#endif
            image = GDIPlus.XGetImage(GDIPlus.Display, window, sourceX, sourceY, blockRegionSize.Width,
                blockRegionSize.Height, AllPlanes, 2 /* ZPixmap*/);
            if (image == IntPtr.Zero)
            {
                string s = String.Format("XGetImage returned NULL when asked to for a {0}x{1} region block",
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
                    pixel = GDIPlus.XGetPixel(image, x, y);

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
            GDIPlus.XDestroyImage(image);
            GDIPlus.XFree(vPtr);
        }

        public void Dispose()
        {
            Status status;
            if (!disposed)
            {
                if (GDIPlus.UseCarbonDrawable || GDIPlus.UseCocoaDrawable)
                {
                    Flush();
                    if (maccontext != null)
                        maccontext.Release();
                }

                status = SafeNativeMethods.Gdip.GdipDeleteGraphics(nativeObject);
                nativeObject = IntPtr.Zero;
                SafeNativeMethods.Gdip.CheckStatus(status);
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
            Status status;
            if (pen == null)
                throw new ArgumentNullException("pen");

            status = SafeNativeMethods.Gdip.GdipDrawArc(nativeObject, pen.NativePen,
                                        x, y, width, height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        // Microsoft documentation states that the signature for this member should be
        // public void DrawArc( Pen pen,  int x,  int y,  int width,  int height,   int startAngle,
        // int sweepAngle. However, GdipDrawArcI uses also float for the startAngle and sweepAngle params
        public void DrawArc(Pen pen, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            Status status;
            if (pen == null)
                throw new ArgumentNullException("pen");
            status = SafeNativeMethods.Gdip.GdipDrawArcI(nativeObject, pen.NativePen,
                        x, y, width, height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawBezier(Pen pen, PointF pt1, PointF pt2, PointF pt3, PointF pt4)
        {
            Status status;
            if (pen == null)
                throw new ArgumentNullException("pen");
            status = SafeNativeMethods.Gdip.GdipDrawBezier(nativeObject, pen.NativePen,
                            pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X,
                            pt3.Y, pt4.X, pt4.Y);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawBezier(Pen pen, Point pt1, Point pt2, Point pt3, Point pt4)
        {
            Status status;
            if (pen == null)
                throw new ArgumentNullException("pen");
            status = SafeNativeMethods.Gdip.GdipDrawBezierI(nativeObject, pen.NativePen,
                            pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X,
                            pt3.Y, pt4.X, pt4.Y);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawBezier(Pen pen, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            Status status;
            if (pen == null)
                throw new ArgumentNullException("pen");
            status = SafeNativeMethods.Gdip.GdipDrawBezier(nativeObject, pen.NativePen, x1,
                            y1, x2, y2, x3, y3, x4, y4);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawBeziers(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            int length = points.Length;
            Status status;

            if (length < 4)
                return;

            for (int i = 0; i < length - 1; i += 3)
            {
                Point p1 = points[i];
                Point p2 = points[i + 1];
                Point p3 = points[i + 2];
                Point p4 = points[i + 3];

                status = SafeNativeMethods.Gdip.GdipDrawBezier(nativeObject,
            pen.NativePen,
                                        p1.X, p1.Y, p2.X, p2.Y,
                                        p3.X, p3.Y, p4.X, p4.Y);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public void DrawBeziers(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            int length = points.Length;
            Status status;

            if (length < 4)
                return;

            for (int i = 0; i < length - 1; i += 3)
            {
                PointF p1 = points[i];
                PointF p2 = points[i + 1];
                PointF p3 = points[i + 2];
                PointF p4 = points[i + 3];

                status = SafeNativeMethods.Gdip.GdipDrawBezier(nativeObject,
            pen.NativePen,
                                        p1.X, p1.Y, p2.X, p2.Y,
                                        p3.X, p3.Y, p4.X, p4.Y);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }


        public void DrawClosedCurve(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            Status status;
            status = SafeNativeMethods.Gdip.GdipDrawClosedCurve(nativeObject, pen.NativePen, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawClosedCurve(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            Status status;
            status = SafeNativeMethods.Gdip.GdipDrawClosedCurveI(nativeObject, pen.NativePen, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        // according to MSDN fillmode "is required but ignored" which makes _some_ sense since the unmanaged 
        // GDI+ call doesn't support it (issue spotted using Gendarme's AvoidUnusedParametersRule)
        public void DrawClosedCurve(Pen pen, Point[] points, float tension, FillMode fillmode)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            Status status;
            status = SafeNativeMethods.Gdip.GdipDrawClosedCurve2I(nativeObject, pen.NativePen, points, points.Length, tension);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        // according to MSDN fillmode "is required but ignored" which makes _some_ sense since the unmanaged 
        // GDI+ call doesn't support it (issue spotted using Gendarme's AvoidUnusedParametersRule)
        public void DrawClosedCurve(Pen pen, PointF[] points, float tension, FillMode fillmode)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            Status status;
            status = SafeNativeMethods.Gdip.GdipDrawClosedCurve2(nativeObject, pen.NativePen, points, points.Length, tension);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            Status status;
            status = SafeNativeMethods.Gdip.GdipDrawCurveI(nativeObject, pen.NativePen, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            Status status;
            status = SafeNativeMethods.Gdip.GdipDrawCurve(nativeObject, pen.NativePen, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, PointF[] points, float tension)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            Status status;
            status = SafeNativeMethods.Gdip.GdipDrawCurve2(nativeObject, pen.NativePen, points, points.Length, tension);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, Point[] points, float tension)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            Status status;
            status = SafeNativeMethods.Gdip.GdipDrawCurve2I(nativeObject, pen.NativePen, points, points.Length, tension);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            Status status;
            status = SafeNativeMethods.Gdip.GdipDrawCurve3(nativeObject, pen.NativePen,
                            points, points.Length, offset,
                            numberOfSegments, 0.5f);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, Point[] points, int offset, int numberOfSegments, float tension)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            Status status;
            status = SafeNativeMethods.Gdip.GdipDrawCurve3I(nativeObject, pen.NativePen,
                            points, points.Length, offset,
                            numberOfSegments, tension);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments, float tension)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");

            Status status;
            status = SafeNativeMethods.Gdip.GdipDrawCurve3(nativeObject, pen.NativePen,
                            points, points.Length, offset,
                            numberOfSegments, tension);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawEllipse(Pen pen, Rectangle rect)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");

            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawEllipse(Pen pen, RectangleF rect)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawEllipse(Pen pen, int x, int y, int width, int height)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            Status status;
            status = SafeNativeMethods.Gdip.GdipDrawEllipseI(nativeObject, pen.NativePen, x, y, width, height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawEllipse(Pen pen, float x, float y, float width, float height)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            Status status = SafeNativeMethods.Gdip.GdipDrawEllipse(nativeObject, pen.NativePen, x, y, width, height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawIcon(Icon icon, Rectangle targetRect)
        {
            if (icon == null)
                throw new ArgumentNullException("icon");

            DrawImage(icon.GetInternalBitmap(), targetRect);
        }

        public void DrawIcon(Icon icon, int x, int y)
        {
            if (icon == null)
                throw new ArgumentNullException("icon");

            DrawImage(icon.GetInternalBitmap(), x, y);
        }

        public void DrawIconUnstretched(Icon icon, Rectangle targetRect)
        {
            if (icon == null)
                throw new ArgumentNullException("icon");

            DrawImageUnscaled(icon.GetInternalBitmap(), targetRect);
        }

        public void DrawImage(Image image, RectangleF rect)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            Status status = SafeNativeMethods.Gdip.GdipDrawImageRect(nativeObject, image.NativeObject, rect.X, rect.Y, rect.Width, rect.Height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, PointF point)
        {
            if (image == null)
                throw new ArgumentNullException("image");

            Status status = SafeNativeMethods.Gdip.GdipDrawImage(nativeObject, image.NativeObject, point.X, point.Y);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Point[] destPoints)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (destPoints == null)
                throw new ArgumentNullException("destPoints");

            Status status = SafeNativeMethods.Gdip.GdipDrawImagePointsI(nativeObject, image.NativeObject, destPoints, destPoints.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Point point)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            DrawImage(image, point.X, point.Y);
        }

        public void DrawImage(Image image, Rectangle rect)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            DrawImage(image, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawImage(Image image, PointF[] destPoints)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (destPoints == null)
                throw new ArgumentNullException("destPoints");
            Status status = SafeNativeMethods.Gdip.GdipDrawImagePoints(nativeObject, image.NativeObject, destPoints, destPoints.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, int x, int y)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageI(nativeObject, image.NativeObject, x, y);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, float x, float y)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImage(nativeObject, image.NativeObject, x, y);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageRectRectI(nativeObject, image.NativeObject,
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                srcUnit, IntPtr.Zero, null, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageRectRect(nativeObject, image.NativeObject,
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                srcUnit, IntPtr.Zero, null, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (destPoints == null)
                throw new ArgumentNullException("destPoints");

            Status status = SafeNativeMethods.Gdip.GdipDrawImagePointsRectI(nativeObject, image.NativeObject,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit, IntPtr.Zero,
                null, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (destPoints == null)
                throw new ArgumentNullException("destPoints");

            Status status = SafeNativeMethods.Gdip.GdipDrawImagePointsRect(nativeObject, image.NativeObject,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit, IntPtr.Zero,
                null, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit,
                                ImageAttributes imageAttr)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (destPoints == null)
                throw new ArgumentNullException("destPoints");
            Status status = SafeNativeMethods.Gdip.GdipDrawImagePointsRectI(nativeObject, image.NativeObject,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit,
                imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, null, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, float x, float y, float width, float height)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageRect(nativeObject, image.NativeObject, x, y,
                           width, height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit,
                                ImageAttributes imageAttr)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (destPoints == null)
                throw new ArgumentNullException("destPoints");
            Status status = SafeNativeMethods.Gdip.GdipDrawImagePointsRect(nativeObject, image.NativeObject,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit,
                imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, null, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, int x, int y, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImagePointRectI(nativeObject, image.NativeObject, x, y, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, srcUnit);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, int x, int y, int width, int height)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageRectI(nativeObject, image.nativeObject, x, y, width, height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, float x, float y, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImagePointRect(nativeObject, image.nativeObject, x, y, srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height, srcUnit);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, ImageAttributes imageAttr, DrawImageAbort callback)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (destPoints == null)
                throw new ArgumentNullException("destPoints");
            Status status = SafeNativeMethods.Gdip.GdipDrawImagePointsRect(nativeObject, image.NativeObject,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit,
                imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, callback, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit, ImageAttributes imageAttr, DrawImageAbort callback)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (destPoints == null)
                throw new ArgumentNullException("destPoints");

            Status status = SafeNativeMethods.Gdip.GdipDrawImagePointsRectI(nativeObject, image.NativeObject,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit,
                imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, callback, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit, ImageAttributes imageAttr, DrawImageAbort callback, int callbackData)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            if (destPoints == null)
                throw new ArgumentNullException("destPoints");

            Status status = SafeNativeMethods.Gdip.GdipDrawImagePointsRectI(nativeObject, image.NativeObject,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit,
                imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, callback, (IntPtr)callbackData);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageRectRect(nativeObject, image.NativeObject,
                                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                               srcX, srcY, srcWidth, srcHeight, srcUnit, IntPtr.Zero,
                               null, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, ImageAttributes imageAttr, DrawImageAbort callback, int callbackData)
        {
            Status status = SafeNativeMethods.Gdip.GdipDrawImagePointsRect(nativeObject, image.NativeObject,
                destPoints, destPoints.Length, srcRect.X, srcRect.Y,
                srcRect.Width, srcRect.Height, srcUnit,
                imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, callback, (IntPtr)callbackData);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageRectRectI(nativeObject, image.NativeObject,
                                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                               srcX, srcY, srcWidth, srcHeight, srcUnit, IntPtr.Zero,
                               null, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttrs)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageRectRect(nativeObject, image.NativeObject,
                                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                               srcX, srcY, srcWidth, srcHeight, srcUnit,
                imageAttrs != null ? imageAttrs.nativeImageAttributes : IntPtr.Zero, null, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttr)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageRectRectI(nativeObject, image.NativeObject,
                                        destRect.X, destRect.Y, destRect.Width,
                    destRect.Height, srcX, srcY, srcWidth, srcHeight,
                    srcUnit, imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, null, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttr, DrawImageAbort callback)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageRectRectI(nativeObject, image.NativeObject,
                                        destRect.X, destRect.Y, destRect.Width,
                    destRect.Height, srcX, srcY, srcWidth, srcHeight,
                    srcUnit, imageAttr != null ? imageAttr.nativeImageAttributes : IntPtr.Zero, callback,
                    IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttrs, DrawImageAbort callback)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageRectRect(nativeObject, image.NativeObject,
                                        destRect.X, destRect.Y, destRect.Width,
                    destRect.Height, srcX, srcY, srcWidth, srcHeight,
                    srcUnit, imageAttrs != null ? imageAttrs.nativeImageAttributes : IntPtr.Zero,
                    callback, IntPtr.Zero);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, float srcX, float srcY, float srcWidth, float srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttrs, DrawImageAbort callback, IntPtr callbackData)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageRectRect(nativeObject, image.NativeObject,
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcX, srcY, srcWidth, srcHeight, srcUnit,
                imageAttrs != null ? imageAttrs.nativeImageAttributes : IntPtr.Zero, callback, callbackData);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawImage(Image image, Rectangle destRect, int srcX, int srcY, int srcWidth, int srcHeight, GraphicsUnit srcUnit, ImageAttributes imageAttrs, DrawImageAbort callback, IntPtr callbackData)
        {
            if (image == null)
                throw new ArgumentNullException("image");
            Status status = SafeNativeMethods.Gdip.GdipDrawImageRectRect(nativeObject, image.NativeObject,
                               destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcX, srcY, srcWidth, srcHeight, srcUnit,
                imageAttrs != null ? imageAttrs.nativeImageAttributes : IntPtr.Zero, callback, callbackData);
            SafeNativeMethods.Gdip.CheckStatus(status);
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
                throw new ArgumentNullException("image");
            DrawImage(image, x, y, image.Width, image.Height);
        }

        public void DrawImageUnscaled(Image image, int x, int y, int width, int height)
        {
            if (image == null)
                throw new ArgumentNullException("image");

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
                throw new ArgumentNullException("image");

            int width = (image.Width > rect.Width) ? rect.Width : image.Width;
            int height = (image.Height > rect.Height) ? rect.Height : image.Height;

            DrawImageUnscaled(image, rect.X, rect.Y, width, height);
        }

        public void DrawLine(Pen pen, PointF pt1, PointF pt2)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            Status status = SafeNativeMethods.Gdip.GdipDrawLine(nativeObject, pen.NativePen,
                            pt1.X, pt1.Y, pt2.X, pt2.Y);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawLine(Pen pen, Point pt1, Point pt2)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            Status status = SafeNativeMethods.Gdip.GdipDrawLineI(nativeObject, pen.NativePen,
                            pt1.X, pt1.Y, pt2.X, pt2.Y);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            Status status = SafeNativeMethods.Gdip.GdipDrawLineI(nativeObject, pen.NativePen, x1, y1, x2, y2);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (!float.IsNaN(x1) && !float.IsNaN(y1) &&
                !float.IsNaN(x2) && !float.IsNaN(y2))
            {
                Status status = SafeNativeMethods.Gdip.GdipDrawLine(nativeObject, pen.NativePen, x1, y1, x2, y2);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public void DrawLines(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");
            Status status = SafeNativeMethods.Gdip.GdipDrawLines(nativeObject, pen.NativePen, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawLines(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");
            Status status = SafeNativeMethods.Gdip.GdipDrawLinesI(nativeObject, pen.NativePen, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawPath(Pen pen, GraphicsPath path)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (path == null)
                throw new ArgumentNullException("path");
            Status status = SafeNativeMethods.Gdip.GdipDrawPath(nativeObject, pen.NativePen, path.nativePath);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawPie(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void DrawPie(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        public void DrawPie(Pen pen, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            Status status = SafeNativeMethods.Gdip.GdipDrawPie(nativeObject, pen.NativePen, x, y, width, height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        // Microsoft documentation states that the signature for this member should be
        // public void DrawPie(Pen pen, int x,  int y,  int width,   int height,   int startAngle
        // int sweepAngle. However, GdipDrawPieI uses also float for the startAngle and sweepAngle params
        public void DrawPie(Pen pen, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            Status status = SafeNativeMethods.Gdip.GdipDrawPieI(nativeObject, pen.NativePen, x, y, width, height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawPolygon(Pen pen, Point[] points)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");
            Status status = SafeNativeMethods.Gdip.GdipDrawPolygonI(nativeObject, pen.NativePen, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawPolygon(Pen pen, PointF[] points)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            if (points == null)
                throw new ArgumentNullException("points");
            Status status = SafeNativeMethods.Gdip.GdipDrawPolygon(nativeObject, pen.NativePen, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawRectangle(Pen pen, Rectangle rect)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            DrawRectangle(pen, rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            Status status = SafeNativeMethods.Gdip.GdipDrawRectangle(nativeObject, pen.NativePen, x, y, width, height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawRectangle(Pen pen, int x, int y, int width, int height)
        {
            if (pen == null)
                throw new ArgumentNullException("pen");
            Status status = SafeNativeMethods.Gdip.GdipDrawRectangleI(nativeObject, pen.NativePen, x, y, width, height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawRectangles(Pen pen, RectangleF[] rects)
        {
            if (pen == null)
                throw new ArgumentNullException("image");
            if (rects == null)
                throw new ArgumentNullException("rects");
            Status status = SafeNativeMethods.Gdip.GdipDrawRectangles(nativeObject, pen.NativePen, rects, rects.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void DrawRectangles(Pen pen, Rectangle[] rects)
        {
            if (pen == null)
                throw new ArgumentNullException("image");
            if (rects == null)
                throw new ArgumentNullException("rects");
            Status status = SafeNativeMethods.Gdip.GdipDrawRectanglesI(nativeObject, pen.NativePen, rects, rects.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
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
                throw new ArgumentNullException("font");
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (s == null || s.Length == 0)
                return;

            Status status = SafeNativeMethods.Gdip.GdipDrawString(nativeObject, s, s.Length, font.NativeObject, ref layoutRectangle, format != null ? format.NativeObject : IntPtr.Zero, brush.NativeBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void EndContainer(GraphicsContainer container)
        {
            if (container == null)
                throw new ArgumentNullException("container");
            int status = SafeNativeMethods.Gdip.GdipEndContainer(new HandleRef(this, nativeObject), container.nativeGraphicsContainer);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        private const string MetafileEnumeration = "Metafiles enumeration, for both WMF and EMF formats, isn't supported.";

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint, RectangleF srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Point destPoint, Rectangle srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint, RectangleF srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Point destPoint, Rectangle srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, Rectangle srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, Rectangle srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, Point destPoint, Rectangle srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, RectangleF srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, RectangleF srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        [MonoTODO(MetafileEnumeration)]
        public void EnumerateMetafile(Metafile metafile, PointF destPoint, RectangleF srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes imageAttr)
        {
            throw new NotImplementedException();
        }

        public void ExcludeClip(Rectangle rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipSetClipRectI(nativeObject, rect.X, rect.Y, rect.Width, rect.Height, CombineMode.Exclude);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ExcludeClip(Region region)
        {
            if (region == null)
                throw new ArgumentNullException("region");
            Status status = SafeNativeMethods.Gdip.GdipSetClipRegion(nativeObject, region.NativeObject, CombineMode.Exclude);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        public void FillClosedCurve(Brush brush, PointF[] points)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (points == null)
                throw new ArgumentNullException("points");
            Status status = SafeNativeMethods.Gdip.GdipFillClosedCurve(nativeObject, brush.NativeBrush, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillClosedCurve(Brush brush, Point[] points)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (points == null)
                throw new ArgumentNullException("points");
            Status status = SafeNativeMethods.Gdip.GdipFillClosedCurveI(nativeObject, brush.NativeBrush, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        public void FillClosedCurve(Brush brush, PointF[] points, FillMode fillmode)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (points == null)
                throw new ArgumentNullException("points");
            FillClosedCurve(brush, points, fillmode, 0.5f);
        }

        public void FillClosedCurve(Brush brush, Point[] points, FillMode fillmode)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (points == null)
                throw new ArgumentNullException("points");
            FillClosedCurve(brush, points, fillmode, 0.5f);
        }

        public void FillClosedCurve(Brush brush, PointF[] points, FillMode fillmode, float tension)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (points == null)
                throw new ArgumentNullException("points");
            Status status = SafeNativeMethods.Gdip.GdipFillClosedCurve2(nativeObject, brush.NativeBrush, points, points.Length, tension, fillmode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillClosedCurve(Brush brush, Point[] points, FillMode fillmode, float tension)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (points == null)
                throw new ArgumentNullException("points");
            Status status = SafeNativeMethods.Gdip.GdipFillClosedCurve2I(nativeObject, brush.NativeBrush, points, points.Length, tension, fillmode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillEllipse(Brush brush, Rectangle rect)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillEllipse(Brush brush, RectangleF rect)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void FillEllipse(Brush brush, float x, float y, float width, float height)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            Status status = SafeNativeMethods.Gdip.GdipFillEllipse(nativeObject, brush.NativeBrush, x, y, width, height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillEllipse(Brush brush, int x, int y, int width, int height)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            Status status = SafeNativeMethods.Gdip.GdipFillEllipseI(nativeObject, brush.NativeBrush, x, y, width, height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillPath(Brush brush, GraphicsPath path)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (path == null)
                throw new ArgumentNullException("path");
            Status status = SafeNativeMethods.Gdip.GdipFillPath(nativeObject, brush.NativeBrush, path.nativePath);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillPie(Brush brush, Rectangle rect, float startAngle, float sweepAngle)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            Status status = SafeNativeMethods.Gdip.GdipFillPie(nativeObject, brush.NativeBrush, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillPie(Brush brush, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            Status status = SafeNativeMethods.Gdip.GdipFillPieI(nativeObject, brush.NativeBrush, x, y, width, height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillPie(Brush brush, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            Status status = SafeNativeMethods.Gdip.GdipFillPie(nativeObject, brush.NativeBrush, x, y, width, height, startAngle, sweepAngle);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillPolygon(Brush brush, PointF[] points)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (points == null)
                throw new ArgumentNullException("points");
            Status status = SafeNativeMethods.Gdip.GdipFillPolygon2(nativeObject, brush.NativeBrush, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillPolygon(Brush brush, Point[] points)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (points == null)
                throw new ArgumentNullException("points");
            Status status = SafeNativeMethods.Gdip.GdipFillPolygon2I(nativeObject, brush.NativeBrush, points, points.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillPolygon(Brush brush, Point[] points, FillMode fillMode)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (points == null)
                throw new ArgumentNullException("points");
            Status status = SafeNativeMethods.Gdip.GdipFillPolygonI(nativeObject, brush.NativeBrush, points, points.Length, fillMode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillPolygon(Brush brush, PointF[] points, FillMode fillMode)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (points == null)
                throw new ArgumentNullException("points");
            Status status = SafeNativeMethods.Gdip.GdipFillPolygon(nativeObject, brush.NativeBrush, points, points.Length, fillMode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillRectangle(Brush brush, RectangleF rect)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            FillRectangle(brush, rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public void FillRectangle(Brush brush, Rectangle rect)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            FillRectangle(brush, rect.Left, rect.Top, rect.Width, rect.Height);
        }

        public void FillRectangle(Brush brush, int x, int y, int width, int height)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");

            Status status = SafeNativeMethods.Gdip.GdipFillRectangleI(nativeObject, brush.NativeBrush, x, y, width, height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillRectangle(Brush brush, float x, float y, float width, float height)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");

            Status status = SafeNativeMethods.Gdip.GdipFillRectangle(nativeObject, brush.NativeBrush, x, y, width, height);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillRectangles(Brush brush, Rectangle[] rects)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (rects == null)
                throw new ArgumentNullException("rects");

            Status status = SafeNativeMethods.Gdip.GdipFillRectanglesI(nativeObject, brush.NativeBrush, rects, rects.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void FillRectangles(Brush brush, RectangleF[] rects)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (rects == null)
                throw new ArgumentNullException("rects");

            Status status = SafeNativeMethods.Gdip.GdipFillRectangles(nativeObject, brush.NativeBrush, rects, rects.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        public void FillRegion(Brush brush, Region region)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");
            if (region == null)
                throw new ArgumentNullException("region");

            Status status = SafeNativeMethods.Gdip.GdipFillRegion(nativeObject, brush.NativeBrush, region.NativeObject);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        public void Flush()
        {
            Flush(FlushIntention.Flush);
        }


        public void Flush(FlushIntention intention)
        {
            if (nativeObject == IntPtr.Zero)
            {
                return;
            }

            Status status = SafeNativeMethods.Gdip.GdipFlush(nativeObject, intention);
            SafeNativeMethods.Gdip.CheckStatus(status);

            if (maccontext != null)
                maccontext.Synchronize();
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdc(IntPtr hdc)
        {
            IntPtr graphics;
            Status status = SafeNativeMethods.Gdip.GdipCreateFromHDC(hdc, out graphics);
            SafeNativeMethods.Gdip.CheckStatus(status);
            return new Graphics(graphics);
        }

        [MonoTODO]
        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdc(IntPtr hdc, IntPtr hdevice)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHdcInternal(IntPtr hdc)
        {
            GDIPlus.Display = hdc;
            return null;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public static Graphics FromHwnd(IntPtr hwnd)
        {
            IntPtr graphics;

            if (GDIPlus.UseCocoaDrawable)
            {
                CocoaContext context = MacSupport.GetCGContextForNSView(hwnd);
                SafeNativeMethods.Gdip.GdipCreateFromContext_macosx(context.ctx, context.width, context.height, out graphics);

                Graphics g = new Graphics(graphics);
                g.maccontext = context;

                return g;
            }

            if (GDIPlus.UseCarbonDrawable)
            {
                CarbonContext context = MacSupport.GetCGContextForView(hwnd);
                SafeNativeMethods.Gdip.GdipCreateFromContext_macosx(context.ctx, context.width, context.height, out graphics);

                Graphics g = new Graphics(graphics);
                g.maccontext = context;

                return g;
            }
            if (GDIPlus.UseX11Drawable)
            {
                if (GDIPlus.Display == IntPtr.Zero)
                {
                    GDIPlus.Display = GDIPlus.XOpenDisplay(IntPtr.Zero);
                    if (GDIPlus.Display == IntPtr.Zero)
                        throw new NotSupportedException("Could not open display (X-Server required. Check your DISPLAY environment variable)");
                }
                if (hwnd == IntPtr.Zero)
                {
                    hwnd = GDIPlus.XRootWindow(GDIPlus.Display, GDIPlus.XDefaultScreen(GDIPlus.Display));
                }

                return FromXDrawable(hwnd, GDIPlus.Display);

            }

            Status status = SafeNativeMethods.Gdip.GdipCreateFromHWND(hwnd, out graphics);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new Graphics(graphics);
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
                throw new ArgumentNullException("image");

            if ((image.PixelFormat & PixelFormat.Indexed) != 0)
                throw new Exception("Cannot create Graphics from an indexed bitmap.");

            Status status = SafeNativeMethods.Gdip.GdipGetImageGraphicsContext(image.nativeObject, out graphics);
            SafeNativeMethods.Gdip.CheckStatus(status);
            Graphics result = new Graphics(graphics);

            if (GDIPlus.RunningOnUnix())
            {
                Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
                SafeNativeMethods.Gdip.GdipSetVisibleClip_linux(result.NativeObject, ref rect);
            }

            return result;
        }

        internal static Graphics FromXDrawable(IntPtr drawable, IntPtr display)
        {
            IntPtr graphics;

            Status s = SafeNativeMethods.Gdip.GdipCreateFromXDrawable_linux(drawable, display, out graphics);
            SafeNativeMethods.Gdip.CheckStatus(s);
            return new Graphics(graphics);
        }

        [MonoTODO]
        public static IntPtr GetHalftonePalette()
        {
            throw new NotImplementedException();
        }

        public IntPtr GetHdc()
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipGetDC(this.nativeObject, out deviceContextHdc));
            return deviceContextHdc;
        }

        public Color GetNearestColor(Color color)
        {
            int argb;

            Status status = SafeNativeMethods.Gdip.GdipGetNearestColor(nativeObject, out argb);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return Color.FromArgb(argb);
        }


        public void IntersectClip(Region region)
        {
            if (region == null)
                throw new ArgumentNullException("region");
            Status status = SafeNativeMethods.Gdip.GdipSetClipRegion(nativeObject, region.NativeObject, CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void IntersectClip(RectangleF rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipSetClipRect(nativeObject, rect.X, rect.Y, rect.Width, rect.Height, CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void IntersectClip(Rectangle rect)
        {
            Status status = SafeNativeMethods.Gdip.GdipSetClipRectI(nativeObject, rect.X, rect.Y, rect.Width, rect.Height, CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public bool IsVisible(Point point)
        {
            bool isVisible = false;

            Status status = SafeNativeMethods.Gdip.GdipIsVisiblePointI(nativeObject, point.X, point.Y, out isVisible);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isVisible;
        }


        public bool IsVisible(RectangleF rect)
        {
            bool isVisible = false;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRect(nativeObject, rect.X, rect.Y, rect.Width, rect.Height, out isVisible);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isVisible;
        }

        public bool IsVisible(PointF point)
        {
            bool isVisible = false;

            Status status = SafeNativeMethods.Gdip.GdipIsVisiblePoint(nativeObject, point.X, point.Y, out isVisible);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isVisible;
        }

        public bool IsVisible(Rectangle rect)
        {
            bool isVisible = false;

            Status status = SafeNativeMethods.Gdip.GdipIsVisibleRectI(nativeObject, rect.X, rect.Y, rect.Width, rect.Height, out isVisible);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isVisible;
        }

        public bool IsVisible(float x, float y)
        {
            return IsVisible(new PointF(x, y));
        }

        public bool IsVisible(int x, int y)
        {
            return IsVisible(new Point(x, y));
        }

        public bool IsVisible(float x, float y, float width, float height)
        {
            return IsVisible(new RectangleF(x, y, width, height));
        }


        public bool IsVisible(int x, int y, int width, int height)
        {
            return IsVisible(new Rectangle(x, y, width, height));
        }


        public Region[] MeasureCharacterRanges(string text, Font font, RectangleF layoutRect, StringFormat stringFormat)
        {
            if ((text == null) || (text.Length == 0))
                return new Region[0];

            if (font == null)
                throw new ArgumentNullException("font");

            if (stringFormat == null)
                throw new ArgumentException("stringFormat");

            int regcount = stringFormat.GetMeasurableCharacterRangeCount();
            if (regcount == 0)
                return new Region[0];

            IntPtr[] native_regions = new IntPtr[regcount];
            Region[] regions = new Region[regcount];

            for (int i = 0; i < regcount; i++)
            {
                regions[i] = new Region();
                native_regions[i] = regions[i].NativeObject;
            }

            Status status = SafeNativeMethods.Gdip.GdipMeasureCharacterRanges(nativeObject, text, text.Length,
                font.NativeObject, ref layoutRect, stringFormat.NativeObject, regcount, out native_regions[0]);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return regions;
        }

        private unsafe SizeF GdipMeasureString(IntPtr graphics, string text, Font font, ref RectangleF layoutRect,
            IntPtr stringFormat)
        {
            if ((text == null) || (text.Length == 0))
                return SizeF.Empty;

            if (font == null)
                throw new ArgumentNullException("font");

            RectangleF boundingBox = new RectangleF();

            Status status = SafeNativeMethods.Gdip.GdipMeasureString(nativeObject, text, text.Length, font.NativeObject,
                ref layoutRect, stringFormat, out boundingBox, null, null);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new SizeF(boundingBox.Width, boundingBox.Height);
        }

        public SizeF MeasureString(string text, Font font)
        {
            return MeasureString(text, font, SizeF.Empty);
        }

        public SizeF MeasureString(string text, Font font, SizeF layoutArea)
        {
            RectangleF rect = new RectangleF(0, 0, layoutArea.Width, layoutArea.Height);
            return GdipMeasureString(nativeObject, text, font, ref rect, IntPtr.Zero);
        }

        public SizeF MeasureString(string text, Font font, int width)
        {
            RectangleF rect = new RectangleF(0, 0, width, Int32.MaxValue);
            return GdipMeasureString(nativeObject, text, font, ref rect, IntPtr.Zero);
        }

        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat)
        {
            RectangleF rect = new RectangleF(0, 0, layoutArea.Width, layoutArea.Height);
            IntPtr format = (stringFormat == null) ? IntPtr.Zero : stringFormat.NativeObject;
            return GdipMeasureString(nativeObject, text, font, ref rect, format);
        }

        public SizeF MeasureString(string text, Font font, int width, StringFormat format)
        {
            RectangleF rect = new RectangleF(0, 0, width, Int32.MaxValue);
            IntPtr stringFormat = (format == null) ? IntPtr.Zero : format.NativeObject;
            return GdipMeasureString(nativeObject, text, font, ref rect, stringFormat);
        }

        public SizeF MeasureString(string text, Font font, PointF origin, StringFormat stringFormat)
        {
            RectangleF rect = new RectangleF(origin.X, origin.Y, 0, 0);
            IntPtr format = (stringFormat == null) ? IntPtr.Zero : stringFormat.NativeObject;
            return GdipMeasureString(nativeObject, text, font, ref rect, format);
        }

        public SizeF MeasureString(string text, Font font, SizeF layoutArea, StringFormat stringFormat,
            out int charactersFitted, out int linesFilled)
        {
            charactersFitted = 0;
            linesFilled = 0;

            if ((text == null) || (text.Length == 0))
                return SizeF.Empty;

            if (font == null)
                throw new ArgumentNullException("font");

            RectangleF boundingBox = new RectangleF();
            RectangleF rect = new RectangleF(0, 0, layoutArea.Width, layoutArea.Height);

            IntPtr format = (stringFormat == null) ? IntPtr.Zero : stringFormat.NativeObject;

            unsafe
            {
                fixed (int* pc = &charactersFitted, pl = &linesFilled)
                {
                    Status status = SafeNativeMethods.Gdip.GdipMeasureString(nativeObject, text, text.Length,
                    font.NativeObject, ref rect, format, out boundingBox, pc, pl);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
            }
            return new SizeF(boundingBox.Width, boundingBox.Height);
        }

        public void MultiplyTransform(Matrix matrix)
        {
            MultiplyTransform(matrix, MatrixOrder.Prepend);
        }

        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
                throw new ArgumentNullException("matrix");

            Status status = SafeNativeMethods.Gdip.GdipMultiplyWorldTransform(nativeObject, matrix.nativeMatrix, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void ReleaseHdc(IntPtr hdc)
        {
            ReleaseHdcInternal(hdc);
        }

        public void ReleaseHdc()
        {
            ReleaseHdcInternal(deviceContextHdc);
        }

        [MonoLimitation("Can only be used when hdc was provided by Graphics.GetHdc() method")]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ReleaseHdcInternal(IntPtr hdc)
        {
            Status status = Status.InvalidParameter;
            if (hdc == deviceContextHdc)
            {
                status = SafeNativeMethods.Gdip.GdipReleaseDC(nativeObject, deviceContextHdc);
                deviceContextHdc = IntPtr.Zero;
            }
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ResetClip()
        {
            Status status = SafeNativeMethods.Gdip.GdipResetClip(nativeObject);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ResetTransform()
        {
            Status status = SafeNativeMethods.Gdip.GdipResetWorldTransform(nativeObject);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Restore(GraphicsState gstate)
        {
            // the possible NRE thrown by gstate.nativeState match MS behaviour
            Status status = SafeNativeMethods.Gdip.GdipRestoreGraphics(nativeObject, (uint)gstate.nativeState);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void RotateTransform(float angle)
        {
            RotateTransform(angle, MatrixOrder.Prepend);
        }

        public void RotateTransform(float angle, MatrixOrder order)
        {
            Status status = SafeNativeMethods.Gdip.GdipRotateWorldTransform(nativeObject, angle, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public GraphicsState Save()
        {
            uint saveState;
            Status status = SafeNativeMethods.Gdip.GdipSaveGraphics(nativeObject, out saveState);
            SafeNativeMethods.Gdip.CheckStatus(status);

            GraphicsState state = new GraphicsState((int)saveState);
            return state;
        }

        public void ScaleTransform(float sx, float sy)
        {
            ScaleTransform(sx, sy, MatrixOrder.Prepend);
        }

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            Status status = SafeNativeMethods.Gdip.GdipScaleWorldTransform(nativeObject, sx, sy, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        public void SetClip(RectangleF rect)
        {
            SetClip(rect, CombineMode.Replace);
        }


        public void SetClip(GraphicsPath path)
        {
            SetClip(path, CombineMode.Replace);
        }


        public void SetClip(Rectangle rect)
        {
            SetClip(rect, CombineMode.Replace);
        }


        public void SetClip(Graphics g)
        {
            SetClip(g, CombineMode.Replace);
        }


        public void SetClip(Graphics g, CombineMode combineMode)
        {
            if (g == null)
                throw new ArgumentNullException("g");

            Status status = SafeNativeMethods.Gdip.GdipSetClipGraphics(nativeObject, g.NativeObject, combineMode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        public void SetClip(Rectangle rect, CombineMode combineMode)
        {
            Status status = SafeNativeMethods.Gdip.GdipSetClipRectI(nativeObject, rect.X, rect.Y, rect.Width, rect.Height, combineMode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        public void SetClip(RectangleF rect, CombineMode combineMode)
        {
            Status status = SafeNativeMethods.Gdip.GdipSetClipRect(nativeObject, rect.X, rect.Y, rect.Width, rect.Height, combineMode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        public void SetClip(Region region, CombineMode combineMode)
        {
            if (region == null)
                throw new ArgumentNullException("region");
            Status status = SafeNativeMethods.Gdip.GdipSetClipRegion(nativeObject, region.NativeObject, combineMode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        public void SetClip(GraphicsPath path, CombineMode combineMode)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            Status status = SafeNativeMethods.Gdip.GdipSetClipPath(nativeObject, path.nativePath, combineMode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        public void TransformPoints(CoordinateSpace destSpace, CoordinateSpace srcSpace, PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");

            IntPtr ptrPt = GDIPlus.FromPointToUnManagedMemory(pts);

            Status status = SafeNativeMethods.Gdip.GdipTransformPoints(nativeObject, destSpace, srcSpace, ptrPt, pts.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);

            GDIPlus.FromUnManagedMemoryToPoint(ptrPt, pts);
        }


        public void TransformPoints(CoordinateSpace destSpace, CoordinateSpace srcSpace, Point[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");
            IntPtr ptrPt = GDIPlus.FromPointToUnManagedMemoryI(pts);

            Status status = SafeNativeMethods.Gdip.GdipTransformPointsI(nativeObject, destSpace, srcSpace, ptrPt, pts.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);

            GDIPlus.FromUnManagedMemoryToPointI(ptrPt, pts);
        }


        public void TranslateClip(int dx, int dy)
        {
            Status status = SafeNativeMethods.Gdip.GdipTranslateClipI(nativeObject, dx, dy);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }


        public void TranslateClip(float dx, float dy)
        {
            Status status = SafeNativeMethods.Gdip.GdipTranslateClip(nativeObject, dx, dy);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TranslateTransform(float dx, float dy)
        {
            TranslateTransform(dx, dy, MatrixOrder.Prepend);
        }


        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            Status status = SafeNativeMethods.Gdip.GdipTranslateWorldTransform(nativeObject, dx, dy, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public Region Clip
        {
            get
            {
                Region reg = new Region();
                Status status = SafeNativeMethods.Gdip.GdipGetClip(nativeObject, reg.NativeObject);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return reg;
            }
            set
            {
                SetClip(value, CombineMode.Replace);
            }
        }

        public RectangleF ClipBounds
        {
            get
            {
                RectangleF rect = new RectangleF();
                Status status = SafeNativeMethods.Gdip.GdipGetClipBounds(nativeObject, out rect);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return rect;
            }
        }

        public CompositingMode CompositingMode
        {
            get
            {
                CompositingMode mode;
                Status status = SafeNativeMethods.Gdip.GdipGetCompositingMode(nativeObject, out mode);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return mode;
            }
            set
            {
                Status status = SafeNativeMethods.Gdip.GdipSetCompositingMode(nativeObject, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }

        }

        public CompositingQuality CompositingQuality
        {
            get
            {
                CompositingQuality quality;

                Status status = SafeNativeMethods.Gdip.GdipGetCompositingQuality(nativeObject, out quality);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return quality;
            }
            set
            {
                Status status = SafeNativeMethods.Gdip.GdipSetCompositingQuality(nativeObject, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public float DpiX
        {
            get
            {
                float x;

                Status status = SafeNativeMethods.Gdip.GdipGetDpiX(nativeObject, out x);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return x;
            }
        }

        public float DpiY
        {
            get
            {
                float y;

                Status status = SafeNativeMethods.Gdip.GdipGetDpiY(nativeObject, out y);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return y;
            }
        }

        public InterpolationMode InterpolationMode
        {
            get
            {
                InterpolationMode imode = InterpolationMode.Invalid;
                Status status = SafeNativeMethods.Gdip.GdipGetInterpolationMode(nativeObject, out imode);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return imode;
            }
            set
            {
                Status status = SafeNativeMethods.Gdip.GdipSetInterpolationMode(nativeObject, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public bool IsClipEmpty
        {
            get
            {
                bool isEmpty = false;

                Status status = SafeNativeMethods.Gdip.GdipIsClipEmpty(nativeObject, out isEmpty);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return isEmpty;
            }
        }

        public bool IsVisibleClipEmpty
        {
            get
            {
                bool isEmpty = false;

                Status status = SafeNativeMethods.Gdip.GdipIsVisibleClipEmpty(nativeObject, out isEmpty);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return isEmpty;
            }
        }

        public float PageScale
        {
            get
            {
                float scale;

                Status status = SafeNativeMethods.Gdip.GdipGetPageScale(nativeObject, out scale);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return scale;
            }
            set
            {
                Status status = SafeNativeMethods.Gdip.GdipSetPageScale(nativeObject, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public GraphicsUnit PageUnit
        {
            get
            {
                GraphicsUnit unit;

                Status status = SafeNativeMethods.Gdip.GdipGetPageUnit(nativeObject, out unit);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return unit;
            }
            set
            {
                Status status = SafeNativeMethods.Gdip.GdipSetPageUnit(nativeObject, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        [MonoTODO("This property does not do anything when used with libgdiplus.")]
        public PixelOffsetMode PixelOffsetMode
        {
            get
            {
                PixelOffsetMode pixelOffset = PixelOffsetMode.Invalid;

                Status status = SafeNativeMethods.Gdip.GdipGetPixelOffsetMode(nativeObject, out pixelOffset);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return pixelOffset;
            }
            set
            {
                Status status = SafeNativeMethods.Gdip.GdipSetPixelOffsetMode(nativeObject, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public Point RenderingOrigin
        {
            get
            {
                int x, y;
                Status status = SafeNativeMethods.Gdip.GdipGetRenderingOrigin(nativeObject, out x, out y);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return new Point(x, y);
            }

            set
            {
                Status status = SafeNativeMethods.Gdip.GdipSetRenderingOrigin(nativeObject, value.X, value.Y);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public SmoothingMode SmoothingMode
        {
            get
            {
                SmoothingMode mode = SmoothingMode.Invalid;

                Status status = SafeNativeMethods.Gdip.GdipGetSmoothingMode(nativeObject, out mode);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return mode;
            }

            set
            {
                Status status = SafeNativeMethods.Gdip.GdipSetSmoothingMode(nativeObject, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        [MonoTODO("This property does not do anything when used with libgdiplus.")]
        public int TextContrast
        {
            get
            {
                int contrast;

                Status status = SafeNativeMethods.Gdip.GdipGetTextContrast(nativeObject, out contrast);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return contrast;
            }

            set
            {
                Status status = SafeNativeMethods.Gdip.GdipSetTextContrast(nativeObject, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public TextRenderingHint TextRenderingHint
        {
            get
            {
                TextRenderingHint hint;

                Status status = SafeNativeMethods.Gdip.GdipGetTextRenderingHint(nativeObject, out hint);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return hint;
            }

            set
            {
                Status status = SafeNativeMethods.Gdip.GdipSetTextRenderingHint(nativeObject, value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public Matrix Transform
        {
            get
            {
                Matrix matrix = new Matrix();
                Status status = SafeNativeMethods.Gdip.GdipGetWorldTransform(nativeObject, matrix.nativeMatrix);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return matrix;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("value");

                Status status = SafeNativeMethods.Gdip.GdipSetWorldTransform(nativeObject, value.nativeMatrix);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public RectangleF VisibleClipBounds
        {
            get
            {
                RectangleF rect;

                Status status = SafeNativeMethods.Gdip.GdipGetVisibleClipBounds(nativeObject, out rect);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return rect;
            }
        }

        [MonoTODO]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object GetContextInfo()
        {
            // only known source of information @ http://blogs.wdevs.com/jdunlap/Default.aspx
            throw new NotImplementedException();
        }
    }
}

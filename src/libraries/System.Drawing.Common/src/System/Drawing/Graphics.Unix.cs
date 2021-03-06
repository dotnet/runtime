// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
using System.Runtime.Versioning;

namespace System.Drawing
{
    public sealed partial class Graphics : MarshalByRefObject, IDisposable, IDeviceContext
    {
        internal IMacContext? maccontext;
        private bool disposed;
        private static float defDpiX;
        private static float defDpiY;
        private Metafile.MetafileHolder? _metafileHolder;

        internal Graphics(IntPtr nativeGraphics) => NativeGraphics = nativeGraphics;

        internal Graphics(IntPtr nativeGraphics, Image image) : this(nativeGraphics)
        {
            if (image is Metafile mf)
            {
                _metafileHolder = mf.AddMetafileHolder();
            }
        }

        ~Graphics()
        {
            Dispose();
        }

        internal static float systemDpiX
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

        internal static float systemDpiY
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
                throw new NotImplementedException(SR.NotImplementedUnderX11);

            if (Gdip.Display == IntPtr.Zero)
            {
                Gdip.Display = LibX11Functions.XOpenDisplay(IntPtr.Zero);
            }

            window = LibX11Functions.XRootWindow(Gdip.Display, 0);
            defvisual = LibX11Functions.XDefaultVisual(Gdip.Display, 0);
            XVisualInfo visual = default;

            /* Get XVisualInfo for this visual */
            visual.visualid = LibX11Functions.XVisualIDFromVisual(defvisual);
            vPtr = LibX11Functions.XGetVisualInfo(Gdip.Display, 0x1 /* VisualIDMask */, ref visual, ref nitems);
            visual = (XVisualInfo)Marshal.PtrToStructure(vPtr, typeof(XVisualInfo))!;
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

                status = Gdip.GdipDeleteGraphics(new HandleRef(this, NativeGraphics));
                NativeGraphics = IntPtr.Zero;
                Gdip.CheckStatus(status);

                if (_metafileHolder != null)
                {
                    var mh = _metafileHolder;
                    _metafileHolder = null;
                    mh.GraphicsDisposed();
                }

                disposed = true;
            }

            GC.SuppressFinalize(this);
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

                status = Gdip.GdipDrawBezier(
                                        new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
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

                status = Gdip.GdipDrawBezier(
                                        new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                                        p1.X, p1.Y, p2.X, p2.Y,
                                        p3.X, p3.Y, p4.X, p4.Y);
                Gdip.CheckStatus(status);
            }
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

        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            if (pen == null)
                throw new ArgumentNullException(nameof(pen));
            if (!float.IsNaN(x1) && !float.IsNaN(y1) &&
                !float.IsNaN(x2) && !float.IsNaN(y2))
            {
                int status = Gdip.GdipDrawLine(new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen), x1, y1, x2, y2);
                Gdip.CheckStatus(status);
            }
        }

        public void EndContainer(GraphicsContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            int status = Gdip.GdipEndContainer(new HandleRef(this, NativeGraphics), container.nativeGraphicsContainer);
            Gdip.CheckStatus(status);
        }

        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes? imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes? imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes? imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes? imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes? imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes? imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, Rectangle srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes? imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, Rectangle srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes? imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, Point destPoint, Rectangle srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes? imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, RectangleF srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes? imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, RectangleF srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes? imageAttr)
        {
            throw new NotImplementedException();
        }

        public void EnumerateMetafile(Metafile metafile, PointF destPoint, RectangleF srcRect, GraphicsUnit unit, EnumerateMetafileProc callback, IntPtr callbackData, ImageAttributes? imageAttr)
        {
            throw new NotImplementedException();
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
        public static Graphics? FromHdcInternal(IntPtr hdc)
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
                        throw new NotSupportedException(SR.CouldNotOpenDisplay);
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
                throw new ArgumentException(SR.CannotCreateGraphics, nameof(image));

            int status = Gdip.GdipGetImageGraphicsContext(image.nativeImage, out graphics);
            Gdip.CheckStatus(status);
            Graphics result = new Graphics(graphics, image);

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

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void ReleaseHdcInternal(IntPtr hdc)
        {
            int status = Gdip.InvalidParameter;
            if (hdc == _nativeHdc)
            {
                status = Gdip.GdipReleaseDC(new HandleRef(this, NativeGraphics), new HandleRef(this, _nativeHdc));
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
            int status = Gdip.GdipSaveGraphics(new HandleRef(this, NativeGraphics), out int state);
            Gdip.CheckStatus(status);

            return new GraphicsState((int)state);
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
        [SupportedOSPlatform("windows")]
        public object GetContextInfo()
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [SupportedOSPlatform("windows")]
        public void GetContextInfo(out PointF offset)
        {
            throw new PlatformNotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [SupportedOSPlatform("windows")]
        public void GetContextInfo(out PointF offset, out Region? clip)
        {
            throw new PlatformNotSupportedException();
        }

        private void CheckErrorStatus(int status)
        {
            Gdip.CheckStatus(status);
        }
    }
}

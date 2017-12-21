// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Drawing.Imaging;
using System.Drawing.Internal;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace System.Drawing
{
    public sealed partial class Bitmap : Image
    {
        private static Color s_defaultTransparentColor = Color.LightGray;

        private Bitmap() { }

        internal Bitmap(IntPtr ptr) => SetNativeImage(ptr);

        public Bitmap(string filename) : this (filename, useIcm: false) { }

        public Bitmap(string filename, bool useIcm)
        {
            // GDI+ will read this file multiple times. Get the fully qualified path
            // so if the app's default directory changes we won't get an error.
            filename = Path.GetFullPath(filename);

            IntPtr bitmap = IntPtr.Zero;
            int status;

            if (useIcm)
            {
                status = SafeNativeMethods.Gdip.GdipCreateBitmapFromFileICM(filename, out bitmap);
            }
            else
            {
                status = SafeNativeMethods.Gdip.GdipCreateBitmapFromFile(filename, out bitmap);
            }
            SafeNativeMethods.Gdip.CheckStatus(status);

            ValidateBitmap(bitmap);

            SetNativeImage(bitmap);
            EnsureSave(this, filename, null);
        }

        public Bitmap(Stream stream) : this(stream, false) { }

        public Bitmap(int width, int height) : this(width, height, PixelFormat.Format32bppArgb)
        {
        }

        public Bitmap(int width, int height, Graphics g)
        {
            if (g == null)
            {
                throw new ArgumentNullException(nameof(g));
            }

            IntPtr bitmap = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateBitmapFromGraphics(width, height, new HandleRef(g, g.NativeGraphics), out bitmap);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeImage(bitmap);
        }

        public Bitmap(int width, int height, int stride, PixelFormat format, IntPtr scan0)
        {
            IntPtr bitmap = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateBitmapFromScan0(width, height, stride, unchecked((int)format), new HandleRef(null, scan0), out bitmap);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeImage(bitmap);
        }

        public Bitmap(int width, int height, PixelFormat format)
        {
            IntPtr bitmap = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateBitmapFromScan0(width, height, 0, unchecked((int)format), NativeMethods.NullHandleRef, out bitmap);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeImage(bitmap);
        }

        public Bitmap(Image original) : this(original, original.Width, original.Height)
        {
        }

        public Bitmap(Image original, Size newSize) : this(original, newSize.Width, newSize.Height)
        {
        }

        public Bitmap(Image original, int width, int height) : this(width, height, PixelFormat.Format32bppArgb)
        {
            using (Graphics g = Graphics.FromImage(this))
            {
                g.Clear(Color.Transparent);
                g.DrawImage(original, 0, 0, width, height);
            }
        }

        public static Bitmap FromHicon(IntPtr hicon)
        {
            IntPtr bitmap = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateBitmapFromHICON(new HandleRef(null, hicon), out bitmap);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new Bitmap(bitmap);
        }

        public static Bitmap FromResource(IntPtr hinstance, string bitmapName)
        {
            IntPtr bitmap;
            IntPtr name = Marshal.StringToHGlobalUni(bitmapName);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipCreateBitmapFromResource(new HandleRef(null, hinstance),
                                                                  new HandleRef(null, name),
                                                                  out bitmap);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
            finally
            {
                Marshal.FreeHGlobal(name);
            }

            return new Bitmap(bitmap);
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public IntPtr GetHbitmap() => GetHbitmap(Color.LightGray);

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public IntPtr GetHbitmap(Color background)
        {
            IntPtr hBitmap = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateHBITMAPFromBitmap(new HandleRef(this, nativeImage), out hBitmap,
                                                             ColorTranslator.ToWin32(background));
            if (status == 2 /* invalid parameter*/ && (Width >= short.MaxValue || Height >= short.MaxValue))
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidSize));
            }

            SafeNativeMethods.Gdip.CheckStatus(status);

            return hBitmap;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public IntPtr GetHicon()
        {
            IntPtr hIcon = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreateHICONFromBitmap(new HandleRef(this, nativeImage), out hIcon);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return hIcon;
        }

        public Bitmap Clone(RectangleF rect, PixelFormat format)
        {
            if (rect.Width == 0 || rect.Height == 0)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));
            }

            IntPtr dstHandle = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCloneBitmapArea(
                                                    rect.X,
                                                    rect.Y,
                                                    rect.Width,
                                                    rect.Height,
                                                    unchecked((int)format),
                                                    new HandleRef(this, nativeImage),
                                                    out dstHandle);

            if (status != SafeNativeMethods.Gdip.Ok || dstHandle == IntPtr.Zero)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new Bitmap(dstHandle);
        }

        public void MakeTransparent()
        {
            Color transparent = s_defaultTransparentColor;
            if (Height > 0 && Width > 0)
            {
                transparent = GetPixel(0, Size.Height - 1);
            }
            if (transparent.A < 255)
            {
                // It's already transparent, and if we proceeded, we will do something
                // unintended like making black transparent
                return;
            }

            MakeTransparent(transparent);
        }

        public void MakeTransparent(Color transparentColor)
        {
            if (RawFormat.Guid == ImageFormat.Icon.Guid)
            {
                throw new InvalidOperationException(SR.Format(SR.CantMakeIconTransparent));
            }

            Size size = Size;

            // The new bitmap must be in 32bppARGB  format, because that's the only
            // thing that supports alpha.  (And that's what the image is initialized to -- transparent)
            using (var result = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb))
            using (Graphics graphics = Graphics.FromImage(result))
            {
                graphics.Clear(Color.Transparent);
                Rectangle rectangle = new Rectangle(0, 0, size.Width, size.Height);

                using (var attributes = new ImageAttributes())
                {
                    attributes.SetColorKey(transparentColor, transparentColor);
                    graphics.DrawImage(this, rectangle,
                                        0, 0, size.Width, size.Height,
                                        GraphicsUnit.Pixel, attributes, null, IntPtr.Zero);
                }

                // Swap nativeImage pointers to make it look like we modified the image in place
                IntPtr temp = nativeImage;
                nativeImage = result.nativeImage;
                result.nativeImage = temp;
            }
        }

        public BitmapData LockBits(Rectangle rect, ImageLockMode flags, PixelFormat format)
        {
            return LockBits(rect, flags, format, new BitmapData());
        }

        public BitmapData LockBits(Rectangle rect, ImageLockMode flags, PixelFormat format, BitmapData bitmapData)
        {
            var gprect = new GPRECT(rect);
            int status = SafeNativeMethods.Gdip.GdipBitmapLockBits(new HandleRef(this, nativeImage), ref gprect,
                                                    flags, format, bitmapData);
            // libgdiplus has the wrong error code mapping for this state.
            if (status == 7)
            {
                status = 8;
            }
            SafeNativeMethods.Gdip.CheckStatus(status);

            return bitmapData;
        }

        public void UnlockBits(BitmapData bitmapdata)
        {
            int status = SafeNativeMethods.Gdip.GdipBitmapUnlockBits(new HandleRef(this, nativeImage), bitmapdata);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public Color GetPixel(int x, int y)
        {
            if (x < 0 || x >= Width)
            {
                throw new ArgumentOutOfRangeException(nameof(x), SR.Format(SR.ValidRangeX));
            }

            if (y < 0 || y >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(y), SR.Format(SR.ValidRangeY));
            }

            int color = 0;
            int status = SafeNativeMethods.Gdip.GdipBitmapGetPixel(new HandleRef(this, nativeImage), x, y, out color);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return Color.FromArgb(color);
        }

        public void SetPixel(int x, int y, Color color)
        {
            if ((PixelFormat & PixelFormat.Indexed) != 0)
            {
                throw new InvalidOperationException(SR.Format(SR.GdiplusCannotSetPixelFromIndexedPixelFormat));
            }

            if (x < 0 || x >= Width)
            {
                throw new ArgumentOutOfRangeException(nameof(x), SR.Format(SR.ValidRangeX));
            }

            if (y < 0 || y >= Height)
            {
                throw new ArgumentOutOfRangeException(nameof(y), SR.Format(SR.ValidRangeY));
            }

            int status = SafeNativeMethods.Gdip.GdipBitmapSetPixel(new HandleRef(this, nativeImage), x, y, color.ToArgb());
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void SetResolution(float xDpi, float yDpi)
        {
            int status = SafeNativeMethods.Gdip.GdipBitmapSetResolution(new HandleRef(this, nativeImage), xDpi, yDpi);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }
        public Bitmap Clone(Rectangle rect, PixelFormat format)
        {
            if (rect.Width == 0 || rect.Height == 0)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));
            }

            IntPtr dstHandle = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCloneBitmapAreaI(
                                                     rect.X,
                                                     rect.Y,
                                                     rect.Width,
                                                     rect.Height,
                                                     unchecked((int)format),
                                                     new HandleRef(this, nativeImage),
                                                     out dstHandle);

            if (status != SafeNativeMethods.Gdip.Ok || dstHandle == IntPtr.Zero)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new Bitmap(dstHandle);
        }
    }
}

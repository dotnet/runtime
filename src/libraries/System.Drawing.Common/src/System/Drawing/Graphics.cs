// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Internal;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    /// <summary>
    /// Encapsulates a GDI+ drawing surface.
    /// </summary>
    public sealed partial class Graphics : MarshalByRefObject, IDisposable, IDeviceContext
    {
        /// <summary>
        /// Handle to native DC - obtained from the GDI+ graphics object. We need to cache it to implement
        /// IDeviceContext interface.
        /// </summary>
        private IntPtr _nativeHdc;

        /// <summary>
        /// Handle to native GDI+ graphics object. This object is created on demand.
        /// </summary>
        internal IntPtr NativeGraphics { get; private set; }

        public Region Clip
        {
            get
            {
                var region = new Region();
                int status = SafeNativeMethods.Gdip.GdipGetClip(new HandleRef(this, NativeGraphics), new HandleRef(region, region._nativeRegion));
                SafeNativeMethods.Gdip.CheckStatus(status);

                return region;
            }
            set => SetClip(value, CombineMode.Replace);
        }

        public RectangleF ClipBounds
        {
            get
            {
                var rect = new GPRECTF();
                int status = SafeNativeMethods.Gdip.GdipGetClipBounds(new HandleRef(this, NativeGraphics), ref rect);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return rect.ToRectangleF();
            }
        }

        /// <summary>
        /// Gets or sets the <see cref='Drawing2D.CompositingMode'/> associated with this <see cref='Graphics'/>.
        /// </summary>
        public CompositingMode CompositingMode
        {
            get
            {
                int mode = 0;
                int status = SafeNativeMethods.Gdip.GdipGetCompositingMode(new HandleRef(this, NativeGraphics), out mode);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return (CompositingMode)mode;
            }
            set
            {
                if (value < CompositingMode.SourceOver || value > CompositingMode.SourceCopy)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(CompositingMode));
                }

                int status = SafeNativeMethods.Gdip.GdipSetCompositingMode(new HandleRef(this, NativeGraphics), unchecked((int)value));
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public CompositingQuality CompositingQuality
        {
            get
            {
                CompositingQuality cq;
                int status = SafeNativeMethods.Gdip.GdipGetCompositingQuality(new HandleRef(this, NativeGraphics), out cq);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return cq;
            }
            set
            {
                if (value < CompositingQuality.Invalid || value > CompositingQuality.AssumeLinear)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(CompositingQuality));
                }

                int status = SafeNativeMethods.Gdip.GdipSetCompositingQuality(new HandleRef(this, NativeGraphics), value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public float DpiX
        {
            get
            {
                var dpi = new float[] { 0.0f };
                int status = SafeNativeMethods.Gdip.GdipGetDpiX(new HandleRef(this, NativeGraphics), dpi);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return dpi[0];
            }
        }

        public float DpiY
        {
            get
            {
                var dpi = new float[] { 0.0f };
                int status = SafeNativeMethods.Gdip.GdipGetDpiY(new HandleRef(this, NativeGraphics), dpi);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return dpi[0];
            }
        }

        /// <summary>
        /// Gets or sets the interpolation mode associated with this Graphics.
        /// </summary>
        public InterpolationMode InterpolationMode
        {
            get
            {
                int mode = 0;
                int status = SafeNativeMethods.Gdip.GdipGetInterpolationMode(new HandleRef(this, NativeGraphics), out mode);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return (InterpolationMode)mode;
            }
            set
            {
                if (value < InterpolationMode.Invalid || value > InterpolationMode.HighQualityBicubic)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(InterpolationMode));
                }

                // GDI+ interprets the value of InterpolationMode and sets a value accordingly.
                // Libgdiplus does not, so do this manually here.
                switch (value)
                {
                    case InterpolationMode.Default:
                    case InterpolationMode.Low:
                        value = InterpolationMode.Bilinear;
                        break;
                    case InterpolationMode.High:
                        value = InterpolationMode.HighQualityBicubic;
                        break;
                    case InterpolationMode.Invalid:
                        throw new ArgumentException(SR.GdiplusInvalidParameter);
                    default:
                        break;
                }

                int status = SafeNativeMethods.Gdip.GdipSetInterpolationMode(new HandleRef(this, NativeGraphics), unchecked((int)value));
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public bool IsClipEmpty
        {
            get
            {
                int isEmpty;
                int status = SafeNativeMethods.Gdip.GdipIsClipEmpty(new HandleRef(this, NativeGraphics), out isEmpty);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return isEmpty != 0;
            }
        }

        public bool IsVisibleClipEmpty
        {
            get
            {
                int isEmpty;
                int status = SafeNativeMethods.Gdip.GdipIsVisibleClipEmpty(new HandleRef(this, NativeGraphics), out isEmpty);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return isEmpty != 0;
            }
        }

        public float PageScale
        {
            get
            {
                var scale = new float[] { 0.0f };
                int status = SafeNativeMethods.Gdip.GdipGetPageScale(new HandleRef(this, NativeGraphics), scale);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return scale[0];
            }

            set
            {
                // Libgdiplus doesn't perform argument validation, so do this here for compatability.
                if (value <= 0 || value > 1000000032)
                {
                    throw new ArgumentException(SR.GdiplusInvalidParameter);
                }

                int status = SafeNativeMethods.Gdip.GdipSetPageScale(new HandleRef(this, NativeGraphics), value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public GraphicsUnit PageUnit
        {
            get
            {
                int unit = 0;
                int status = SafeNativeMethods.Gdip.GdipGetPageUnit(new HandleRef(this, NativeGraphics), out unit);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return (GraphicsUnit)unit;
            }
            set
            {
                if (value < GraphicsUnit.World || value > GraphicsUnit.Millimeter)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(GraphicsUnit));
                }

                // GDI+ doesn't allow GraphicsUnit.World as a valid value for PageUnit.
                // Libgdiplus doesn't perform argument validation, so do this here.
                if (value == GraphicsUnit.World)
                {
                    throw new ArgumentException(SR.GdiplusInvalidParameter);
                }

                int status = SafeNativeMethods.Gdip.GdipSetPageUnit(new HandleRef(this, NativeGraphics), unchecked((int)value));
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public PixelOffsetMode PixelOffsetMode
        {
            get
            {
                PixelOffsetMode mode = 0;
                int status = SafeNativeMethods.Gdip.GdipGetPixelOffsetMode(new HandleRef(this, NativeGraphics), out mode);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return mode;
            }
            set
            {
                if (value < PixelOffsetMode.Invalid || value > PixelOffsetMode.Half)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(PixelOffsetMode));
                }

                // GDI+ doesn't allow PixelOffsetMode.Invalid as a valid value for PixelOffsetMode.
                // Libgdiplus doesn't perform argument validation, so do this here.
                if (value == PixelOffsetMode.Invalid)
                {
                    throw new ArgumentException(SR.GdiplusInvalidParameter);
                }

                int status = SafeNativeMethods.Gdip.GdipSetPixelOffsetMode(new HandleRef(this, NativeGraphics), value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public Point RenderingOrigin
        {
            get
            {
                int x, y;
                int status = SafeNativeMethods.Gdip.GdipGetRenderingOrigin(new HandleRef(this, NativeGraphics), out x, out y);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return new Point(x, y);
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetRenderingOrigin(new HandleRef(this, NativeGraphics), value.X, value.Y);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public SmoothingMode SmoothingMode
        {
            get
            {
                SmoothingMode mode = 0;
                int status = SafeNativeMethods.Gdip.GdipGetSmoothingMode(new HandleRef(this, NativeGraphics), out mode);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return mode;
            }
            set
            {
                if (value < SmoothingMode.Invalid || value > SmoothingMode.AntiAlias)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(SmoothingMode));
                }

                // GDI+ interprets the value of SmoothingMode and sets a value accordingly.
                // Libgdiplus does not, so do this manually here.
                switch (value)
                {
                    case SmoothingMode.Default:
                    case SmoothingMode.HighSpeed:
                        value = SmoothingMode.None;
                        break;
                    case SmoothingMode.HighQuality:
                        value = SmoothingMode.AntiAlias;
                        break;
                    case SmoothingMode.Invalid:
                        throw new ArgumentException(SR.GdiplusInvalidParameter);
                    default:
                        break;
                }

                int status = SafeNativeMethods.Gdip.GdipSetSmoothingMode(new HandleRef(this, NativeGraphics), value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public int TextContrast
        {
            get
            {
                int textContrast = 0;
                int status = SafeNativeMethods.Gdip.GdipGetTextContrast(new HandleRef(this, NativeGraphics), out textContrast);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return textContrast;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetTextContrast(new HandleRef(this, NativeGraphics), value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        /// <summary>
        /// Gets or sets the rendering mode for text associated with this <see cref='Graphics'/>.
        /// </summary>
        public TextRenderingHint TextRenderingHint
        {
            get
            {
                TextRenderingHint hint = 0;

                int status = SafeNativeMethods.Gdip.GdipGetTextRenderingHint(new HandleRef(this, NativeGraphics), out hint);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return hint;
            }
            set
            {
                if (value < TextRenderingHint.SystemDefault || value > TextRenderingHint.ClearTypeGridFit)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(TextRenderingHint));
                }

                int status = SafeNativeMethods.Gdip.GdipSetTextRenderingHint(new HandleRef(this, NativeGraphics), value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        /// <summary>
        /// Gets or sets the world transform for this <see cref='Graphics'/>.
        /// </summary>
        public Matrix Transform
        {
            get
            {
                var matrix = new Matrix();
                int status = SafeNativeMethods.Gdip.GdipGetWorldTransform(new HandleRef(this, NativeGraphics),
                                                           new HandleRef(matrix, matrix.nativeMatrix));
                SafeNativeMethods.Gdip.CheckStatus(status);

                return matrix;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetWorldTransform(new HandleRef(this, NativeGraphics),
                                                           new HandleRef(value, value.nativeMatrix));
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public IntPtr GetHdc()
        {
            IntPtr hdc = IntPtr.Zero;
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipGetDC(new HandleRef(this, NativeGraphics), out hdc));

            _nativeHdc = hdc; // need to cache the hdc to be able to release with a call to IDeviceContext.ReleaseHdc().
            return _nativeHdc;
        }

        [EditorBrowsable(EditorBrowsableState.Advanced)]
        public void ReleaseHdc(IntPtr hdc) => ReleaseHdcInternal(hdc);

        public void ReleaseHdc() => ReleaseHdcInternal(_nativeHdc);

        /// <summary>
        /// Forces immediate execution of all operations currently on the stack.
        /// </summary>
        public void Flush() => Flush(FlushIntention.Flush);

        /// <summary>
        /// Forces execution of all operations currently on the stack.
        /// </summary>
        public void Flush(FlushIntention intention)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipFlush(new HandleRef(this, NativeGraphics), intention));
            FlushCore();
        }

        public void SetClip(Graphics g) => SetClip(g, CombineMode.Replace);

        public void SetClip(Graphics g, CombineMode combineMode)
        {
            if (g == null)
                throw new ArgumentNullException(nameof(g));

            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipSetClipGraphics(
                new HandleRef(this, NativeGraphics),
                new HandleRef(g, g.NativeGraphics),
                combineMode));
        }

        public void SetClip(Rectangle rect) => SetClip(rect, CombineMode.Replace);

        public void SetClip(Rectangle rect, CombineMode combineMode)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipSetClipRectI(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                combineMode));
        }

        public void SetClip(RectangleF rect) => SetClip(rect, CombineMode.Replace);

        public void SetClip(RectangleF rect, CombineMode combineMode)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipSetClipRect(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                combineMode));
        }

        public void SetClip(GraphicsPath path) => SetClip(path, CombineMode.Replace);

        public void SetClip(GraphicsPath path, CombineMode combineMode)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipSetClipPath(
                new HandleRef(this, NativeGraphics),
                new HandleRef(path, path._nativePath),
                combineMode));
        }

        public void SetClip(Region region, CombineMode combineMode)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipSetClipRegion(
                new HandleRef(this, NativeGraphics),
                new HandleRef(region, region._nativeRegion),
                combineMode));
        }

        public void IntersectClip(Rectangle rect)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipSetClipRectI(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                CombineMode.Intersect));
        }

        public void IntersectClip(RectangleF rect)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipSetClipRect(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                CombineMode.Intersect));
        }

        public void IntersectClip(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipSetClipRegion(
                new HandleRef(this, NativeGraphics),
                new HandleRef(region, region._nativeRegion),
                CombineMode.Intersect));
        }

        public void ExcludeClip(Rectangle rect)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipSetClipRectI(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                CombineMode.Exclude));
        }

        public void ExcludeClip(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipSetClipRegion(
                new HandleRef(this, NativeGraphics),
                new HandleRef(region, region._nativeRegion),
                CombineMode.Exclude));
        }

        public void ResetClip()
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipResetClip(new HandleRef(this, NativeGraphics)));
        }

        public void TranslateClip(float dx, float dy)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipTranslateClip(new HandleRef(this, NativeGraphics), dx, dy));
        }

        public void TranslateClip(int dx, int dy)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateClip(new HandleRef(this, NativeGraphics), dx, dy);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public bool IsVisible(int x, int y) => IsVisible(new Point(x, y));

        public bool IsVisible(Point point)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipIsVisiblePointI(
                new HandleRef(this, NativeGraphics),
                point.X, point.Y,
                out int isVisible));

            return isVisible != 0;
        }

        public bool IsVisible(float x, float y) => IsVisible(new PointF(x, y));

        public bool IsVisible(PointF point)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipIsVisiblePoint(
                new HandleRef(this, NativeGraphics),
                point.X, point.Y,
                out int isVisible));

            return isVisible != 0;
        }

        public bool IsVisible(int x, int y, int width, int height)
        {
            return IsVisible(new Rectangle(x, y, width, height));
        }

        public bool IsVisible(Rectangle rect)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipIsVisibleRectI(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                out int isVisible));

            return isVisible != 0;
        }

        public bool IsVisible(float x, float y, float width, float height)
        {
            return IsVisible(new RectangleF(x, y, width, height));
        }

        public bool IsVisible(RectangleF rect)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipIsVisibleRect(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                out int isVisible));

            return isVisible != 0;
        }

        /// <summary>
        /// Resets the world transform to identity.
        /// </summary>
        public void ResetTransform()
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipResetWorldTransform(new HandleRef(this, NativeGraphics)));
        }

        /// <summary>
        /// Multiplies the <see cref='Matrix'/> that represents the world transform and <paramref name="matrix"/>.
        /// </summary>
        public void MultiplyTransform(Matrix matrix) => MultiplyTransform(matrix, MatrixOrder.Prepend);

        /// <summary>
        /// Multiplies the <see cref='Matrix'/> that represents the world transform and <paramref name="matrix"/>.
        /// </summary>
        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));

            // Multiplying the transform by a disposed matrix is a nop in GDI+, but throws
            // with the libgdiplus backend. Simulate a nop for compatability with GDI+.
            if (matrix.nativeMatrix == IntPtr.Zero)
            {
                return;
            }

            int status = SafeNativeMethods.Gdip.GdipMultiplyWorldTransform(new HandleRef(this, NativeGraphics),
                                                            new HandleRef(matrix, matrix.nativeMatrix),
                                                            order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipTranslateWorldTransform(new HandleRef(this, NativeGraphics), dx, dy, order));
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipScaleWorldTransform(new HandleRef(this, NativeGraphics), sx, sy, order));
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            SafeNativeMethods.Gdip.CheckStatus(SafeNativeMethods.Gdip.GdipRotateWorldTransform(new HandleRef(this, NativeGraphics), angle, order));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

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
                int status = Gdip.GdipGetClip(new HandleRef(this, NativeGraphics), new HandleRef(region, region.NativeRegion));
                Gdip.CheckStatus(status);

                return region;
            }
            set => SetClip(value, CombineMode.Replace);
        }

        public RectangleF ClipBounds
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetClipBounds(new HandleRef(this, NativeGraphics), out RectangleF rect));
                return rect;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref='Drawing2D.CompositingMode'/> associated with this <see cref='Graphics'/>.
        /// </summary>
        public CompositingMode CompositingMode
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetCompositingMode(new HandleRef(this, NativeGraphics), out CompositingMode mode));
                return mode;
            }
            set
            {
                if (value < CompositingMode.SourceOver || value > CompositingMode.SourceCopy)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(CompositingMode));

                Gdip.CheckStatus(Gdip.GdipSetCompositingMode(new HandleRef(this, NativeGraphics), value));
            }
        }

        public CompositingQuality CompositingQuality
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetCompositingQuality(new HandleRef(this, NativeGraphics), out CompositingQuality cq));
                return cq;
            }
            set
            {
                if (value < CompositingQuality.Invalid || value > CompositingQuality.AssumeLinear)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(CompositingQuality));

                Gdip.CheckStatus(Gdip.GdipSetCompositingQuality(new HandleRef(this, NativeGraphics), value));
            }
        }

        public float DpiX
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetDpiX(new HandleRef(this, NativeGraphics), out float dpi));
                return dpi;
            }
        }

        public float DpiY
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetDpiY(new HandleRef(this, NativeGraphics), out float dpi));
                return dpi;
            }
        }

        /// <summary>
        /// Gets or sets the interpolation mode associated with this Graphics.
        /// </summary>
        public InterpolationMode InterpolationMode
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetInterpolationMode(new HandleRef(this, NativeGraphics), out InterpolationMode mode));
                return mode;
            }
            set
            {
                if (value < InterpolationMode.Invalid || value > InterpolationMode.HighQualityBicubic)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(InterpolationMode));

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

                Gdip.CheckStatus(Gdip.GdipSetInterpolationMode(new HandleRef(this, NativeGraphics), value));
            }
        }

        public bool IsClipEmpty
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipIsClipEmpty(new HandleRef(this, NativeGraphics), out bool isEmpty));
                return isEmpty;
            }
        }

        public bool IsVisibleClipEmpty
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipIsVisibleClipEmpty(new HandleRef(this, NativeGraphics), out bool isEmpty));
                return isEmpty;
            }
        }

        public float PageScale
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPageScale(new HandleRef(this, NativeGraphics), out float scale));
                return scale;
            }
            set
            {
                // Libgdiplus doesn't perform argument validation, so do this here for compatability.
                if (value <= 0 || value > 1000000032)
                    throw new ArgumentException(SR.GdiplusInvalidParameter);

                Gdip.CheckStatus(Gdip.GdipSetPageScale(new HandleRef(this, NativeGraphics), value));
            }
        }

        public GraphicsUnit PageUnit
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPageUnit(new HandleRef(this, NativeGraphics), out GraphicsUnit unit));
                return unit;
            }
            set
            {
                if (value < GraphicsUnit.World || value > GraphicsUnit.Millimeter)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(GraphicsUnit));

                // GDI+ doesn't allow GraphicsUnit.World as a valid value for PageUnit.
                // Libgdiplus doesn't perform argument validation, so do this here.
                if (value == GraphicsUnit.World)
                    throw new ArgumentException(SR.GdiplusInvalidParameter);

                Gdip.CheckStatus(Gdip.GdipSetPageUnit(new HandleRef(this, NativeGraphics), value));
            }
        }

        public PixelOffsetMode PixelOffsetMode
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPixelOffsetMode(new HandleRef(this, NativeGraphics), out PixelOffsetMode mode));
                return mode;
            }
            set
            {
                if (value < PixelOffsetMode.Invalid || value > PixelOffsetMode.Half)
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(PixelOffsetMode));

                // GDI+ doesn't allow PixelOffsetMode.Invalid as a valid value for PixelOffsetMode.
                // Libgdiplus doesn't perform argument validation, so do this here.
                if (value == PixelOffsetMode.Invalid)
                    throw new ArgumentException(SR.GdiplusInvalidParameter);

                Gdip.CheckStatus(Gdip.GdipSetPixelOffsetMode(new HandleRef(this, NativeGraphics), value));
            }
        }

        public Point RenderingOrigin
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetRenderingOrigin(new HandleRef(this, NativeGraphics), out int x, out int y));
                return new Point(x, y);
            }
            set
            {
                Gdip.CheckStatus(Gdip.GdipSetRenderingOrigin(new HandleRef(this, NativeGraphics), value.X, value.Y));
            }
        }

        public SmoothingMode SmoothingMode
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetSmoothingMode(new HandleRef(this, NativeGraphics), out SmoothingMode mode));
                return mode;
            }
            set
            {
                if (value < SmoothingMode.Invalid || value > SmoothingMode.AntiAlias)
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(SmoothingMode));

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

                Gdip.CheckStatus(Gdip.GdipSetSmoothingMode(new HandleRef(this, NativeGraphics), value));
            }
        }

        public int TextContrast
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetTextContrast(new HandleRef(this, NativeGraphics), out int textContrast));
                return textContrast;
            }
            set
            {
                Gdip.CheckStatus(Gdip.GdipSetTextContrast(new HandleRef(this, NativeGraphics), value));
            }
        }

        /// <summary>
        /// Gets or sets the rendering mode for text associated with this <see cref='Graphics'/>.
        /// </summary>
        public TextRenderingHint TextRenderingHint
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetTextRenderingHint(new HandleRef(this, NativeGraphics), out TextRenderingHint hint));
                return hint;
            }
            set
            {
                if (value < TextRenderingHint.SystemDefault || value > TextRenderingHint.ClearTypeGridFit)
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(TextRenderingHint));

                Gdip.CheckStatus(Gdip.GdipSetTextRenderingHint(new HandleRef(this, NativeGraphics), value));
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
                Gdip.CheckStatus(Gdip.GdipGetWorldTransform(
                    new HandleRef(this, NativeGraphics), new HandleRef(matrix, matrix.NativeMatrix)));

                return matrix;
            }
            set
            {
                Gdip.CheckStatus(Gdip.GdipSetWorldTransform(
                    new HandleRef(this, NativeGraphics), new HandleRef(value, value.NativeMatrix)));
            }
        }

        public IntPtr GetHdc()
        {
            IntPtr hdc = IntPtr.Zero;
            Gdip.CheckStatus(Gdip.GdipGetDC(new HandleRef(this, NativeGraphics), out hdc));

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
            Gdip.CheckStatus(Gdip.GdipFlush(new HandleRef(this, NativeGraphics), intention));
            FlushCore();
        }

        public void SetClip(Graphics g) => SetClip(g, CombineMode.Replace);

        public void SetClip(Graphics g, CombineMode combineMode)
        {
            if (g == null)
                throw new ArgumentNullException(nameof(g));

            Gdip.CheckStatus(Gdip.GdipSetClipGraphics(
                new HandleRef(this, NativeGraphics),
                new HandleRef(g, g.NativeGraphics),
                combineMode));
        }

        public void SetClip(Rectangle rect) => SetClip(rect, CombineMode.Replace);

        public void SetClip(Rectangle rect, CombineMode combineMode)
        {
            Gdip.CheckStatus(Gdip.GdipSetClipRectI(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                combineMode));
        }

        public void SetClip(RectangleF rect) => SetClip(rect, CombineMode.Replace);

        public void SetClip(RectangleF rect, CombineMode combineMode)
        {
            Gdip.CheckStatus(Gdip.GdipSetClipRect(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                combineMode));
        }

        public void SetClip(GraphicsPath path) => SetClip(path, CombineMode.Replace);

        public void SetClip(GraphicsPath path, CombineMode combineMode)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));

            Gdip.CheckStatus(Gdip.GdipSetClipPath(
                new HandleRef(this, NativeGraphics),
                new HandleRef(path, path._nativePath),
                combineMode));
        }

        public void SetClip(Region region, CombineMode combineMode)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipSetClipRegion(
                new HandleRef(this, NativeGraphics),
                new HandleRef(region, region.NativeRegion),
                combineMode));
        }

        public void IntersectClip(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipSetClipRectI(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                CombineMode.Intersect));
        }

        public void IntersectClip(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipSetClipRect(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                CombineMode.Intersect));
        }

        public void IntersectClip(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipSetClipRegion(
                new HandleRef(this, NativeGraphics),
                new HandleRef(region, region.NativeRegion),
                CombineMode.Intersect));
        }

        public void ExcludeClip(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipSetClipRectI(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                CombineMode.Exclude));
        }

        public void ExcludeClip(Region region)
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            Gdip.CheckStatus(Gdip.GdipSetClipRegion(
                new HandleRef(this, NativeGraphics),
                new HandleRef(region, region.NativeRegion),
                CombineMode.Exclude));
        }

        public void ResetClip()
        {
            Gdip.CheckStatus(Gdip.GdipResetClip(new HandleRef(this, NativeGraphics)));
        }

        public void TranslateClip(float dx, float dy)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateClip(new HandleRef(this, NativeGraphics), dx, dy));
        }

        public void TranslateClip(int dx, int dy)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateClip(new HandleRef(this, NativeGraphics), dx, dy));
        }

        public bool IsVisible(int x, int y) => IsVisible(new Point(x, y));

        public bool IsVisible(Point point)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisiblePointI(
                new HandleRef(this, NativeGraphics),
                point.X, point.Y,
                out bool isVisible));

            return isVisible;
        }

        public bool IsVisible(float x, float y) => IsVisible(new PointF(x, y));

        public bool IsVisible(PointF point)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisiblePoint(
                new HandleRef(this, NativeGraphics),
                point.X, point.Y,
                out bool isVisible));

            return isVisible;
        }

        public bool IsVisible(int x, int y, int width, int height)
        {
            return IsVisible(new Rectangle(x, y, width, height));
        }

        public bool IsVisible(Rectangle rect)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisibleRectI(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                out bool isVisible));

            return isVisible;
        }

        public bool IsVisible(float x, float y, float width, float height)
        {
            return IsVisible(new RectangleF(x, y, width, height));
        }

        public bool IsVisible(RectangleF rect)
        {
            Gdip.CheckStatus(Gdip.GdipIsVisibleRect(
                new HandleRef(this, NativeGraphics),
                rect.X, rect.Y, rect.Width, rect.Height,
                out bool isVisible));

            return isVisible;
        }

        /// <summary>
        /// Resets the world transform to identity.
        /// </summary>
        public void ResetTransform()
        {
            Gdip.CheckStatus(Gdip.GdipResetWorldTransform(new HandleRef(this, NativeGraphics)));
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
            if (matrix.NativeMatrix == IntPtr.Zero)
                return;

            Gdip.CheckStatus(Gdip.GdipMultiplyWorldTransform(
                new HandleRef(this, NativeGraphics), new HandleRef(matrix, matrix.NativeMatrix), order));
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateWorldTransform(new HandleRef(this, NativeGraphics), dx, dy, order));
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipScaleWorldTransform(new HandleRef(this, NativeGraphics), sx, sy, order));
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipRotateWorldTransform(new HandleRef(this, NativeGraphics), angle, order));
        }
    }
}

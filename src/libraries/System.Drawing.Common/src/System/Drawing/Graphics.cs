// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Numerics;
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

        public delegate bool DrawImageAbort(IntPtr callbackdata);

        /// <summary>
        /// Callback for EnumerateMetafile methods.
        /// This method can then call Metafile.PlayRecord to play the record that was just enumerated.
        /// </summary>
        /// <param name="recordType">if >= MinRecordType, it's an EMF+ record</param>
        /// <param name="flags">always 0 for EMF records</param>
        /// <param name="dataSize">size of the data, or 0 if no data</param>
        /// <param name="data">pointer to the data, or NULL if no data (UINT32 aligned)</param>
        /// <param name="callbackData">pointer to callbackData, if any</param>
        /// <returns>False to abort enumerating, true to continue.</returns>
        public delegate bool EnumerateMetafileProc(
            EmfPlusRecordType recordType,
            int flags,
            int dataSize,
            IntPtr data,
            PlayRecordCallback callbackData);

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

        /// <summary>
        /// Gets or sets the world transform elements for this <see cref="Graphics"/>.
        /// </summary>
        /// <remarks>
        /// This is a more performant alternative to <see cref="Transform"/> that does not need disposal.
        /// </remarks>
        public unsafe Matrix3x2 TransformElements
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipCreateMatrix(out IntPtr nativeMatrix));

                try
                {
                    Gdip.CheckStatus(Gdip.GdipGetWorldTransform(
                        new HandleRef(this, NativeGraphics), new HandleRef(null, nativeMatrix)));

                    Matrix3x2 matrix = default;
                    Gdip.CheckStatus(Gdip.GdipGetMatrixElements(new HandleRef(null, nativeMatrix), (float*)&matrix));
                    return matrix;
                }
                finally
                {
                    if (nativeMatrix != IntPtr.Zero)
                    {
                        Gdip.GdipDeleteMatrix(new HandleRef(null, nativeMatrix));
                    }
                }
            }
            set
            {
                IntPtr nativeMatrix = Matrix.CreateNativeHandle(value);

                try
                {
                    Gdip.CheckStatus(Gdip.GdipSetWorldTransform(
                        new HandleRef(this, NativeGraphics), new HandleRef(null, nativeMatrix)));
                }
                finally
                {
                    if (nativeMatrix != IntPtr.Zero)
                    {
                        Gdip.GdipDeleteMatrix(new HandleRef(null, nativeMatrix));
                    }
                }
            }
        }

        public IntPtr GetHdc()
        {
            IntPtr hdc;
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

        public void SetClip(Graphics g!!, CombineMode combineMode)
        {
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

        public void SetClip(GraphicsPath path!!, CombineMode combineMode)
        {
            Gdip.CheckStatus(Gdip.GdipSetClipPath(
                new HandleRef(this, NativeGraphics),
                new HandleRef(path, path._nativePath),
                combineMode));
        }

        public void SetClip(Region region!!, CombineMode combineMode)
        {
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

        public void IntersectClip(Region region!!)
        {
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

        public void ExcludeClip(Region region!!)
        {
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
        public void MultiplyTransform(Matrix matrix!!, MatrixOrder order)
        {
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

        /// <summary>
        /// Draws an arc from the specified ellipse.
        /// </summary>
        public void DrawArc(Pen pen!!, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            CheckErrorStatus(Gdip.GdipDrawArc(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Draws an arc from the specified ellipse.
        /// </summary>
        public void DrawArc(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
        {
            DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Draws an arc from the specified ellipse.
        /// </summary>
        public void DrawArc(Pen pen!!, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            CheckErrorStatus(Gdip.GdipDrawArcI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Draws an arc from the specified ellipse.
        /// </summary>
        public void DrawArc(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            DrawArc(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Draws a cubic bezier curve defined by four ordered pairs that represent points.
        /// </summary>
        public void DrawBezier(Pen pen!!, float x1, float y1, float x2, float y2, float x3, float y3, float x4, float y4)
        {
            CheckErrorStatus(Gdip.GdipDrawBezier(
                new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                x1, y1, x2, y2, x3, y3, x4, y4));
        }

        /// <summary>
        /// Draws a cubic bezier curve defined by four points.
        /// </summary>
        public void DrawBezier(Pen pen, PointF pt1, PointF pt2, PointF pt3, PointF pt4)
        {
            DrawBezier(pen, pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
        }

        /// <summary>
        /// Draws a cubic bezier curve defined by four points.
        /// </summary>
        public void DrawBezier(Pen pen, Point pt1, Point pt2, Point pt3, Point pt4)
        {
            DrawBezier(pen, pt1.X, pt1.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
        }

        /// <summary>
        /// Draws the outline of a rectangle specified by <paramref name="rect"/>.
        /// </summary>
        /// <param name="pen">A Pen that determines the color, width, and style of the rectangle.</param>
        /// <param name="rect">A Rectangle structure that represents the rectangle to draw.</param>
        public void DrawRectangle(Pen pen, RectangleF rect)
        {
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Draws the outline of a rectangle specified by <paramref name="rect"/>.
        /// </summary>
        public void DrawRectangle(Pen pen, Rectangle rect)
        {
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Draws the outline of the specified rectangle.
        /// </summary>
        public void DrawRectangle(Pen pen!!, float x, float y, float width, float height)
        {
            CheckErrorStatus(Gdip.GdipDrawRectangle(
                new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                x, y, width, height));
        }

        /// <summary>
        /// Draws the outline of the specified rectangle.
        /// </summary>
        public void DrawRectangle(Pen pen!!, int x, int y, int width, int height)
        {
            CheckErrorStatus(Gdip.GdipDrawRectangleI(
                new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                x, y, width, height));
        }

        /// <summary>
        /// Draws the outlines of a series of rectangles.
        /// </summary>
        public unsafe void DrawRectangles(Pen pen!!, RectangleF[] rects!!)
        {
            fixed (RectangleF* r = rects)
            {
                CheckErrorStatus(Gdip.GdipDrawRectangles(
                    new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                    r, rects.Length));
            }
        }

        /// <summary>
        /// Draws the outlines of a series of rectangles.
        /// </summary>
        public unsafe void DrawRectangles(Pen pen!!, Rectangle[] rects!!)
        {
            fixed (Rectangle* r = rects)
            {
                CheckErrorStatus(Gdip.GdipDrawRectanglesI(
                    new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                    r, rects.Length));
            }
        }

        /// <summary>
        /// Draws the outline of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void DrawEllipse(Pen pen, RectangleF rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Draws the outline of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void DrawEllipse(Pen pen!!, float x, float y, float width, float height)
        {
            CheckErrorStatus(Gdip.GdipDrawEllipse(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height));
        }

        /// <summary>
        /// Draws the outline of an ellipse specified by a bounding rectangle.
        /// </summary>
        public void DrawEllipse(Pen pen, Rectangle rect)
        {
            DrawEllipse(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Draws the outline of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void DrawEllipse(Pen pen!!, int x, int y, int width, int height)
        {
            CheckErrorStatus(Gdip.GdipDrawEllipseI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height));
        }

        /// <summary>
        /// Draws the outline of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void DrawPie(Pen pen, RectangleF rect, float startAngle, float sweepAngle)
        {
            DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Draws the outline of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void DrawPie(Pen pen!!, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            CheckErrorStatus(Gdip.GdipDrawPie(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Draws the outline of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void DrawPie(Pen pen, Rectangle rect, float startAngle, float sweepAngle)
        {
            DrawPie(pen, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Draws the outline of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void DrawPie(Pen pen!!, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            CheckErrorStatus(Gdip.GdipDrawPieI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Draws the outline of a polygon defined by an array of points.
        /// </summary>
        public unsafe void DrawPolygon(Pen pen!!, PointF[] points!!)
        {
            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawPolygon(
                    new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws the outline of a polygon defined by an array of points.
        /// </summary>
        public unsafe void DrawPolygon(Pen pen!!, Point[] points!!)
        {
            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawPolygonI(
                    new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws the lines and curves defined by a <see cref='GraphicsPath'/>.
        /// </summary>
        public void DrawPath(Pen pen!!, GraphicsPath path!!)
        {
            CheckErrorStatus(Gdip.GdipDrawPath(
                new HandleRef(this, NativeGraphics),
                new HandleRef(pen, pen.NativePen),
                new HandleRef(path, path._nativePath)));
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen!!, PointF[] points!!)
        {
            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen!!, PointF[] points!!, float tension)
        {
            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve2(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    tension));
            }
        }

        public void DrawCurve(Pen pen, PointF[] points, int offset, int numberOfSegments)
        {
            DrawCurve(pen, points, offset, numberOfSegments, 0.5f);
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen!!, PointF[] points!!, int offset, int numberOfSegments, float tension)
        {
            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve3(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    offset,
                    numberOfSegments,
                    tension));
            }
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen!!, Point[] points!!)
        {
            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurveI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen!!, Point[] points!!, float tension)
        {
            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve2I(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    tension));
            }
        }

        /// <summary>
        /// Draws a curve defined by an array of points.
        /// </summary>
        public unsafe void DrawCurve(Pen pen!!, Point[] points!!, int offset, int numberOfSegments, float tension)
        {
            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawCurve3I(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    offset,
                    numberOfSegments,
                    tension));
            }
        }

        /// <summary>
        /// Draws a closed curve defined by an array of points.
        /// </summary>
        public unsafe void DrawClosedCurve(Pen pen!!, PointF[] points!!)
        {
            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawClosedCurve(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a closed curve defined by an array of points.
        /// </summary>
        public unsafe void DrawClosedCurve(Pen pen!!, PointF[] points!!, float tension, FillMode fillmode)
        {
            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawClosedCurve2(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    tension));
            }
        }

        /// <summary>
        /// Draws a closed curve defined by an array of points.
        /// </summary>
        public unsafe void DrawClosedCurve(Pen pen!!, Point[] points!!)
        {
            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawClosedCurveI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Draws a closed curve defined by an array of points.
        /// </summary>
        public unsafe void DrawClosedCurve(Pen pen!!, Point[] points!!, float tension, FillMode fillmode)
        {
            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawClosedCurve2I(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length,
                    tension));
            }
        }

        /// <summary>
        /// Fills the entire drawing surface with the specified color.
        /// </summary>
        public void Clear(Color color)
        {
            Gdip.CheckStatus(Gdip.GdipGraphicsClear(new HandleRef(this, NativeGraphics), color.ToArgb()));
        }

        /// <summary>
        /// Fills the interior of a rectangle with a <see cref='Brush'/>.
        /// </summary>
        public void FillRectangle(Brush brush, RectangleF rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Fills the interior of a rectangle with a <see cref='Brush'/>.
        /// </summary>
        public void FillRectangle(Brush brush!!, float x, float y, float width, float height)
        {
            CheckErrorStatus(Gdip.GdipFillRectangle(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height));
        }

        /// <summary>
        /// Fills the interior of a rectangle with a <see cref='Brush'/>.
        /// </summary>
        public void FillRectangle(Brush brush, Rectangle rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Fills the interior of a rectangle with a <see cref='Brush'/>.
        /// </summary>
        public void FillRectangle(Brush brush!!, int x, int y, int width, int height)
        {
            CheckErrorStatus(Gdip.GdipFillRectangleI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height));
        }

        /// <summary>
        /// Fills the interiors of a series of rectangles with a <see cref='Brush'/>.
        /// </summary>
        public unsafe void FillRectangles(Brush brush!!, RectangleF[] rects!!)
        {
            fixed (RectangleF* r = rects)
            {
                CheckErrorStatus(Gdip.GdipFillRectangles(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    r, rects.Length));
            }
        }

        /// <summary>
        /// Fills the interiors of a series of rectangles with a <see cref='Brush'/>.
        /// </summary>
        public unsafe void FillRectangles(Brush brush!!, Rectangle[] rects!!)
        {
            fixed (Rectangle* r = rects)
            {
                CheckErrorStatus(Gdip.GdipFillRectanglesI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    r, rects.Length));
            }
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points.
        /// </summary>
        public void FillPolygon(Brush brush, PointF[] points)
        {
            FillPolygon(brush, points, FillMode.Alternate);
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points.
        /// </summary>
        public unsafe void FillPolygon(Brush brush!!, PointF[] points!!, FillMode fillMode)
        {
            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillPolygon(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length,
                    fillMode));
            }
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points.
        /// </summary>
        public void FillPolygon(Brush brush, Point[] points)
        {
            FillPolygon(brush, points, FillMode.Alternate);
        }

        /// <summary>
        /// Fills the interior of a polygon defined by an array of points.
        /// </summary>
        public unsafe void FillPolygon(Brush brush!!, Point[] points!!, FillMode fillMode)
        {
            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillPolygonI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length,
                    fillMode));
            }
        }

        /// <summary>
        /// Fills the interior of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void FillEllipse(Brush brush, RectangleF rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Fills the interior of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void FillEllipse(Brush brush!!, float x, float y, float width, float height)
        {
            CheckErrorStatus(Gdip.GdipFillEllipse(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height));
        }

        /// <summary>
        /// Fills the interior of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void FillEllipse(Brush brush, Rectangle rect)
        {
            FillEllipse(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// Fills the interior of an ellipse defined by a bounding rectangle.
        /// </summary>
        public void FillEllipse(Brush brush!!, int x, int y, int width, int height)
        {
            CheckErrorStatus(Gdip.GdipFillEllipseI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height));
        }

        /// <summary>
        /// Fills the interior of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void FillPie(Brush brush, Rectangle rect, float startAngle, float sweepAngle)
        {
            FillPie(brush, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Fills the interior of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        /// <param name="brush">A Brush that determines the characteristics of the fill.</param>
        /// <param name="rect">A Rectangle structure that represents the bounding rectangle that defines the ellipse from which the pie section comes.</param>
        /// <param name="startAngle">Angle in degrees measured clockwise from the x-axis to the first side of the pie section.</param>
        /// <param name="sweepAngle">Angle in degrees measured clockwise from the <paramref name="startAngle"/> parameter to the second side of the pie section.</param>
        public void FillPie(Brush brush, RectangleF rect, float startAngle, float sweepAngle)
        {
            FillPie(brush, rect.X, rect.Y, rect.Width, rect.Height, startAngle, sweepAngle);
        }

        /// <summary>
        /// Fills the interior of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void FillPie(Brush brush!!, float x, float y, float width, float height, float startAngle, float sweepAngle)
        {
            CheckErrorStatus(Gdip.GdipFillPie(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Fills the interior of a pie section defined by an ellipse and two radial lines.
        /// </summary>
        public void FillPie(Brush brush!!, int x, int y, int width, int height, int startAngle, int sweepAngle)
        {
            CheckErrorStatus(Gdip.GdipFillPieI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(brush, brush.NativeBrush),
                x, y, width, height,
                startAngle,
                sweepAngle));
        }

        /// <summary>
        /// Fills the interior a closed curve defined by an array of points.
        /// </summary>
        public unsafe void FillClosedCurve(Brush brush!!, PointF[] points!!)
        {
            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillClosedCurve(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length));
            }
        }

        /// <summary>
        /// Fills the interior of a closed curve defined by an array of points.
        /// </summary>
        public void FillClosedCurve(Brush brush, PointF[] points, FillMode fillmode)
        {
            FillClosedCurve(brush, points, fillmode, 0.5f);
        }

        public unsafe void FillClosedCurve(Brush brush!!, PointF[] points!!, FillMode fillmode, float tension)
        {
            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillClosedCurve2(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length,
                    tension,
                    fillmode));
            }
        }

        /// <summary>
        /// Fills the interior a closed curve defined by an array of points.
        /// </summary>
        public unsafe void FillClosedCurve(Brush brush!!, Point[] points!!)
        {
            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillClosedCurveI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length));
            }
        }

        public void FillClosedCurve(Brush brush, Point[] points, FillMode fillmode)
        {
            FillClosedCurve(brush, points, fillmode, 0.5f);
        }

        public unsafe void FillClosedCurve(Brush brush!!, Point[] points!!, FillMode fillmode, float tension)
        {
            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipFillClosedCurve2I(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(brush, brush.NativeBrush),
                    p, points.Length,
                    tension,
                    fillmode));
            }
        }

        /// <summary>
        /// Draws a string with the specified font.
        /// </summary>
        public void DrawString(string? s, Font font, Brush brush, float x, float y)
        {
            DrawString(s, font, brush, new RectangleF(x, y, 0, 0), null);
        }

        public void DrawString(string? s, Font font, Brush brush, PointF point)
        {
            DrawString(s, font, brush, new RectangleF(point.X, point.Y, 0, 0), null);
        }

        public void DrawString(string? s, Font font, Brush brush, float x, float y, StringFormat? format)
        {
            DrawString(s, font, brush, new RectangleF(x, y, 0, 0), format);
        }

        public void DrawString(string? s, Font font, Brush brush, PointF point, StringFormat? format)
        {
            DrawString(s, font, brush, new RectangleF(point.X, point.Y, 0, 0), format);
        }

        public void DrawString(string? s, Font font, Brush brush, RectangleF layoutRectangle)
        {
            DrawString(s, font, brush, layoutRectangle, null);
        }

        public void DrawString(string? s, Font font, Brush brush!!, RectangleF layoutRectangle, StringFormat? format)
        {
            if (string.IsNullOrEmpty(s))
                return;
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            CheckErrorStatus(Gdip.GdipDrawString(
                new HandleRef(this, NativeGraphics),
                s,
                s.Length,
                new HandleRef(font, font.NativeFont),
                ref layoutRectangle,
                new HandleRef(format, format?.nativeFormat ?? IntPtr.Zero),
                new HandleRef(brush, brush.NativeBrush)));
        }

        public SizeF MeasureString(
            string? text,
            Font font,
            SizeF layoutArea,
            StringFormat? stringFormat,
            out int charactersFitted,
            out int linesFilled)
        {
            if (string.IsNullOrEmpty(text))
            {
                charactersFitted = 0;
                linesFilled = 0;
                return SizeF.Empty;
            }

            if (font == null)
                throw new ArgumentNullException(nameof(font));

            RectangleF layout = new RectangleF(0, 0, layoutArea.Width, layoutArea.Height);
            RectangleF boundingBox = default;

            Gdip.CheckStatus(Gdip.GdipMeasureString(
                new HandleRef(this, NativeGraphics),
                text,
                text.Length,
                new HandleRef(font, font.NativeFont),
                ref layout,
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                ref boundingBox,
                out charactersFitted,
                out linesFilled));

            return boundingBox.Size;
        }

        public SizeF MeasureString(string? text, Font font, PointF origin, StringFormat? stringFormat)
        {
            if (string.IsNullOrEmpty(text))
                return SizeF.Empty;
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            RectangleF layout = new RectangleF(origin.X, origin.Y, 0, 0);
            RectangleF boundingBox = default;

            Gdip.CheckStatus(Gdip.GdipMeasureString(
                new HandleRef(this, NativeGraphics),
                text,
                text.Length,
                new HandleRef(font, font.NativeFont),
                ref layout,
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                ref boundingBox,
                out _,
                out _));

            return boundingBox.Size;
        }

        public SizeF MeasureString(string? text, Font font, SizeF layoutArea) => MeasureString(text, font, layoutArea, null);

        public SizeF MeasureString(string? text, Font font, SizeF layoutArea, StringFormat? stringFormat)
        {
            if (string.IsNullOrEmpty(text))
                return SizeF.Empty;
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            RectangleF layout = new RectangleF(0, 0, layoutArea.Width, layoutArea.Height);
            RectangleF boundingBox = default;

            Gdip.CheckStatus(Gdip.GdipMeasureString(
                new HandleRef(this, NativeGraphics),
                text,
                text.Length,
                new HandleRef(font, font.NativeFont),
                ref layout,
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                ref boundingBox,
                out _,
                out _));

            return boundingBox.Size;
        }

        public SizeF MeasureString(string? text, Font font)
        {
            return MeasureString(text, font, new SizeF(0, 0));
        }

        public SizeF MeasureString(string? text, Font font, int width)
        {
            return MeasureString(text, font, new SizeF(width, 999999));
        }

        public SizeF MeasureString(string? text, Font font, int width, StringFormat? format)
        {
            return MeasureString(text, font, new SizeF(width, 999999), format);
        }

        public Region[] MeasureCharacterRanges(string? text, Font font, RectangleF layoutRect, StringFormat? stringFormat)
        {
            if (string.IsNullOrEmpty(text))
                return Array.Empty<Region>();
            if (font == null)
                throw new ArgumentNullException(nameof(font));

            Gdip.CheckStatus(Gdip.GdipGetStringFormatMeasurableCharacterRangeCount(
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                out int count));

            IntPtr[] gpRegions = new IntPtr[count];
            Region[] regions = new Region[count];

            for (int f = 0; f < count; f++)
            {
                regions[f] = new Region();
                gpRegions[f] = regions[f].NativeRegion;
            }

            Gdip.CheckStatus(Gdip.GdipMeasureCharacterRanges(
                new HandleRef(this, NativeGraphics),
                text,
                text.Length,
                new HandleRef(font, font.NativeFont),
                ref layoutRect,
                new HandleRef(stringFormat, stringFormat?.nativeFormat ?? IntPtr.Zero),
                count,
                gpRegions));

            return regions;
        }

        /// <summary>
        /// Draws the specified image at the specified location.
        /// </summary>
        public void DrawImage(Image image, PointF point)
        {
            DrawImage(image, point.X, point.Y);
        }

        public void DrawImage(Image image!!, float x, float y)
        {
            int status = Gdip.GdipDrawImage(
                new HandleRef(this, NativeGraphics), new HandleRef(image, image.nativeImage),
                x, y);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(Image image, RectangleF rect)
        {
            DrawImage(image, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawImage(Image image!!, float x, float y, float width, float height)
        {
            int status = Gdip.GdipDrawImageRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y,
                width, height);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(Image image, Point point)
        {
            DrawImage(image, point.X, point.Y);
        }

        public void DrawImage(Image image!!, int x, int y)
        {
            int status = Gdip.GdipDrawImageI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(Image image, Rectangle rect)
        {
            DrawImage(image, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void DrawImage(Image image!!, int x, int y, int width, int height)
        {
            int status = Gdip.GdipDrawImageRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y,
                width, height);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImageUnscaled(Image image, Point point)
        {
            DrawImage(image, point.X, point.Y);
        }

        public void DrawImageUnscaled(Image image, int x, int y)
        {
            DrawImage(image, x, y);
        }

        public void DrawImageUnscaled(Image image, Rectangle rect)
        {
            DrawImage(image, rect.X, rect.Y);
        }

        public void DrawImageUnscaled(Image image, int x, int y, int width, int height)
        {
            DrawImage(image, x, y);
        }

        public void DrawImageUnscaledAndClipped(Image image!!, Rectangle rect)
        {
            int width = Math.Min(rect.Width, image.Width);
            int height = Math.Min(rect.Height, image.Height);

            // We could put centering logic here too for the case when the image
            // is smaller than the rect.
            DrawImage(image, rect, 0, 0, width, height, GraphicsUnit.Pixel);
        }

        // Affine or perspective blt
        //  destPoints.Length = 3: rect => parallelogram
        // destPoints[0] <=> top-left corner of the source rectangle
        //      destPoints[1] <=> top-right corner
        //       destPoints[2] <=> bottom-left corner
        //  destPoints.Length = 4: rect => quad
        // destPoints[3] <=> bottom-right corner
        //
        //  @notes Perspective blt only works for bitmap images.

        public unsafe void DrawImage(Image image!!, PointF[] destPoints!!)
        {
            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (PointF* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePoints(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, count);

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        public unsafe void DrawImage(Image image!!, Point[] destPoints!!)
        {
            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (Point* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePointsI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, count);

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        public void DrawImage(Image image!!, float x, float y, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            int status = Gdip.GdipDrawImagePointRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                (int)srcUnit);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(Image image!!, int x, int y, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            int status = Gdip.GdipDrawImagePointRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                x, y,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                (int)srcUnit);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(Image image!!, RectangleF destRect, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            int status = Gdip.GdipDrawImageRectRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                srcUnit,
                NativeMethods.NullHandleRef,
                null,
                NativeMethods.NullHandleRef);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(Image image!!, Rectangle destRect, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            int status = Gdip.GdipDrawImageRectRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                srcUnit,
                NativeMethods.NullHandleRef,
                null,
                NativeMethods.NullHandleRef);

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public unsafe void DrawImage(Image image!!, PointF[] destPoints!!, RectangleF srcRect, GraphicsUnit srcUnit)
        {
            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (PointF* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePointsRect(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, destPoints.Length,
                    srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                    srcUnit,
                    NativeMethods.NullHandleRef,
                    null,
                    NativeMethods.NullHandleRef);

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        public void DrawImage(Image image, PointF[] destPoints, RectangleF srcRect, GraphicsUnit srcUnit, ImageAttributes? imageAttr)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, imageAttr, null, 0);
        }

        public void DrawImage(
            Image image,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr,
            DrawImageAbort? callback)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, imageAttr, callback, 0);
        }

        public unsafe void DrawImage(
            Image image!!,
            PointF[] destPoints!!,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr,
            DrawImageAbort? callback,
            int callbackData)
        {
            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (PointF* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePointsRect(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, destPoints.Length,
                    srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                    srcUnit,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero),
                    callback,
                    new HandleRef(null, (IntPtr)callbackData));

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        public void DrawImage(Image image, Point[] destPoints, Rectangle srcRect, GraphicsUnit srcUnit)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, null, null, 0);
        }

        public void DrawImage(
            Image image,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, imageAttr, null, 0);
        }

        public void DrawImage(
            Image image,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr,
            DrawImageAbort? callback)
        {
            DrawImage(image, destPoints, srcRect, srcUnit, imageAttr, callback, 0);
        }

        public unsafe void DrawImage(
            Image image!!,
            Point[] destPoints!!,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr,
            DrawImageAbort? callback,
            int callbackData)
        {
            int count = destPoints.Length;
            if (count != 3 && count != 4)
                throw new ArgumentException(SR.GdiplusDestPointsInvalidLength);

            fixed (Point* p = destPoints)
            {
                int status = Gdip.GdipDrawImagePointsRectI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(image, image.nativeImage),
                    p, destPoints.Length,
                    srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height,
                    srcUnit,
                    new HandleRef(imageAttr, imageAttr?.nativeImageAttributes ?? IntPtr.Zero),
                    callback,
                    new HandleRef(null, (IntPtr)callbackData));

                IgnoreMetafileErrors(image, ref status);
                CheckErrorStatus(status);
            }
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            float srcX,
            float srcY,
            float srcWidth,
            float srcHeight,
            GraphicsUnit srcUnit)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, null);
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            float srcX,
            float srcY,
            float srcWidth,
            float srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttrs)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttrs, null);
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            float srcX,
            float srcY,
            float srcWidth,
            float srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttrs,
            DrawImageAbort? callback)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttrs, callback, IntPtr.Zero);
        }

        public void DrawImage(
            Image image!!,
            Rectangle destRect,
            float srcX,
            float srcY,
            float srcWidth,
            float srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttrs,
            DrawImageAbort? callback,
            IntPtr callbackData)
        {
            int status = Gdip.GdipDrawImageRectRect(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcX, srcY, srcWidth, srcHeight,
                srcUnit,
                new HandleRef(imageAttrs, imageAttrs?.nativeImageAttributes ?? IntPtr.Zero),
                callback,
                new HandleRef(null, callbackData));

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            GraphicsUnit srcUnit)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, null);
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttr, null);
        }

        public void DrawImage(
            Image image,
            Rectangle destRect,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttr,
            DrawImageAbort? callback)
        {
            DrawImage(image, destRect, srcX, srcY, srcWidth, srcHeight, srcUnit, imageAttr, callback, IntPtr.Zero);
        }

        public void DrawImage(
            Image image!!,
            Rectangle destRect,
            int srcX,
            int srcY,
            int srcWidth,
            int srcHeight,
            GraphicsUnit srcUnit,
            ImageAttributes? imageAttrs,
            DrawImageAbort? callback,
            IntPtr callbackData)
        {
            int status = Gdip.GdipDrawImageRectRectI(
                new HandleRef(this, NativeGraphics),
                new HandleRef(image, image.nativeImage),
                destRect.X, destRect.Y, destRect.Width, destRect.Height,
                srcX, srcY, srcWidth, srcHeight,
                srcUnit,
                new HandleRef(imageAttrs, imageAttrs?.nativeImageAttributes ?? IntPtr.Zero),
                callback,
                new HandleRef(null, callbackData));

            IgnoreMetafileErrors(image, ref status);
            CheckErrorStatus(status);
        }

        /// <summary>
        /// Draws a line connecting the two specified points.
        /// </summary>
        public void DrawLine(Pen pen, PointF pt1, PointF pt2)
        {
            DrawLine(pen, pt1.X, pt1.Y, pt2.X, pt2.Y);
        }

        /// <summary>
        /// Draws a series of line segments that connect an array of points.
        /// </summary>
        public unsafe void DrawLines(Pen pen!!, PointF[] points!!)
        {
            fixed (PointF* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawLines(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p, points.Length));
            }
        }


        /// <summary>
        /// Draws a line connecting the two specified points.
        /// </summary>
        public void DrawLine(Pen pen!!, int x1, int y1, int x2, int y2)
        {
            CheckErrorStatus(Gdip.GdipDrawLineI(new HandleRef(this, NativeGraphics), new HandleRef(pen, pen.NativePen), x1, y1, x2, y2));
        }

        /// <summary>
        /// Draws a line connecting the two specified points.
        /// </summary>
        public void DrawLine(Pen pen, Point pt1, Point pt2)
        {
            DrawLine(pen, pt1.X, pt1.Y, pt2.X, pt2.Y);
        }

        /// <summary>
        /// Draws a series of line segments that connect an array of points.
        /// </summary>
        public unsafe void DrawLines(Pen pen!!, Point[] points!!)
        {
            fixed (Point* p = points)
            {
                CheckErrorStatus(Gdip.GdipDrawLinesI(
                    new HandleRef(this, NativeGraphics),
                    new HandleRef(pen, pen.NativePen),
                    p,
                    points.Length));
            }
        }

        /// <summary>
        /// CopyPixels will perform a gdi "bitblt" operation to the source from the destination with the given size.
        /// </summary>
        public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize)
        {
            CopyFromScreen(upperLeftSource.X, upperLeftSource.Y, upperLeftDestination.X, upperLeftDestination.Y, blockRegionSize);
        }

        /// <summary>
        /// CopyPixels will perform a gdi "bitblt" operation to the source from the destination with the given size.
        /// </summary>
        public void CopyFromScreen(int sourceX, int sourceY, int destinationX, int destinationY, Size blockRegionSize)
        {
            CopyFromScreen(sourceX, sourceY, destinationX, destinationY, blockRegionSize, CopyPixelOperation.SourceCopy);
        }

        /// <summary>
        /// CopyPixels will perform a gdi "bitblt" operation to the source from the destination with the given size
        /// and specified raster operation.
        /// </summary>
        public void CopyFromScreen(Point upperLeftSource, Point upperLeftDestination, Size blockRegionSize, CopyPixelOperation copyPixelOperation)
        {
            CopyFromScreen(upperLeftSource.X, upperLeftSource.Y, upperLeftDestination.X, upperLeftDestination.Y, blockRegionSize, copyPixelOperation);
        }

        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoint, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(Metafile metafile, PointF destPoint, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoint, callback, callbackData, null);
        }

        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoint, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(Metafile metafile, Point destPoint, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoint, callback, callbackData, null);
        }

        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destRect, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(Metafile metafile, RectangleF destRect, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destRect, callback, callbackData, null);
        }

        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destRect, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(Metafile metafile, Rectangle destRect, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destRect, callback, callbackData, null);
        }

        public void EnumerateMetafile(Metafile metafile, PointF[] destPoints, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoints, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoints, callback, IntPtr.Zero, null);
        }

        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoints, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(Metafile metafile, Point[] destPoints, EnumerateMetafileProc callback, IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoints, callback, callbackData, null);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF destPoint,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoint, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF destPoint,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoint, srcRect, srcUnit, callback, callbackData, null);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Point destPoint,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoint, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Point destPoint,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoint, srcRect, srcUnit, callback, callbackData, null);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            RectangleF destRect,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destRect, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            RectangleF destRect,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destRect, srcRect, srcUnit, callback, callbackData, null);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Rectangle destRect,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destRect, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Rectangle destRect,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destRect, srcRect, srcUnit, callback, callbackData, null);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoints, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            PointF[] destPoints,
            RectangleF srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoints, srcRect, srcUnit, callback, callbackData, null);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback)
        {
            EnumerateMetafile(metafile, destPoints, srcRect, srcUnit, callback, IntPtr.Zero);
        }

        public void EnumerateMetafile(
            Metafile metafile,
            Point[] destPoints,
            Rectangle srcRect,
            GraphicsUnit srcUnit,
            EnumerateMetafileProc callback,
            IntPtr callbackData)
        {
            EnumerateMetafile(metafile, destPoints, srcRect, srcUnit, callback, callbackData, null);
        }

        public unsafe void TransformPoints(CoordinateSpace destSpace, CoordinateSpace srcSpace, PointF[] pts!!)
        {
            fixed (PointF* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipTransformPoints(
                    new HandleRef(this, NativeGraphics),
                    (int)destSpace,
                    (int)srcSpace,
                    p,
                    pts.Length));
            }
        }

        public unsafe void TransformPoints(CoordinateSpace destSpace, CoordinateSpace srcSpace, Point[] pts!!)
        {
            fixed (Point* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipTransformPointsI(
                    new HandleRef(this, NativeGraphics),
                    (int)destSpace,
                    (int)srcSpace,
                    p,
                    pts.Length));
            }
        }

        /// <summary>
        /// GDI+ will return a 'generic error' when we attempt to draw an Emf
        /// image with width/height == 1. Here, we will hack around this by
        /// resetting the errorstatus. Note that we don't do simple arg checking
        /// for height || width == 1 here because transforms can be applied to
        /// the Graphics object making it difficult to identify this scenario.
        /// </summary>
        private static void IgnoreMetafileErrors(Image image, ref int errorStatus)
        {
            if (errorStatus != Gdip.Ok && image.RawFormat.Equals(ImageFormat.Emf))
                errorStatus = Gdip.Ok;
        }

        /// <summary>
        /// Creates a Region class only if the native region is not infinite.
        /// </summary>
        internal Region? GetRegionIfNotInfinite()
        {
            Gdip.CheckStatus(Gdip.GdipCreateRegion(out IntPtr regionHandle));
            try
            {
                Gdip.GdipGetClip(new HandleRef(this, NativeGraphics), new HandleRef(null, regionHandle));
                Gdip.CheckStatus(Gdip.GdipIsInfiniteRegion(
                    new HandleRef(null, regionHandle),
                    new HandleRef(this, NativeGraphics),
                    out int isInfinite));

                if (isInfinite != 0)
                {
                    // Infinite
                    return null;
                }

                Region region = new Region(regionHandle);
                regionHandle = IntPtr.Zero;
                return region;
            }
            finally
            {
                if (regionHandle != IntPtr.Zero)
                {
                    Gdip.GdipDeleteRegion(new HandleRef(null, regionHandle));
                }
            }
        }
    }
}

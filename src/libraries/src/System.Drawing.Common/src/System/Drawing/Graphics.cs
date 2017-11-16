// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Drawing.Drawing2D;
using System.Drawing.Internal;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    /// <summary>
    /// Encapsulates a GDI+ drawing surface.
    /// </summary>
    public sealed partial class Graphics : MarshalByRefObject, IDisposable, IDeviceContext
    {
        public void SetClip(Graphics g) => SetClip(g, CombineMode.Replace);

        public void SetClip(Graphics g, CombineMode combineMode)
        {
            if (g == null)
            {
                throw new ArgumentNullException(nameof(g));
            }

            int status = SafeNativeMethods.Gdip.GdipSetClipGraphics(new HandleRef(this, NativeGraphics), new HandleRef(g, g.NativeGraphics), combineMode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void SetClip(Rectangle rect) => SetClip(rect, CombineMode.Replace);

        public void SetClip(Rectangle rect, CombineMode combineMode)
        {
            int status = SafeNativeMethods.Gdip.GdipSetClipRectI(new HandleRef(this, NativeGraphics), rect.X, rect.Y,
                                                  rect.Width, rect.Height, combineMode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void SetClip(RectangleF rect) => SetClip(rect, CombineMode.Replace);

        public void SetClip(RectangleF rect, CombineMode combineMode)
        {
            int status = SafeNativeMethods.Gdip.GdipSetClipRect(new HandleRef(this, NativeGraphics), rect.X, rect.Y,
                                                 rect.Width, rect.Height, combineMode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void SetClip(GraphicsPath path) => SetClip(path, CombineMode.Replace);

        public void SetClip(GraphicsPath path, CombineMode combineMode)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            int status = SafeNativeMethods.Gdip.GdipSetClipPath(new HandleRef(this, NativeGraphics), new HandleRef(path, path.nativePath), combineMode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void SetClip(Region region, CombineMode combineMode)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            int status = SafeNativeMethods.Gdip.GdipSetClipRegion(new HandleRef(this, NativeGraphics), new HandleRef(region, region._nativeRegion), combineMode);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void IntersectClip(Rectangle rect)
        {
            int status = SafeNativeMethods.Gdip.GdipSetClipRectI(new HandleRef(this, NativeGraphics), rect.X, rect.Y,
                                                  rect.Width, rect.Height, CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void IntersectClip(RectangleF rect)
        {
            int status = SafeNativeMethods.Gdip.GdipSetClipRect(new HandleRef(this, NativeGraphics), rect.X, rect.Y,
                                                 rect.Width, rect.Height, CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void IntersectClip(Region region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            int status = SafeNativeMethods.Gdip.GdipSetClipRegion(new HandleRef(this, NativeGraphics), new HandleRef(region, region._nativeRegion),
                                                   CombineMode.Intersect);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ExcludeClip(Rectangle rect)
        {
            int status = SafeNativeMethods.Gdip.GdipSetClipRectI(new HandleRef(this, NativeGraphics), rect.X, rect.Y,
                                                  rect.Width, rect.Height, CombineMode.Exclude);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ExcludeClip(Region region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            int status = SafeNativeMethods.Gdip.GdipSetClipRegion(new HandleRef(this, NativeGraphics),
                                                   new HandleRef(region, region._nativeRegion),
                                                   CombineMode.Exclude);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ResetClip()
        {
            int status = SafeNativeMethods.Gdip.GdipResetClip(new HandleRef(this, NativeGraphics));
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TranslateClip(float dx, float dy)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateClip(new HandleRef(this, NativeGraphics), dx, dy);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TranslateClip(int dx, int dy)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateClip(new HandleRef(this, NativeGraphics), dx, dy);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

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


        public bool IsVisible(int x, int y) => IsVisible(new Point(x, y));

        public bool IsVisible(Point point)
        {
            int isVisible;
            int status = SafeNativeMethods.Gdip.GdipIsVisiblePointI(new HandleRef(this, NativeGraphics), point.X, point.Y, out isVisible);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isVisible != 0;
        }

        public bool IsVisible(float x, float y) => IsVisible(new PointF(x, y));

        public bool IsVisible(PointF point)
        {
            int isVisible;
            int status = SafeNativeMethods.Gdip.GdipIsVisiblePoint(new HandleRef(this, NativeGraphics), point.X, point.Y, out isVisible);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isVisible != 0;
        }

        public bool IsVisible(int x, int y, int width, int height)
        {
            return IsVisible(new Rectangle(x, y, width, height));
        }

        public bool IsVisible(Rectangle rect)
        {
            int isVisible;
            int status = SafeNativeMethods.Gdip.GdipIsVisibleRectI(new HandleRef(this, NativeGraphics), rect.X, rect.Y,
                                                    rect.Width, rect.Height, out isVisible);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isVisible != 0;
        }

        public bool IsVisible(float x, float y, float width, float height)
        {
            return IsVisible(new RectangleF(x, y, width, height));
        }

        public bool IsVisible(RectangleF rect)
        {
            int isVisible;
            int status = SafeNativeMethods.Gdip.GdipIsVisibleRect(new HandleRef(this, NativeGraphics), rect.X, rect.Y,
                                                   rect.Width, rect.Height, out isVisible);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return isVisible != 0;
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

        /// <summary>
        /// Resets the world transform to identity.
        /// </summary>
        public void ResetTransform()
        {
            int status = SafeNativeMethods.Gdip.GdipResetWorldTransform(new HandleRef(this, NativeGraphics));
            SafeNativeMethods.Gdip.CheckStatus(status);
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
            {
                throw new ArgumentNullException(nameof(matrix));
            }

            int status = SafeNativeMethods.Gdip.GdipMultiplyWorldTransform(new HandleRef(this, NativeGraphics),
                                                            new HandleRef(matrix, matrix.nativeMatrix),
                                                            order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateWorldTransform(new HandleRef(this, NativeGraphics), dx, dy, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipScaleWorldTransform(new HandleRef(this, NativeGraphics), sx, sy, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipRotateWorldTransform(new HandleRef(this, NativeGraphics), angle, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }
    }
}

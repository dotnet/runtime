// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System.ComponentModel;
    using System.Drawing.Internal;

    /**
     * Represent a PathGradient brush object
     */
    /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush"]/*' />
    /// <devdoc>
    ///    Encapsulates a <see cref='System.Drawing.Brush'/> that fills the interior of a
    /// <see cref='System.Drawing.Drawing2D.GraphicsPath'/> with a gradient.
    /// </devdoc>
    public sealed class PathGradientBrush : Brush
    {
        /**
         * Create a new rectangle gradient brush object
         */
        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.PathGradientBrush"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.PathGradientBrush'/> class with the specified points.
        ///    </para>
        /// </devdoc>
        public PathGradientBrush(PointF[] points)
            : this(points, System.Drawing.Drawing2D.WrapMode.Clamp)
        {
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.PathGradientBrush1"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Drawing2D.PathGradientBrush'/> class with the specified points and
        ///    wrap mode.
        /// </devdoc>
        public PathGradientBrush(PointF[] points, WrapMode wrapMode)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            //validate the WrapMode enum
            //valid values are 0x0 to 0x4
            if (!ClientUtils.IsEnumValid(wrapMode, unchecked((int)wrapMode), (int)WrapMode.Tile, (int)WrapMode.Clamp))
            {
                throw new InvalidEnumArgumentException("wrapMode", unchecked((int)wrapMode), typeof(WrapMode));
            }

            IntPtr brush = IntPtr.Zero;
            IntPtr pointsBuf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipCreatePathGradient(new HandleRef(null, pointsBuf),
                                                            points.Length,
                                                            unchecked((int)wrapMode),
                                                            out brush);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                SetNativeBrushInternal(brush);
            }
            finally
            {
                // Inside SafeNativeMethods.Gdip.ConvertPointToMemory, Marshal.AllocHGlobal
                // is used to allocate unmanaged memory. Therefore, we need to free it
                // manually with Marshal.FreeHGlobal
                if (pointsBuf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pointsBuf);
                }
            }
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.PathGradientBrush2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.PathGradientBrush'/> class with the
        ///       specified points.
        ///    </para>
        /// </devdoc>
        public PathGradientBrush(Point[] points)
            : this(points, System.Drawing.Drawing2D.WrapMode.Clamp)
        {
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.PathGradientBrush3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.PathGradientBrush'/> class with the
        ///       specified points and wrap mode.
        ///    </para>
        /// </devdoc>
        public PathGradientBrush(Point[] points, WrapMode wrapMode)
        {
            if (points == null)
                throw new ArgumentNullException("points");

            //validate the WrapMode enum
            if (!ClientUtils.IsEnumValid(wrapMode, unchecked((int)wrapMode), (int)WrapMode.Tile, (int)WrapMode.Clamp))
            {
                throw new InvalidEnumArgumentException("wrapMode", unchecked((int)wrapMode), typeof(WrapMode));
            }

            IntPtr brush = IntPtr.Zero;
            IntPtr pointsBuf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                int status = SafeNativeMethods.Gdip.GdipCreatePathGradientI(new HandleRef(null, pointsBuf),
                                                             points.Length,
                                                             unchecked((int)wrapMode),
                                                             out brush);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                SetNativeBrushInternal(brush);
            }
            finally
            {
                // Inside SafeNativeMethods.Gdip.ConvertPointToMemory, Marshal.AllocHGlobal
                // is used to allocate unmanaged memory. Therefore, we need to free it
                // manually with Marshal.FreeHGlobal
                if (pointsBuf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(pointsBuf);
                }
            }
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.PathGradientBrush4"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.PathGradientBrush'/>
        ///       class with the specified path.
        ///    </para>
        /// </devdoc>
        public PathGradientBrush(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            IntPtr brush = IntPtr.Zero;
            int status = SafeNativeMethods.Gdip.GdipCreatePathGradientFromPath(new HandleRef(path, path.nativePath),
                                                                out brush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeBrushInternal(brush);
        }

        /// <devdoc>
        ///     Constructor to initialized this object to be owned by GDI+.
        /// </devdoc>
        internal PathGradientBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.Clone"]/*' />
        /// <devdoc>
        ///    Creates an exact copy of this <see cref='System.Drawing.Drawing2D.PathGradientBrush'/>.
        /// </devdoc>
        public override object Clone()
        {
            IntPtr cloneBrush = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out cloneBrush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new PathGradientBrush(cloneBrush);
        }

        /**
         * Set/get center color attributes
         */
        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.CenterColor"]/*' />
        /// <devdoc>
        ///    Gets or sets the color at the center of the
        ///    path gradient.
        /// </devdoc>
        public Color CenterColor
        {
            get
            {
                int argb;

                int status = SafeNativeMethods.Gdip.GdipGetPathGradientCenterColor(new HandleRef(this, NativeBrush), out argb);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return Color.FromArgb(argb);
            }

            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetPathGradientCenterColor(new HandleRef(this, NativeBrush), value.ToArgb());

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /**
         * Get/set colors
         * !! NOTE: We do not have methods for GetSurroundColor or SetSurroundColor,
         *    May need to add usage of Collection class
         */

        private void _SetSurroundColors(Color[] colors)
        {
            int count;

            int status = SafeNativeMethods.Gdip.GdipGetPathGradientSurroundColorCount(new HandleRef(this, NativeBrush),
                                                                       out count);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            if ((colors.Length > count) || (count <= 0))
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);

            count = colors.Length;
            int[] argbs = new int[count];
            for (int i = 0; i < colors.Length; i++)
                argbs[i] = colors[i].ToArgb();

            status = SafeNativeMethods.Gdip.GdipSetPathGradientSurroundColorsWithCount(new HandleRef(this, NativeBrush),
                                                                        argbs,
                                                                        ref count);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private Color[] _GetSurroundColors()
        {
            int count;

            int status = SafeNativeMethods.Gdip.GdipGetPathGradientSurroundColorCount(new HandleRef(this, NativeBrush),
                                                                       out count);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            int[] argbs = new int[count];

            status = SafeNativeMethods.Gdip.GdipGetPathGradientSurroundColorsWithCount(new HandleRef(this, NativeBrush),
                                                                        argbs,
                                                                        ref count);


            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            Color[] colors = new Color[count];
            for (int i = 0; i < count; i++)
                colors[i] = Color.FromArgb(argbs[i]);

            return colors;
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.SurroundColors"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets an array of colors that
        ///       correspond to the points in the path this <see cref='System.Drawing.Drawing2D.LinearGradientBrush'/> fills.
        ///    </para>
        /// </devdoc>
        public Color[] SurroundColors
        {
            get { return _GetSurroundColors(); }
            set { _SetSurroundColors(value); }
        }

        /**
          * Set/get center point
          */
        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.CenterPoint"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets the center point of the path
        ///       gradient.
        ///    </para>
        /// </devdoc>
        public PointF CenterPoint
        {
            get
            {
                GPPOINTF point = new GPPOINTF();

                int status = SafeNativeMethods.Gdip.GdipGetPathGradientCenterPoint(new HandleRef(this, NativeBrush), point);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return point.ToPoint();
            }

            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetPathGradientCenterPoint(new HandleRef(this, NativeBrush), new GPPOINTF(value));

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /**
         * Get source rectangle
         */
        private RectangleF _GetRectangle()
        {
            GPRECTF rect = new GPRECTF();

            int status = SafeNativeMethods.Gdip.GdipGetPathGradientRect(new HandleRef(this, NativeBrush), ref rect);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return rect.ToRectangleF();
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.Rectangle"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets a bounding rectangle for this <see cref='System.Drawing.Drawing2D.PathGradientBrush'/>.
        ///    </para>
        /// </devdoc>
        public RectangleF Rectangle
        {
            get { return _GetRectangle(); }
        }

        /**
         * Set/get blend factors
         */
        private Blend _GetBlend()
        {
            Blend blend;

            // Figure out the size of blend factor array
            int retval = 0;
            int status = SafeNativeMethods.Gdip.GdipGetPathGradientBlendCount(new HandleRef(this, NativeBrush), out retval);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            // Allocate temporary native memory buffer

            int count = retval;

            IntPtr factors = IntPtr.Zero;
            IntPtr positions = IntPtr.Zero;

            try
            {
                int size = checked(4 * count);
                factors = Marshal.AllocHGlobal(size);
                positions = Marshal.AllocHGlobal(size);

                // Retrieve horizontal blend factors

                status = SafeNativeMethods.Gdip.GdipGetPathGradientBlend(new HandleRef(this, NativeBrush), factors, positions, count);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                // Return the result in a managed array

                blend = new Blend(count);

                Marshal.Copy(factors, blend.Factors, 0, count);
                Marshal.Copy(positions, blend.Positions, 0, count);
            }
            finally
            {
                if (factors != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(factors);
                }
                if (positions != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(positions);
                }
            }

            return blend;
        }

        private void _SetBlend(Blend blend)
        {
            // Allocate temporary native memory buffer
            // and copy input blend factors into it.

            int count = blend.Factors.Length;

            IntPtr factors = IntPtr.Zero;
            IntPtr positions = IntPtr.Zero;

            try
            {
                int size = checked(4 * count);
                factors = Marshal.AllocHGlobal(size);
                positions = Marshal.AllocHGlobal(size);

                Marshal.Copy(blend.Factors, 0, factors, count);
                Marshal.Copy(blend.Positions, 0, positions, count);

                // Set blend factors

                int status = SafeNativeMethods.Gdip.GdipSetPathGradientBlend(new HandleRef(this, NativeBrush), new HandleRef(null, factors), new HandleRef(null, positions), count);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }
            }
            finally
            {
                if (factors != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(factors);
                }
                if (positions != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(positions);
                }
            }
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.Blend"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a <see cref='System.Drawing.Drawing2D.Blend'/> that specifies positions and factors
        ///       that define a custom falloff for the gradient.
        ///    </para>
        /// </devdoc>
        public Blend Blend
        {
            get { return _GetBlend(); }
            set { _SetBlend(value); }
        }

        /*
         * SigmaBlend & LinearBlend
         */

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.SetSigmaBellShape"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Creates a gradient falloff based on a bell-shaped curve.
        ///    </para>
        /// </devdoc>
        public void SetSigmaBellShape(float focus)
        {
            SetSigmaBellShape(focus, (float)1.0);
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.SetSigmaBellShape1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Creates a gradient falloff based on a bell-shaped curve.
        ///    </para>
        /// </devdoc>
        public void SetSigmaBellShape(float focus, float scale)
        {
            int status = SafeNativeMethods.Gdip.GdipSetPathGradientSigmaBlend(new HandleRef(this, NativeBrush), focus, scale);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.SetBlendTriangularShape"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Creates a triangular gradient.
        ///    </para>
        /// </devdoc>
        public void SetBlendTriangularShape(float focus)
        {
            SetBlendTriangularShape(focus, (float)1.0);
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.SetBlendTriangularShape1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Creates a triangular gradient.
        ///    </para>
        /// </devdoc>
        public void SetBlendTriangularShape(float focus, float scale)
        {
            int status = SafeNativeMethods.Gdip.GdipSetPathGradientLinearBlend(new HandleRef(this, NativeBrush), focus, scale);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /*
         * Preset Color Blend
         */

        private ColorBlend _GetInterpolationColors()
        {
            ColorBlend blend;

            // Figure out the size of blend factor array
            int retval = 0;
            int status = SafeNativeMethods.Gdip.GdipGetPathGradientPresetBlendCount(new HandleRef(this, NativeBrush), out retval);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }

            // If retVal is 0, then there is nothing to marshal.
            // In this case, we'll return an empty ColorBlend...
            //
            if (retval == 0)
            {
                return new ColorBlend();
            }

            // Allocate temporary native memory buffer

            int count = retval;

            IntPtr colors = IntPtr.Zero;
            IntPtr positions = IntPtr.Zero;

            try
            {
                int size = checked(4 * count);
                colors = Marshal.AllocHGlobal(size);
                positions = Marshal.AllocHGlobal(size);

                // Retrieve horizontal blend factors

                status = SafeNativeMethods.Gdip.GdipGetPathGradientPresetBlend(new HandleRef(this, NativeBrush), colors, positions, count);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                // Return the result in a managed array

                blend = new ColorBlend(count);

                int[] argb = new int[count];
                Marshal.Copy(colors, argb, 0, count);
                Marshal.Copy(positions, blend.Positions, 0, count);

                // copy ARGB values into Color array of ColorBlend
                blend.Colors = new Color[argb.Length];

                for (int i = 0; i < argb.Length; i++)
                {
                    blend.Colors[i] = Color.FromArgb(argb[i]);
                }
            }
            finally
            {
                if (colors != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(colors);
                }
                if (positions != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(positions);
                }
            }

            return blend;
        }

        private void _SetInterpolationColors(ColorBlend blend)
        {
            // Allocate temporary native memory buffer
            // and copy input blend factors into it.

            int count = blend.Colors.Length;

            IntPtr colors = IntPtr.Zero;
            IntPtr positions = IntPtr.Zero;

            try
            {
                int size = checked(4 * count);
                colors = Marshal.AllocHGlobal(size);
                positions = Marshal.AllocHGlobal(size);

                int[] argbs = new int[count];
                for (int i = 0; i < count; i++)
                {
                    argbs[i] = blend.Colors[i].ToArgb();
                }

                Marshal.Copy(argbs, 0, colors, count);
                Marshal.Copy(blend.Positions, 0, positions, count);

                // Set blend factors

                int status = SafeNativeMethods.Gdip.GdipSetPathGradientPresetBlend(new HandleRef(this, NativeBrush), new HandleRef(null, colors), new HandleRef(null, positions), count);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }
            }
            finally
            {
                if (colors != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(colors);
                }
                if (positions != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(positions);
                }
            }
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.InterpolationColors"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a <see cref='System.Drawing.Drawing2D.ColorBlend'/> that defines a multi-color linear
        ///       gradient.
        ///    </para>
        /// </devdoc>
        public ColorBlend InterpolationColors
        {
            get { return _GetInterpolationColors(); }
            set { _SetInterpolationColors(value); }
        }

        /**
         * Set/get brush transform
         */
        private void _SetTransform(Matrix matrix)
        {
            if (matrix == null)
                throw new ArgumentNullException("matrix");

            int status = SafeNativeMethods.Gdip.GdipSetPathGradientTransform(new HandleRef(this, NativeBrush), new HandleRef(matrix, matrix.nativeMatrix));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private Matrix _GetTransform()
        {
            Matrix matrix = new Matrix();

            int status = SafeNativeMethods.Gdip.GdipGetPathGradientTransform(new HandleRef(this, NativeBrush), new HandleRef(matrix, matrix.nativeMatrix));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return matrix;
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.Transform"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a <see cref='System.Drawing.Drawing2D.Matrix'/> that defines a local geometrical
        ///       transform for this <see cref='System.Drawing.Drawing2D.PathGradientBrush'/>.
        ///    </para>
        /// </devdoc>
        public Matrix Transform
        {
            get { return _GetTransform(); }
            set { _SetTransform(value); }
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.ResetTransform"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Resets the <see cref='System.Drawing.Drawing2D.PathGradientBrush.Transform'/> property to
        ///       identity.
        ///    </para>
        /// </devdoc>
        public void ResetTransform()
        {
            int status = SafeNativeMethods.Gdip.GdipResetPathGradientTransform(new HandleRef(this, NativeBrush));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.MultiplyTransform"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Multiplies the <see cref='System.Drawing.Drawing2D.Matrix'/> that represents the local geometrical
        ///       transform of this <see cref='System.Drawing.Drawing2D.PathGradientBrush'/> by the specified <see cref='System.Drawing.Drawing2D.Matrix'/> by prepending the specified <see cref='System.Drawing.Drawing2D.Matrix'/>.
        ///    </para>
        /// </devdoc>
        public void MultiplyTransform(Matrix matrix)
        {
            MultiplyTransform(matrix, MatrixOrder.Prepend);
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.MultiplyTransform1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Multiplies the <see cref='System.Drawing.Drawing2D.Matrix'/> that represents the local geometrical
        ///       transform of this <see cref='System.Drawing.Drawing2D.PathGradientBrush'/> by the specified <see cref='System.Drawing.Drawing2D.Matrix'/> in the specified order.
        ///    </para>
        /// </devdoc>
        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
                throw new ArgumentNullException("matrix");

            int status = SafeNativeMethods.Gdip.GdipMultiplyPathGradientTransform(new HandleRef(this, NativeBrush),
                                                new HandleRef(matrix, matrix.nativeMatrix),
                                                order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.TranslateTransform"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Translates the local geometrical transform by the specified dimmensions. This
        ///       method prepends the translation to the transform.
        ///    </para>
        /// </devdoc>
        public void TranslateTransform(float dx, float dy)
        {
            TranslateTransform(dx, dy, MatrixOrder.Prepend);
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.TranslateTransform1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Translates the local geometrical transform by the specified dimmensions in
        ///       the specified order.
        ///    </para>
        /// </devdoc>
        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslatePathGradientTransform(new HandleRef(this, NativeBrush),
                                                dx, dy, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.ScaleTransform"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Scales the local geometric transform by the specified amounts. This method
        ///       prepends the scaling matrix to the transform.
        ///    </para>
        /// </devdoc>
        public void ScaleTransform(float sx, float sy)
        {
            ScaleTransform(sx, sy, MatrixOrder.Prepend);
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.ScaleTransform1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Scales the local geometric transform by the specified amounts in the
        ///       specified order.
        ///    </para>
        /// </devdoc>
        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipScalePathGradientTransform(new HandleRef(this, NativeBrush),
                                                sx, sy, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.RotateTransform"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Rotates the local geometric transform by the specified amount. This method
        ///       prepends the rotation to the transform.
        ///    </para>
        /// </devdoc>
        public void RotateTransform(float angle)
        {
            RotateTransform(angle, MatrixOrder.Prepend);
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.RotateTransform1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Rotates the local geometric transform by the specified amount in the
        ///       specified order.
        ///    </para>
        /// </devdoc>
        public void RotateTransform(float angle, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipRotatePathGradientTransform(new HandleRef(this, NativeBrush),
                                                angle, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /**
         * Set/get brush focus scales
         */
        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.FocusScales"]/*' />
        /// <devdoc>
        ///    Gets or sets the focus point for the
        ///    gradient falloff.
        /// </devdoc>
        public PointF FocusScales
        {
            get
            {
                float[] scaleX = new float[] { 0.0f };
                float[] scaleY = new float[] { 0.0f };

                int status = SafeNativeMethods.Gdip.GdipGetPathGradientFocusScales(new HandleRef(this, NativeBrush), scaleX, scaleY);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return new PointF(scaleX[0], scaleY[0]);
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetPathGradientFocusScales(new HandleRef(this, NativeBrush), value.X, value.Y);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /**
         * Set/get brush wrapping mode
         */
        private void _SetWrapMode(WrapMode wrapMode)
        {
            int status = SafeNativeMethods.Gdip.GdipSetPathGradientWrapMode(new HandleRef(this, NativeBrush), unchecked((int)wrapMode));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private WrapMode _GetWrapMode()
        {
            int mode = 0;

            int status = SafeNativeMethods.Gdip.GdipGetPathGradientWrapMode(new HandleRef(this, NativeBrush), out mode);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return (WrapMode)mode;
        }

        /// <include file='doc\PathGradientBrush.uex' path='docs/doc[@for="PathGradientBrush.WrapMode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Gets or sets a <see cref='System.Drawing.Drawing2D.WrapMode'/> that indicates the wrap mode for this
        ///    <see cref='System.Drawing.Drawing2D.PathGradientBrush'/>. 
        ///    </para>
        /// </devdoc>
        public WrapMode WrapMode
        {
            get
            {
                return _GetWrapMode();
            }
            set
            {
                //validate the WrapMode enum
                if (!ClientUtils.IsEnumValid(value, unchecked((int)value), (int)WrapMode.Tile, (int)WrapMode.Clamp))
                {
                    throw new InvalidEnumArgumentException("value", unchecked((int)value), typeof(WrapMode));
                }

                _SetWrapMode(value);
            }
        }
    }
}

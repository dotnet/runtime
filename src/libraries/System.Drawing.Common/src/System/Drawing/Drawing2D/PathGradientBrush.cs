// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Internal;

namespace System.Drawing.Drawing2D
{
    public sealed class PathGradientBrush : Brush
    {
        public PathGradientBrush(PointF[] points) : this(points, WrapMode.Clamp) { }

        public PathGradientBrush(PointF[] points, WrapMode wrapMode)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }
            
            if (wrapMode < WrapMode.Tile || wrapMode > WrapMode.Clamp)
            {
                throw new InvalidEnumArgumentException(nameof(wrapMode), unchecked((int)wrapMode), typeof(WrapMode));
            }

            IntPtr pointsBuf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                IntPtr nativeBrush;
                int status = SafeNativeMethods.Gdip.GdipCreatePathGradient(new HandleRef(null, pointsBuf),
                                                            points.Length,
                                                            unchecked((int)wrapMode),
                                                            out nativeBrush);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                SetNativeBrushInternal(nativeBrush);
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

        public PathGradientBrush(Point[] points) : this(points, WrapMode.Clamp) { }

        public PathGradientBrush(Point[] points, WrapMode wrapMode)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            if (wrapMode < WrapMode.Tile || wrapMode > WrapMode.Clamp)
            {
                throw new InvalidEnumArgumentException(nameof(wrapMode), unchecked((int)wrapMode), typeof(WrapMode));
            }

            IntPtr pointsBuf = SafeNativeMethods.Gdip.ConvertPointToMemory(points);
            try
            {
                IntPtr nativeBrush;
                int status = SafeNativeMethods.Gdip.GdipCreatePathGradientI(new HandleRef(null, pointsBuf),
                                                             points.Length,
                                                             unchecked((int)wrapMode),
                                                             out nativeBrush);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                SetNativeBrushInternal(nativeBrush);
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

        public PathGradientBrush(GraphicsPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            IntPtr nativeBrush;
            int status = SafeNativeMethods.Gdip.GdipCreatePathGradientFromPath(new HandleRef(path, path.nativePath), out nativeBrush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeBrushInternal(nativeBrush);
        }

        internal PathGradientBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        public override object Clone()
        {
            IntPtr clonedBrush;
            int status = SafeNativeMethods.Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out clonedBrush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new PathGradientBrush(clonedBrush);
        }

        public Color CenterColor
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetPathGradientCenterColor(new HandleRef(this, NativeBrush), out int argb);

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

        public Color[] SurroundColors
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetPathGradientSurroundColorCount(new HandleRef(this, NativeBrush),
                                                                           out int count);

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
            set
            {
                int count = value.Length;
                int[] argbs = new int[count];
                for (int i = 0; i < value.Length; i++)
                    argbs[i] = value[i].ToArgb();

                int status = SafeNativeMethods.Gdip.GdipSetPathGradientSurroundColorsWithCount(new HandleRef(this, NativeBrush),
                                                                            argbs,
                                                                            ref count);

                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

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

        public RectangleF Rectangle
        {
            get
            {
                GPRECTF rect = new GPRECTF();

                int status = SafeNativeMethods.Gdip.GdipGetPathGradientRect(new HandleRef(this, NativeBrush), ref rect);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return rect.ToRectangleF();
            }
        }

        public Blend Blend
        {
            get
            {
                // Figure out the size of blend factor array
                int status = SafeNativeMethods.Gdip.GdipGetPathGradientBlendCount(new HandleRef(this, NativeBrush), out int retval);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                // Allocate temporary native memory buffer

                int count = retval;

                var factors = new float[count];
                var positions = new float[count];

                // Retrieve horizontal blend factors

                status = SafeNativeMethods.Gdip.GdipGetPathGradientBlend(new HandleRef(this, NativeBrush), factors, positions, count);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                // Return the result in a managed array

                Blend blend = new Blend(count);
                blend.Factors = factors;
                blend.Positions = positions;

                return blend;
            }
            set
            {
                if (value == null || value.Factors == null)
                {
                    // This is the behavior on Desktop
                    throw new NullReferenceException();
                }

                // The Desktop implementation throws ArgumentNullException("source") because it never validates the value of value.Positions, and then passes it
                // on to Marshal.Copy(value.Positions, 0, positions, count);. The first argument of Marshal.Copy is source, hence this exception.
                if (value.Positions == null)
                {
                    throw new ArgumentNullException("source");
                }

                int count = value.Factors.Length;

                // Explicit argument validation, because libgdiplus does not correctly validate all parameters.
                if (count == 0 || value.Positions.Length == 0)
                {
                    throw new ArgumentException(SR.BlendObjectMustHaveTwoElements);
                }

                if (count >= 2 && count != value.Positions.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (count >= 2 && value.Positions[0] != 0.0F)
                {
                    throw new ArgumentException(SR.BlendObjectFirstElementInvalid);
                }

                if (count >= 2 && value.Positions[count - 1] != 1.0F)
                {
                    throw new ArgumentException(SR.BlendObjectLastElementInvalid);
                }

                // Allocate temporary native memory buffer
                // and copy input blend factors into it.
                IntPtr factors = IntPtr.Zero;
                IntPtr positions = IntPtr.Zero;

                try
                {
                    int size = checked(4 * count);
                    factors = Marshal.AllocHGlobal(size);
                    positions = Marshal.AllocHGlobal(size);

                    Marshal.Copy(value.Factors, 0, factors, count);
                    Marshal.Copy(value.Positions, 0, positions, count);

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
        }

        public void SetSigmaBellShape(float focus) => SetSigmaBellShape(focus, (float)1.0);

        public void SetSigmaBellShape(float focus, float scale)
        {
            if (focus < 0 || focus > 1)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidParameter), nameof(focus));
            }

            if (scale < 0 || scale > 1)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidParameter), nameof(scale));
            }

            int status = SafeNativeMethods.Gdip.GdipSetPathGradientSigmaBlend(new HandleRef(this, NativeBrush), focus, scale);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void SetBlendTriangularShape(float focus) => SetBlendTriangularShape(focus, (float)1.0);

        public void SetBlendTriangularShape(float focus, float scale)
        {
            if (focus < 0 || focus > 1)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidParameter), nameof(focus));
            }

            if (scale < 0 || scale > 1)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidParameter), nameof(scale));
            }

            int status = SafeNativeMethods.Gdip.GdipSetPathGradientLinearBlend(new HandleRef(this, NativeBrush), focus, scale);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public ColorBlend InterpolationColors
        {
            get
            {
                // Figure out the size of blend factor array
                int status = SafeNativeMethods.Gdip.GdipGetPathGradientPresetBlendCount(new HandleRef(this, NativeBrush), out int count);
                SafeNativeMethods.Gdip.CheckStatus(status);

                // If count is 0, then there is nothing to marshal.
                // In this case, we'll return an empty ColorBlend...
                if (count == 0)
                {
                    return new ColorBlend();
                }

                int[] colors = new int[count];
                float[] positions = new float[count];

                ColorBlend blend = new ColorBlend(count);

                // status would fail if we ask points or types with a < 2 count
                if (count > 1)
                {
                    // Retrieve horizontal blend factors
                    status = SafeNativeMethods.Gdip.GdipGetPathGradientPresetBlend(new HandleRef(this, NativeBrush), colors, positions, count);

                    SafeNativeMethods.Gdip.CheckStatus(status);

                    // Return the result in a managed array

                    blend.Positions = positions;

                    // copy ARGB values into Color array of ColorBlend
                    blend.Colors = new Color[count];

                    for (int i = 0; i < count; i++)
                    {
                        blend.Colors[i] = Color.FromArgb(colors[i]);
                    }
                }

                return blend;
            }
            set
            {
                // The Desktop implementation will throw various exceptions - ranging from NullReferenceExceptions to Argument(OutOfRange)Exceptions
                // depending on how sane the input is. These checks exist to replicate the exact Desktop behavior.
                int count = value.Colors.Length;

                if (value.Positions == null)
                {
                    throw new ArgumentNullException("source");
                }

                if (value.Colors.Length > value.Positions.Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                if (value.Colors.Length < value.Positions.Length)
                {
                    throw new ArgumentException();
                }

                float[] positions = value.Positions;
                int[] argbs = new int[count];
                for (int i = 0; i < count; i++)
                {
                    argbs[i] = value.Colors[i].ToArgb();
                }

                // Set blend factors
                int status = SafeNativeMethods.Gdip.GdipSetPathGradientPresetBlend(new HandleRef(this, NativeBrush), argbs, positions, count);

                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public Matrix Transform
        {
            get
            {
                Matrix matrix = new Matrix();

                int status = SafeNativeMethods.Gdip.GdipGetPathGradientTransform(new HandleRef(this, NativeBrush), new HandleRef(matrix, matrix.nativeMatrix));
                SafeNativeMethods.Gdip.CheckStatus(status);

                return matrix;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                int status = SafeNativeMethods.Gdip.GdipSetPathGradientTransform(new HandleRef(this, NativeBrush), new HandleRef(value, value.nativeMatrix));
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public void ResetTransform()
        {
            int status = SafeNativeMethods.Gdip.GdipResetPathGradientTransform(new HandleRef(this, NativeBrush));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void MultiplyTransform(Matrix matrix) => MultiplyTransform(matrix, MatrixOrder.Prepend);

        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException(nameof(matrix));
            }

            // Multiplying the transform by a disposed matrix is a nop in GDI+, but throws
            // with the libgdiplus backend. Simulate a nop for compatability with GDI+.
            if (matrix.nativeMatrix == IntPtr.Zero)
            {
                return;
            }

            int status = SafeNativeMethods.Gdip.GdipMultiplyPathGradientTransform(new HandleRef(this, NativeBrush),
                                                new HandleRef(matrix, matrix.nativeMatrix),
                                                order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslatePathGradientTransform(new HandleRef(this, NativeBrush),
                                                dx, dy, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipScalePathGradientTransform(new HandleRef(this, NativeBrush),
                                                sx, sy, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipRotatePathGradientTransform(new HandleRef(this, NativeBrush),
                                                angle, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

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

        public WrapMode WrapMode
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetPathGradientWrapMode(new HandleRef(this, NativeBrush), out int mode);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return (WrapMode)mode;
            }
            set
            {
                if (value < WrapMode.Tile || value > WrapMode.Clamp)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(WrapMode));
                }

                int status = SafeNativeMethods.Gdip.GdipSetPathGradientWrapMode(new HandleRef(this, NativeBrush), unchecked((int)value));

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }
    }
}

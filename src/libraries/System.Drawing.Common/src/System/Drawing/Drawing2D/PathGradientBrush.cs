// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Internal;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Drawing2D
{
    public sealed class PathGradientBrush : Brush
    {
        public PathGradientBrush(PointF[] points) : this(points, WrapMode.Clamp) { }

        public unsafe PathGradientBrush(PointF[] points, WrapMode wrapMode)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            if (wrapMode < WrapMode.Tile || wrapMode > WrapMode.Clamp)
                throw new InvalidEnumArgumentException(nameof(wrapMode), unchecked((int)wrapMode), typeof(WrapMode));

            // GdipCreatePathGradient returns InsufficientBuffer for less than 3 points, which we turn into
            // OutOfMemoryException(). We used to copy nothing into an empty native buffer for zero points,
            // which gives a valid pointer. Fixing an empty array gives a null pointer, which causes an
            // InvalidParameter result, which we turn into an ArgumentException. Matching the old throw.
            if (points.Length == 0)
                throw new OutOfMemoryException();

            fixed (PointF* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipCreatePathGradient(
                    p, points.Length, wrapMode, out IntPtr nativeBrush));
                SetNativeBrushInternal(nativeBrush);
            }
        }

        public PathGradientBrush(Point[] points) : this(points, WrapMode.Clamp) { }

        public unsafe PathGradientBrush(Point[] points, WrapMode wrapMode)
        {
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            if (wrapMode < WrapMode.Tile || wrapMode > WrapMode.Clamp)
                throw new InvalidEnumArgumentException(nameof(wrapMode), unchecked((int)wrapMode), typeof(WrapMode));

            // GdipCreatePathGradient returns InsufficientBuffer for less than 3 points, which we turn into
            // OutOfMemoryException(). We used to copy nothing into an empty native buffer for zero points,
            // which gives a valid pointer. Fixing an empty array gives a null pointer, which causes an
            // InvalidParameter result, which we turn into an ArgumentException. Matching the old throw.
            if (points.Length == 0)
                throw new OutOfMemoryException();

            fixed (Point* p = points)
            {
                Gdip.CheckStatus(Gdip.GdipCreatePathGradientI(
                    p, points.Length, wrapMode, out IntPtr nativeBrush));
                SetNativeBrushInternal(nativeBrush);
            }
        }

        public PathGradientBrush(GraphicsPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            int status = Gdip.GdipCreatePathGradientFromPath(new HandleRef(path, path._nativePath), out IntPtr nativeBrush);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            SetNativeBrushInternal(nativeBrush);
        }

        internal PathGradientBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        public override object Clone()
        {
            int status = Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out IntPtr clonedBrush);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);

            return new PathGradientBrush(clonedBrush);
        }

        public Color CenterColor
        {
            get
            {
                int status = Gdip.GdipGetPathGradientCenterColor(new HandleRef(this, NativeBrush), out int argb);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return Color.FromArgb(argb);
            }

            set
            {
                int status = Gdip.GdipSetPathGradientCenterColor(new HandleRef(this, NativeBrush), value.ToArgb());

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }

        public Color[] SurroundColors
        {
            get
            {
                int status = Gdip.GdipGetPathGradientSurroundColorCount(new HandleRef(this, NativeBrush),
                                                                           out int count);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                int[] argbs = new int[count];

                status = Gdip.GdipGetPathGradientSurroundColorsWithCount(new HandleRef(this, NativeBrush),
                                                                            argbs,
                                                                            ref count);


                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

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

                int status = Gdip.GdipSetPathGradientSurroundColorsWithCount(new HandleRef(this, NativeBrush),
                                                                            argbs,
                                                                            ref count);

                Gdip.CheckStatus(status);
            }
        }

        public PointF CenterPoint
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPathGradientCenterPoint(new HandleRef(this, NativeBrush), out PointF point));
                return point;
            }
            set
            {
                Gdip.CheckStatus(Gdip.GdipSetPathGradientCenterPoint(new HandleRef(this, NativeBrush), ref value));
            }
        }

        public RectangleF Rectangle
        {
            get
            {
                GPRECTF rect = new GPRECTF();

                int status = Gdip.GdipGetPathGradientRect(new HandleRef(this, NativeBrush), ref rect);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return rect.ToRectangleF();
            }
        }

        public Blend Blend
        {
            get
            {
                // Figure out the size of blend factor array
                int status = Gdip.GdipGetPathGradientBlendCount(new HandleRef(this, NativeBrush), out int retval);

                if (status != Gdip.Ok)
                {
                    throw Gdip.StatusException(status);
                }

                // Allocate temporary native memory buffer

                int count = retval;

                var factors = new float[count];
                var positions = new float[count];

                // Retrieve horizontal blend factors

                status = Gdip.GdipGetPathGradientBlend(new HandleRef(this, NativeBrush), factors, positions, count);

                if (status != Gdip.Ok)
                {
                    throw Gdip.StatusException(status);
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

                    int status = Gdip.GdipSetPathGradientBlend(new HandleRef(this, NativeBrush), new HandleRef(null, factors), new HandleRef(null, positions), count);

                    if (status != Gdip.Ok)
                    {
                        throw Gdip.StatusException(status);
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

            int status = Gdip.GdipSetPathGradientSigmaBlend(new HandleRef(this, NativeBrush), focus, scale);
            Gdip.CheckStatus(status);
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

            int status = Gdip.GdipSetPathGradientLinearBlend(new HandleRef(this, NativeBrush), focus, scale);
            Gdip.CheckStatus(status);
        }

        public ColorBlend InterpolationColors
        {
            get
            {
                // Figure out the size of blend factor array
                int status = Gdip.GdipGetPathGradientPresetBlendCount(new HandleRef(this, NativeBrush), out int count);
                Gdip.CheckStatus(status);

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
                    status = Gdip.GdipGetPathGradientPresetBlend(new HandleRef(this, NativeBrush), colors, positions, count);

                    Gdip.CheckStatus(status);

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
                int status = Gdip.GdipSetPathGradientPresetBlend(new HandleRef(this, NativeBrush), argbs, positions, count);

                Gdip.CheckStatus(status);
            }
        }

        public Matrix Transform
        {
            get
            {
                Matrix matrix = new Matrix();

                int status = Gdip.GdipGetPathGradientTransform(new HandleRef(this, NativeBrush), new HandleRef(matrix, matrix.nativeMatrix));
                Gdip.CheckStatus(status);

                return matrix;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                int status = Gdip.GdipSetPathGradientTransform(new HandleRef(this, NativeBrush), new HandleRef(value, value.nativeMatrix));
                Gdip.CheckStatus(status);
            }
        }

        public void ResetTransform()
        {
            int status = Gdip.GdipResetPathGradientTransform(new HandleRef(this, NativeBrush));

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
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

            int status = Gdip.GdipMultiplyPathGradientTransform(new HandleRef(this, NativeBrush),
                                                new HandleRef(matrix, matrix.nativeMatrix),
                                                order);
            Gdip.CheckStatus(status);
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            int status = Gdip.GdipTranslatePathGradientTransform(new HandleRef(this, NativeBrush),
                                                dx, dy, order);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            int status = Gdip.GdipScalePathGradientTransform(new HandleRef(this, NativeBrush),
                                                sx, sy, order);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            int status = Gdip.GdipRotatePathGradientTransform(new HandleRef(this, NativeBrush),
                                                angle, order);

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public PointF FocusScales
        {
            get
            {
                float[] scaleX = new float[] { 0.0f };
                float[] scaleY = new float[] { 0.0f };

                int status = Gdip.GdipGetPathGradientFocusScales(new HandleRef(this, NativeBrush), scaleX, scaleY);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return new PointF(scaleX[0], scaleY[0]);
            }
            set
            {
                int status = Gdip.GdipSetPathGradientFocusScales(new HandleRef(this, NativeBrush), value.X, value.Y);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }

        public WrapMode WrapMode
        {
            get
            {
                int status = Gdip.GdipGetPathGradientWrapMode(new HandleRef(this, NativeBrush), out int mode);

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);

                return (WrapMode)mode;
            }
            set
            {
                if (value < WrapMode.Tile || value > WrapMode.Clamp)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(WrapMode));
                }

                int status = Gdip.GdipSetPathGradientWrapMode(new HandleRef(this, NativeBrush), unchecked((int)value));

                if (status != Gdip.Ok)
                    throw Gdip.StatusException(status);
            }
        }
    }
}

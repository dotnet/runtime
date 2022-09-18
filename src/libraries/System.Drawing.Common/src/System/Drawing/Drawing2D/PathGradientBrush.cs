// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            ArgumentNullException.ThrowIfNull(points);

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
            ArgumentNullException.ThrowIfNull(points);

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
            ArgumentNullException.ThrowIfNull(path);

            Gdip.CheckStatus(Gdip.GdipCreatePathGradientFromPath(new HandleRef(path, path._nativePath), out IntPtr nativeBrush));
            SetNativeBrushInternal(nativeBrush);
        }

        internal PathGradientBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        public override object Clone()
        {
            Gdip.CheckStatus(Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out IntPtr clonedBrush));
            return new PathGradientBrush(clonedBrush);
        }

        public Color CenterColor
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPathGradientCenterColor(new HandleRef(this, NativeBrush), out int argb));
                return Color.FromArgb(argb);
            }
            set
            {
                Gdip.CheckStatus(Gdip.GdipSetPathGradientCenterColor(new HandleRef(this, NativeBrush), value.ToArgb()));
            }
        }

        public Color[] SurroundColors
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPathGradientSurroundColorCount(
                    new HandleRef(this, NativeBrush),
                    out int count));

                int[] argbs = new int[count];

                Gdip.CheckStatus(Gdip.GdipGetPathGradientSurroundColorsWithCount(
                    new HandleRef(this, NativeBrush),
                    argbs,
                    ref count));

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

                Gdip.CheckStatus(Gdip.GdipSetPathGradientSurroundColorsWithCount(
                    new HandleRef(this, NativeBrush),
                    argbs,
                    ref count));
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
                Gdip.CheckStatus(Gdip.GdipGetPathGradientRect(new HandleRef(this, NativeBrush), out RectangleF rect));
                return rect;
            }
        }

        public Blend Blend
        {
            get
            {
                // Figure out the size of blend factor array
                Gdip.CheckStatus(Gdip.GdipGetPathGradientBlendCount(new HandleRef(this, NativeBrush), out int retval));

                // Allocate temporary native memory buffer

                int count = retval;

                var factors = new float[count];
                var positions = new float[count];

                // Retrieve horizontal blend factors

                Gdip.CheckStatus(Gdip.GdipGetPathGradientBlend(new HandleRef(this, NativeBrush), factors, positions, count));

                // Return the result in a managed array

                Blend blend = new Blend(count)
                {
                    Factors = factors,
                    Positions = positions
                };

                return blend;
            }
            set
            {
                // This is the behavior on Desktop
                if (value == null || value.Factors == null)
                    throw new NullReferenceException();

                if (value.Positions == null)
                    throw new ArgumentException(SR.Format(SR.InvalidArgumentValue, "value.Positions", value.Positions), nameof(value));

                int count = value.Factors.Length;

                // Explicit argument validation, because libgdiplus does not correctly validate all parameters.
                if (count == 0 || value.Positions.Length == 0)
                    throw new ArgumentException(SR.BlendObjectMustHaveTwoElements);
                if (count >= 2 && count != value.Positions.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (count >= 2 && value.Positions[0] != 0.0F)
                    throw new ArgumentException(SR.BlendObjectFirstElementInvalid);
                if (count >= 2 && value.Positions[count - 1] != 1.0F)
                    throw new ArgumentException(SR.BlendObjectLastElementInvalid);

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

                    Gdip.CheckStatus(Gdip.GdipSetPathGradientBlend(new HandleRef(this, NativeBrush), factors, positions, count));
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
                throw new ArgumentException(SR.GdiplusInvalidParameter, nameof(focus));
            if (scale < 0 || scale > 1)
                throw new ArgumentException(SR.GdiplusInvalidParameter, nameof(scale));

            Gdip.CheckStatus(Gdip.GdipSetPathGradientSigmaBlend(new HandleRef(this, NativeBrush), focus, scale));
        }

        public void SetBlendTriangularShape(float focus) => SetBlendTriangularShape(focus, (float)1.0);

        public void SetBlendTriangularShape(float focus, float scale)
        {
            if (focus < 0 || focus > 1)
                throw new ArgumentException(SR.GdiplusInvalidParameter, nameof(focus));
            if (scale < 0 || scale > 1)
                throw new ArgumentException(SR.GdiplusInvalidParameter, nameof(scale));

            Gdip.CheckStatus(Gdip.GdipSetPathGradientLinearBlend(new HandleRef(this, NativeBrush), focus, scale));
        }

        public ColorBlend InterpolationColors
        {
            get
            {
                // Figure out the size of blend factor array
                Gdip.CheckStatus(Gdip.GdipGetPathGradientPresetBlendCount(new HandleRef(this, NativeBrush), out int count));

                // If count is 0, then there is nothing to marshal.
                // In this case, we'll return an empty ColorBlend...
                if (count == 0)
                    return new ColorBlend();

                int[] colors = new int[count];
                float[] positions = new float[count];

                ColorBlend blend = new ColorBlend(count);

                // status would fail if we ask points or types with a < 2 count
                if (count < 2)
                    return blend;

                // Retrieve horizontal blend factors
                Gdip.CheckStatus(Gdip.GdipGetPathGradientPresetBlend(new HandleRef(this, NativeBrush), colors, positions, count));

                // Return the result in a managed array

                blend.Positions = positions;

                // copy ARGB values into Color array of ColorBlend
                blend.Colors = new Color[count];

                for (int i = 0; i < count; i++)
                {
                    blend.Colors[i] = Color.FromArgb(colors[i]);
                }

                return blend;
            }
            set
            {
                int count = value.Colors.Length;

                if (value.Positions == null)
                    throw new ArgumentException(SR.Format(SR.InvalidArgumentValue, "value.Positions", value.Positions), nameof(value));
                if (value.Colors.Length != value.Positions.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));

                float[] positions = value.Positions;
                int[] argbs = new int[count];
                for (int i = 0; i < count; i++)
                {
                    argbs[i] = value.Colors[i].ToArgb();
                }

                // Set blend factors
                Gdip.CheckStatus(Gdip.GdipSetPathGradientPresetBlend(new HandleRef(this, NativeBrush), argbs, positions, count));
            }
        }

        public Matrix Transform
        {
            get
            {
                Matrix matrix = new Matrix();
                Gdip.CheckStatus(Gdip.GdipGetPathGradientTransform(new HandleRef(this, NativeBrush), new HandleRef(matrix, matrix.NativeMatrix)));
                return matrix;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                Gdip.CheckStatus(Gdip.GdipSetPathGradientTransform(new HandleRef(this, NativeBrush), new HandleRef(value, value.NativeMatrix)));
            }
        }

        public void ResetTransform()
        {
            Gdip.CheckStatus(Gdip.GdipResetPathGradientTransform(new HandleRef(this, NativeBrush)));
        }

        public void MultiplyTransform(Matrix matrix) => MultiplyTransform(matrix, MatrixOrder.Prepend);

        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            ArgumentNullException.ThrowIfNull(matrix);

            // Multiplying the transform by a disposed matrix is a nop in GDI+, but throws
            // with the libgdiplus backend. Simulate a nop for compatibility with GDI+.
            if (matrix.NativeMatrix == IntPtr.Zero)
                return;

            Gdip.CheckStatus(Gdip.GdipMultiplyPathGradientTransform(
                new HandleRef(this, NativeBrush),
                new HandleRef(matrix, matrix.NativeMatrix),
                order));
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipTranslatePathGradientTransform(new HandleRef(this, NativeBrush), dx, dy, order));
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipScalePathGradientTransform(new HandleRef(this, NativeBrush), sx, sy, order));
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipRotatePathGradientTransform(new HandleRef(this, NativeBrush), angle, order));
        }

        public PointF FocusScales
        {
            get
            {
                float[] scaleX = new float[] { 0.0f };
                float[] scaleY = new float[] { 0.0f };

                Gdip.CheckStatus(Gdip.GdipGetPathGradientFocusScales(new HandleRef(this, NativeBrush), scaleX, scaleY));
                return new PointF(scaleX[0], scaleY[0]);
            }
            set
            {
                Gdip.CheckStatus(Gdip.GdipSetPathGradientFocusScales(new HandleRef(this, NativeBrush), value.X, value.Y));
            }
        }

        public WrapMode WrapMode
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetPathGradientWrapMode(new HandleRef(this, NativeBrush), out int mode));
                return (WrapMode)mode;
            }
            set
            {
                if (value < WrapMode.Tile || value > WrapMode.Clamp)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(WrapMode));

                Gdip.CheckStatus(Gdip.GdipSetPathGradientWrapMode(new HandleRef(this, NativeBrush), unchecked((int)value)));
            }
        }
    }
}

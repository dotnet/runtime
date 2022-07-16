// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Drawing2D
{
    public sealed class LinearGradientBrush : Brush
    {
        private bool _interpolationColorsWasSet;

        public LinearGradientBrush(PointF point1, PointF point2, Color color1, Color color2)
        {
            Gdip.CheckStatus(Gdip.GdipCreateLineBrush(
                ref point1, ref point2,
                color1.ToArgb(), color2.ToArgb(),
                WrapMode.Tile,
                out IntPtr nativeBrush));

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(Point point1, Point point2, Color color1, Color color2)
        {
            Gdip.CheckStatus(Gdip.GdipCreateLineBrushI(
                ref point1, ref point2,
                color1.ToArgb(), color2.ToArgb(),
                WrapMode.Tile,
                out IntPtr nativeBrush));

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, LinearGradientMode linearGradientMode)
        {
            if (linearGradientMode < LinearGradientMode.Horizontal || linearGradientMode > LinearGradientMode.BackwardDiagonal)
                throw new InvalidEnumArgumentException(nameof(linearGradientMode), unchecked((int)linearGradientMode), typeof(LinearGradientMode));

            if (rect.Width == 0.0 || rect.Height == 0.0)
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));

            Gdip.CheckStatus(Gdip.GdipCreateLineBrushFromRect(
                ref rect,
                color1.ToArgb(),
                color2.ToArgb(),
                linearGradientMode,
                WrapMode.Tile,
                out IntPtr nativeBrush));

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, LinearGradientMode linearGradientMode)
        {
            if (linearGradientMode < LinearGradientMode.Horizontal || linearGradientMode > LinearGradientMode.BackwardDiagonal)
                throw new InvalidEnumArgumentException(nameof(linearGradientMode), unchecked((int)linearGradientMode), typeof(LinearGradientMode));
            if (rect.Width == 0 || rect.Height == 0)
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));

            Gdip.CheckStatus(Gdip.GdipCreateLineBrushFromRectI(
                ref rect,
                color1.ToArgb(),
                color2.ToArgb(),
                linearGradientMode,
                WrapMode.Tile,
                out IntPtr nativeBrush));

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, float angle) : this(rect, color1, color2, angle, false)
        {
        }

        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, float angle, bool isAngleScaleable)
        {
            if (rect.Width == 0.0 || rect.Height == 0.0)
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));

            Gdip.CheckStatus(Gdip.GdipCreateLineBrushFromRectWithAngle(
                ref rect,
                color1.ToArgb(),
                color2.ToArgb(),
                angle,
                isAngleScaleable,
                (int)WrapMode.Tile,
                out IntPtr nativeBrush));

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, float angle) : this(rect, color1, color2, angle, false)
        {
        }

        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, float angle, bool isAngleScaleable)
        {
            if (rect.Width == 0 || rect.Height == 0)
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));

            Gdip.CheckStatus(Gdip.GdipCreateLineBrushFromRectWithAngleI(
                ref rect,
                color1.ToArgb(),
                color2.ToArgb(),
                angle,
                isAngleScaleable,
                WrapMode.Tile,
                out IntPtr nativeBrush));

            SetNativeBrushInternal(nativeBrush);
        }

        internal LinearGradientBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        public override object Clone()
        {
            Gdip.CheckStatus(Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out IntPtr clonedBrush));
            return new LinearGradientBrush(clonedBrush);
        }

        public Color[] LinearColors
        {
            get
            {
                int[] colors = new int[] { 0, 0 };
                Gdip.CheckStatus(Gdip.GdipGetLineColors(new HandleRef(this, NativeBrush), colors));

                return new Color[]
                {
                    Color.FromArgb(colors[0]),
                    Color.FromArgb(colors[1])
                };
            }
            set
            {
                Gdip.CheckStatus(Gdip.GdipSetLineColors(
                    new HandleRef(this, NativeBrush),
                    value[0].ToArgb(),
                    value[1].ToArgb()));
            }
        }

        public RectangleF Rectangle
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetLineRect(new HandleRef(this, NativeBrush), out RectangleF rect));
                return rect;
            }
        }

        public bool GammaCorrection
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetLineGammaCorrection(
                    new HandleRef(this, NativeBrush),
                    out bool useGammaCorrection));

                return useGammaCorrection;
            }
            set
            {
                Gdip.CheckStatus(Gdip.GdipSetLineGammaCorrection(new HandleRef(this, NativeBrush), value));
            }
        }

        public Blend? Blend
        {
            get
            {
                // Interpolation colors and blends don't work together very well. Getting the Blend when InterpolationColors
                // is set puts the Brush into an unusable state afterwards.
                // Bail out here to avoid that.
                if (_interpolationColorsWasSet)
                    return null;

                // Figure out the size of blend factor array.
                Gdip.CheckStatus(Gdip.GdipGetLineBlendCount(new HandleRef(this, NativeBrush), out int retval));

                if (retval <= 0)
                    return null;

                // Allocate a temporary native memory buffer.
                int count = retval;
                IntPtr factors = IntPtr.Zero;
                IntPtr positions = IntPtr.Zero;

                try
                {
                    int size = checked(4 * count);
                    factors = Marshal.AllocHGlobal(size);
                    positions = Marshal.AllocHGlobal(size);

                    // Retrieve horizontal blend factors.
                    Gdip.CheckStatus(Gdip.GdipGetLineBlend(new HandleRef(this, NativeBrush), factors, positions, count));

                    // Return the result in a managed array.
                    var blend = new Blend(count);

                    Marshal.Copy(factors, blend.Factors, 0, count);
                    Marshal.Copy(positions, blend.Positions, 0, count);

                    return blend;
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
            set
            {
                // Do explicit parameter validation here; libgdiplus does not correctly validate the arguments

                // This is the original behavior on Desktop .NET
                if (value == null || value.Factors == null)
                    throw new NullReferenceException();

                if (value.Positions == null)
                    throw new ArgumentException(SR.Format(SR.InvalidArgumentValue, "value.Positions", value.Positions), nameof(value));

                int count = value.Factors.Length;

                if (count == 0 || value.Positions.Length == 0)
                    throw new ArgumentException(SR.BlendObjectMustHaveTwoElements);
                if (count >= 2 && count != value.Positions.Length)
                    throw new ArgumentOutOfRangeException(nameof(value));
                if (count >= 2 && value.Positions[0] != 0.0F)
                    throw new ArgumentException(SR.BlendObjectFirstElementInvalid);
                if (count >= 2 && value.Positions[count - 1] != 1.0F)
                    throw new ArgumentException(SR.BlendObjectLastElementInvalid);

                // Allocate temporary native memory buffer and copy input blend factors into it.
                IntPtr factors = IntPtr.Zero;
                IntPtr positions = IntPtr.Zero;

                try
                {
                    int size = checked(4 * count);
                    factors = Marshal.AllocHGlobal(size);
                    positions = Marshal.AllocHGlobal(size);

                    Marshal.Copy(value.Factors, 0, factors, count);
                    Marshal.Copy(value.Positions, 0, positions, count);

                    // Set blend factors.
                    Gdip.CheckStatus(Gdip.GdipSetLineBlend(
                        new HandleRef(this, NativeBrush),
                        factors,
                        positions,
                        count));
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

            Gdip.CheckStatus(Gdip.GdipSetLineSigmaBlend(new HandleRef(this, NativeBrush), focus, scale));
        }

        public void SetBlendTriangularShape(float focus) => SetBlendTriangularShape(focus, (float)1.0);

        public void SetBlendTriangularShape(float focus, float scale)
        {
            if (focus < 0 || focus > 1)
                throw new ArgumentException(SR.GdiplusInvalidParameter, nameof(focus));
            if (scale < 0 || scale > 1)
                throw new ArgumentException(SR.GdiplusInvalidParameter, nameof(scale));

            Gdip.CheckStatus(Gdip.GdipSetLineLinearBlend(new HandleRef(this, NativeBrush), focus, scale));

            // Setting a triangular shape overrides the explicitly set interpolation colors. libgdiplus correctly clears
            // the interpolation colors (https://github.com/mono/libgdiplus/blob/master/src/lineargradientbrush.c#L959) but
            // returns WrongState instead of ArgumentException (https://github.com/mono/libgdiplus/blob/master/src/lineargradientbrush.c#L814)
            // when calling GdipGetLinePresetBlend, so it is important we set this to false. This way, we are sure get_InterpolationColors
            // will return an ArgumentException.
            _interpolationColorsWasSet = false;
        }

        public ColorBlend InterpolationColors
        {
            get
            {
                if (!_interpolationColorsWasSet)
                    throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                                SR.InterpolationColorsColorBlendNotSet, string.Empty));

                // Figure out the size of blend factor array.
                Gdip.CheckStatus(Gdip.GdipGetLinePresetBlendCount(new HandleRef(this, NativeBrush), out int retval));

                // Allocate temporary native memory buffer.
                int count = retval;

                IntPtr colors = IntPtr.Zero;
                IntPtr positions = IntPtr.Zero;

                try
                {
                    int size = checked(4 * count);
                    colors = Marshal.AllocHGlobal(size);
                    positions = Marshal.AllocHGlobal(size);

                    // Retrieve horizontal blend factors.
                    Gdip.CheckStatus(Gdip.GdipGetLinePresetBlend(new HandleRef(this, NativeBrush), colors, positions, count));

                    // Return the result in a managed array.
                    var blend = new ColorBlend(count);

                    int[] argb = new int[count];
                    Marshal.Copy(colors, argb, 0, count);
                    Marshal.Copy(positions, blend.Positions, 0, count);

                    // Copy ARGB values into Color array of ColorBlend.
                    blend.Colors = new Color[argb.Length];

                    for (int i = 0; i < argb.Length; i++)
                    {
                        blend.Colors[i] = Color.FromArgb(argb[i]);
                    }

                    return blend;
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
            set
            {
                _interpolationColorsWasSet = true;

                if (value == null)
                {
                    throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                                SR.InterpolationColorsInvalidColorBlendObject, string.Empty));
                }
                else if (value.Colors.Length < 2)
                {
                    throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                                SR.InterpolationColorsInvalidColorBlendObject,
                                                SR.InterpolationColorsLength));
                }
                else if (value.Colors.Length != value.Positions.Length)
                {
                    throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                                SR.InterpolationColorsInvalidColorBlendObject,
                                                SR.InterpolationColorsLengthsDiffer));
                }
                else if (value.Positions[0] != 0.0f)
                {
                    throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                                SR.InterpolationColorsInvalidColorBlendObject,
                                                SR.InterpolationColorsInvalidStartPosition));
                }
                else if (value.Positions[value.Positions.Length - 1] != 1.0f)
                {
                    throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                                SR.InterpolationColorsInvalidColorBlendObject,
                                                SR.InterpolationColorsInvalidEndPosition));
                }


                // Allocate a temporary native memory buffer and copy input blend factors into it.
                int count = value.Colors.Length;
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
                        argbs[i] = value.Colors[i].ToArgb();
                    }

                    Marshal.Copy(argbs, 0, colors, count);
                    Marshal.Copy(value.Positions, 0, positions, count);

                    // Set blend factors.
                    Gdip.CheckStatus(Gdip.GdipSetLinePresetBlend(new HandleRef(this, NativeBrush), colors, positions, count));
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
        }

        public WrapMode WrapMode
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipGetLineWrapMode(new HandleRef(this, NativeBrush), out int mode));
                return (WrapMode)mode;
            }
            set
            {
                if (value < WrapMode.Tile || value > WrapMode.Clamp)
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(WrapMode));

                Gdip.CheckStatus(Gdip.GdipSetLineWrapMode(new HandleRef(this, NativeBrush), unchecked((int)value)));
            }
        }

        public Matrix Transform
        {
            get
            {
                var matrix = new Matrix();
                Gdip.CheckStatus(Gdip.GdipGetLineTransform(new HandleRef(this, NativeBrush), new HandleRef(matrix, matrix.NativeMatrix)));
                return matrix;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                Gdip.CheckStatus(Gdip.GdipSetLineTransform(new HandleRef(this, NativeBrush), new HandleRef(value, value.NativeMatrix)));
            }
        }

        public void ResetTransform()
        {
            Gdip.CheckStatus(Gdip.GdipResetLineTransform(new HandleRef(this, NativeBrush)));
        }

        public void MultiplyTransform(Matrix matrix) => MultiplyTransform(matrix, MatrixOrder.Prepend);

        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            ArgumentNullException.ThrowIfNull(matrix);

            // Multiplying the transform by a disposed matrix is a nop in GDI+, but throws
            // with the libgdiplus backend. Simulate a nop for compatibility with GDI+.
            if (matrix.NativeMatrix == IntPtr.Zero)
                return;

            Gdip.CheckStatus(Gdip.GdipMultiplyLineTransform(
                new HandleRef(this, NativeBrush),
                new HandleRef(matrix, matrix.NativeMatrix),
                order));
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateLineTransform(
                new HandleRef(this, NativeBrush), dx, dy, order));
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipScaleLineTransform(
                new HandleRef(this, NativeBrush), sx, sy, order));
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipRotateLineTransform(
                new HandleRef(this, NativeBrush), angle, order));
        }
    }
}

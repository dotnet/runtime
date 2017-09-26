// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Drawing.Internal;

namespace System.Drawing.Drawing2D
{
    public sealed class LinearGradientBrush : Brush
    {
        private bool _interpolationColorsWasSet;

        public LinearGradientBrush(PointF point1, PointF point2, Color color1, Color color2)
        {
            IntPtr nativeBrush;
            int status = SafeNativeMethods.Gdip.GdipCreateLineBrush(new GPPOINTF(point1),
                                                     new GPPOINTF(point2),
                                                     color1.ToArgb(),
                                                     color2.ToArgb(),
                                                     (int)WrapMode.Tile,
                                                     out nativeBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);
            
            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(Point point1, Point point2, Color color1, Color color2)
        {
            IntPtr nativeBrush;
            int status = SafeNativeMethods.Gdip.GdipCreateLineBrushI(new GPPOINT(point1),
                                                      new GPPOINT(point2),
                                                      color1.ToArgb(),
                                                      color2.ToArgb(),
                                                      (int)WrapMode.Tile,
                                                      out nativeBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, LinearGradientMode linearGradientMode)
        {
            if (linearGradientMode < LinearGradientMode.Horizontal || linearGradientMode > LinearGradientMode.BackwardDiagonal)
            {
                throw new InvalidEnumArgumentException(nameof(linearGradientMode), unchecked((int)linearGradientMode), typeof(LinearGradientMode));
            }

            if (rect.Width == 0.0 || rect.Height == 0.0)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));
            }

            var gprectf = new GPRECTF(rect);
            IntPtr nativeBrush;
            int status = SafeNativeMethods.Gdip.GdipCreateLineBrushFromRect(ref gprectf,
                                                             color1.ToArgb(),
                                                             color2.ToArgb(),
                                                             unchecked((int)linearGradientMode),
                                                             (int)WrapMode.Tile,
                                                             out nativeBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, LinearGradientMode linearGradientMode)
        {
            if (linearGradientMode < LinearGradientMode.Horizontal || linearGradientMode > LinearGradientMode.BackwardDiagonal)
            {
                throw new InvalidEnumArgumentException(nameof(linearGradientMode), unchecked((int)linearGradientMode), typeof(LinearGradientMode));
            }
            
            if (rect.Width == 0 || rect.Height == 0)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));
            }

            var gpRect = new GPRECT(rect);
            IntPtr nativeBrush;
            int status = SafeNativeMethods.Gdip.GdipCreateLineBrushFromRectI(ref gpRect,
                                                              color1.ToArgb(),
                                                              color2.ToArgb(),
                                                              unchecked((int)linearGradientMode),
                                                              (int)WrapMode.Tile,
                                                              out nativeBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, float angle) : this(rect, color1, color2, angle, false)
        {
        }

        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, float angle, bool isAngleScaleable)
        {
            if (rect.Width == 0.0 || rect.Height == 0.0)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));
            }

            var gprectf = new GPRECTF(rect);
            IntPtr nativeBrush;
            int status = SafeNativeMethods.Gdip.GdipCreateLineBrushFromRectWithAngle(ref gprectf,
                                                                      color1.ToArgb(),
                                                                      color2.ToArgb(),
                                                                      angle,
                                                                      isAngleScaleable,
                                                                      (int)WrapMode.Tile,
                                                                      out nativeBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, float angle) : this(rect, color1, color2, angle, false)
        {
        }

        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, float angle, bool isAngleScaleable)
        {
            if (rect.Width == 0 || rect.Height == 0)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));
            }

            var gprect = new GPRECT(rect);
            IntPtr nativeBrush;
            int status = SafeNativeMethods.Gdip.GdipCreateLineBrushFromRectWithAngleI(ref gprect,
                                                                       color1.ToArgb(),
                                                                       color2.ToArgb(),
                                                                       angle,
                                                                       isAngleScaleable,
                                                                       (int)WrapMode.Tile,
                                                                       out nativeBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            SetNativeBrushInternal(nativeBrush);
        }

        internal LinearGradientBrush(IntPtr nativeBrush)
        {
            Debug.Assert(nativeBrush != IntPtr.Zero, "Initializing native brush with null.");
            SetNativeBrushInternal(nativeBrush);
        }

        public override object Clone()
        {
            IntPtr clonedBrush;
            int status = SafeNativeMethods.Gdip.GdipCloneBrush(new HandleRef(this, NativeBrush), out clonedBrush);
            SafeNativeMethods.Gdip.CheckStatus(status);

            return new LinearGradientBrush(clonedBrush);
        }

        public Color[] LinearColors
        {
            get
            {
                int[] colors = new int[] { 0, 0 };
                int status = SafeNativeMethods.Gdip.GdipGetLineColors(new HandleRef(this, NativeBrush), colors);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return new Color[]
                {
                    Color.FromArgb(colors[0]),
                    Color.FromArgb(colors[1])
                };
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetLineColors(new HandleRef(this, NativeBrush),
                                                       value[0].ToArgb(),
                                                       value[1].ToArgb());
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public RectangleF Rectangle
        {
            get
            {
                var rect = new GPRECTF();
                int status = SafeNativeMethods.Gdip.GdipGetLineRect(new HandleRef(this, NativeBrush), ref rect);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return rect.ToRectangleF();
            }
        }

        public bool GammaCorrection
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetLineGammaCorrection(new HandleRef(this, NativeBrush),
                                                       out bool useGammaCorrection);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return useGammaCorrection;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetLineGammaCorrection(new HandleRef(this, NativeBrush), value);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public Blend Blend
        {
            get
            {
                // Interpolation colors and blends don't work together very well. Getting the Blend when InterpolationColors
                // is set set puts the Brush into an unusable state afterwards.
                // Bail out here to avoid that.
                if (_interpolationColorsWasSet)
                {
                    return null;
                }

                // Figure out the size of blend factor array.
                int status = SafeNativeMethods.Gdip.GdipGetLineBlendCount(new HandleRef(this, NativeBrush), out int retval);
                SafeNativeMethods.Gdip.CheckStatus(status);

                if (retval <= 0)
                {
                    return null;
                }

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
                    status = SafeNativeMethods.Gdip.GdipGetLineBlend(new HandleRef(this, NativeBrush), factors, positions, count);
                    SafeNativeMethods.Gdip.CheckStatus(status);

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
                if (value == null || value.Factors == null)
                {
                    // This is the original behavior on Desktop .NET
                    throw new NullReferenceException();
                }

                if (value.Positions == null)
                {
                    throw new ArgumentNullException("source");
                }

                int count = value.Factors.Length;

                if (count == 0 || value.Positions.Length == 0)
                {
                    throw new ArgumentException(SR.BlendObjectMustHaveTwoElements);
                }

                if (count >=2 && count != value.Positions.Length)
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
                    int status = SafeNativeMethods.Gdip.GdipSetLineBlend(new HandleRef(this, NativeBrush), new HandleRef(null, factors), new HandleRef(null, positions), count);
                    SafeNativeMethods.Gdip.CheckStatus(status);
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

            int status = SafeNativeMethods.Gdip.GdipSetLineSigmaBlend(new HandleRef(this, NativeBrush), focus, scale);
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

            int status = SafeNativeMethods.Gdip.GdipSetLineLinearBlend(new HandleRef(this, NativeBrush), focus, scale);
            SafeNativeMethods.Gdip.CheckStatus(status);

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
                {
                    throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                                SR.Format(SR.InterpolationColorsColorBlendNotSet), string.Empty));
                }

                // Figure out the size of blend factor array.
                int status = SafeNativeMethods.Gdip.GdipGetLinePresetBlendCount(new HandleRef(this, NativeBrush), out int retval);
                SafeNativeMethods.Gdip.CheckStatus(status);

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
                    status = SafeNativeMethods.Gdip.GdipGetLinePresetBlend(new HandleRef(this, NativeBrush), colors, positions, count);
                    SafeNativeMethods.Gdip.CheckStatus(status);

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
                                                SR.Format(SR.InterpolationColorsInvalidColorBlendObject), string.Empty));
                }
                else if (value.Colors.Length < 2)
                {
                    throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                                SR.Format(SR.InterpolationColorsInvalidColorBlendObject),
                                                SR.Format(SR.InterpolationColorsLength)));
                }
                else if (value.Colors.Length != value.Positions.Length)
                {
                    throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                                SR.Format(SR.InterpolationColorsInvalidColorBlendObject),
                                                SR.Format(SR.InterpolationColorsLengthsDiffer)));
                }
                else if (value.Positions[0] != 0.0f)
                {
                    throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                                SR.Format(SR.InterpolationColorsInvalidColorBlendObject),
                                                SR.Format(SR.InterpolationColorsInvalidStartPosition)));
                }
                else if (value.Positions[value.Positions.Length - 1] != 1.0f)
                {
                    throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                                SR.Format(SR.InterpolationColorsInvalidColorBlendObject),
                                                SR.Format(SR.InterpolationColorsInvalidEndPosition)));
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
                    int status = SafeNativeMethods.Gdip.GdipSetLinePresetBlend(new HandleRef(this, NativeBrush), new HandleRef(null, colors), new HandleRef(null, positions), count);
                    SafeNativeMethods.Gdip.CheckStatus(status);
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
                int status = SafeNativeMethods.Gdip.GdipGetLineWrapMode(new HandleRef(this, NativeBrush), out int mode);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return (WrapMode)mode;
            }
            set
            {
                if (value < WrapMode.Tile || value > WrapMode.Clamp)
                {
                    throw new InvalidEnumArgumentException(nameof(value), unchecked((int)value), typeof(WrapMode));
                }

                int status = SafeNativeMethods.Gdip.GdipSetLineWrapMode(new HandleRef(this, NativeBrush), unchecked((int)value));
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public Matrix Transform
        {
            get
            {
                var matrix = new Matrix();
                int status = SafeNativeMethods.Gdip.GdipGetLineTransform(new HandleRef(this, NativeBrush), new HandleRef(matrix, matrix.nativeMatrix));
                SafeNativeMethods.Gdip.CheckStatus(status);

                return matrix;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                int status = SafeNativeMethods.Gdip.GdipSetLineTransform(new HandleRef(this, NativeBrush), new HandleRef(value, value.nativeMatrix));
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        public void ResetTransform()
        {
            int status = SafeNativeMethods.Gdip.GdipResetLineTransform(new HandleRef(this, NativeBrush));
            SafeNativeMethods.Gdip.CheckStatus(status);
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

            int status = SafeNativeMethods.Gdip.GdipMultiplyLineTransform(new HandleRef(this, NativeBrush),
                                                new HandleRef(matrix, matrix.nativeMatrix),
                                                order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateLineTransform(new HandleRef(this, NativeBrush),
                                                            dx,
                                                            dy,
                                                            order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipScaleLineTransform(new HandleRef(this, NativeBrush),
                                                        sx,
                                                        sy,
                                                        order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipRotateLineTransform(new HandleRef(this, NativeBrush),
                                                         angle,
                                                         order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }
    }
}

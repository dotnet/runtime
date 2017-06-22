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

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

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

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, LinearGradientMode linearGradientMode)
        {
            //validate the LinearGradientMode enum
            //valid values are 0x0 to 0x3
            if (!ClientUtils.IsEnumValid(linearGradientMode, unchecked((int)linearGradientMode), (int)LinearGradientMode.Horizontal, (int)LinearGradientMode.BackwardDiagonal))
            {
                throw new InvalidEnumArgumentException("linearGradientMode", unchecked((int)linearGradientMode), typeof(LinearGradientMode));
            }

            //validate the rect
            if (rect.Width == 0.0 || rect.Height == 0.0)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));
            }

            GPRECTF gprectf = new GPRECTF(rect);
            IntPtr nativeBrush;
            int status = SafeNativeMethods.Gdip.GdipCreateLineBrushFromRect(ref gprectf,
                                                             color1.ToArgb(),
                                                             color2.ToArgb(),
                                                             unchecked((int)linearGradientMode),
                                                             (int)WrapMode.Tile,
                                                             out nativeBrush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, LinearGradientMode linearGradientMode)
        {
            //validate the LinearGradientMode enum
            //valid values are 0x0 to 0x3
            if (!ClientUtils.IsEnumValid(linearGradientMode, unchecked((int)linearGradientMode), (int)LinearGradientMode.Horizontal, (int)LinearGradientMode.BackwardDiagonal))
            {
                throw new InvalidEnumArgumentException("linearGradientMode", unchecked((int)linearGradientMode), typeof(LinearGradientMode));
            }

            //validate the rect
            if (rect.Width == 0 || rect.Height == 0)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));
            }

            GPRECT gpRect = new GPRECT(rect);
            IntPtr nativeBrush;
            int status = SafeNativeMethods.Gdip.GdipCreateLineBrushFromRectI(ref gpRect,
                                                              color1.ToArgb(),
                                                              color2.ToArgb(),
                                                              unchecked((int)linearGradientMode),
                                                              (int)WrapMode.Tile,
                                                              out nativeBrush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, float angle) : this(rect, color1, color2, angle, false)
        {
        }

        public LinearGradientBrush(RectangleF rect, Color color1, Color color2, float angle, bool isAngleScaleable)
        {
            //validate the rect
            if (rect.Width == 0.0 || rect.Height == 0.0)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));
            }

            GPRECTF gprectf = new GPRECTF(rect);
            IntPtr nativeBrush;
            int status = SafeNativeMethods.Gdip.GdipCreateLineBrushFromRectWithAngle(ref gprectf,
                                                                      color1.ToArgb(),
                                                                      color2.ToArgb(),
                                                                      angle,
                                                                      isAngleScaleable,
                                                                      (int)WrapMode.Tile,
                                                                      out nativeBrush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            SetNativeBrushInternal(nativeBrush);
        }

        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, float angle) : this(rect, color1, color2, angle, false)
        {
        }

        public LinearGradientBrush(Rectangle rect, Color color1, Color color2, float angle, bool isAngleScaleable)
        {
            //validate the rect
            if (rect.Width == 0 || rect.Height == 0)
            {
                throw new ArgumentException(SR.Format(SR.GdiplusInvalidRectangle, rect.ToString()));
            }

            GPRECT gprect = new GPRECT(rect);
            IntPtr nativeBrush;
            int status = SafeNativeMethods.Gdip.GdipCreateLineBrushFromRectWithAngleI(ref gprect,
                                                                       color1.ToArgb(),
                                                                       color2.ToArgb(),
                                                                       angle,
                                                                       isAngleScaleable,
                                                                       (int)WrapMode.Tile,
                                                                       out nativeBrush);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

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

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new LinearGradientBrush(clonedBrush);
        }

        public Color[] LinearColors
        {
            get
            {
                int[] colors = new int[] { 0, 0 };
                int status = SafeNativeMethods.Gdip.GdipGetLineColors(new HandleRef(this, NativeBrush), colors);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

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

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        public RectangleF Rectangle
        {
            get
            {
                GPRECTF rect = new GPRECTF();

                int status = SafeNativeMethods.Gdip.GdipGetLineRect(new HandleRef(this, NativeBrush), ref rect);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return rect.ToRectangleF();
            }
        }

        public bool GammaCorrection
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetLineGammaCorrection(new HandleRef(this, NativeBrush),
                                                       out bool useGammaCorrection);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return useGammaCorrection;
            }
            set
            {
                int status = SafeNativeMethods.Gdip.GdipSetLineGammaCorrection(new HandleRef(this, NativeBrush), value);
                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        /**
         * Get/set blend factors
         *
         * @notes If the blendFactors.Length = 1, then it's treated
         *  as the falloff parameter. Otherwise, it's the array
         *  of blend factors.
         */

        private Blend _GetBlend()
        {
            // VSWHidbey 518309 - interpolation colors and blends don't get along.  Just getting
            // the Blend when InterpolationColors was set puts the Brush into an unusable state afterwards.
            // so to avoid that (mostly the problem of having Blend pop up in the debugger window and cause this problem)
            // we just bail here.
            //
            if (_interpolationColorsWasSet)
            {
                return null;
            }

            Blend blend;

            // Figure out the size of blend factor array
            int status = SafeNativeMethods.Gdip.GdipGetLineBlendCount(new HandleRef(this, NativeBrush), out int retval);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
            }
            if (retval <= 0)
            {
                return null;
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
                status = SafeNativeMethods.Gdip.GdipGetLineBlend(new HandleRef(this, NativeBrush), factors, positions, count);

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

                int status = SafeNativeMethods.Gdip.GdipSetLineBlend(new HandleRef(this, NativeBrush), new HandleRef(null, factors), new HandleRef(null, positions), count);

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
        public Blend Blend
        {
            get { return _GetBlend(); }
            set { _SetBlend(value); }
        }

        public void SetSigmaBellShape(float focus) => SetSigmaBellShape(focus, (float)1.0);

        public void SetSigmaBellShape(float focus, float scale)
        {
            int status = SafeNativeMethods.Gdip.GdipSetLineSigmaBlend(new HandleRef(this, NativeBrush), focus, scale);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void SetBlendTriangularShape(float focus) => SetBlendTriangularShape(focus, (float)1.0);

        public void SetBlendTriangularShape(float focus, float scale)
        {
            int status = SafeNativeMethods.Gdip.GdipSetLineLinearBlend(new HandleRef(this, NativeBrush), focus, scale);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        private ColorBlend _GetInterpolationColors()
        {
            ColorBlend blend;

            if (!_interpolationColorsWasSet)
            {
                throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                            SR.Format(SR.InterpolationColorsColorBlendNotSet), ""));
            }
            // Figure out the size of blend factor array

            int status = SafeNativeMethods.Gdip.GdipGetLinePresetBlendCount(new HandleRef(this, NativeBrush), out int retval);

            if (status != SafeNativeMethods.Gdip.Ok)
            {
                throw SafeNativeMethods.Gdip.StatusException(status);
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

                status = SafeNativeMethods.Gdip.GdipGetLinePresetBlend(new HandleRef(this, NativeBrush), colors, positions, count);

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
            _interpolationColorsWasSet = true;

            // Validate the ColorBlend object.
            if (blend == null)
            {
                throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                            SR.Format(SR.InterpolationColorsInvalidColorBlendObject), ""));
            }
            else if (blend.Colors.Length < 2)
            {
                throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                            SR.Format(SR.InterpolationColorsInvalidColorBlendObject),
                                            SR.Format(SR.InterpolationColorsLength)));
            }
            else if (blend.Colors.Length != blend.Positions.Length)
            {
                throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                            SR.Format(SR.InterpolationColorsInvalidColorBlendObject),
                                            SR.Format(SR.InterpolationColorsLengthsDiffer)));
            }
            else if (blend.Positions[0] != 0.0f)
            {
                throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                            SR.Format(SR.InterpolationColorsInvalidColorBlendObject),
                                            SR.Format(SR.InterpolationColorsInvalidStartPosition)));
            }
            else if (blend.Positions[blend.Positions.Length - 1] != 1.0f)
            {
                throw new ArgumentException(SR.Format(SR.InterpolationColorsCommon,
                                            SR.Format(SR.InterpolationColorsInvalidColorBlendObject),
                                            SR.Format(SR.InterpolationColorsInvalidEndPosition)));
            }


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

                int status = SafeNativeMethods.Gdip.GdipSetLinePresetBlend(new HandleRef(this, NativeBrush), new HandleRef(null, colors), new HandleRef(null, positions), count);

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

        public ColorBlend InterpolationColors
        {
            get => _GetInterpolationColors();
            set => _SetInterpolationColors(value);
        }

        public WrapMode WrapMode
        {
            get
            {
                int status = SafeNativeMethods.Gdip.GdipGetLineWrapMode(new HandleRef(this, NativeBrush), out int mode);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return (WrapMode)mode;
            }
            set
            {
                //validate the WrapMode enum
                //valid values are 0x0 to 0x4
                if (!ClientUtils.IsEnumValid(value, unchecked((int)value), (int)WrapMode.Tile, (int)WrapMode.Clamp))
                {
                    throw new InvalidEnumArgumentException("value", unchecked((int)value), typeof(WrapMode));
                }

                int status = SafeNativeMethods.Gdip.GdipSetLineWrapMode(new HandleRef(this, NativeBrush), unchecked((int)value));

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        public Matrix Transform
        {
            get
            {
                Matrix matrix = new Matrix();

                // NOTE: new Matrix() will throw an exception if matrix == null.

                int status = SafeNativeMethods.Gdip.GdipGetLineTransform(new HandleRef(this, NativeBrush), new HandleRef(matrix, matrix.nativeMatrix));

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return matrix;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("matrix");

                int status = SafeNativeMethods.Gdip.GdipSetLineTransform(new HandleRef(this, NativeBrush), new HandleRef(value, value.nativeMatrix));

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);
            }
        }

        public void ResetTransform()
        {
            int status = SafeNativeMethods.Gdip.GdipResetLineTransform(new HandleRef(this, NativeBrush));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void MultiplyTransform(Matrix matrix) => MultiplyTransform(matrix, MatrixOrder.Prepend);

        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            int status = SafeNativeMethods.Gdip.GdipMultiplyLineTransform(new HandleRef(this, NativeBrush),
                                                new HandleRef(matrix, matrix.nativeMatrix),
                                                order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void TranslateTransform(float dx, float dy) => TranslateTransform(dx, dy, MatrixOrder.Prepend);

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateLineTransform(new HandleRef(this, NativeBrush),
                                                            dx,
                                                            dy,
                                                            order);
            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void ScaleTransform(float sx, float sy) => ScaleTransform(sx, sy, MatrixOrder.Prepend);

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipScaleLineTransform(new HandleRef(this, NativeBrush),
                                                        sx,
                                                        sy,
                                                        order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        public void RotateTransform(float angle) => RotateTransform(angle, MatrixOrder.Prepend);

        public void RotateTransform(float angle, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipRotateLineTransform(new HandleRef(this, NativeBrush),
                                                         angle,
                                                         order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }
    }
}

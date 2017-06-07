// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Drawing2D.PathGradientBrush.cs
//
// Authors:
//   Dennis Hayes (dennish@Raytek.com)
//   Andreas Nahr (ClassDevelopment@A-SoftTech.com)
//   Ravindra (rkumar@novell.com)
//
// Copyright (C) 2002/3 Ximian, Inc. http://www.ximian.com
// Copyright (C) 2004,2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.ComponentModel;

namespace System.Drawing.Drawing2D
{

    [MonoTODO("libgdiplus/cairo doesn't support path gradients - unless it can be mapped to a radial gradient")]
    public sealed class PathGradientBrush : Brush
    {

        internal PathGradientBrush(IntPtr native)
        {
            SetNativeBrush(native);
        }

        public PathGradientBrush(GraphicsPath path)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            IntPtr nativeObject;
            Status status = GDIPlus.GdipCreatePathGradientFromPath(path.nativePath, out nativeObject);
            GDIPlus.CheckStatus(status);
            SetNativeBrush(nativeObject);
        }

        public PathGradientBrush(Point[] points) : this(points, WrapMode.Clamp)
        {
        }

        public PathGradientBrush(PointF[] points) : this(points, WrapMode.Clamp)
        {
        }

        public PathGradientBrush(Point[] points, WrapMode wrapMode)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            if ((wrapMode < WrapMode.Tile) || (wrapMode > WrapMode.Clamp))
                throw new InvalidEnumArgumentException("WrapMode");

            IntPtr nativeObject;
            Status status = GDIPlus.GdipCreatePathGradientI(points, points.Length, wrapMode, out nativeObject);
            GDIPlus.CheckStatus(status);
            SetNativeBrush(nativeObject);
        }

        public PathGradientBrush(PointF[] points, WrapMode wrapMode)
        {
            if (points == null)
                throw new ArgumentNullException("points");
            if ((wrapMode < WrapMode.Tile) || (wrapMode > WrapMode.Clamp))
                throw new InvalidEnumArgumentException("WrapMode");
            
            IntPtr nativeObject;
            Status status = GDIPlus.GdipCreatePathGradient(points, points.Length, wrapMode, out nativeObject);
            GDIPlus.CheckStatus(status);
            SetNativeBrush(nativeObject);
        }

        // Properties

        public Blend Blend
        {
            get
            {
                int count;
                Status status = GDIPlus.GdipGetPathGradientBlendCount(NativeBrush, out count);
                GDIPlus.CheckStatus(status);
                float[] factors = new float[count];
                float[] positions = new float[count];
                status = GDIPlus.GdipGetPathGradientBlend(NativeBrush, factors, positions, count);
                GDIPlus.CheckStatus(status);

                Blend blend = new Blend();
                blend.Factors = factors;
                blend.Positions = positions;

                return blend;
            }
            set
            {
                // no null check, MS throws a NullReferenceException here
                int count;
                float[] factors = value.Factors;
                float[] positions = value.Positions;
                count = factors.Length;

                if (count == 0 || positions.Length == 0)
                    throw new ArgumentException("Invalid Blend object. It should have at least 2 elements in each of the factors and positions arrays.");

                if (count != positions.Length)
                    throw new ArgumentException("Invalid Blend object. It should contain the same number of factors and positions values.");

                if (positions[0] != 0.0F)
                    throw new ArgumentException("Invalid Blend object. The positions array must have 0.0 as its first element.");

                if (positions[count - 1] != 1.0F)
                    throw new ArgumentException("Invalid Blend object. The positions array must have 1.0 as its last element.");

                Status status = GDIPlus.GdipSetPathGradientBlend(NativeBrush, factors, positions, count);
                GDIPlus.CheckStatus(status);
            }
        }

        public Color CenterColor
        {
            get
            {
                int centerColor;
                Status status = GDIPlus.GdipGetPathGradientCenterColor(NativeBrush, out centerColor);
                GDIPlus.CheckStatus(status);
                return Color.FromArgb(centerColor);
            }
            set
            {
                Status status = GDIPlus.GdipSetPathGradientCenterColor(NativeBrush, value.ToArgb());
                GDIPlus.CheckStatus(status);
            }
        }

        public PointF CenterPoint
        {
            get
            {
                PointF center;
                Status status = GDIPlus.GdipGetPathGradientCenterPoint(NativeBrush, out center);
                GDIPlus.CheckStatus(status);

                return center;
            }
            set
            {
                PointF center = value;
                Status status = GDIPlus.GdipSetPathGradientCenterPoint(NativeBrush, ref center);
                GDIPlus.CheckStatus(status);
            }
        }

        public PointF FocusScales
        {
            get
            {
                float xScale;
                float yScale;
                Status status = GDIPlus.GdipGetPathGradientFocusScales(NativeBrush, out xScale, out yScale);
                GDIPlus.CheckStatus(status);

                return new PointF(xScale, yScale);
            }
            set
            {
                Status status = GDIPlus.GdipSetPathGradientFocusScales(NativeBrush, value.X, value.Y);
                GDIPlus.CheckStatus(status);
            }
        }

        public ColorBlend InterpolationColors
        {
            get
            {
                int count;
                Status status = GDIPlus.GdipGetPathGradientPresetBlendCount(NativeBrush, out count);
                GDIPlus.CheckStatus(status);
                // if no failure, then the "managed" minimum is 1
                if (count < 1)
                    count = 1;

                int[] intcolors = new int[count];
                float[] positions = new float[count];
                // status would fail if we ask points or types with a < 2 count
                if (count > 1)
                {
                    status = GDIPlus.GdipGetPathGradientPresetBlend(NativeBrush, intcolors, positions, count);
                    GDIPlus.CheckStatus(status);
                }

                ColorBlend interpolationColors = new ColorBlend();
                Color[] colors = new Color[count];
                for (int i = 0; i < count; i++)
                    colors[i] = Color.FromArgb(intcolors[i]);
                interpolationColors.Colors = colors;
                interpolationColors.Positions = positions;

                return interpolationColors;
            }
            set
            {
                // no null check, MS throws a NullReferenceException here
                int count;
                Color[] colors = value.Colors;
                float[] positions = value.Positions;
                count = colors.Length;

                if (count == 0 || positions.Length == 0)
                    throw new ArgumentException("Invalid ColorBlend object. It should have at least 2 elements in each of the colors and positions arrays.");

                if (count != positions.Length)
                    throw new ArgumentException("Invalid ColorBlend object. It should contain the same number of positions and color values.");

                if (positions[0] != 0.0F)
                    throw new ArgumentException("Invalid ColorBlend object. The positions array must have 0.0 as its first element.");

                if (positions[count - 1] != 1.0F)
                    throw new ArgumentException("Invalid ColorBlend object. The positions array must have 1.0 as its last element.");

                int[] blend = new int[colors.Length];
                for (int i = 0; i < colors.Length; i++)
                    blend[i] = colors[i].ToArgb();

                Status status = GDIPlus.GdipSetPathGradientPresetBlend(NativeBrush, blend, positions, count);
                GDIPlus.CheckStatus(status);
            }
        }

        public RectangleF Rectangle
        {
            get
            {
                RectangleF rect;
                Status status = GDIPlus.GdipGetPathGradientRect(NativeBrush, out rect);
                GDIPlus.CheckStatus(status);

                return rect;
            }
        }

        public Color[] SurroundColors
        {
            get
            {
                int count;
                Status status = GDIPlus.GdipGetPathGradientSurroundColorCount(NativeBrush, out count);
                GDIPlus.CheckStatus(status);

                int[] intcolors = new int[count];
                status = GDIPlus.GdipGetPathGradientSurroundColorsWithCount(NativeBrush, intcolors, ref count);
                GDIPlus.CheckStatus(status);

                Color[] colors = new Color[count];
                for (int i = 0; i < count; i++)
                    colors[i] = Color.FromArgb(intcolors[i]);

                return colors;
            }
            set
            {
                // no null check, MS throws a NullReferenceException here
                int count = value.Length;
                int[] colors = new int[count];
                for (int i = 0; i < count; i++)
                    colors[i] = value[i].ToArgb();

                Status status = GDIPlus.GdipSetPathGradientSurroundColorsWithCount(NativeBrush, colors, ref count);
                GDIPlus.CheckStatus(status);
            }
        }

        public Matrix Transform
        {
            get
            {
                Matrix matrix = new Matrix();
                Status status = GDIPlus.GdipGetPathGradientTransform(NativeBrush, matrix.nativeMatrix);
                GDIPlus.CheckStatus(status);

                return matrix;
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Transform");

                Status status = GDIPlus.GdipSetPathGradientTransform(NativeBrush, value.nativeMatrix);
                GDIPlus.CheckStatus(status);
            }
        }

        public WrapMode WrapMode
        {
            get
            {
                WrapMode wrapMode;
                Status status = GDIPlus.GdipGetPathGradientWrapMode(NativeBrush, out wrapMode);
                GDIPlus.CheckStatus(status);

                return wrapMode;
            }
            set
            {
                if ((value < WrapMode.Tile) || (value > WrapMode.Clamp))
                    throw new InvalidEnumArgumentException("WrapMode");

                Status status = GDIPlus.GdipSetPathGradientWrapMode(NativeBrush, value);
                GDIPlus.CheckStatus(status);
            }
        }

        // Methods

        public void MultiplyTransform(Matrix matrix)
        {
            MultiplyTransform(matrix, MatrixOrder.Prepend);
        }

        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
                throw new ArgumentNullException("matrix");

            Status status = GDIPlus.GdipMultiplyPathGradientTransform(NativeBrush, matrix.nativeMatrix, order);
            GDIPlus.CheckStatus(status);
        }

        public void ResetTransform()
        {
            Status status = GDIPlus.GdipResetPathGradientTransform(NativeBrush);
            GDIPlus.CheckStatus(status);
        }

        public void RotateTransform(float angle)
        {
            RotateTransform(angle, MatrixOrder.Prepend);
        }

        public void RotateTransform(float angle, MatrixOrder order)
        {
            Status status = GDIPlus.GdipRotatePathGradientTransform(NativeBrush, angle, order);
            GDIPlus.CheckStatus(status);
        }

        public void ScaleTransform(float sx, float sy)
        {
            ScaleTransform(sx, sy, MatrixOrder.Prepend);
        }

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            Status status = GDIPlus.GdipScalePathGradientTransform(NativeBrush, sx, sy, order);
            GDIPlus.CheckStatus(status);
        }

        public void SetBlendTriangularShape(float focus)
        {
            SetBlendTriangularShape(focus, 1.0F);
        }

        public void SetBlendTriangularShape(float focus, float scale)
        {
            if (focus < 0 || focus > 1 || scale < 0 || scale > 1)
                throw new ArgumentException("Invalid parameter passed.");

            Status status = GDIPlus.GdipSetPathGradientLinearBlend(NativeBrush, focus, scale);
            GDIPlus.CheckStatus(status);
        }

        public void SetSigmaBellShape(float focus)
        {
            SetSigmaBellShape(focus, 1.0F);
        }

        public void SetSigmaBellShape(float focus, float scale)
        {
            if (focus < 0 || focus > 1 || scale < 0 || scale > 1)
                throw new ArgumentException("Invalid parameter passed.");

            Status status = GDIPlus.GdipSetPathGradientSigmaBlend(NativeBrush, focus, scale);
            GDIPlus.CheckStatus(status);
        }

        public void TranslateTransform(float dx, float dy)
        {
            TranslateTransform(dx, dy, MatrixOrder.Prepend);
        }

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            Status status = GDIPlus.GdipTranslatePathGradientTransform(NativeBrush, dx, dy, order);
            GDIPlus.CheckStatus(status);
        }

        public override object Clone()
        {
            IntPtr clonePtr;
            Status status = GDIPlus.GdipCloneBrush(NativeBrush, out clonePtr);
            GDIPlus.CheckStatus(status);

            PathGradientBrush clone = new PathGradientBrush(clonePtr);
            return clone;
        }
    }
}

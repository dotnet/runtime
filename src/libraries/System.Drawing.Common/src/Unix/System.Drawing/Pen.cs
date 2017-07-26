// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Pen.cs
//
// Authors:
//   Miguel de Icaza (miguel@ximian.com)
//   Alexandre Pigolkine (pigolkine@gmx.de)
//   Duncan Mak (duncan@ximian.com)
//   Ravindra (rkumar@novell.com)
//
// Copyright (C) Ximian, Inc.  http://www.ximian.com
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
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace System.Drawing
{
    public sealed class Pen : MarshalByRefObject, ICloneable, IDisposable
    {
        internal IntPtr nativeObject;
        internal bool isModifiable = true;
        private Color color;
        private CustomLineCap startCap;
        private CustomLineCap endCap;

        internal Pen(IntPtr p)
        {
            nativeObject = p;
        }

        public Pen(Brush brush) : this(brush, 1.0F)
        {
        }

        public Pen(Color color) : this(color, 1.0F)
        {
        }

        public Pen(Brush brush, float width)
        {
            if (brush == null)
                throw new ArgumentNullException("brush");

            Status status = SafeNativeMethods.Gdip.GdipCreatePen2(brush.NativeBrush, width, GraphicsUnit.World, out nativeObject);
            SafeNativeMethods.Gdip.CheckStatus(status);
            color = Color.Empty;
        }

        public Pen(Color color, float width)
        {
            Status status = SafeNativeMethods.Gdip.GdipCreatePen1(color.ToArgb(), width, GraphicsUnit.World, out nativeObject);
            SafeNativeMethods.Gdip.CheckStatus(status);
            this.color = color;
        }

        //
        // Properties
        //
        [MonoLimitation("Libgdiplus doesn't use this property for rendering")]
        public PenAlignment Alignment
        {
            get
            {
                PenAlignment retval;
                Status status = SafeNativeMethods.Gdip.GdipGetPenMode(nativeObject, out retval);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return retval;
            }

            set
            {
                if ((value < PenAlignment.Center) || (value > PenAlignment.Right))
                    throw new InvalidEnumArgumentException("Alignment", (int)value, typeof(PenAlignment));

                if (isModifiable)
                {
                    Status status = SafeNativeMethods.Gdip.GdipSetPenMode(nativeObject, value);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }
        }

        public Brush Brush
        {
            get
            {
                IntPtr brush;
                Status status = SafeNativeMethods.Gdip.GdipGetPenBrushFill(nativeObject, out brush);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return new SolidBrush(brush);
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("Brush");
                if (!isModifiable)
                    throw new ArgumentException("This Pen object can't be modified.");

                Status status = SafeNativeMethods.Gdip.GdipSetPenBrushFill(nativeObject, value.NativeBrush);
                SafeNativeMethods.Gdip.CheckStatus(status);
                color = Color.Empty;
            }
        }

        public Color Color
        {
            get
            {
                if (color.Equals(Color.Empty))
                {
                    int c;
                    Status status = SafeNativeMethods.Gdip.GdipGetPenColor(nativeObject, out c);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                    color = Color.FromArgb(c);
                }
                return color;
            }

            set
            {
                if (!isModifiable)
                    throw new ArgumentException("This Pen object can't be modified.");

                Status status = SafeNativeMethods.Gdip.GdipSetPenColor(nativeObject, value.ToArgb());
                SafeNativeMethods.Gdip.CheckStatus(status);
                color = value;
            }
        }

        public float[] CompoundArray
        {
            get
            {
                int count;
                Status status = SafeNativeMethods.Gdip.GdipGetPenCompoundCount(nativeObject, out count);
                SafeNativeMethods.Gdip.CheckStatus(status);

                float[] compArray = new float[count];
                status = SafeNativeMethods.Gdip.GdipGetPenCompoundArray(nativeObject, compArray, count);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return compArray;
            }

            set
            {
                if (isModifiable)
                {
                    int length = value.Length;
                    if (length < 2)
                        throw new ArgumentException("Invalid parameter.");
                    foreach (float val in value)
                        if (val < 0 || val > 1)
                            throw new ArgumentException("Invalid parameter.");

                    Status status = SafeNativeMethods.Gdip.GdipSetPenCompoundArray(nativeObject, value, value.Length);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }
        }

        public CustomLineCap CustomEndCap
        {
            get
            {
                return endCap;
            }

            set
            {
                if (isModifiable)
                {
                    Status status = SafeNativeMethods.Gdip.GdipSetPenCustomEndCap(nativeObject, value.nativeCap);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                    endCap = value;
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }
        }

        public CustomLineCap CustomStartCap
        {
            get
            {
                return startCap;
            }

            set
            {
                if (isModifiable)
                {
                    Status status = SafeNativeMethods.Gdip.GdipSetPenCustomStartCap(nativeObject, value.nativeCap);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                    startCap = value;
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }
        }

        public DashCap DashCap
        {

            get
            {
                DashCap retval;
                Status status = SafeNativeMethods.Gdip.GdipGetPenDashCap197819(nativeObject, out retval);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return retval;
            }

            set
            {
                if ((value < DashCap.Flat) || (value > DashCap.Triangle))
                    throw new InvalidEnumArgumentException("DashCap", (int)value, typeof(DashCap));

                if (isModifiable)
                {
                    Status status = SafeNativeMethods.Gdip.GdipSetPenDashCap197819(nativeObject, value);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }
        }

        public float DashOffset
        {

            get
            {
                float retval;
                Status status = SafeNativeMethods.Gdip.GdipGetPenDashOffset(nativeObject, out retval);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return retval;
            }

            set
            {
                if (isModifiable)
                {
                    Status status = SafeNativeMethods.Gdip.GdipSetPenDashOffset(nativeObject, value);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }
        }

        public float[] DashPattern
        {
            get
            {
                int count;
                Status status = SafeNativeMethods.Gdip.GdipGetPenDashCount(nativeObject, out count);
                SafeNativeMethods.Gdip.CheckStatus(status);

                float[] pattern;
                // don't call GdipGetPenDashArray with a 0 count
                if (count > 0)
                {
                    pattern = new float[count];
                    status = SafeNativeMethods.Gdip.GdipGetPenDashArray(nativeObject, pattern, count);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else if (DashStyle == DashStyle.Custom)
                {
                    // special case (not handled inside GDI+)
                    pattern = new float[1];
                    pattern[0] = 1.0f;
                }
                else
                {
                    pattern = new float[0];
                }
                return pattern;
            }

            set
            {
                if (isModifiable)
                {
                    int length = value.Length;
                    if (length == 0)
                        throw new ArgumentException("Invalid parameter.");
                    foreach (float val in value)
                        if (val <= 0)
                            throw new ArgumentException("Invalid parameter.");
                    Status status = SafeNativeMethods.Gdip.GdipSetPenDashArray(nativeObject, value, value.Length);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }
        }

        public DashStyle DashStyle
        {
            get
            {
                DashStyle retval;
                Status status = SafeNativeMethods.Gdip.GdipGetPenDashStyle(nativeObject, out retval);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return retval;
            }

            set
            {
                if ((value < DashStyle.Solid) || (value > DashStyle.Custom))
                    throw new InvalidEnumArgumentException("DashStyle", (int)value, typeof(DashStyle));

                if (isModifiable)
                {
                    Status status = SafeNativeMethods.Gdip.GdipSetPenDashStyle(nativeObject, value);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }
        }

        public LineCap StartCap
        {
            get
            {
                LineCap retval;
                Status status = SafeNativeMethods.Gdip.GdipGetPenStartCap(nativeObject, out retval);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return retval;
            }

            set
            {
                if ((value < LineCap.Flat) || (value > LineCap.Custom))
                    throw new InvalidEnumArgumentException("StartCap", (int)value, typeof(LineCap));

                if (isModifiable)
                {
                    Status status = SafeNativeMethods.Gdip.GdipSetPenStartCap(nativeObject, value);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }
        }

        public LineCap EndCap
        {
            get
            {
                LineCap retval;
                Status status = SafeNativeMethods.Gdip.GdipGetPenEndCap(nativeObject, out retval);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return retval;
            }

            set
            {
                if ((value < LineCap.Flat) || (value > LineCap.Custom))
                    throw new InvalidEnumArgumentException("EndCap", (int)value, typeof(LineCap));

                if (isModifiable)
                {
                    Status status = SafeNativeMethods.Gdip.GdipSetPenEndCap(nativeObject, value);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }
        }

        public LineJoin LineJoin
        {

            get
            {
                LineJoin result;
                Status status = SafeNativeMethods.Gdip.GdipGetPenLineJoin(nativeObject, out result);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return result;
            }

            set
            {
                if ((value < LineJoin.Miter) || (value > LineJoin.MiterClipped))
                    throw new InvalidEnumArgumentException("LineJoin", (int)value, typeof(LineJoin));

                if (isModifiable)
                {
                    Status status = SafeNativeMethods.Gdip.GdipSetPenLineJoin(nativeObject, value);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }

        }

        public float MiterLimit
        {

            get
            {
                float result;
                Status status = SafeNativeMethods.Gdip.GdipGetPenMiterLimit(nativeObject, out result);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return result;
            }

            set
            {
                if (isModifiable)
                {
                    Status status = SafeNativeMethods.Gdip.GdipSetPenMiterLimit(nativeObject, value);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }

        }

        public PenType PenType
        {
            get
            {
                PenType type;
                Status status = SafeNativeMethods.Gdip.GdipGetPenFillType(nativeObject, out type);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return type;
            }
        }

        public Matrix Transform
        {

            get
            {
                Matrix matrix = new Matrix();
                Status status = SafeNativeMethods.Gdip.GdipGetPenTransform(nativeObject, matrix.nativeMatrix);
                SafeNativeMethods.Gdip.CheckStatus(status);

                return matrix;
            }

            set
            {
                if (value == null)
                    throw new ArgumentNullException("Transform");

                if (isModifiable)
                {
                    Status status = SafeNativeMethods.Gdip.GdipSetPenTransform(nativeObject, value.nativeMatrix);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }
        }

        public float Width
        {
            get
            {
                float f;
                Status status = SafeNativeMethods.Gdip.GdipGetPenWidth(nativeObject, out f);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return f;
            }
            set
            {
                if (isModifiable)
                {
                    Status status = SafeNativeMethods.Gdip.GdipSetPenWidth(nativeObject, value);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                }
                else
                    throw new ArgumentException("This Pen object can't be modified.");
            }
        }

        internal IntPtr NativePen => nativeObject;

        public object Clone()
        {
            IntPtr ptr;
            Status status = SafeNativeMethods.Gdip.GdipClonePen(nativeObject, out ptr);
            SafeNativeMethods.Gdip.CheckStatus(status);
            Pen p = new Pen(ptr);
            p.startCap = startCap;
            p.endCap = endCap;

            return p;
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && !isModifiable)
                throw new ArgumentException("This Pen object can't be modified.");

            if (nativeObject != IntPtr.Zero)
            {
                Status status = SafeNativeMethods.Gdip.GdipDeletePen(nativeObject);
                nativeObject = IntPtr.Zero;
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
        }

        ~Pen()
        {
            Dispose(false);
        }

        public void MultiplyTransform(Matrix matrix)
        {
            MultiplyTransform(matrix, MatrixOrder.Prepend);
        }

        public void MultiplyTransform(Matrix matrix, MatrixOrder order)
        {
            Status status = SafeNativeMethods.Gdip.GdipMultiplyPenTransform(nativeObject, matrix.nativeMatrix, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ResetTransform()
        {
            Status status = SafeNativeMethods.Gdip.GdipResetPenTransform(nativeObject);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void RotateTransform(float angle)
        {
            RotateTransform(angle, MatrixOrder.Prepend);
        }

        public void RotateTransform(float angle, MatrixOrder order)
        {
            Status status = SafeNativeMethods.Gdip.GdipRotatePenTransform(nativeObject, angle, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void ScaleTransform(float sx, float sy)
        {
            ScaleTransform(sx, sy, MatrixOrder.Prepend);
        }

        public void ScaleTransform(float sx, float sy, MatrixOrder order)
        {
            Status status = SafeNativeMethods.Gdip.GdipScalePenTransform(nativeObject, sx, sy, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void SetLineCap(LineCap startCap, LineCap endCap, DashCap dashCap)
        {
            if (isModifiable)
            {
                Status status = SafeNativeMethods.Gdip.GdipSetPenLineCap197819(nativeObject, startCap, endCap, dashCap);
                SafeNativeMethods.Gdip.CheckStatus(status);
            }
            else
                throw new ArgumentException("This Pen object can't be modified.");
        }

        public void TranslateTransform(float dx, float dy)
        {
            TranslateTransform(dx, dy, MatrixOrder.Prepend);
        }

        public void TranslateTransform(float dx, float dy, MatrixOrder order)
        {
            Status status = SafeNativeMethods.Gdip.GdipTranslatePenTransform(nativeObject, dx, dy, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }
    }
}

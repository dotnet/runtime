// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// System.Drawing.Drawing2D.Matrix.cs
//
// Authors:
//   Stefan Maierhofer <sm@cg.tuwien.ac.at>
//   Dennis Hayes (dennish@Raytek.com)
//   Duncan Mak (duncan@ximian.com)
//   Ravindra (rkumar@novell.com)
//
// (C) Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004, 2006 Novell, Inc (http://www.novell.com)
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

using System.Runtime.InteropServices;

namespace System.Drawing.Drawing2D
{
    public sealed class Matrix : MarshalByRefObject, IDisposable
    {
        internal IntPtr nativeMatrix;

        // constructors
        internal Matrix(IntPtr ptr)
        {
            nativeMatrix = ptr;
        }

        public Matrix()
        {
            Status status = SafeNativeMethods.Gdip.GdipCreateMatrix(out nativeMatrix);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public Matrix(Rectangle rect, Point[] plgpts)
        {
            if (plgpts == null)
                throw new ArgumentNullException("plgpts");
            if (plgpts.Length != 3)
                throw new ArgumentException("plgpts");

            Status status = SafeNativeMethods.Gdip.GdipCreateMatrix3I(ref rect, plgpts, out nativeMatrix);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public Matrix(RectangleF rect, PointF[] plgpts)
        {
            if (plgpts == null)
                throw new ArgumentNullException("plgpts");
            if (plgpts.Length != 3)
                throw new ArgumentException("plgpts");

            Status status = SafeNativeMethods.Gdip.GdipCreateMatrix3(ref rect, plgpts, out nativeMatrix);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public Matrix(float m11, float m12, float m21, float m22, float dx, float dy)
        {
            Status status = SafeNativeMethods.Gdip.GdipCreateMatrix2(m11, m12, m21, m22, dx, dy, out nativeMatrix);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        // properties
        public float[] Elements
        {
            get
            {
                float[] retval = new float[6];
                IntPtr tmp = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(float)) * 6);
                try
                {
                    Status status = SafeNativeMethods.Gdip.GdipGetMatrixElements(nativeMatrix, tmp);
                    SafeNativeMethods.Gdip.CheckStatus(status);
                    Marshal.Copy(tmp, retval, 0, 6);
                }
                finally
                {
                    Marshal.FreeHGlobal(tmp);
                }
                return retval;
            }
        }

        public bool IsIdentity
        {
            get
            {
                bool retval;
                Status status = SafeNativeMethods.Gdip.GdipIsMatrixIdentity(nativeMatrix, out retval);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return retval;
            }
        }

        public bool IsInvertible
        {
            get
            {
                bool retval;
                Status status = SafeNativeMethods.Gdip.GdipIsMatrixInvertible(nativeMatrix, out retval);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return retval;
            }
        }

        public float OffsetX
        {
            get
            {
                return this.Elements[4];
            }
        }

        public float OffsetY
        {
            get
            {
                return this.Elements[5];
            }
        }

        public Matrix Clone()
        {
            IntPtr retval;
            Status status = SafeNativeMethods.Gdip.GdipCloneMatrix(nativeMatrix, out retval);
            SafeNativeMethods.Gdip.CheckStatus(status);
            return new Matrix(retval);
        }


        public void Dispose()
        {
            if (nativeMatrix != IntPtr.Zero)
            {
                Status status = SafeNativeMethods.Gdip.GdipDeleteMatrix(nativeMatrix);
                SafeNativeMethods.Gdip.CheckStatus(status);
                nativeMatrix = IntPtr.Zero;
            }

            GC.SuppressFinalize(this);
        }

        public override bool Equals(object obj)
        {
            Matrix m = obj as Matrix;

            if (m != null)
            {
                bool retval;
                Status status = SafeNativeMethods.Gdip.GdipIsMatrixEqual(nativeMatrix, m.nativeMatrix, out retval);
                SafeNativeMethods.Gdip.CheckStatus(status);
                return retval;

            }
            else
                return false;
        }

        ~Matrix()
        {
            Dispose();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void Invert()
        {
            Status status = SafeNativeMethods.Gdip.GdipInvertMatrix(nativeMatrix);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Multiply(Matrix matrix)
        {
            Multiply(matrix, MatrixOrder.Prepend);
        }

        public void Multiply(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
                throw new ArgumentNullException("matrix");

            Status status = SafeNativeMethods.Gdip.GdipMultiplyMatrix(nativeMatrix, matrix.nativeMatrix, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Reset()
        {
            Status status = SafeNativeMethods.Gdip.GdipSetMatrixElements(nativeMatrix, 1, 0, 0, 1, 0, 0);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Rotate(float angle)
        {
            Rotate(angle, MatrixOrder.Prepend);
        }

        public void Rotate(float angle, MatrixOrder order)
        {
            Status status = SafeNativeMethods.Gdip.GdipRotateMatrix(nativeMatrix, angle, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void RotateAt(float angle, PointF point)
        {
            RotateAt(angle, point, MatrixOrder.Prepend);
        }

        public void RotateAt(float angle, PointF point, MatrixOrder order)
        {
            if ((order < MatrixOrder.Prepend) || (order > MatrixOrder.Append))
                throw new ArgumentException("order");

            angle *= (float)(Math.PI / 180.0);  // degrees to radians
            float cos = (float)Math.Cos(angle);
            float sin = (float)Math.Sin(angle);
            float e4 = -point.X * cos + point.Y * sin + point.X;
            float e5 = -point.X * sin - point.Y * cos + point.Y;
            float[] m = this.Elements;

            Status status;

            if (order == MatrixOrder.Prepend)
                status = SafeNativeMethods.Gdip.GdipSetMatrixElements(nativeMatrix,
                                cos * m[0] + sin * m[2],
                                cos * m[1] + sin * m[3],
                                -sin * m[0] + cos * m[2],
                                -sin * m[1] + cos * m[3],
                                e4 * m[0] + e5 * m[2] + m[4],
                                e4 * m[1] + e5 * m[3] + m[5]);
            else
                status = SafeNativeMethods.Gdip.GdipSetMatrixElements(nativeMatrix,
                                m[0] * cos + m[1] * -sin,
                                m[0] * sin + m[1] * cos,
                                m[2] * cos + m[3] * -sin,
                                m[2] * sin + m[3] * cos,
                                m[4] * cos + m[5] * -sin + e4,
                                m[4] * sin + m[5] * cos + e5);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Scale(float scaleX, float scaleY)
        {
            Scale(scaleX, scaleY, MatrixOrder.Prepend);
        }

        public void Scale(float scaleX, float scaleY, MatrixOrder order)
        {
            Status status = SafeNativeMethods.Gdip.GdipScaleMatrix(nativeMatrix, scaleX, scaleY, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Shear(float shearX, float shearY)
        {
            Shear(shearX, shearY, MatrixOrder.Prepend);
        }

        public void Shear(float shearX, float shearY, MatrixOrder order)
        {
            Status status = SafeNativeMethods.Gdip.GdipShearMatrix(nativeMatrix, shearX, shearY, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TransformPoints(Point[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");

            Status status = SafeNativeMethods.Gdip.GdipTransformMatrixPointsI(nativeMatrix, pts, pts.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TransformPoints(PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");

            Status status = SafeNativeMethods.Gdip.GdipTransformMatrixPoints(nativeMatrix, pts, pts.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TransformVectors(Point[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");

            Status status = SafeNativeMethods.Gdip.GdipVectorTransformMatrixPointsI(nativeMatrix, pts, pts.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void TransformVectors(PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");

            Status status = SafeNativeMethods.Gdip.GdipVectorTransformMatrixPoints(nativeMatrix, pts, pts.Length);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void Translate(float offsetX, float offsetY)
        {
            Translate(offsetX, offsetY, MatrixOrder.Prepend);
        }

        public void Translate(float offsetX, float offsetY, MatrixOrder order)
        {
            Status status = SafeNativeMethods.Gdip.GdipTranslateMatrix(nativeMatrix, offsetX, offsetY, order);
            SafeNativeMethods.Gdip.CheckStatus(status);
        }

        public void VectorTransformPoints(Point[] pts)
        {
            TransformVectors(pts);
        }

        internal IntPtr NativeObject
        {
            get
            {
                return nativeMatrix;
            }
            set
            {
                nativeMatrix = value;
            }
        }
    }
}

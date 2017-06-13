// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing.Drawing2D
{
    using System.Runtime.InteropServices;
    using System.Drawing.Internal;

    /**
     * Represent a Matrix object
     */
    /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix"]/*' />
    /// <devdoc>
    ///    Encapsulates a 3 X 3 affine matrix that
    ///    represents a geometric transform.
    /// </devdoc>
    public sealed class Matrix : MarshalByRefObject, IDisposable
    {
        internal IntPtr nativeMatrix;

        /*
         * Create a new identity matrix
         */

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Matrix"]/*' />
        /// <devdoc>
        ///    Initializes a new instance of the <see cref='System.Drawing.Drawing2D.Matrix'/> class.
        /// </devdoc>
        public Matrix()
        {
            IntPtr matrix = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateMatrix(out matrix);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            nativeMatrix = matrix;
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Matrix1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initialized a new instance of the <see cref='System.Drawing.Drawing2D.Matrix'/> class with the specified
        ///       elements.
        ///    </para>
        /// </devdoc>
        public Matrix(float m11,
                      float m12,
                      float m21,
                      float m22,
                      float dx,
                      float dy)
        {
            IntPtr matrix = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCreateMatrix2(m11, m12, m21, m22, dx, dy,
                                                   out matrix);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            nativeMatrix = matrix;
        }

        // float version
        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Matrix2"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.Matrix'/> class to the geometrical transform
        ///       defined by the specified rectangle and array of points.
        ///    </para>
        /// </devdoc>
        public Matrix(RectangleF rect, PointF[] plgpts)
        {
            if (plgpts == null)
            {
                throw new ArgumentNullException("plgpts");
            }
            if (plgpts.Length != 3)
            {
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);
            }

            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(plgpts);

            try
            {
                IntPtr matrix = IntPtr.Zero;

                GPRECTF gprectf = new GPRECTF(rect);
                int status = SafeNativeMethods.Gdip.GdipCreateMatrix3(ref gprectf, new HandleRef(null, buf), out matrix);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                nativeMatrix = matrix;
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        // int version
        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Matrix3"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.Drawing.Drawing2D.Matrix'/> class to the geometrical transform
        ///       defined by the specified rectangle and array of points.
        ///    </para>
        /// </devdoc>
        public Matrix(Rectangle rect, Point[] plgpts)
        {
            if (plgpts == null)
            {
                throw new ArgumentNullException("plgpts");
            }
            if (plgpts.Length != 3)
            {
                throw SafeNativeMethods.Gdip.StatusException(SafeNativeMethods.Gdip.InvalidParameter);
            }

            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(plgpts);

            try
            {
                IntPtr matrix = IntPtr.Zero;

                GPRECT gprect = new GPRECT(rect);
                int status = SafeNativeMethods.Gdip.GdipCreateMatrix3I(ref gprect, new HandleRef(null, buf), out matrix);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                nativeMatrix = matrix;
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Dispose"]/*' />
        /// <devdoc>
        ///    Cleans up resources allocated for this
        /// <see cref='System.Drawing.Drawing2D.Matrix'/>.
        /// </devdoc>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (nativeMatrix != IntPtr.Zero)
            {
                SafeNativeMethods.Gdip.GdipDeleteMatrix(new HandleRef(this, nativeMatrix));
                nativeMatrix = IntPtr.Zero;
            }
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Finalize"]/*' />
        /// <devdoc>
        ///    Cleans up resources allocated for this
        /// <see cref='System.Drawing.Drawing2D.Matrix'/>.
        /// </devdoc>
        ~Matrix()
        {
            Dispose(false);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Clone"]/*' />
        /// <devdoc>
        ///    Creates an exact copy of this <see cref='System.Drawing.Drawing2D.Matrix'/>.
        /// </devdoc>
        public Matrix Clone()
        {
            IntPtr cloneMatrix = IntPtr.Zero;

            int status = SafeNativeMethods.Gdip.GdipCloneMatrix(new HandleRef(this, nativeMatrix), out cloneMatrix);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return new Matrix(cloneMatrix);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Elements"]/*' />
        /// <devdoc>
        ///    Gets an array of floating-point values that
        ///    represent the elements of this <see cref='System.Drawing.Drawing2D.Matrix'/>.
        /// </devdoc>
        public float[] Elements
        {
            get
            {
                float[] m;

                IntPtr buf = Marshal.AllocHGlobal(6 * 8); // 6 elements x 8 bytes (float)

                try
                {
                    int status = SafeNativeMethods.Gdip.GdipGetMatrixElements(new HandleRef(this, nativeMatrix), buf);

                    if (status != SafeNativeMethods.Gdip.Ok)
                    {
                        throw SafeNativeMethods.Gdip.StatusException(status);
                    }

                    m = new float[6];

                    Marshal.Copy(buf, m, 0, 6);
                }
                finally
                {
                    Marshal.FreeHGlobal(buf);
                }

                return m;
            }
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.OffsetX"]/*' />
        /// <devdoc>
        ///    Gets the x translation value (the dx value,
        ///    or the element in the third row and first column) of this <see cref='System.Drawing.Drawing2D.Matrix'/>.
        /// </devdoc>
        public float OffsetX
        {
            get { return Elements[4]; }
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.OffsetY"]/*' />
        /// <devdoc>
        ///    Gets the y translation value (the dy
        ///    value, or the element in the third row and second column) of this <see cref='System.Drawing.Drawing2D.Matrix'/>.
        /// </devdoc>
        public float OffsetY
        {
            get { return Elements[5]; }
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Reset"]/*' />
        /// <devdoc>
        ///    Resets this <see cref='System.Drawing.Drawing2D.Matrix'/> to identity.
        /// </devdoc>
        public void Reset()
        {
            int status = SafeNativeMethods.Gdip.GdipSetMatrixElements(new HandleRef(this, nativeMatrix),
                                                       1.0f, 0.0f, 0.0f,
                                                       1.0f, 0.0f, 0.0f);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Multiply"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Multiplies this <see cref='System.Drawing.Drawing2D.Matrix'/> by the specified <see cref='System.Drawing.Drawing2D.Matrix'/> by prepending the specified <see cref='System.Drawing.Drawing2D.Matrix'/>.
        ///    </para>
        /// </devdoc>
        public void Multiply(Matrix matrix)
        {
            Multiply(matrix, MatrixOrder.Prepend);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Multiply1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Multiplies this <see cref='System.Drawing.Drawing2D.Matrix'/> by the specified <see cref='System.Drawing.Drawing2D.Matrix'/> in the specified order.
        ///    </para>
        /// </devdoc>
        public void Multiply(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException("matrix");
            }

            int status = SafeNativeMethods.Gdip.GdipMultiplyMatrix(new HandleRef(this, nativeMatrix), new HandleRef(matrix, matrix.nativeMatrix),
                                                    order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Translate"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Applies the specified translation vector to
        ///       the this <see cref='System.Drawing.Drawing2D.Matrix'/> by
        ///       prepending the translation vector.
        ///    </para>
        /// </devdoc>
        public void Translate(float offsetX, float offsetY)
        {
            Translate(offsetX, offsetY, MatrixOrder.Prepend);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Translate1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Applies the specified translation vector to
        ///       the this <see cref='System.Drawing.Drawing2D.Matrix'/> in the specified order.
        ///    </para>
        /// </devdoc>
        public void Translate(float offsetX, float offsetY, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipTranslateMatrix(new HandleRef(this, nativeMatrix),
                                                     offsetX, offsetY, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Scale"]/*' />
        /// <devdoc>
        ///    Applies the specified scale vector to this
        /// <see cref='System.Drawing.Drawing2D.Matrix'/> by prepending the scale vector.
        /// </devdoc>
        public void Scale(float scaleX, float scaleY)
        {
            Scale(scaleX, scaleY, MatrixOrder.Prepend);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Scale1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Applies the specified scale vector to this
        ///    <see cref='System.Drawing.Drawing2D.Matrix'/> using the specified order.
        ///    </para>
        /// </devdoc>
        public void Scale(float scaleX, float scaleY, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipScaleMatrix(new HandleRef(this, nativeMatrix), scaleX, scaleY, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Rotate"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Rotates this <see cref='System.Drawing.Drawing2D.Matrix'/> clockwise about the
        ///       origin by the specified angle.
        ///    </para>
        /// </devdoc>
        public void Rotate(float angle)
        {
            Rotate(angle, MatrixOrder.Prepend);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Rotate1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Rotates this <see cref='System.Drawing.Drawing2D.Matrix'/> clockwise about the
        ///       origin by the specified
        ///       angle in the specified order.
        ///    </para>
        /// </devdoc>
        public void Rotate(float angle, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipRotateMatrix(new HandleRef(this, nativeMatrix), angle, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.RotateAt"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Applies a clockwise rotation about the
        ///       specified point to this <see cref='System.Drawing.Drawing2D.Matrix'/> by prepending the rotation.
        ///    </para>
        /// </devdoc>
        public void RotateAt(float angle, PointF point)
        {
            RotateAt(angle, point, MatrixOrder.Prepend);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.RotateAt1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Applies a clockwise rotation about the specified point
        ///       to this <see cref='System.Drawing.Drawing2D.Matrix'/> in the
        ///       specified order.
        ///    </para>
        /// </devdoc>
        public void RotateAt(float angle, PointF point, MatrixOrder order)
        {
            int status;

            // !! TO DO: We cheat with error codes here...
            if (order == MatrixOrder.Prepend)
            {
                status = SafeNativeMethods.Gdip.GdipTranslateMatrix(new HandleRef(this, nativeMatrix), point.X, point.Y, order);
                status |= SafeNativeMethods.Gdip.GdipRotateMatrix(new HandleRef(this, nativeMatrix), angle, order);
                status |= SafeNativeMethods.Gdip.GdipTranslateMatrix(new HandleRef(this, nativeMatrix), -point.X, -point.Y, order);
            }
            else
            {
                status = SafeNativeMethods.Gdip.GdipTranslateMatrix(new HandleRef(this, nativeMatrix), -point.X, -point.Y, order);
                status |= SafeNativeMethods.Gdip.GdipRotateMatrix(new HandleRef(this, nativeMatrix), angle, order);
                status |= SafeNativeMethods.Gdip.GdipTranslateMatrix(new HandleRef(this, nativeMatrix), point.X, point.Y, order);
            }

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Shear"]/*' />
        /// <devdoc>
        ///    Applies the specified shear
        ///    vector to this <see cref='System.Drawing.Drawing2D.Matrix'/> by prepending the shear vector.
        /// </devdoc>
        public void Shear(float shearX, float shearY)
        {
            int status = SafeNativeMethods.Gdip.GdipShearMatrix(new HandleRef(this, nativeMatrix), shearX, shearY, MatrixOrder.Prepend);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Shear1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Applies the specified shear
        ///       vector to this <see cref='System.Drawing.Drawing2D.Matrix'/> in the specified order.
        ///    </para>
        /// </devdoc>
        public void Shear(float shearX, float shearY, MatrixOrder order)
        {
            int status = SafeNativeMethods.Gdip.GdipShearMatrix(new HandleRef(this, nativeMatrix), shearX, shearY, order);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Invert"]/*' />
        /// <devdoc>
        ///    Inverts this <see cref='System.Drawing.Drawing2D.Matrix'/>, if it is
        ///    invertible.
        /// </devdoc>
        public void Invert()
        {
            int status = SafeNativeMethods.Gdip.GdipInvertMatrix(new HandleRef(this, nativeMatrix));

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);
        }

        // float version
        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.TransformPoints"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Applies the geometrical transform this <see cref='System.Drawing.Drawing2D.Matrix'/>represents to an
        ///       array of points.
        ///    </para>
        /// </devdoc>
        public void TransformPoints(PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(pts);

            try
            {
                int status = SafeNativeMethods.Gdip.GdipTransformMatrixPoints(new HandleRef(this, nativeMatrix),
                    new HandleRef(null, buf),
                    pts.Length);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                PointF[] newPts = SafeNativeMethods.Gdip.ConvertGPPOINTFArrayF(buf, pts.Length);

                for (int i = 0; i < pts.Length; i++)
                {
                    pts[i] = newPts[i];
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        // int version
        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.TransformPoints1"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Applies the geometrical transform this <see cref='System.Drawing.Drawing2D.Matrix'/> represents to an array of points.
        ///    </para>
        /// </devdoc>
        public void TransformPoints(Point[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException("pts");
            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(pts);

            try
            {
                int status = SafeNativeMethods.Gdip.GdipTransformMatrixPointsI(new HandleRef(this, nativeMatrix),
                    new HandleRef(null, buf),
                    pts.Length);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                // must do an in-place copy because we only have a reference
                Point[] newPts = SafeNativeMethods.Gdip.ConvertGPPOINTArray(buf, pts.Length);

                for (int i = 0; i < pts.Length; i++)
                {
                    pts[i] = newPts[i];
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.TransformVectors"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void TransformVectors(PointF[] pts)
        {
            if (pts == null)
            {
                throw new ArgumentNullException("pts");
            }

            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(pts);

            try
            {
                int status = SafeNativeMethods.Gdip.GdipVectorTransformMatrixPoints(new HandleRef(this, nativeMatrix),
                    new HandleRef(null, buf),
                    pts.Length);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                // must do an in-place copy because we only have a reference
                PointF[] newPts = SafeNativeMethods.Gdip.ConvertGPPOINTFArrayF(buf, pts.Length);

                for (int i = 0; i < pts.Length; i++)
                {
                    pts[i] = newPts[i];
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        // int version
        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.VectorTransformPoints"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void VectorTransformPoints(Point[] pts)
        {
            TransformVectors(pts);
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.TransformVectors1"]/*' />
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public void TransformVectors(Point[] pts)
        {
            if (pts == null)
            {
                throw new ArgumentNullException("pts");
            }

            IntPtr buf = SafeNativeMethods.Gdip.ConvertPointToMemory(pts);

            try
            {
                int status = SafeNativeMethods.Gdip.GdipVectorTransformMatrixPointsI(new HandleRef(this, nativeMatrix),
                    new HandleRef(null, buf),
                    pts.Length);

                if (status != SafeNativeMethods.Gdip.Ok)
                {
                    throw SafeNativeMethods.Gdip.StatusException(status);
                }

                // must do an in-place copy because we only have a reference
                Point[] newPts = SafeNativeMethods.Gdip.ConvertGPPOINTArray(buf, pts.Length);

                for (int i = 0; i < pts.Length; i++)
                {
                    pts[i] = newPts[i];
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buf);
            }
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.IsInvertible"]/*' />
        /// <devdoc>
        ///    Gets a value indicating whether this
        /// <see cref='System.Drawing.Drawing2D.Matrix'/> is invertible.
        /// </devdoc>
        public bool IsInvertible
        {
            get
            {
                int isInvertible;

                int status = SafeNativeMethods.Gdip.GdipIsMatrixInvertible(new HandleRef(this, nativeMatrix), out isInvertible);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return isInvertible != 0;
            }
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.IsIdentity"]/*' />
        /// <devdoc>
        ///    Gets a value indicating whether this <see cref='System.Drawing.Drawing2D.Matrix'/> is the identity matrix.
        /// </devdoc>
        public bool IsIdentity
        {
            get
            {
                int isIdentity;

                int status = SafeNativeMethods.Gdip.GdipIsMatrixIdentity(new HandleRef(this, nativeMatrix), out isIdentity);

                if (status != SafeNativeMethods.Gdip.Ok)
                    throw SafeNativeMethods.Gdip.StatusException(status);

                return isIdentity != 0;
            }
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.Equals"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Tests whether the specified object is a
        ///    <see cref='System.Drawing.Drawing2D.Matrix'/> and is identical to this <see cref='System.Drawing.Drawing2D.Matrix'/>.
        ///    </para>
        /// </devdoc>
        public override bool Equals(object obj)
        {
            Matrix matrix2 = obj as Matrix;
            if (matrix2 == null) return false;

            int isEqual;

            int status = SafeNativeMethods.Gdip.GdipIsMatrixEqual(new HandleRef(this, nativeMatrix),
                                                   new HandleRef(matrix2, matrix2.nativeMatrix),
                                                   out isEqual);

            if (status != SafeNativeMethods.Gdip.Ok)
                throw SafeNativeMethods.Gdip.StatusException(status);

            return isEqual != 0;
        }

        /// <include file='doc\Matrix.uex' path='docs/doc[@for="Matrix.GetHashCode"]/*' />
        /// <devdoc>
        ///    <para>
        ///       Returns a hash code.
        ///    </para>
        /// </devdoc>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        internal Matrix(IntPtr nativeMatrix)
        {
            SetNativeMatrix(nativeMatrix);
        }

        internal void SetNativeMatrix(IntPtr nativeMatrix)
        {
            this.nativeMatrix = nativeMatrix;
        }
    }
}

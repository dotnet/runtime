// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Gdip = System.Drawing.SafeNativeMethods.Gdip;

namespace System.Drawing.Drawing2D
{
    public sealed class Matrix : MarshalByRefObject, IDisposable
    {
        private readonly SafeMatrixHandle _nativeMatrix;

        internal SafeMatrixHandle SafeMatrixHandle => _nativeMatrix;

        public Matrix()
        {
            Gdip.CheckStatus(Gdip.GdipCreateMatrix(out _nativeMatrix));
        }

        public Matrix(float m11, float m12, float m21, float m22, float dx, float dy)
        {
            Gdip.CheckStatus(Gdip.GdipCreateMatrix2(m11, m12, m21, m22, dx, dy, out _nativeMatrix));
        }

        /// <summary>
        /// Construct a <see cref="Matrix"/> utilizing the given <paramref name="matrix"/>.
        /// </summary>
        /// <param name="matrix">Matrix data to construct from.</param>
        public Matrix(Matrix3x2 matrix) : this(CreateNativeHandle(matrix))
        {
        }

        private Matrix(SafeMatrixHandle nativeMatrix)
        {
            _nativeMatrix = nativeMatrix;
        }

        internal static SafeMatrixHandle CreateNativeHandle(Matrix3x2 matrix)
        {
            Gdip.CheckStatus(Gdip.GdipCreateMatrix2(
                matrix.M11,
                matrix.M12,
                matrix.M21,
                matrix.M22,
                matrix.M31,
                matrix.M32,
                out SafeMatrixHandle nativeMatrix));

            return nativeMatrix;
        }

        public unsafe Matrix(RectangleF rect, PointF[] plgpts)
        {
            if (plgpts == null)
                throw new ArgumentNullException(nameof(plgpts));
            if (plgpts.Length != 3)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            fixed (PointF* p = plgpts)
            {
                Gdip.CheckStatus(Gdip.GdipCreateMatrix3(ref rect, p, out _nativeMatrix));
            }
        }

        public unsafe Matrix(Rectangle rect, Point[] plgpts)
        {
            if (plgpts == null)
                throw new ArgumentNullException(nameof(plgpts));
            if (plgpts.Length != 3)
                throw Gdip.StatusException(Gdip.InvalidParameter);

            fixed (Point* p = plgpts)
            {
                Gdip.CheckStatus(Gdip.GdipCreateMatrix3I(ref rect, p, out _nativeMatrix));
            }
        }

        public void Dispose()
        {
            SafeMatrixHandle.Dispose();
        }

        public Matrix Clone()
        {
            Gdip.CheckStatus(Gdip.GdipCloneMatrix(SafeMatrixHandle, out SafeMatrixHandle clonedMatrix));
            return new Matrix(clonedMatrix);
        }

        public float[] Elements
        {
            get
            {
                float[] elements = new float[6];
                GetElements(elements);
                return elements;
            }
        }

        /// <summary>
        ///  Gets/sets the elements for the matrix.
        /// </summary>
        public unsafe Matrix3x2 MatrixElements
        {
            get
            {
                Matrix3x2 matrix = default;
                Gdip.CheckStatus(Gdip.GdipGetMatrixElements(SafeMatrixHandle, (float*)&matrix));
                return matrix;
            }
            set
            {
                Gdip.CheckStatus(Gdip.GdipSetMatrixElements(
                    SafeMatrixHandle,
                    value.M11,
                    value.M12,
                    value.M21,
                    value.M22,
                    value.M31,
                    value.M32));
            }
        }

        internal unsafe void GetElements(Span<float> elements)
        {
            Debug.Assert(elements.Length >= 6);

            fixed (float* m = elements)
            {
                Gdip.CheckStatus(Gdip.GdipGetMatrixElements(SafeMatrixHandle, m));
            }
        }

        public unsafe float OffsetX => Offset.X;

        public unsafe float OffsetY => Offset.Y;

        internal unsafe PointF Offset
        {
            get
            {
                Span<float> elements = stackalloc float[6];
                GetElements(elements);
                return new PointF(elements[4], elements[5]);
            }
        }

        public void Reset()
        {
            Gdip.CheckStatus(Gdip.GdipSetMatrixElements(
                SafeMatrixHandle,
                1.0f, 0.0f, 0.0f,
                1.0f, 0.0f, 0.0f));
        }

        public void Multiply(Matrix matrix) => Multiply(matrix, MatrixOrder.Prepend);

        public void Multiply(Matrix matrix, MatrixOrder order)
        {
            if (matrix == null)
                throw new ArgumentNullException(nameof(matrix));
            if (matrix.SafeMatrixHandle.HasEqualHandle(SafeMatrixHandle))
                throw new InvalidOperationException(SR.GdiplusObjectBusy);

            Gdip.CheckStatus(Gdip.GdipMultiplyMatrix(
                SafeMatrixHandle,
                matrix.SafeMatrixHandle,
                order));
        }

        public void Translate(float offsetX, float offsetY) => Translate(offsetX, offsetY, MatrixOrder.Prepend);

        public void Translate(float offsetX, float offsetY, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipTranslateMatrix(
                SafeMatrixHandle,
                offsetX, offsetY, order));
        }

        public void Scale(float scaleX, float scaleY) => Scale(scaleX, scaleY, MatrixOrder.Prepend);

        public void Scale(float scaleX, float scaleY, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipScaleMatrix(SafeMatrixHandle, scaleX, scaleY, order));
        }

        public void Rotate(float angle) => Rotate(angle, MatrixOrder.Prepend);

        public void Rotate(float angle, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipRotateMatrix(SafeMatrixHandle, angle, order));
        }

        public void RotateAt(float angle, PointF point) => RotateAt(angle, point, MatrixOrder.Prepend);
        public void RotateAt(float angle, PointF point, MatrixOrder order)
        {
            int status;
            if (order == MatrixOrder.Prepend)
            {
                status = Gdip.GdipTranslateMatrix(SafeMatrixHandle, point.X, point.Y, order);
                status |= Gdip.GdipRotateMatrix(SafeMatrixHandle, angle, order);
                status |= Gdip.GdipTranslateMatrix(SafeMatrixHandle, -point.X, -point.Y, order);
            }
            else
            {
                status = Gdip.GdipTranslateMatrix(SafeMatrixHandle, -point.X, -point.Y, order);
                status |= Gdip.GdipRotateMatrix(SafeMatrixHandle, angle, order);
                status |= Gdip.GdipTranslateMatrix(SafeMatrixHandle, point.X, point.Y, order);
            }

            if (status != Gdip.Ok)
                throw Gdip.StatusException(status);
        }

        public void Shear(float shearX, float shearY)
        {
            Gdip.CheckStatus(Gdip.GdipShearMatrix(SafeMatrixHandle, shearX, shearY, MatrixOrder.Prepend));
        }

        public void Shear(float shearX, float shearY, MatrixOrder order)
        {
            Gdip.CheckStatus(Gdip.GdipShearMatrix(SafeMatrixHandle, shearX, shearY, order));
        }

        public void Invert()
        {
            Gdip.CheckStatus(Gdip.GdipInvertMatrix(SafeMatrixHandle));
        }

        public unsafe void TransformPoints(PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            fixed (PointF* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipTransformMatrixPoints(
                    SafeMatrixHandle,
                    p,
                    pts.Length));
            }
        }

        public unsafe void TransformPoints(Point[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            fixed (Point* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipTransformMatrixPointsI(
                    SafeMatrixHandle,
                    p,
                    pts.Length));
            }
        }

        public unsafe void TransformVectors(PointF[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            fixed (PointF* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipVectorTransformMatrixPoints(
                    SafeMatrixHandle,
                    p,
                    pts.Length));
            }
        }

        public void VectorTransformPoints(Point[] pts) => TransformVectors(pts);

        public unsafe void TransformVectors(Point[] pts)
        {
            if (pts == null)
                throw new ArgumentNullException(nameof(pts));

            fixed (Point* p = pts)
            {
                Gdip.CheckStatus(Gdip.GdipVectorTransformMatrixPointsI(
                    SafeMatrixHandle,
                    p,
                    pts.Length));
            }
        }

        public bool IsInvertible
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipIsMatrixInvertible(SafeMatrixHandle, out int isInvertible));
                return isInvertible != 0;
            }
        }

        public bool IsIdentity
        {
            get
            {
                Gdip.CheckStatus(Gdip.GdipIsMatrixIdentity(SafeMatrixHandle, out int isIdentity));
                return isIdentity != 0;
            }
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (!(obj is Matrix matrix2))
                return false;

            Gdip.CheckStatus(Gdip.GdipIsMatrixEqual(
                SafeMatrixHandle,
                matrix2.SafeMatrixHandle,
                out int isEqual));

            return isEqual != 0;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}

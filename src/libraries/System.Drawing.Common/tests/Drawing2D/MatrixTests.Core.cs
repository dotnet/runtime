// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using Xunit;

namespace System.Drawing.Drawing2D.Tests
{
    public partial class MatrixTests
    {
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(MatrixElements_TestData))]
        public void Ctor_Matrix3x2(float m11, float m12, float m21, float m22, float dx, float dy, bool isIdentity, bool isInvertible)
        {
            Matrix3x2 matrix3X2 = new Matrix3x2(m11, m12, m21, m22, dx, dy);
            using (var matrix = new Matrix(matrix3X2))
            {
                Assert.Equal(new float[] { m11, m12, m21, m22, dx, dy }, matrix.Elements);
                Assert.Equal(matrix3X2, matrix.MatrixElements);
                Assert.Equal(isIdentity, matrix.IsIdentity);
                Assert.Equal(isInvertible, matrix.IsInvertible);
                Assert.Equal(dx, matrix.OffsetX);
                Assert.Equal(dy, matrix.OffsetY);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(MatrixElements_TestData))]
        public void MatrixElements_RoundTrip(float m11, float m12, float m21, float m22, float dx, float dy, bool isIdentity, bool isInvertible)
        {
            Matrix3x2 matrix3X2 = new Matrix3x2(m11, m12, m21, m22, dx, dy);
            using (var matrix = new Matrix())
            {
                matrix.MatrixElements = matrix3X2;
                Assert.Equal(new float[] { m11, m12, m21, m22, dx, dy }, matrix.Elements);
                Assert.Equal(matrix3X2, matrix.MatrixElements);
                Assert.Equal(isIdentity, matrix.IsIdentity);
                Assert.Equal(isInvertible, matrix.IsInvertible);
                Assert.Equal(dx, matrix.OffsetX);
                Assert.Equal(dy, matrix.OffsetY);
            }
        }
    }
}

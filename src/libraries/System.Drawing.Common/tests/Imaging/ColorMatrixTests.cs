// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
//
// Copyright (C) 2005-2007 Novell, Inc (http://www.novell.com)
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
//
// Authors:
//   Jordi Mas i Hernandez (jordi@ximian.com)
//   Sebastien Pouliot  <sebastien@ximian.com>
//
using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Imaging.Tests
{
    public class ColorMatrixTests
    {
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Default()
        {
            ColorMatrix cm = new ColorMatrix();

            Assert.Equal(1, cm.Matrix00);
            Assert.Equal(1, cm.Matrix11);
            Assert.Equal(1, cm.Matrix22);
            Assert.Equal(1, cm.Matrix33);
            Assert.Equal(1, cm.Matrix44);
            Assert.Equal(0, cm.Matrix01);
            Assert.Equal(0, cm.Matrix02);
            Assert.Equal(0, cm.Matrix03);
            Assert.Equal(0, cm.Matrix04);
            Assert.Equal(0, cm.Matrix10);
            Assert.Equal(0, cm.Matrix12);
            Assert.Equal(0, cm.Matrix13);
            Assert.Equal(0, cm.Matrix14);
            Assert.Equal(0, cm.Matrix20);
            Assert.Equal(0, cm.Matrix21);
            Assert.Equal(0, cm.Matrix23);
            Assert.Equal(0, cm.Matrix24);
            Assert.Equal(0, cm.Matrix30);
            Assert.Equal(0, cm.Matrix31);
            Assert.Equal(0, cm.Matrix32);
            Assert.Equal(0, cm.Matrix34);
            Assert.Equal(0, cm.Matrix40);
            Assert.Equal(0, cm.Matrix41);
            Assert.Equal(0, cm.Matrix42);
            Assert.Equal(0, cm.Matrix43);
        }

        public static IEnumerable<object[]> BadCtorParams
        {
            get
            {
                yield return new object[] { null, typeof(NullReferenceException) };
                yield return new object[] { new float[][] { }, typeof(IndexOutOfRangeException) };
                yield return new object[] { new float[][] { new float[] { 0.0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f } }, typeof(IndexOutOfRangeException) };
                yield return new object[] { new float[][] {
                    new float[] { 0.0f },
                    new float[] { 1.0f },
                    new float[] { 2.0f },
                    new float[] { 3.0f },
                    new float[] { 4.0f },
                    new float[] { 5.0f } }
                , typeof(IndexOutOfRangeException) };
            }
        }

        public static float[][] IndexedColorMatrix
        {
            get
            {
                return new float[][] {
                new float[] { 0.0f, 0.1f, 0.2f, 0.3f, 0.4f},
                new float[] { 1.0f, 1.1f, 1.2f, 1.3f, 1.4f},
                new float[] { 2.0f, 2.1f, 2.2f, 2.3f, 2.4f},
                new float[] { 3.0f, 3.1f, 3.2f, 3.3f, 3.4f},
                new float[] { 4.0f, 4.1f, 4.2f, 4.3f, 4.4f},
            };
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(BadCtorParams))]
        public void Ctor_BadValues_ThrowsExpectedException(float[][] newColorMatrix, Type expectedException)
        {
            Assert.Throws(expectedException, () => new ColorMatrix(newColorMatrix));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_TooBigArraySize_MapOnly4and4Elements()
        {
            ColorMatrix cm = new ColorMatrix(new float[][] {
                new float[] { 0.0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f },
                new float[] { 1.0f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f },
                new float[] { 2.0f, 2.1f, 2.2f, 2.3f, 2.4f, 2.5f },
                new float[] { 3.0f, 3.1f, 3.2f, 3.3f, 3.4f, 3.5f },
                new float[] { 4.0f, 4.1f, 4.2f, 4.3f, 4.4f, 4.5f },
                new float[] { 5.0f, 5.1f, 5.2f, 5.3f, 5.4f, 5.5f }
            });

            Assert.Equal(0.0f, cm.Matrix00);
            Assert.Equal(0.1f, cm.Matrix01);
            Assert.Equal(0.2f, cm.Matrix02);
            Assert.Equal(0.3f, cm.Matrix03);
            Assert.Equal(0.4f, cm.Matrix04);
            Assert.Equal(1.0f, cm.Matrix10);
            Assert.Equal(1.1f, cm.Matrix11);
            Assert.Equal(1.2f, cm.Matrix12);
            Assert.Equal(1.3f, cm.Matrix13);
            Assert.Equal(1.4f, cm.Matrix14);
            Assert.Equal(2.0f, cm.Matrix20);
            Assert.Equal(2.1f, cm.Matrix21);
            Assert.Equal(2.2f, cm.Matrix22);
            Assert.Equal(2.3f, cm.Matrix23);
            Assert.Equal(2.4f, cm.Matrix24);
            Assert.Equal(3.0f, cm.Matrix30);
            Assert.Equal(3.1f, cm.Matrix31);
            Assert.Equal(3.2f, cm.Matrix32);
            Assert.Equal(3.3f, cm.Matrix33);
            Assert.Equal(3.4f, cm.Matrix34);
            Assert.Equal(4.0f, cm.Matrix40);
            Assert.Equal(4.1f, cm.Matrix41);
            Assert.Equal(4.2f, cm.Matrix42);
            Assert.Equal(4.3f, cm.Matrix43);
            Assert.Equal(4.4f, cm.Matrix44);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void AccessToNotExistingElement_ThrowsIndexOutOfRangeException()
        {
            ColorMatrix cm = new ColorMatrix(new float[][] {
                new float[] { 0.0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f },
                new float[] { 1.0f, 1.1f, 1.2f, 1.3f, 1.4f, 1.5f },
                new float[] { 2.0f, 2.1f, 2.2f, 2.3f, 2.4f, 2.5f },
                new float[] { 3.0f, 3.1f, 3.2f, 3.3f, 3.4f, 3.5f },
                new float[] { 4.0f, 4.1f, 4.2f, 4.3f, 4.4f, 4.5f },
                new float[] { 5.0f, 5.1f, 5.2f, 5.3f, 5.4f, 5.5f }
            });
            Assert.Throws<IndexOutOfRangeException>(() => { var x = cm[5, 5]; });
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_SetValue_ReturnsExpected()
        {
            ColorMatrix cm = new ColorMatrix(IndexedColorMatrix);

            Assert.Equal(0.0f, cm.Matrix00);
            Assert.Equal(1.0f, cm.Matrix10);
            Assert.Equal(2.0f, cm.Matrix20);
            Assert.Equal(3.0f, cm.Matrix30);
            Assert.Equal(4.0f, cm.Matrix40);

            Assert.Equal(0.1f, cm.Matrix01);
            Assert.Equal(1.1f, cm.Matrix11);
            Assert.Equal(2.1f, cm.Matrix21);
            Assert.Equal(3.1f, cm.Matrix31);
            Assert.Equal(4.1f, cm.Matrix41);

            Assert.Equal(0.2f, cm.Matrix02);
            Assert.Equal(1.2f, cm.Matrix12);
            Assert.Equal(2.2f, cm.Matrix22);
            Assert.Equal(3.2f, cm.Matrix32);
            Assert.Equal(4.2f, cm.Matrix42);

            Assert.Equal(0.3f, cm.Matrix03);
            Assert.Equal(1.3f, cm.Matrix13);
            Assert.Equal(2.3f, cm.Matrix23);
            Assert.Equal(3.3f, cm.Matrix33);
            Assert.Equal(4.3f, cm.Matrix43);

            Assert.Equal(0.4f, cm.Matrix04);
            Assert.Equal(1.4f, cm.Matrix14);
            Assert.Equal(2.4f, cm.Matrix24);
            Assert.Equal(3.4f, cm.Matrix34);
            Assert.Equal(4.4f, cm.Matrix44);

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Assert.Equal(IndexedColorMatrix[i][j], cm[i, j]);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MatrixElement_SetValues_ReturnsExpected()
        {
            ColorMatrix cm = new ColorMatrix();

            cm.Matrix00 = 1;
            cm.Matrix01 = 2;
            cm.Matrix02 = 3;
            cm.Matrix03 = 4;
            cm.Matrix04 = 5;
            cm.Matrix10 = 6;
            cm.Matrix11 = 7;
            cm.Matrix12 = 8;
            cm.Matrix13 = 9;
            cm.Matrix14 = 10;
            cm.Matrix20 = 11;
            cm.Matrix21 = 12;
            cm.Matrix22 = 13;
            cm.Matrix23 = 14;
            cm.Matrix24 = 15;
            cm.Matrix30 = 16;
            cm.Matrix31 = 17;
            cm.Matrix32 = 18;
            cm.Matrix33 = 19;
            cm.Matrix34 = 20;
            cm.Matrix40 = 21;
            cm.Matrix41 = 22;
            cm.Matrix42 = 23;
            cm.Matrix43 = 24;
            cm.Matrix44 = 25;

            Assert.Equal(1, cm.Matrix00);
            Assert.Equal(2, cm.Matrix01);
            Assert.Equal(3, cm.Matrix02);
            Assert.Equal(4, cm.Matrix03);
            Assert.Equal(5, cm.Matrix04);
            Assert.Equal(6, cm.Matrix10);
            Assert.Equal(7, cm.Matrix11);
            Assert.Equal(8, cm.Matrix12);
            Assert.Equal(9, cm.Matrix13);
            Assert.Equal(10, cm.Matrix14);
            Assert.Equal(11, cm.Matrix20);
            Assert.Equal(12, cm.Matrix21);
            Assert.Equal(13, cm.Matrix22);
            Assert.Equal(14, cm.Matrix23);
            Assert.Equal(15, cm.Matrix24);
            Assert.Equal(16, cm.Matrix30);
            Assert.Equal(17, cm.Matrix31);
            Assert.Equal(18, cm.Matrix32);
            Assert.Equal(19, cm.Matrix33);
            Assert.Equal(20, cm.Matrix34);
            Assert.Equal(21, cm.Matrix40);
            Assert.Equal(22, cm.Matrix41);
            Assert.Equal(23, cm.Matrix42);
            Assert.Equal(24, cm.Matrix43);
            Assert.Equal(25, cm.Matrix44);
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MatrixElementByIndexer_SetValue_ReturnsExpetecd()
        {
            ColorMatrix cm = new ColorMatrix(IndexedColorMatrix);

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    cm[i, j] = IndexedColorMatrix[i][j];
                }
            }

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    Assert.Equal(IndexedColorMatrix[i][j], cm[i, j]);
                }
            }
        }
    }
}

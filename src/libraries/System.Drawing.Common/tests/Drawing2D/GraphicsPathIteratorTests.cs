// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Copyright (C) 2006-2007 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Drawing2D.Tests
{
    public class GraphicsPathIteratorTests
    {
        private readonly PointF[] _twoPoints = new PointF[2] { new PointF(1, 2), new PointF(20, 30) };

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Path_Success()
        {
            byte[] types = new byte[] { 0, 1 };

            using (GraphicsPath gp = new GraphicsPath(_twoPoints, types))
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Equal(2, gpi.Count);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_EmptyPath_Success()
        {
            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Equal(0, gpi.Count);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_NullPath_Success()
        {
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(null))
            {
                Assert.Equal(0, gpi.Count);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void NextSubpath_PathFigureNotClosed_ReturnsExpected()
        {
            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                gp.AddLines(_twoPoints);
                Assert.Equal(0, gpi.NextSubpath(gp, out bool isClosed));
                Assert.False(isClosed);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void NextSubpath_PathFigureClosed_ReturnsExpected()
        {
            using (GraphicsPath gp = new GraphicsPath(_twoPoints, new byte[] { 0, 129 }))
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Equal(2, gpi.NextSubpath(gp, out bool isClosed));
                Assert.True(isClosed);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void NextSubpath_NullPath_ReturnsExpected()
        {
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(null))
            {
                Assert.Equal(0, gpi.NextSubpath(null, out bool isClosed));
                Assert.False(isClosed);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void NextSubpath_FigureNotClosed_ReturnsExpected()
        {
            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                gp.AddLines(_twoPoints);
                Assert.Equal(0, gpi.NextSubpath(out int startIndex, out int endIndex, out bool isClosed));
                Assert.False(isClosed);
                Assert.Equal(0, startIndex);
                Assert.Equal(0, endIndex);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void NextSubpath_FigureClosed_ReturnsExpected()
        {
            using (GraphicsPath gp = new GraphicsPath(_twoPoints, new byte[] { 0, 129 }))
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Equal(2, gpi.NextSubpath(out int startIndex, out int endIndex, out bool isClosed));
                Assert.True(isClosed);
                Assert.Equal(0, startIndex);
                Assert.Equal(1, endIndex);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void NextMarker_ReturnsExpected()
        {
            using (GraphicsPath gp = new GraphicsPath(_twoPoints, new byte[] { 0, 1 }))
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Equal(2, gpi.NextMarker(out int startIndex, out int endIndex));
                Assert.Equal(0, startIndex);
                Assert.Equal(1, endIndex);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void NextMarker_Empty_ReturnsExpected()
        {
            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                gp.AddLines(_twoPoints);
                Assert.Equal(0, gpi.NextMarker(out int startIndex, out int endIndex));
                Assert.Equal(0, startIndex);
                Assert.Equal(0, endIndex);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void NextMarker_NullPath_ReturnsExpected()
        {
            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                gp.AddLines(_twoPoints);
                Assert.Equal(0, gpi.NextMarker(null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void NextMarker_EmptyPath_ReturnsExpected()
        {
            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                gp.AddLines(_twoPoints);
                Assert.Equal(0, gpi.NextMarker(gp));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void NextMarker_Path_ReturnsExpected()
        {
            using (GraphicsPath gp = new GraphicsPath(_twoPoints, new byte[] { 0, 1 }))
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Equal(2, gpi.NextMarker(gp));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Count_ReturnsExpected()
        {
            using (GraphicsPath gp = new GraphicsPath(_twoPoints, new byte[] { 0, 1 }))
            using (GraphicsPath gpEmpty = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            using (GraphicsPathIterator gpiEmpty = new GraphicsPathIterator(gpEmpty))
            using (GraphicsPathIterator gpiNull = new GraphicsPathIterator(null))
            {
                Assert.Equal(2, gpi.Count);
                Assert.Equal(0, gpiEmpty.Count);
                Assert.Equal(0, gpiNull.Count);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void SubpathCount_ReturnsExpected()
        {
            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            using (GraphicsPathIterator gpiNull = new GraphicsPathIterator(null))
            {
                Assert.Equal(0, gpi.SubpathCount);
                Assert.Equal(0, gpiNull.SubpathCount);

                gp.AddLine(0, 1, 2, 3);
                gp.SetMarkers();
                gp.StartFigure();
                gp.AddLine(20, 21, 22, 23);
                gp.AddBezier(5, 6, 7, 8, 9, 10, 11, 12);

                using (GraphicsPathIterator gpiWithSubpaths = new GraphicsPathIterator(gp))
                {
                    Assert.Equal(2, gpiWithSubpaths.SubpathCount);
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void HasCurve_ReturnsExpected()
        {
            Point[] points = new Point[] { new Point(1, 1), new Point(2, 2), new Point(3, 3), new Point(4, 4) };
            byte[] types = new byte[] { 0, 3, 3, 3 };

            using (GraphicsPath gp = new GraphicsPath(points, types))
            using (GraphicsPath gpEmpty = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            using (GraphicsPathIterator gpiEmpty = new GraphicsPathIterator(gpEmpty))
            using (GraphicsPathIterator gpiNull = new GraphicsPathIterator(null))
            {
                Assert.True(gpi.HasCurve());
                Assert.False(gpiEmpty.HasCurve());
                Assert.False(gpiNull.HasCurve());
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Rewind_Success()
        {
            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPath inner = new GraphicsPath())
            {
                gp.AddLine(0, 1, 2, 3);
                gp.SetMarkers();
                gp.StartFigure();
                gp.AddLine(20, 21, 22, 23);
                gp.AddBezier(5, 6, 7, 8, 9, 10, 11, 12);

                using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
                {
                    Assert.Equal(2, gpi.SubpathCount);
                    Assert.Equal(2, gpi.NextMarker(gp));
                    Assert.Equal(6, gpi.NextMarker(gp));
                    Assert.Equal(0, gpi.NextMarker(gp));
                    gpi.Rewind();
                    Assert.Equal(8, gpi.NextMarker(gp));
                    Assert.Equal(0, gpi.NextMarker(gp));
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Enumerate_ZeroPoints_ReturnsExpected()
        {
            PointF[] points = new PointF[0];
            byte[] types = new byte[0];

            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Equal(0, gpi.Enumerate(ref points, ref types));
                Assert.Equal(0, points.Length);
                Assert.Equal(0, types.Length);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Enumerate_ReturnsExpected()
        {
            PointF[] points = new PointF[] { new PointF(1f, 1f), new PointF(2f, 2f), new PointF(3f, 3f), new PointF(4f, 4f) };
            byte[] types = new byte[] { 0, 3, 3, 3 };

            PointF[] actualPoints = new PointF[4];
            byte[] actualTypes = new byte[4];

            using (GraphicsPath gp = new GraphicsPath(points, types))
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Equal(4, gpi.Enumerate(ref actualPoints, ref actualTypes));
                Assert.Equal(gp.PathPoints, actualPoints);
                Assert.Equal(gp.PathTypes, actualTypes);
            }
        }

        public static IEnumerable<object[]> PointsTypesLengthMismatch_TestData()
        {
            yield return new object[] { new PointF[1], new byte[2] };
            yield return new object[] { new PointF[2], new byte[1] };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(PointsTypesLengthMismatch_TestData))]
        public void Enumerate_PointsTypesMismatch_ThrowsArgumentException(PointF[] points, byte[] types)
        {
            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => gpi.Enumerate(ref points, ref types));
            }
        }

        public static IEnumerable<object[]> NullPointsTypes_TestData()
        {
            yield return new object[] { null, new byte[1] };
            yield return new object[] { new PointF[1], null };
            yield return new object[] { null, null };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(NullPointsTypes_TestData))]
        public void Enumerate_NullPointsTypes_ThrowsNullReferenceException(PointF[] points, byte[] types)
        {
            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Throws<NullReferenceException>(() => gpi.Enumerate(ref points, ref types));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(PointsTypesLengthMismatch_TestData))]
        public void CopyData_PointsTypesMismatch_ThrowsArgumentException(PointF[] points, byte[] types)
        {
            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => gpi.CopyData(ref points, ref types, 0, points.Length));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(NullPointsTypes_TestData))]
        public void CopyData_NullPointsTypes_ThrowsNullReferenceException(PointF[] points, byte[] types)
        {
            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Throws<NullReferenceException>(() => gpi.CopyData(ref points, ref types, 0, 1));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-1, 2)]
        [InlineData(0, 3)]
        public void CopyData_StartEndIndexesOutOfRange_ThrowsArgumentException(int startIndex, int endIndex)
        {
            PointF[] resultPoints = new PointF[0];
            byte[] resultTypes = new byte[0];

            using (GraphicsPath gp = new GraphicsPath())
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                AssertExtensions.Throws<ArgumentException>(null, () => gpi.CopyData(ref resultPoints, ref resultTypes, startIndex, endIndex));
            }
        }

        public static IEnumerable<object[]> CopyData_StartEndIndexesOutOfRange_TestData()
        {
            yield return new object[] { new PointF[3], new byte[3], int.MinValue, 2 };
            yield return new object[] { new PointF[3], new byte[3], 0, int.MaxValue };
            yield return new object[] { new PointF[3], new byte[3], 2, 0 };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(CopyData_StartEndIndexesOutOfRange_TestData))]
        public void CopyData_StartEndIndexesOutOfRange_ReturnsExpected(PointF[] points, byte[] types, int startIndex, int endIndex)
        {
            PointF[] resultPoints = new PointF[points.Length];
            byte[] resultTypes = new byte[points.Length];

            using (GraphicsPath gp = new GraphicsPath(points, types))
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Equal(0, gpi.CopyData(ref resultPoints, ref resultTypes, startIndex, endIndex));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void CopyData_EqualStartEndIndexes_ReturnsExpected()
        {
            PointF[] points = new PointF[] { new PointF(1f, 1f), new PointF(2f, 2f), new PointF(3f, 3f), new PointF(4f, 4f) };
            byte[] types = new byte[] { 0, 3, 3, 3 };

            PointF[] actualPoints = new PointF[1];
            byte[] actualTypes = new byte[1];

            using (GraphicsPath gp = new GraphicsPath(points, types))
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Equal(1, gpi.CopyData(ref actualPoints, ref actualTypes, 0, 0));
                Assert.Equal(gp.PathPoints[0], actualPoints[0]);
                Assert.Equal(gp.PathTypes[0], actualTypes[0]);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void CopyData_ReturnsExpected()
        {
            PointF[] points = new PointF[] { new PointF(1f, 1f), new PointF(2f, 2f), new PointF(3f, 3f), new PointF(4f, 4f) };
            byte[] types = new byte[] { 0, 3, 3, 3 };

            PointF[] actualPoints = new PointF[3];
            byte[] actualTypes = new byte[3];

            using (GraphicsPath gp = new GraphicsPath(points, types))
            using (GraphicsPathIterator gpi = new GraphicsPathIterator(gp))
            {
                Assert.Equal(3, gpi.CopyData(ref actualPoints, ref actualTypes, 0, 2));
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.
// 
// Copyright (C) 2004-2008 Novell, Inc (http://www.novell.com)
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

using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Drawing.Tests
{
    public class RegionTests
    {
        private static readonly Graphics s_graphic = Graphics.FromImage(new Bitmap(1, 1));

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_Default()
        {
            using (var region = new Region())
            {
                Assert.False(region.IsEmpty(s_graphic));
                Assert.True(region.IsInfinite(s_graphic));
                Assert.Equal(new RectangleF(-4194304, -4194304, 8388608, 8388608), region.GetBounds(s_graphic));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(-1, -2, -3, -4, true)]
        [InlineData(0, 0, 0, 0, true)]
        [InlineData(1, 2, 3, 4, false)]
        public void Ctor_Rectangle(int x, int y, int width, int height, bool isEmpty)
        {
            var rectangle = new Rectangle(x, y, width, height);

            using (var region = new Region(rectangle))
            {
                Assert.Equal(isEmpty, region.IsEmpty(s_graphic));
                Assert.False(region.IsInfinite(s_graphic));
                Assert.Equal(new RectangleF(x, y, width, height), region.GetBounds(s_graphic));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(1, 2, 3, float.NegativeInfinity, true)]
        [InlineData(-1, -2, -3, -4, true)]
        [InlineData(0, 0, 0, 0, true)]
        [InlineData(1, 2, 3, 4, false)]
        [InlineData(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue, true)]
        public void Ctor_RectangleF(float x, float y, float width, float height, bool isEmpty)
        {
            var rectangle = new RectangleF(x, y, width, height);

            using (var region = new Region(rectangle))
            {
                Assert.Equal(isEmpty, region.IsEmpty(s_graphic));
                Assert.False(region.IsInfinite(s_graphic));
                Assert.Equal(rectangle, region.GetBounds(s_graphic));
            }
        }

        public static IEnumerable<object[]> Region_TestData()
        {
            yield return new object[] { new Region() };
            yield return new object[] { new Region(new Rectangle(0, 0, 0, 0)) };
            yield return new object[] { new Region(new Rectangle(1, 2, 3, 4)) };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Region_TestData))]
        public void Ctor_RegionData(Region region)
        {
            try
            {
                using (var otherRegion = new Region(region.GetRegionData()))
                using (var matrix = new Matrix())
                {
                    Assert.Equal(region.GetBounds(s_graphic), otherRegion.GetBounds(s_graphic));
                    Assert.Equal(region.GetRegionScans(matrix), otherRegion.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_RegionDataOfRegionWithPath_Success()
        {
            using (var graphicsPath = new GraphicsPath())
            {
                graphicsPath.AddRectangle(new Rectangle(1, 2, 3, 4));
                Ctor_RegionData(new Region(graphicsPath));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_RegionDataOfRegionWithRegionData_Success()
        {
            using (var region = new Region(new Rectangle(1, 2, 3, 4)))
            {
                Ctor_RegionData(new Region(region.GetRegionData()));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_NullRegionData_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("rgnData", () => new Region((RegionData)null));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(7)]
        [InlineData(256)]
        public void Ctor_InvalidRegionData_ThrowsExternalException(int dataLength)
        {
            using (var region = new Region())
            {
                RegionData regionData = region.GetRegionData();
                regionData.Data = new byte[dataLength];
                Assert.Throws<ExternalException>(() => new Region(regionData));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_EmptyGraphicsPath_ThrowsExternalException()
        {
            using (var graphicsPath = new GraphicsPath())
            using (var region = new Region(graphicsPath))
            {
                RegionData regionData = region.GetRegionData();
                Assert.Throws<ExternalException>(() => new Region(regionData));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_NullDataInRegionData_ThrowsNullReferenceException()
        {
            using (var region = new Region())
            {
                RegionData regionData = region.GetRegionData();
                regionData.Data = null;
                Assert.Throws<NullReferenceException>(() => new Region(regionData));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_GraphicsPath()
        {
            using (var graphicsPath = new GraphicsPath())
            {
                graphicsPath.AddRectangle(new Rectangle(1, 2, 3, 4));
                graphicsPath.AddRectangle(new Rectangle(4, 5, 6, 7));

                using (var region = new Region(graphicsPath))
                using (var matrix = new Matrix())
                {
                    Assert.Equal(new RectangleF[]
                    {
                        new RectangleF(1, 2, 3, 3),
                        new RectangleF(1, 5, 9, 1),
                        new RectangleF(4, 6, 6, 6)
                    }, region.GetRegionScans(matrix));
                    Assert.Equal(new RectangleF(1, 2, 9, 10), region.GetBounds(s_graphic));
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_EmptyGraphicsPath()
        {
            using (var graphicsPath = new GraphicsPath())
            using (var region = new Region(graphicsPath))
            using (var matrix = new Matrix())
            {
                Assert.True(region.IsEmpty(s_graphic));
                Assert.Empty(region.GetRegionScans(matrix));
            }
        }

        public static IEnumerable<object[]> Ctor_InfiniteGraphicsPath_TestData()
        {
            var path1 = new GraphicsPath();
            path1.AddRectangle(new Rectangle(-4194304, -4194304, 8388608, 8388608));
            yield return new object[] { path1, true };

            var path2 = new GraphicsPath();
            path2.AddRectangle(new Rectangle(-4194304, -4194304, 8388608, 8388608));
            path2.AddRectangle(Rectangle.Empty);
            yield return new object[] { path2, true };

            var path3 = new GraphicsPath();
            path3.AddRectangle(new Rectangle(-4194304, -4194304, 8388608, 8388608));
            path3.AddRectangle(new Rectangle(1, 2, 3, 4));
            yield return new object[] { path3, false };

            var path4 = new GraphicsPath();
            path4.AddCurve(new Point[] { new Point(-4194304, -4194304), new Point(4194304, 4194304) });
            yield return new object[] { path4, false };

            var path5 = new GraphicsPath();
            path5.AddPolygon(new Point[] { new Point(-4194304, -4194304), new Point(-4194304, 4194304), new Point(4194304, 4194304), new Point(4194304, -4194304) });
            yield return new object[] { path5, true };

            var path6 = new GraphicsPath();
            path6.AddPolygon(new Point[] { new Point(-4194304, -4194304), new Point(-4194304, 4194304), new Point(4194304, 4194304), new Point(4194304, -4194304), new Point(-4194304, -4194304) });
            yield return new object[] { path6, true };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_InfiniteGraphicsPath_TestData))]
        public void Ctor_InfiniteGraphicsPath_IsInfinite(GraphicsPath path, bool isInfinite)
        {
            try
            {
                using (var region = new Region(path))
                {
                    Assert.Equal(isInfinite, region.IsInfinite(s_graphic));
                }
            }
            finally
            {
                path.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_GraphicsPathTooLarge_SetsToEmpty()
        {
            using (var path = new GraphicsPath())
            {
                path.AddCurve(new Point[] { new Point(-4194304, -4194304), new Point(4194304, 4194304) });

                using (var region = new Region(path))
                using (var matrix = new Matrix())
                {
                    Assert.Empty(region.GetRegionScans(matrix));
                }
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_NullGraphicsPath_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("path", () => new Region((GraphicsPath)null));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Ctor_DisposedGraphicsPath_ThrowsArgumentException()
        {
            var path = new GraphicsPath();
            path.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => new Region(path));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Region_TestData))]
        public void Clone(Region region)
        {
            try
            {
                using (Region clone = Assert.IsType<Region>(region.Clone()))
                using (var matrix = new Matrix())
                {
                    Assert.NotSame(region, clone);

                    Assert.Equal(region.GetBounds(s_graphic), clone.GetBounds(s_graphic));
                    Assert.Equal(region.GetRegionScans(matrix), clone.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Clone();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clone_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            AssertExtensions.Throws<ArgumentException>(null, () => region.Clone());
        }

        public static IEnumerable<object[]> Complement_TestData()
        {
            yield return new object[]
            {
                new Region(new RectangleF(10, 10, 100, 100)),
                new RectangleF[] { new RectangleF(40, 60, 100, 20) },
                new RectangleF[] { new RectangleF(110, 60, 30, 20) }
            };

            yield return new object[]
            {
                new Region(new RectangleF(70, 10, 100, 100)),
                new RectangleF[] { new RectangleF(40, 60, 100, 20) },
                new RectangleF[] { new RectangleF(40, 60, 30, 20) }
            };

            yield return new object[]
            {
                new Region(new RectangleF(40, 100, 100, 100)),
                new RectangleF[] { new RectangleF(70, 80, 50, 40) },
                new RectangleF[] { new RectangleF(70, 80, 50, 20) }
            };

            yield return new object[]
            {
                new Region(new RectangleF(40, 10, 100, 100)),
                new RectangleF[] { new RectangleF(70, 80, 50, 40) },
                new RectangleF[] { new RectangleF(70, 110, 50, 10) }
            };

            yield return new object[]
            {
                new Region(new RectangleF(30, 30, 80, 80)),
                new RectangleF[]
                {
                    new RectangleF(45, 45, 200, 200),
                    new RectangleF(160, 260, 10, 10),
                    new RectangleF(170, 260, 10, 10),
                },
                new RectangleF[] { new RectangleF(170, 260, 10, 10) }
            };

            yield return new object[]
            {
                new Region(),
                new RectangleF[] { RectangleF.Empty },
                new RectangleF[0]
            };

            yield return new object[]
            {
                new Region(),
                new RectangleF[] { new RectangleF(1, 2, 3, 4) },
                new RectangleF[0]
            };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Complement_TestData))]
        public void Complement_Region_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    using (var other = new Region(rect))
                    {
                        region.Complement(other);
                    }
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Complement_UnionRegion_Success()
        {
            using (var region = new Region(new Rectangle(20, 20, 20, 20)))
            using (var other = new Region(new Rectangle(20, 80, 20, 10)))
            using (var matrix = new Matrix())
            {
                other.Union(new Rectangle(60, 60, 30, 10));

                region.Complement(other);
                Assert.Equal(new RectangleF[]
                {
                    new RectangleF(60, 60, 30, 10),
                    new RectangleF(20, 80, 20, 10)
                }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Complement_InfiniteAndWithIntersectRegion_Success()
        {
            using (var region = new Region())
            using (var matrix = new Matrix())
            {
                region.Intersect(new Rectangle(5, 5, -10, -10));
                region.Complement(new Rectangle(-5, -5, 12, 12));

                Assert.False(region.IsEmpty(s_graphic));
                Assert.False(region.IsInfinite(s_graphic));
                Assert.Equal(new RectangleF[]
                {
                    new RectangleF(5, -5, 2, 10),
                    new RectangleF(-5, 5, 12, 2)
                }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Complement_InfiniteRegion_Success()
        {
            using (var region = new Region(new Rectangle(1, 2, 3, 4)))
            using (var matrix = new Matrix())
            using (var other = new Region())
            {
                region.Complement(other);

                Assert.Equal(new RectangleF[]
                {
                    new RectangleF(-4194304, -4194304, 8388608, 4194306),
                    new RectangleF(-4194304, 2, 4194305, 4),
                    new RectangleF(4, 2, 4194300, 4),
                    new RectangleF(-4194304, 6, 8388608, 4194298)
                }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Complement_NullRegion_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("region", () => region.Complement((Region)null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Complement_DisposedRegion_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => new Region().Complement(region));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Complement_SameRegion_ThrowsInvalidOperationException()
        {
            using (var region = new Region())
            {
                Assert.Throws<InvalidOperationException>(() => region.Complement(region));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Complement_TestData))]
        public void Complement_Rectangle_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    region.Complement(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Complement_TestData))]
        public void Complement_RectangleF_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    region.Complement(rect);
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Complement_TestData))]
        public void Complement_GraphicsPath_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                using (var path = new GraphicsPath())
                {
                    path.AddRectangle(rect);
                    region.Complement(path);
                }
            }

            using (var matrix = new Matrix())
            {
                Assert.Equal(expectedScans, region.GetRegionScans(matrix));
            }
        }

        [ActiveIssue(24525, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Complement_GraphicsPathWithMultipleRectangles_Success()
        {
            Graphics graphics = Graphics.FromImage(new Bitmap(600, 800));

            var rect1 = new Rectangle(20, 30, 60, 80);
            var rect2 = new Rectangle(50, 40, 60, 80);
            using (var region1 = new Region(rect1))
            using (var region2 = new Region(rect2))
            using (var matrix = new Matrix())
            {
                graphics.DrawRectangle(Pens.Green, rect1);
                graphics.DrawRectangle(Pens.Red, rect2);

                region1.Complement(region2);
                graphics.FillRegion(Brushes.Blue, region1);
                graphics.DrawRectangles(Pens.Yellow, region1.GetRegionScans(matrix));

                Assert.Equal(new RectangleF[]
                {
                    new RectangleF(80, 40, 30, 70),
                    new RectangleF(50, 110, 60, 10)
                }, region1.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Complement_EmptyPathWithInfiniteRegion_MakesEmpty()
        {
            using (var region = new Region())
            using (var graphicsPath = new GraphicsPath())
            {
                region.Complement(graphicsPath);
                Assert.True(region.IsEmpty(s_graphic));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Complement_NullGraphicsPath_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("path", () => region.Complement((GraphicsPath)null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Complement_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            using (var graphicPath = new GraphicsPath())
            using (var other = new Region())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => region.Complement(graphicPath));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Complement(new Rectangle()));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Complement(new RectangleF()));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Complement(region));
            }
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            Func<Region> empty = () =>
            {
                var emptyRegion = new Region();
                emptyRegion.MakeEmpty();
                return emptyRegion;
            };

            var createdRegion = new Region();
            yield return new object[] { createdRegion, createdRegion, true };
            yield return new object[] { new Region(), new Region(), true };
            yield return new object[] { new Region(), empty(), false };
            yield return new object[] { new Region(), new Region(new Rectangle(1, 2, 3, 4)), false };

            yield return new object[] { empty(), empty(), true };
            yield return new object[] { empty(), new Region(new Rectangle(0, 0, 0, 0)), true };
            yield return new object[] { empty(), new Region(new Rectangle(1, 2, 3, 3)), false };

            yield return new object[] { new Region(new Rectangle(1, 2, 3, 4)), new Region(new Rectangle(1, 2, 3, 4)), true };
            yield return new object[] { new Region(new Rectangle(1, 2, 3, 4)), new Region(new RectangleF(1, 2, 3, 4)), true };
            yield return new object[] { new Region(new Rectangle(1, 2, 3, 4)), new Region(new Rectangle(2, 2, 3, 4)), false };
            yield return new object[] { new Region(new Rectangle(1, 2, 3, 4)), new Region(new Rectangle(1, 3, 3, 4)), false };
            yield return new object[] { new Region(new Rectangle(1, 2, 3, 4)), new Region(new Rectangle(1, 2, 4, 4)), false };
            yield return new object[] { new Region(new Rectangle(1, 2, 3, 4)), new Region(new Rectangle(1, 2, 3, 5)), false };

            var graphics1 = new GraphicsPath();
            graphics1.AddRectangle(new Rectangle(1, 2, 3, 4));

            var graphics2 = new GraphicsPath();
            graphics2.AddRectangle(new Rectangle(1, 2, 3, 4));

            var graphics3 = new GraphicsPath();
            graphics3.AddRectangle(new Rectangle(2, 2, 3, 4));

            var graphics4 = new GraphicsPath();
            graphics4.AddRectangle(new Rectangle(1, 3, 3, 4));

            var graphics5 = new GraphicsPath();
            graphics5.AddRectangle(new Rectangle(1, 2, 4, 4));

            var graphics6 = new GraphicsPath();
            graphics6.AddRectangle(new Rectangle(1, 2, 3, 5));

            yield return new object[] { new Region(graphics1), new Region(graphics1), true };
            yield return new object[] { new Region(graphics1), new Region(graphics2), true };
            yield return new object[] { new Region(graphics1), new Region(graphics3), false };
            yield return new object[] { new Region(graphics1), new Region(graphics4), false };
            yield return new object[] { new Region(graphics1), new Region(graphics5), false };
            yield return new object[] { new Region(graphics1), new Region(graphics6), false };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Valid_ReturnsExpected(Region region, Region other, bool expected)
        {
            try
            {
                Assert.Equal(expected, region.Equals(other, s_graphic));
            }
            finally
            {
                region.Dispose();
                other.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Equals_NullRegion_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("region", () => region.Equals(null, s_graphic));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Equals_NullGraphics_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("g", () => region.Equals(region, null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Equals_DisposedGraphics_ThrowsArgumentException()
        {
            using (var region = new Region())
            using (var other = new Region())
            using (var image = new Bitmap(10, 10))
            {
                var graphics = Graphics.FromImage(image);
                graphics.Dispose();
                AssertExtensions.Throws<ArgumentException>(null, () => region.Equals(region, graphics));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Equals_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => region.Equals(new Region(), s_graphic));
            AssertExtensions.Throws<ArgumentException>(null, () => new Region().Equals(region, s_graphic));
        }

        public static IEnumerable<object[]> Exclude_TestData()
        {
            yield return new object[]
            {
                new Region(new Rectangle(500, 30, 60, 80)),
                new RectangleF[] { new RectangleF(500, 30, 60, 80) },
                new RectangleF[0]
            };

            yield return new object[]
            {
                new Region(new Rectangle(500, 30, 60, 80)),
                new RectangleF[] { RectangleF.Empty },
                new RectangleF[] { new RectangleF(500, 30, 60, 80) }
            };

            yield return new object[]
            {
                new Region(),
                new RectangleF[] { new RectangleF(520, 40, 60, 80) },
                new RectangleF[]
                {
                    new RectangleF(-4194304, -4194304, 8388608, 4194344),
                    new RectangleF(-4194304, 40, 4194824, 80),
                    new RectangleF(580, 40, 4193724, 80),
                    new RectangleF(-4194304, 120, 8388608, 4194184)
                }
            };

            yield return new object[]
            {
                new Region(),
                new RectangleF[] { RectangleF.Empty },
                new RectangleF[] { new Rectangle(-4194304, -4194304, 8388608, 8388608) }
            };

            // Intersecting from the right.
            yield return new object[]
            {
                new Region(new Rectangle(10, 10, 100, 100)),
                new RectangleF[] { new RectangleF(40, 60, 100, 20) },
                new RectangleF[]
                {
                    new RectangleF(10, 10, 100, 50),
                    new RectangleF(10, 60, 30, 20),
                    new RectangleF(10, 80, 100, 30)
                }
            };

            // Intersecting from the left.
            yield return new object[]
            {
                new Region(new Rectangle(70, 10, 100, 100)),
                new RectangleF[] { new RectangleF(40, 60, 100, 20) },
                new RectangleF[]
                {
                    new RectangleF(70, 10, 100, 50),
                    new RectangleF(140, 60, 30, 20),
                    new RectangleF(70, 80, 100, 30)
                }
            };

            // Intersecting from the top.
            yield return new object[]
            {
                new Region(new Rectangle(40, 100, 100, 100)),
                new RectangleF[] { new RectangleF(70, 80, 50, 40) },
                new RectangleF[]
                {
                    new RectangleF(40, 100, 30, 20),
                    new RectangleF(120, 100, 20, 20),
                    new RectangleF(40, 120, 100, 80)
                }
            };

            // Intersecting from the bottom.
            yield return new object[]
            {
                new Region(new Rectangle(40, 10, 100, 100)),
                new RectangleF[] { new RectangleF(70, 80, 50, 40) },
                new RectangleF[]
                {
                    new RectangleF(40, 10, 100, 70),
                    new RectangleF(40, 80, 30, 30),
                    new RectangleF(120, 80, 20, 30)
                }
            };

            // Multiple regions.
            yield return new object[]
            {
                new Region(new Rectangle(30, 30, 80, 80)),
                new RectangleF[]
                {
                    new RectangleF(45, 45, 200, 200),
                    new RectangleF(160, 260, 10, 10),
                    new RectangleF(170, 260, 10, 10)
                },
                new RectangleF[]
                {
                    new RectangleF(30, 30, 80, 15),
                    new RectangleF(30, 45, 15, 65)
                }
            };

            // Intersecting from the top with a larger rect.
            yield return new object[]
            {
                new Region(new Rectangle(50, 100, 100, 100)),
                new RectangleF[] { new RectangleF(30, 70, 150, 40) },
                new RectangleF[] { new RectangleF(50, 110, 100, 90) }
            };

            // Intersecting from the right with a larger rect.
            yield return new object[]
            {
                new Region(new Rectangle(70, 60, 100, 70)),
                new RectangleF[] { new RectangleF(40, 10, 100, 150) },
                new RectangleF[] { new RectangleF(140, 60, 30, 70) }
            };

            // Intersecting from the left with a larger rect.
            yield return new object[]
            {
                new Region(new Rectangle(70, 60, 100, 70)),
                new RectangleF[] { new RectangleF(100, 10, 100, 150) },
                new RectangleF[] { new RectangleF(70, 60, 30, 70) }
            };

            // Intersecting from the bottom with a larger rect.
            yield return new object[]
            {
                new Region(new Rectangle(20, 20, 100, 100)),
                new RectangleF[] { new RectangleF(10, 80, 140, 150) },
                new RectangleF[] { new RectangleF(20, 20, 100, 60) }
            };

            yield return new object[]
            {
                new Region(new Rectangle(130, 30, 60, 80)),
                new RectangleF[] { new RectangleF(170, 40, 60, 80) },
                new RectangleF[]
                {
                    new RectangleF(130, 30, 60, 10),
                    new RectangleF(130, 40, 40, 70)
                }
            };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Exclude_TestData))]
        public void Exclude_Region_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                using (var other = new Region(rect))
                {
                    region.Exclude(other);
                }
            }

            using (var matrix = new Matrix())
            {
                Assert.Equal(expectedScans, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Exclude_UnionRegion_Success()
        {
            using (var region = new Region(new RectangleF(20, 20, 20, 20)))
            using (var union = new Region(new RectangleF(20, 80, 20, 10)))
            using (var matrix = new Matrix())
            {
                union.Union(new RectangleF(60, 60, 30, 10));
                region.Exclude(union);
                Assert.Equal(new RectangleF[] { new RectangleF(20, 20, 20, 20) }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Exclude_InfiniteRegion_Success()
        {
            using (var region = new Region(new Rectangle(1, 2, 3, 4)))
            using (var other = new Region())
            using (var matrix = new Matrix())
            {
                region.Exclude(other);
                Assert.Equal(new RectangleF[0], region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Exclude_NullRegion_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("region", () => region.Exclude((Region)null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Exclude_DisposedRegion_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => new Region().Exclude(region));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Exclude_SameRegion_ThrowsInvalidOperationException()
        {
            using (var region = new Region())
            {
                Assert.Throws<InvalidOperationException>(() => region.Exclude(region));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Exclude_TestData))]
        public void Exclude_Rectangle_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    region.Exclude(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Exclude_TestData))]
        public void Exclude_RectangleF_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    region.Exclude(rect);
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Exclude_TestData))]
        public void Exclude_GraphicsPath_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    using (var path = new GraphicsPath())
                    {
                        path.AddRectangle(rect);
                        region.Exclude(path);
                    }
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Exclude_EmptyPathWithInfiniteRegion_MakesInfinite()
        {
            using (var region = new Region())
            using (var graphicsPath = new GraphicsPath())
            {
                region.Exclude(graphicsPath);
                Assert.True(region.IsInfinite(s_graphic));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Exclude_NullGraphicsPath_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("path", () => region.Exclude((GraphicsPath)null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Exclude_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            using (var graphicsPath = new GraphicsPath())
            using (var other = new Region())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => region.Exclude(graphicsPath));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Exclude(new Rectangle()));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Exclude(new RectangleF()));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Exclude(other));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FromHrgn_ValidHrgn_ReturnsExpected()
        {
            using (var region = new Region(new Rectangle(1, 2, 3, 4)))
            {
                IntPtr handle1 = region.GetHrgn(s_graphic);
                IntPtr handle2 = region.GetHrgn(s_graphic);
                Assert.NotEqual(IntPtr.Zero, handle1);
                Assert.NotEqual(handle1, handle2);

                Region newRegion = Region.FromHrgn(handle1);
                IntPtr handle3 = newRegion.GetHrgn(s_graphic);
                Assert.NotEqual(handle3, handle1);
                Assert.Equal(new RectangleF(1, 2, 3, 4), newRegion.GetBounds(s_graphic));

                region.ReleaseHrgn(handle1);
                region.ReleaseHrgn(handle2);
                newRegion.ReleaseHrgn(handle3);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void FromHrgn_ZeroHrgn_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>(null, () => Region.FromHrgn(IntPtr.Zero));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetHrgn_Infinite_ReturnsZero()
        {
            using (var region = new Region(new Rectangle(1, 2, 3, 4)))
            {
                IntPtr handle = region.GetHrgn(s_graphic);
                Assert.NotEqual(IntPtr.Zero, handle);
                region.ReleaseHrgn(handle);

                region.MakeInfinite();
                Assert.Equal(IntPtr.Zero, region.GetHrgn(s_graphic));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetHrgn_Empty_ReturnsNonZero()
        {
            using (var region = new Region())
            {
                Assert.Equal(IntPtr.Zero, region.GetHrgn(s_graphic));

                region.MakeEmpty();
                IntPtr handle = region.GetHrgn(s_graphic);
                Assert.NotEqual(IntPtr.Zero, handle);
                region.ReleaseHrgn(handle);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetHrgn_NullGraphics_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("g", () => region.GetHrgn(null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetHrgn_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => region.GetHrgn(s_graphic));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void ReleaseHrgn_ZeroHandle_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("regionHandle", () => region.ReleaseHrgn(IntPtr.Zero));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetBounds_NullGraphics_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("g", () => region.GetBounds(null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetBounds_DisposedGraphics_ThrowsArgumentException()
        {
            using (var region = new Region())
            using (var image = new Bitmap(10, 10))
            {
                var graphics = Graphics.FromImage(image);
                graphics.Dispose();
                AssertExtensions.Throws<ArgumentException>(null, () => region.GetBounds(graphics));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetBounds_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => region.GetBounds(s_graphic));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetRegionData_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => region.GetRegionData());
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetRegionScans_CustomMatrix_TransformsRegionScans()
        {
            using (var matrix = new Matrix())
            using (var region = new Region(new Rectangle(1, 2, 3, 4)))
            using (var emptyMatrix = new Matrix())
            {
                matrix.Translate(10, 11);
                matrix.Scale(5, 6);

                Assert.Equal(new RectangleF[] { new RectangleF(1, 2, 3, 4) }, region.GetRegionScans(emptyMatrix));
                Assert.Equal(new RectangleF[] { new RectangleF(15, 23, 15, 24) }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetRegionScans_NullMatrix_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("matrix", () => region.GetRegionScans(null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetRegionScans_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            using (var matrix = new Matrix())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => region.GetRegionScans(matrix));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void GetRegionScans_DisposedMatrix_ThrowsArgumentException()
        {
            using (var region = new Region())
            {
                var matrix = new Matrix();
                matrix.Dispose();
                AssertExtensions.Throws<ArgumentException>(null, () => region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Intersect_SmallerRect_Success()
        {
            using (var clipRegion = new Region())
            using (var matrix = new Matrix())
            {
                Rectangle smaller = new Rectangle(5, 5, -10, -10);

                clipRegion.Intersect(smaller);
                Assert.False(clipRegion.IsEmpty(s_graphic));
                Assert.False(clipRegion.IsInfinite(s_graphic));

                RectangleF[] rects = clipRegion.GetRegionScans(matrix);
                Assert.Equal(1, rects.Length);
                Assert.Equal(new RectangleF(-5, -5, 10, 10), rects[0]);
            }
        }

        public static IEnumerable<object[]> Intersect_TestData()
        {
            yield return new object[]
            {
                new Region(new Rectangle(500, 30, 60, 80)),
                new RectangleF[] { new RectangleF(500, 30, 60, 80) },
                new RectangleF[] { new RectangleF(500, 30, 60, 80) }
            };
            yield return new object[]
            {
                new Region(new Rectangle(0, 0, 0, 0)),
                new RectangleF[] { new RectangleF(500, 30, 60, 80) },
                new RectangleF[0]
            };

            yield return new object[]
            {
                new Region(new Rectangle(500, 30, 60, 80)),
                new RectangleF[] { RectangleF.Empty },
                new RectangleF[0]
            };

            yield return new object[]
            {
                new Region(),
                new RectangleF[] { new RectangleF(520, 40, 60, 80) },
                new RectangleF[] { new Rectangle(520, 40, 60, 80) }
            };

            yield return new object[]
            {
                new Region(),
                new RectangleF[] { RectangleF.Empty },
                new RectangleF[0]
            };

            yield return new object[]
            {
                new Region(new RectangleF(260, 30, 60, 80)),
                new RectangleF[] { new RectangleF(290, 40, 60, 90) },
                new RectangleF[] { new RectangleF(290, 40, 30, 70) }
            };

            yield return new object[]
            {
                new Region(new RectangleF(20, 330, 40, 50)),
                new RectangleF[]
                {
                    new RectangleF(50, 340, 40, 50),
                    new RectangleF(70, 360, 30, 50),
                    new RectangleF(80, 400, 30, 10)
                },
                new RectangleF[0]
            };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Intersect_TestData))]
        public void Intersect_Region_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    region.Intersect(new Region(rect));
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Intersect_InfiniteRegion_Success()
        {
            using (var region = new Region(new Rectangle(1, 2, 3, 4)))
            using (var matrix = new Matrix())
            {
                region.Intersect(new Region());

                Assert.Equal(new RectangleF[] { new Rectangle(1, 2, 3, 4) }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Intersect_NullRegion_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("region", () => region.Intersect((Region)null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Intersect_DisposedRegion_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => new Region().Intersect(region));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Intersect_SameRegion_ThrowsInvalidOperationException()
        {
            using (var region = new Region())
            {
                Assert.Throws<InvalidOperationException>(() => region.Intersect(region));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Intersect_TestData))]
        public void Intersect_Rectangle_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    region.Intersect(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Intersect_InfiniteRegionWithSmallerRectangle_Success()
        {
            using (var region = new Region())
            using (var matrix = new Matrix())
            {
                region.Intersect(new Rectangle(5, 5, -10, -10));

                Assert.False(region.IsEmpty(s_graphic));
                Assert.False(region.IsInfinite(s_graphic));
                Assert.Equal(new RectangleF[] { new RectangleF(-5, -5, 10, 10) }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Intersect_TestData))]
        public void Intersect_RectangleF_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    region.Intersect(rect);
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Intersect_InfiniteRegionWithSmallerRectangleF_Success()
        {
            using (var region = new Region())
            using (var matrix = new Matrix())
            {
                region.Intersect(new RectangleF(5, 5, -10, -10));

                Assert.False(region.IsEmpty(s_graphic));
                Assert.False(region.IsInfinite(s_graphic));
                Assert.Equal(new RectangleF[] { new RectangleF(-5, -5, 10, 10) }, region.GetRegionScans(matrix));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Intersect_TestData))]
        public void Intersect_GraphicsPath_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    using (var path = new GraphicsPath())
                    {
                        path.AddRectangle(rect);
                        region.Intersect(path);
                    }
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Intersect_EmptyPathWithInfiniteRegion_MakesEmpty()
        {
            using (var region = new Region())
            using (var graphicsPath = new GraphicsPath())
            {
                region.Intersect(graphicsPath);
                Assert.True(region.IsEmpty(s_graphic));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Intersect_NullGraphicsPath_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("path", () => region.Intersect((GraphicsPath)null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Intersect_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            using (var graphicsPath = new GraphicsPath())
            using (var other = new Region())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => region.Intersect(graphicsPath));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Intersect(new Rectangle()));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Intersect(new RectangleF()));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Intersect(other));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void IsEmpty_NullGraphics_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("g", () => region.IsEmpty(null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void IsEmpty_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => region.IsEmpty(s_graphic));
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void IsInfinite_NullGraphics_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("g", () => region.IsInfinite(null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void IsInfinite_DisposedGraphics_ThrowsArgumentException()
        {
            using (var region = new Region())
            using (var image = new Bitmap(10, 10))
            {
                var graphics = Graphics.FromImage(image);
                graphics.Dispose();
                AssertExtensions.Throws<ArgumentException>(null, () => region.IsInfinite(graphics));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void IsInfinite_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => region.IsInfinite(s_graphic));
        }

        public static IEnumerable<object[]> IsVisible_Rectangle_TestData()
        {
            var infiniteExclude = new Region();
            infiniteExclude.Exclude(new Rectangle(387, 292, 189, 133));
            infiniteExclude.Exclude(new Rectangle(387, 66, 189, 133));

            yield return new object[] { infiniteExclude, new Rectangle(66, 292, 189, 133), true };
            yield return new object[] { new Region(), Rectangle.Empty, false };

            yield return new object[] { new Region(new Rectangle(0, 0, 10, 10)), new Rectangle(0, 0, 0, 1), false };
            yield return new object[] { new Region(new Rectangle(500, 30, 60, 80)), new Rectangle(500, 30, 60, 80), true };
            yield return new object[] { new Region(new Rectangle(500, 30, 60, 80)), new Rectangle(520, 40, 60, 80), true };

            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Rectangle(1, 1, 2, 1), true };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Rectangle(1, 1, 2, 2), true };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Rectangle(1, 1, 10, 10), true };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Rectangle(1, 1, 1, 1), true };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Rectangle(2, 2, 1, 1), false };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Rectangle(0, 0, 1, 1), false };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Rectangle(3, 3, 1, 1), false };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(IsVisible_Rectangle_TestData))]
        public void IsVisible_Rectangle_ReturnsExpected(Region region, Rectangle rectangle, bool expected)
        {
            try
            {
                using (var image = new Bitmap(10, 10))
                {
                    var disposedGraphics = Graphics.FromImage(image);
                    disposedGraphics.Dispose();

                    Assert.Equal(expected, region.IsVisible(rectangle));
                    Assert.Equal(expected, region.IsVisible((RectangleF)rectangle));
                    Assert.Equal(expected, region.IsVisible(rectangle, s_graphic));
                    Assert.Equal(expected, region.IsVisible(rectangle, disposedGraphics));
                    Assert.Equal(expected, region.IsVisible(rectangle, null));
                    Assert.Equal(expected, region.IsVisible((RectangleF)rectangle, s_graphic));
                    Assert.Equal(expected, region.IsVisible((RectangleF)rectangle, disposedGraphics));
                    Assert.Equal(expected, region.IsVisible((RectangleF)rectangle, null));

                    Assert.Equal(expected, region.IsVisible(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
                    Assert.Equal(expected, region.IsVisible((float)rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
                    Assert.Equal(expected, region.IsVisible(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, s_graphic));
                    Assert.Equal(expected, region.IsVisible(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, disposedGraphics));
                    Assert.Equal(expected, region.IsVisible(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, null));
                    Assert.Equal(expected, region.IsVisible((float)rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, s_graphic));
                    Assert.Equal(expected, region.IsVisible((float)rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, disposedGraphics));
                    Assert.Equal(expected, region.IsVisible((float)rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, null));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        public static IEnumerable<object[]> IsVisible_Point_TestData()
        {
            var infiniteExclude = new Region();
            infiniteExclude.Exclude(new Rectangle(387, 292, 189, 133));
            infiniteExclude.Exclude(new Rectangle(387, 66, 189, 133));

            yield return new object[] { infiniteExclude, new Point(66, 292), true };
            yield return new object[] { new Region(), Point.Empty, true };

            yield return new object[] { new Region(new Rectangle(500, 30, 60, 80)), new Point(500, 29), false };
            yield return new object[] { new Region(new Rectangle(500, 30, 60, 80)), new Point(500, 30), true };

            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Point(0, 1), false };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Point(1, 0), false };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Point(2, 0), false };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Point(3, 0), false };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Point(1, 1), true };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Point(2, 1), true };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Point(3, 1), false };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Point(0, 2), false };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Point(2, 2), false };
            yield return new object[] { new Region(new Rectangle(1, 1, 2, 1)), new Point(3, 2), false };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(IsVisible_Point_TestData))]
        public void IsVisible_Point_ReturnsExpected(Region region, Point point, bool expected)
        {
            try
            {
                using (var image = new Bitmap(10, 10))
                {
                    var disposedGraphics = Graphics.FromImage(image);
                    disposedGraphics.Dispose();

                    Assert.Equal(expected, region.IsVisible(point));
                    Assert.Equal(expected, region.IsVisible((PointF)point));
                    Assert.Equal(expected, region.IsVisible(point, s_graphic));
                    Assert.Equal(expected, region.IsVisible(point, disposedGraphics));
                    Assert.Equal(expected, region.IsVisible(point, null));
                    Assert.Equal(expected, region.IsVisible((PointF)point, s_graphic));
                    Assert.Equal(expected, region.IsVisible((PointF)point, disposedGraphics));
                    Assert.Equal(expected, region.IsVisible((PointF)point, null));

                    Assert.Equal(expected, region.IsVisible(point.X, point.Y));
                    Assert.Equal(expected, region.IsVisible(point.X, point.Y, s_graphic));
                    Assert.Equal(expected, region.IsVisible(point.X, point.Y, disposedGraphics));
                    Assert.Equal(expected, region.IsVisible(point.X, point.Y, null));

                    Assert.Equal(expected, region.IsVisible(point.X, point.Y, s_graphic));
                    Assert.Equal(expected, region.IsVisible(point.X, point.Y, disposedGraphics));
                    Assert.Equal(expected, region.IsVisible(point.X, point.Y, null));
                    Assert.Equal(expected, region.IsVisible((float)point.X, point.Y, s_graphic));
                    Assert.Equal(expected, region.IsVisible((float)point.X, point.Y, disposedGraphics));
                    Assert.Equal(expected, region.IsVisible((float)point.X, point.Y, null));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void IsVisible_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(1f, 2f));
            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(new PointF(1, 2)));
            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(new Point(1, 2)));

            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(1f, 2f, s_graphic));
            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(new PointF(1, 2), s_graphic));
            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(new Point(1, 2), s_graphic));

            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(1f, 2f, 3f, 4f));
            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(new Rectangle(1, 2, 3, 4)));
            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(new RectangleF(1, 2, 3, 4)));

            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(1f, 2f, 3f, 4f, s_graphic));
            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(new Rectangle(1, 2, 3, 4), s_graphic));
            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(new RectangleF(1, 2, 3, 4), s_graphic));

            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(1, 2, s_graphic));
            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(1, 2, 3, 4));
            AssertExtensions.Throws<ArgumentException>(null, () => region.IsVisible(1, 2, 3, 4, s_graphic));
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Region_TestData))]
        public void MakeEmpty_NonEmpty_Success(Region region)
        {
            try
            {
                region.MakeEmpty();
                Assert.True(region.IsEmpty(s_graphic));
                Assert.False(region.IsInfinite(s_graphic));
                Assert.Equal(RectangleF.Empty, region.GetBounds(s_graphic));

                using (var matrix = new Matrix())
                {
                    Assert.Empty(region.GetRegionScans(matrix));
                }

                region.MakeEmpty();
                Assert.True(region.IsEmpty(s_graphic));
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MakeEmpty_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => region.MakeEmpty());
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Region_TestData))]
        public void MakeInfinite_NonInfinity_Success(Region region)
        {
            try
            {
                region.MakeInfinite();
                Assert.False(region.IsEmpty(s_graphic));
                Assert.True(region.IsInfinite(s_graphic));
                Assert.Equal(new RectangleF(-4194304, -4194304, 8388608, 8388608), region.GetBounds(s_graphic));

                region.MakeInfinite();
                Assert.False(region.IsEmpty(s_graphic));
                Assert.True(region.IsInfinite(s_graphic));
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void MakeInfinite_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => region.MakeInfinite());
        }

        public static IEnumerable<object[]> Union_TestData()
        {
            yield return new object[]
            {
                new Region(new Rectangle(500, 30, 60, 80)),
                new RectangleF[] { new RectangleF(500, 30, 60, 80) },
                new RectangleF[] { new RectangleF(500, 30, 60, 80) }
            };

            yield return new object[]
            {
                new Region(new Rectangle(500, 30, 60, 80)),
                new RectangleF[] { RectangleF.Empty },
                new RectangleF[] { new RectangleF(500, 30, 60, 80) }
            };

            yield return new object[]
            {
                new Region(new Rectangle(500, 30, 60, 80)),
                new RectangleF[] { new RectangleF(520, 30, 60, 80) },
                new RectangleF[] { new RectangleF(500, 30, 80, 80) }
            };

            yield return new object[]
            {
                new Region(new Rectangle(500, 30, 60, 80)),
                new RectangleF[] { new RectangleF(520, 40, 60, 80) },
                new RectangleF[]
                {
                    new RectangleF(500, 30, 60, 10),
                    new RectangleF(500, 40, 80, 70),
                    new RectangleF(520, 110, 60, 10),
                }
            };

            yield return new object[]
            {
                new Region(),
                new RectangleF[] { new RectangleF(520, 40, 60, 80) },
                new RectangleF[] { new Rectangle(-4194304, -4194304, 8388608, 8388608) }
            };

            yield return new object[]
            {
                new Region(),
                new RectangleF[] { RectangleF.Empty },
                new RectangleF[] { new Rectangle(-4194304, -4194304, 8388608, 8388608) }
            };

            // No intersecting rects.
            yield return new object[]
            {
                new Region(new Rectangle(20, 20, 20, 20)),
                new RectangleF[]
                {
                    new RectangleF(20, 80, 20, 10),
                    new RectangleF(60, 60, 30, 10)
                },
                new RectangleF[]
                {
                    new RectangleF(20, 20, 20, 20),
                    new RectangleF(60, 60, 30, 10),
                    new RectangleF(20, 80, 20, 10)
                }
            };

            yield return new object[]
            {
                new Region(new Rectangle(20, 180, 40, 50)),
                new RectangleF[]
                {
                    new RectangleF(50, 190, 40, 50),
                    new RectangleF(70, 210, 30, 50)
                },
                new RectangleF[]
                {
                    new RectangleF(20, 180, 40, 10),
                    new RectangleF(20, 190, 70, 20),
                    new RectangleF(20, 210, 80, 20),
                    new RectangleF(50, 230, 50, 10),
                    new RectangleF(70, 240, 30, 20)
                }
            };

            yield return new object[]
            {
                new Region(new Rectangle(20, 330, 40, 50)),
                new RectangleF[]
                {
                    new RectangleF(50, 340, 40, 50),
                    new RectangleF(70, 360, 30, 50),
                    new RectangleF(80, 400, 30, 10)
                },
                new RectangleF[]
                {
                    new RectangleF(20, 330, 40, 10),
                    new RectangleF(20, 340, 70, 20),
                    new RectangleF(20, 360, 80, 20),
                    new RectangleF(50, 380, 50, 10),
                    new RectangleF(70, 390, 30, 10),
                    new RectangleF(70, 400, 40, 10)
                }
            };

            yield return new object[]
            {
                new Region(new Rectangle(10, 20, 50, 50)),
                new RectangleF[]
                {
                    new RectangleF(100, 100, 60, 60),
                    new RectangleF(200, 200, 80, 80)
                },
                new RectangleF[]
                {
                    new RectangleF(10, 20, 50, 50),
                    new RectangleF(100, 100, 60, 60),
                    new RectangleF(200, 200, 80, 80)
                }
            };

            // Intersecting from the right.
            yield return new object[]
            {
                new Region(new Rectangle(10, 10, 100, 100)),
                new RectangleF[] { new RectangleF(40, 60, 100, 20) },
                new RectangleF[]
                {
                    new RectangleF(10, 10, 100, 50),
                    new RectangleF(10, 60, 130, 20),
                    new RectangleF(10, 80, 100, 30)
                }
            };

            // Intersecting from the left.
            yield return new object[]
            {
                new Region(new Rectangle(70, 10, 100, 100)),
                new RectangleF[] { new RectangleF(40, 60, 100, 20) },
                new RectangleF[]
                {
                    new RectangleF(70, 10, 100, 50),
                    new RectangleF(40, 60, 130, 20),
                    new RectangleF(70, 80, 100, 30)
                }
            };

            // Intersecting from the top.
            yield return new object[]
            {
                new Region(new Rectangle(40, 100, 100, 100)),
                new RectangleF[] { new RectangleF(70, 80, 50, 40) },
                new RectangleF[]
                {
                    new RectangleF(70, 80, 50, 20),
                    new RectangleF(40, 100, 100, 100)
                }
            };

            // Intersecting from the bottom.
            yield return new object[]
            {
                new Region(new Rectangle(40, 10, 100, 100)),
                new RectangleF[] { new RectangleF(70, 80, 50, 40) },
                new RectangleF[]
                {
                    new RectangleF(40, 10, 100, 100),
                    new RectangleF(70, 110, 50, 10)
                }
            };

            // Multiple regions separated by 0 pixels.
            yield return new object[]
            {
                new Region(new Rectangle(30, 30, 80, 80)),
                new RectangleF[]
                {
                    new RectangleF(45, 45, 200, 200),
                    new RectangleF(160, 260, 10, 10),
                    new RectangleF(170, 260, 10, 10)
                },
                new RectangleF[]
                {
                    new RectangleF(30, 30, 80, 15),
                    new RectangleF(30, 45, 215, 65),
                    new RectangleF(45, 110, 200, 135),
                    new RectangleF(160, 260, 20, 10)
                }
            };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Union_TestData))]
        public void Union_Region_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    using (var other = new Region(rect))
                    {
                        region.Union(other);
                    }
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Union_InfiniteRegion_Success()
        {
            using (var region = new Region(new Rectangle(1, 2, 3, 4)))
            using (var other = new Region())
            using (var matrix = new Matrix())
            {
                region.Union(other);

                Assert.Equal(new RectangleF[] { new Rectangle(-4194304, -4194304, 8388608, 8388608) }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Union_NullRegion_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("region", () => region.Union((Region)null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Union_DisposedRegion_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => new Region().Union(region));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Union_SameRegion_ThrowsInvalidOperationException()
        {
            using (var region = new Region())
            {
                Assert.Throws<InvalidOperationException>(() => region.Union(region));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Union_TestData))]
        public void Union_Rectangle_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    region.Union(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Union_TestData))]
        public void Union_RectangleF_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    region.Union(rect);
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Union_TestData))]
        public void Union_GraphicsPath_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    using (var path = new GraphicsPath())
                    {
                        path.AddRectangle(rect);
                        region.Union(path);
                    }
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Union_EmptyPathWithInfiniteRegion_MakesInfinite()
        {
            using (var region = new Region())
            using (var graphicsPath = new GraphicsPath())
            {
                region.Union(graphicsPath);
                Assert.True(region.IsInfinite(s_graphic));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Union_NullGraphicsPath_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("path", () => region.Union((GraphicsPath)null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Union_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            using (var graphicsPath = new GraphicsPath())
            using (var other = new Region())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => region.Union(graphicsPath));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Union(new Rectangle()));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Union(new RectangleF()));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Union(region));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_EmptyMatrix_Nop()
        {
            using (var region = new Region(new RectangleF(1, 2, 3, 4)))
            using (var matrix = new Matrix())
            {
                region.Transform(matrix);
                Assert.Equal(new RectangleF[] { new RectangleF(1, 2, 3, 4) }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_CustomMatrix_Success()
        {
            using (var region = new Region(new RectangleF(1, 2, 3, 4)))
            using (var matrix = new Matrix())
            using (var emptyMatrix = new Matrix())
            {
                matrix.Translate(10, 11);
                matrix.Scale(5, 6);

                region.Transform(matrix);
                Assert.Equal(new RectangleF[] { new RectangleF(15, 23, 15, 24) }, region.GetRegionScans(emptyMatrix));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(1, 2, 0, 0, 0)]
        [InlineData(0, 0, 2, 2, 0)]
        [InlineData(0, 0, 0.5, 0.5, 0)]
        [InlineData(0, 0, 1, 1, 45)]
        public void Transform_Infinity_Nop(int x, int y, float scaleX, float scaleY, int angle)
        {
            using (var region = new Region())
            using (var matrix = new Matrix())
            using (var emptyMatrix = new Matrix())
            {
                matrix.Translate(10, 11);
                matrix.Scale(scaleX, scaleY);
                matrix.Rotate(angle);
                
                region.Transform(matrix);
                Assert.True(region.IsInfinite(s_graphic));
                Assert.Equal(new RectangleF[] { new RectangleF(-4194304, -4194304, 8388608, 8388608) }, region.GetRegionScans(emptyMatrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Tranform_InfinityIntersectScale_Success()
        {
            using (var region = new Region())
            using (var matrix = new Matrix())
            using (var emptyMatrix = new Matrix())
            {
                matrix.Scale(2, 0.5f);

                region.Intersect(new Rectangle(-10, -10, 20, 20));
                region.Transform(matrix);
                Assert.False(region.IsInfinite(s_graphic));
                Assert.Equal(new RectangleF[] { new RectangleF(-20, -5, 40, 10) }, region.GetRegionScans(emptyMatrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Tranform_InfinityIntersectTransform_Success()
        {
            using (var region = new Region())
            using (var matrix = new Matrix(2, 0, 0, 0.5f, 10, 10))
            using (var emptyMatrix = new Matrix())
            {
                region.Intersect(new Rectangle(-10, -10, 20, 20));
                region.Transform(matrix);

                Assert.False(region.IsInfinite(s_graphic));
                Assert.Equal(new RectangleF[] { new RectangleF(-10, 5, 40, 10) }, region.GetRegionScans(emptyMatrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_NullMatrix_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("matrix", () => region.Transform(null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Transform_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            using (var matrix = new Matrix())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => region.Transform(matrix));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0, 0)]
        [InlineData(2, 3)]
        [InlineData(-2, -3)]
        public void Translate_Int_Success(float dx, float dy)
        {
            using (var region = new Region(new RectangleF(1, 2, 3, 4)))
            using (var matrix = new Matrix())
            {
                region.Translate(dx, dy);
                Assert.Equal(new RectangleF[] { new RectangleF(1 + dx, 2 + dy, 3, 4) }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Translate_IntInfinityIntersect_Success()
        {
            using (var region = new Region())
            using (var matrix = new Matrix())
            {
                region.Intersect(new Rectangle(-10, -10, 20, 20));
                region.Translate(10, 10);

                Assert.False(region.IsInfinite(s_graphic));
                Assert.Equal(new RectangleF[] { new RectangleF(0, 0, 20, 20) }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(0, 0)]
        [InlineData(2, 3)]
        public void Translate_Float_Success(int dx, int dy)
        {
            using (var region = new Region(new RectangleF(1, 2, 3, 4)))
            using (var matrix = new Matrix())
            {
                region.Translate(dx, dy);
                Assert.Equal(new RectangleF[] { new RectangleF(1 + dx, 2 + dy, 3, 4) }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Translate_FloatInfinityIntersect_Success()
        {
            using (var region = new Region())
            using (var matrix = new Matrix())
            {
                region.Intersect(new Rectangle(-10, -10, 20, 20));
                region.Translate(10f, 10f);

                Assert.False(region.IsInfinite(s_graphic));
                Assert.Equal(new RectangleF[] { new RectangleF(0, 0, 20, 20) }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Translate_Infinity_Nop()
        {
            using (var region = new Region())
            using (var matrix = new Matrix())
            {
                region.Translate(10, 10);
                region.Translate(10f, 10f);

                Assert.True(region.IsInfinite(s_graphic));
                Assert.Equal(new RectangleF[] { new RectangleF(-4194304, -4194304, 8388608, 8388608) }, region.GetRegionScans(matrix));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(float.MaxValue)]
        [InlineData(float.MinValue)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void Translate_InvalidFloatValue_EmptiesRegion(float f)
        {
            using (var region = new Region(new RectangleF(1, 2, 3, 4)))
            using (var matrix = new Matrix())
            {
                region.Translate(f, 0);

                Assert.True(region.IsEmpty(s_graphic));
                Assert.False(region.IsInfinite(s_graphic));
                Assert.Empty(region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Translate_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => region.Translate(1, 2));
            AssertExtensions.Throws<ArgumentException>(null, () => region.Translate(1f, 2f));
        }

        public static IEnumerable<object[]> Xor_TestData()
        {
            yield return new object[]
            {
                new Region(new RectangleF(500, 30, 60, 80)),
                new RectangleF[] { new RectangleF(500, 30, 60, 80) },
                new RectangleF[0]
            };

            yield return new object[]
            {
                new Region(new RectangleF(500, 30, 60, 80)),
                new RectangleF[] { RectangleF.Empty },
                new RectangleF[] { new RectangleF(500, 30, 60, 80) }
            };

            yield return new object[]
            {
                new Region(new RectangleF(0, 0, 0, 0)),
                new RectangleF[] { new RectangleF(500, 30, 60, 80) },
                new RectangleF[] { new RectangleF(500, 30, 60, 80) }
            };

            yield return new object[]
            {
                new Region(),
                new RectangleF[] { new RectangleF(520, 40, 60, 80) },
                new RectangleF[]
                {
                    new RectangleF(-4194304, -4194304, 8388608, 4194344),
                    new RectangleF(-4194304, 40, 4194824, 80),
                    new RectangleF(580, 40, 4193724, 80),
                    new RectangleF(-4194304, 120, 8388608, 4194184)
                }
            };

            yield return new object[]
            {
                new Region(),
                new RectangleF[] { RectangleF.Empty },
                new RectangleF[] { new Rectangle(-4194304, -4194304, 8388608, 8388608) }
            };

            yield return new object[]
            {
                new Region(new RectangleF(380, 30, 60, 80)),
                new RectangleF[] { new RectangleF(410, 40, 60, 80) },
                new RectangleF[]
                {
                    new RectangleF(380, 30, 60, 10),
                    new RectangleF(380, 40, 30, 70),
                    new RectangleF(440, 40, 30, 70),
                    new RectangleF(410, 110, 60, 10)
                }
            };
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Xor_TestData))]
        public void Xor_Region_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    using (var other = new Region(rect))
                    {
                        region.Xor(other);
                    }
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Xor_InfiniteRegion_Success()
        {
            using (var region = new Region(new Rectangle(1, 2, 3, 4)))
            using (var other = new Region())
            using (var matrix = new Matrix())
            {
                region.Xor(other);

                Assert.Equal(new RectangleF[]
                {
                    new RectangleF(-4194304, -4194304, 8388608, 4194306),
                    new RectangleF(-4194304, 2, 4194305, 4),
                    new RectangleF(4, 2, 4194300, 4),
                    new RectangleF(-4194304, 6, 8388608, 4194298)
                }, region.GetRegionScans(matrix));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Xor_NullRegion_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("region", () => region.Xor((Region)null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Xor_DisposedRegion_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            AssertExtensions.Throws<ArgumentException>(null, () => new Region().Xor(region));
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Xor_SameRegion_ThrowsInvalidOperationException()
        {
            using (var region = new Region())
            {
                Assert.Throws<InvalidOperationException>(() => region.Xor(region));
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Xor_TestData))]
        public void Xor_Rectangle_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    region.Xor(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Xor_TestData))]
        public void Xor_RectangleF_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    region.Xor(rect);
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Xor_TestData))]
        public void Xor_GraphicsPath_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            try
            {
                foreach (RectangleF rect in rectangles)
                {
                    using (var path = new GraphicsPath())
                    {
                        path.AddRectangle(rect);
                        region.Xor(path);
                    }
                }

                using (var matrix = new Matrix())
                {
                    Assert.Equal(expectedScans, region.GetRegionScans(matrix));
                }
            }
            finally
            {
                region.Dispose();
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Xor_EmptyPathWithInfiniteRegion_MakesInfinite()
        {
            using (var region = new Region())
            using (var graphicsPath = new GraphicsPath())
            {
                region.Xor(graphicsPath);
                Assert.True(region.IsInfinite(s_graphic));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Xor_NullGraphicsPath_ThrowsArgumentNullException()
        {
            using (var region = new Region())
            {
                AssertExtensions.Throws<ArgumentNullException>("path", () => region.Xor((GraphicsPath)null));
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Xor_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();

            using (var graphicsPath = new GraphicsPath())
            using (var other = new Region())
            {
                AssertExtensions.Throws<ArgumentException>(null, () => region.Xor(graphicsPath));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Xor(new Rectangle()));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Xor(new RectangleF()));
                AssertExtensions.Throws<ArgumentException>(null, () => region.Xor(other));
            }
        }
    }
}

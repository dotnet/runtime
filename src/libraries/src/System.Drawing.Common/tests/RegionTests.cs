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
//

using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Drawing.Tests
{
    public class RegionTests
    {
        private static readonly Graphics s_graphic = Graphics.FromImage(new Bitmap(1, 1));

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Ctor_Default()
        {
            var region = new Region();
            Assert.False(region.IsEmpty(s_graphic));
            Assert.True(region.IsInfinite(s_graphic));
            Assert.Equal(new RectangleF(-4194304, -4194304, 8388608, 8388608), region.GetBounds(s_graphic));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(-1, -2, -3, -4, true)]
        [InlineData(0, 0, 0, 0, true)]
        [InlineData(1, 2, 3, 4, false)]
        public void Ctor_Rectangle(int x, int y, int width, int height, bool isEmpty)
        {
            var rectangle = new Rectangle(x, y, width, height);
            
            var region = new Region(rectangle);
            Assert.Equal(isEmpty, region.IsEmpty(s_graphic));
            Assert.False(region.IsInfinite(s_graphic));
            Assert.Equal(new RectangleF(x, y, width, height), region.GetBounds(s_graphic));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(1, 2, 3, float.NegativeInfinity, true)]
        [InlineData(-1, -2, -3, -4, true)]
        [InlineData(0, 0, 0, 0, true)]
        [InlineData(1, 2, 3, 4, false)]
        [InlineData(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue, true)]
        public void Ctor_RectangleF(float x, float y, float width, float height, bool isEmpty)
        {
            var rectangle = new RectangleF(x, y, width, height);

            var region = new Region(rectangle);
            Assert.Equal(isEmpty, region.IsEmpty(s_graphic));
            Assert.False(region.IsInfinite(s_graphic));
            Assert.Equal(rectangle, region.GetBounds(s_graphic));
        }

        public static IEnumerable<object[]> Region_TestData()
        {
            yield return new object[] { new Region() };
            yield return new object[] { new Region(new Rectangle(0, 0, 0, 0)) };
            yield return new object[] { new Region(new Rectangle(1, 2, 3, 4)) };
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Region_TestData))]
        public void Ctor_RegionData(Region region)
        {
            var otherRegion = new Region(region.GetRegionData());

            Assert.Equal(region.GetBounds(s_graphic), otherRegion.GetBounds(s_graphic));
            Assert.Equal(region.GetRegionScans(new Matrix()), otherRegion.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Ctor_NullRegionData_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("rgnData", () => new Region((RegionData)null));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(256)]
        public void Ctor_InvalidRegionData_ThrowsExternalException(int dataLength)
        {
            RegionData regionData = new Region().GetRegionData();
            regionData.Data = new byte[dataLength];
            Assert.Throws<ExternalException>(() => new Region(regionData));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Ctor_EmptyGraphicsPath_ThrowsExternalException()
        {
            var region = new Region(new GraphicsPath());
            RegionData regionData = region.GetRegionData();
            Assert.Throws<ExternalException>(() => new Region(regionData));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Ctor_NullDataInRegionData_ThrowsNullReferenceException()
        {
            RegionData regionData = new Region().GetRegionData();
            regionData.Data = null;
            Assert.Throws<NullReferenceException>(() => new Region(regionData));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Ctor_GraphicsPath()
        {
            var graphicsPath = new GraphicsPath();
            graphicsPath.AddRectangle(new Rectangle(1, 2, 3, 4));
            graphicsPath.AddRectangle(new Rectangle(4, 5, 6, 7));

            var region = new Region(graphicsPath);
            Assert.Equal(new RectangleF[]
            {
                new RectangleF(1, 2, 3, 3),
                new RectangleF(1, 5, 9, 1),
                new RectangleF(4, 6, 6, 6)
            }, region.GetRegionScans(new Matrix()));
            Assert.Equal(new RectangleF(1, 2, 9, 10), region.GetBounds(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Ctor_EmptyGraphicsPath()
        {
            var region = new Region(new GraphicsPath());
            Assert.True(region.IsEmpty(s_graphic));
            Assert.Empty(region.GetRegionScans(new Matrix()));
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

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Ctor_InfiniteGraphicsPath_TestData))]
        public void Ctor_InfiniteGraphicsPath_IsInfinite(GraphicsPath path, bool isInfinite)
        {
            var region = new Region(path);
            Assert.Equal(isInfinite, region.IsInfinite(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Ctor_GraphicsPathTooLarge_SetsToEmpty()
        {
            var path = new GraphicsPath();
            path.AddCurve(new Point[] { new Point(-4194304, -4194304), new Point(4194304, 4194304) });

            var rect = new Region(path);
            Assert.Empty(rect.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Ctor_NullGraphicsPath_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("path", () => new Region((GraphicsPath)null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Ctor_DisposedGraphicsPath_ThrowsArgumentException()
        {
            var path = new GraphicsPath();
            path.Dispose();
            Assert.Throws<ArgumentException>(null, () => new Region(path));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Region_TestData))]
        public void Clone(Region region)
        {
            Region clone = Assert.IsType<Region>(region.Clone());
            Assert.NotSame(region, clone);

            Assert.Equal(region.GetBounds(s_graphic), clone.GetBounds(s_graphic));
            Assert.Equal(region.GetRegionScans(new Matrix()), clone.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Clone_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.Clone());
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

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Complement_TestData))]
        public void Complement_Region_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Complement(new Region(rect));
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Complement_UnionRegion_Success()
        {
            var complement = new Region(new Rectangle(20, 80, 20, 10));
            complement.Union(new Rectangle(60, 60, 30, 10));

            var region = new Region(new Rectangle(20, 20, 20, 20));
            region.Complement(complement);

            Assert.Equal(new RectangleF[]
            {
                new RectangleF(60, 60, 30, 10),
                new RectangleF(20, 80, 20, 10)
            }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Complement_InfiniteAndWithIntersectRegion_Success()
        {
            var region = new Region();
            region.Intersect(new Rectangle(5, 5, -10, -10));
            region.Complement(new Rectangle(-5, -5, 12, 12));

            Assert.False(region.IsEmpty(s_graphic));
            Assert.False(region.IsInfinite(s_graphic));
            Assert.Equal(new RectangleF[]
            {
                new RectangleF(5, -5, 2, 10),
                new RectangleF(-5, 5, 12, 2)
            }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Complement_InfiniteRegion_Success()
        {
            var region = new Region(new Rectangle(1, 2, 3, 4));
            region.Complement(new Region());

            Assert.Equal(new RectangleF[]
            {
                new RectangleF(-4194304, -4194304, 8388608, 4194306),
                new RectangleF(-4194304, 2, 4194305, 4),
                new RectangleF(4, 2, 4194300, 4),
                new RectangleF(-4194304, 6, 8388608, 4194298)
            }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Complement_NullRegion_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("region", () => region.Complement((Region)null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Complement_DisposedRegion_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => new Region().Complement(region));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Complement_SameRegion_ThrowsInvalidOperationException()
        {
            var region = new Region();
            Assert.Throws<InvalidOperationException>(() => region.Complement(region));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Complement_TestData))]
        public void Complement_Rectangle_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Complement(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Complement_TestData))]
        public void Complement_RectangleF_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Complement(rect);
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Complement_TestData))]
        public void Complement_GraphicsPath_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                var path = new GraphicsPath();
                path.AddRectangle(rect);
                region.Complement(path);
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Complement_GraphicsPathWithMultipleRectangles_Success()
        {
            Graphics graphics = Graphics.FromImage(new Bitmap(600, 800));

            var rect1 = new Rectangle(20, 30, 60, 80);
            var rect2 = new Rectangle(50, 40, 60, 80);
            var region1 = new Region(rect1);
            var region2 = new Region(rect2);
            graphics.DrawRectangle(Pens.Green, rect1);
            graphics.DrawRectangle(Pens.Red, rect2);

            region1.Complement(region2);
            graphics.FillRegion(Brushes.Blue, region1);
            graphics.DrawRectangles(Pens.Yellow, region1.GetRegionScans(new Matrix()));

            Assert.Equal(new RectangleF[]
            {
                new RectangleF(80, 40, 30, 70),
                new RectangleF(50, 110, 60, 10)
            }, region1.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Complement_EmptyPathWithInfiniteRegion_MakesEmpty()
        {
            var region = new Region();
            region.Complement(new GraphicsPath());
            Assert.True(region.IsEmpty(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Complement_NullGraphicsPath_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("path", () => region.Complement((GraphicsPath)null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Complement_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.Complement(new GraphicsPath()));
            Assert.Throws<ArgumentException>(null, () => region.Complement(new Rectangle()));
            Assert.Throws<ArgumentException>(null, () => region.Complement(new RectangleF()));
            Assert.Throws<ArgumentException>(null, () => region.Complement(new Region()));
        }

        public static IEnumerable<object[]> Equals_TestData()
        {
            Func<Region> empty = () =>
            {
                var emptyRegion = new Region();
                emptyRegion.MakeEmpty();
                return emptyRegion;
            };

            var region = new Region();
            yield return new object[] { region, region, true };
            yield return new object[] { region, new Region(), true };
            yield return new object[] { region, empty(), false };
            yield return new object[] { region, new Region(new Rectangle(1, 2, 3, 4)), false };

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

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Equals_TestData))]
        public void Equals_Valid_ReturnsExpected(Region region, Region other, bool expected)
        {
            Assert.Equal(expected, region.Equals(other, s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Equals_NullRegion_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("region", () => region.Equals(null, s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Equals_NullGraphics_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("g", () => region.Equals(new Region(), null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Equals_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.Equals(new Region(), s_graphic));
            Assert.Throws<ArgumentException>(null, () => new Region().Equals(region, s_graphic));
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

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Exclude_TestData))]
        public void Exclude_Region_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Exclude(new Region(rect));
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Exclude_UnionRegion_Success()
        {
            var union = new Region(new RectangleF(20, 80, 20, 10));
            union.Union(new RectangleF(60, 60, 30, 10));

            var region = new Region(new RectangleF(20, 20, 20, 20));
            region.Exclude(union);
            Assert.Equal(new RectangleF[] { new RectangleF(20, 20, 20, 20) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Exclude_InfiniteRegion_Success()
        {
            var region = new Region(new Rectangle(1, 2, 3, 4));
            region.Exclude(new Region());

            Assert.Equal(new RectangleF[0], region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Exclude_NullRegion_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("region", () => region.Exclude((Region)null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Exclude_DisposedRegion_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => new Region().Exclude(region));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Exclude_SameRegion_ThrowsInvalidOperationException()
        {
            var region = new Region();
            Assert.Throws<InvalidOperationException>(() => region.Exclude(region));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Exclude_TestData))]
        public void Exclude_Rectangle_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Exclude(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Exclude_TestData))]
        public void Exclude_RectangleF_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Exclude(rect);
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Exclude_TestData))]
        public void Exclude_GraphicsPath_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                var path = new GraphicsPath();
                path.AddRectangle(rect);
                region.Exclude(path);
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Exclude_EmptyPathWithInfiniteRegion_MakesInfinite()
        {
            var region = new Region();
            region.Exclude(new GraphicsPath());
            Assert.True(region.IsInfinite(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Exclude_NullGraphicsPath_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("path", () => region.Exclude((GraphicsPath)null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Exclude_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.Exclude(new GraphicsPath()));
            Assert.Throws<ArgumentException>(null, () => region.Exclude(new Rectangle()));
            Assert.Throws<ArgumentException>(null, () => region.Exclude(new RectangleF()));
            Assert.Throws<ArgumentException>(null, () => region.Exclude(new Region()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void FromHrgn_ValidHrgn_ReturnsExpected()
        {
            var region = new Region(new Rectangle(1, 2, 3, 4));
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

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void FromHrgn_ZeroHrgn_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(null, () => Region.FromHrgn(IntPtr.Zero));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetHrgn_Infinite_ReturnsZero()
        {
            var region = new Region(new Rectangle(1, 2, 3, 4));
            IntPtr handle = region.GetHrgn(s_graphic);
            Assert.NotEqual(IntPtr.Zero, handle);
            region.ReleaseHrgn(handle);

            region.MakeInfinite();
            Assert.Equal(IntPtr.Zero, region.GetHrgn(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetHrgn_Empty_ReturnsNonZero()
        {
            var region = new Region();
            Assert.Equal(IntPtr.Zero, region.GetHrgn(s_graphic));

            region.MakeEmpty();
            IntPtr handle = region.GetHrgn(s_graphic);
            Assert.NotEqual(IntPtr.Zero, handle);
            region.ReleaseHrgn(handle);
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetHrgn_NullGraphics_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("g", () => region.GetHrgn(null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetHrgn_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.GetHrgn(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void ReleaseHrgn_ZeroHandle_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("regionHandle", () => region.ReleaseHrgn(IntPtr.Zero));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetBounds_NullGraphics_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("g", () => region.GetBounds(null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetBounds_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.GetBounds(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetRegionData_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.GetRegionData());
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetRegionScans_CustomMatrix_TransformsRegionScans()
        {
            var matrix = new Matrix();
            matrix.Translate(10, 11);
            matrix.Scale(5, 6);

            var region = new Region(new Rectangle(1, 2, 3, 4));
            Assert.Equal(new RectangleF[] { new RectangleF(1, 2, 3, 4) }, region.GetRegionScans(new Matrix()));
            Assert.Equal(new RectangleF[] { new RectangleF(15, 23, 15, 24) }, region.GetRegionScans(matrix));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetRegionScans_NullMatrix_ThrowsArgumentNullException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentNullException>("matrix", () => region.GetRegionScans(null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetRegionScans_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void GetRegionScans_DisposedMatrix_ThrowsArgumentException()
        {
            var region = new Region();
            var matrix = new Matrix();
            matrix.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.GetRegionScans(matrix));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Test()
        {
            var clipRegion = new Region();

            Rectangle smaller = new Rectangle(5, 5, -10, -10);

            clipRegion.Intersect(smaller);
            Assert.False(clipRegion.IsEmpty(s_graphic), "IsEmpty");
            Assert.False(clipRegion.IsInfinite(s_graphic), "IsInfinite");

            RectangleF[] rects = clipRegion.GetRegionScans(new Matrix());
            Assert.Equal(1, rects.Length);
            Assert.Equal(new RectangleF(-5, -5, 10, 10), rects[0]);
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

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Intersect_TestData))]
        public void Intersect_Region_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Intersect(new Region(rect));
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Intersect_InfiniteRegion_Success()
        {
            var region = new Region(new Rectangle(1, 2, 3, 4));
            region.Intersect(new Region());

            Assert.Equal(new RectangleF[] { new Rectangle(1, 2, 3, 4) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Intersect_NullRegion_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("region", () => region.Intersect((Region)null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Intersect_DisposedRegion_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => new Region().Intersect(region));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Intersect_SameRegion_ThrowsInvalidOperationException()
        {
            var region = new Region();
            Assert.Throws<InvalidOperationException>(() => region.Intersect(region));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Intersect_TestData))]
        public void Intersect_Rectangle_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Intersect(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Intersect_InfiniteRegionWithSmallerRectangle_Success()
        {
            var region = new Region();
            region.Intersect(new Rectangle(5, 5, -10, -10));

            Assert.False(region.IsEmpty(s_graphic));
            Assert.False(region.IsInfinite(s_graphic));
            Assert.Equal(new RectangleF[] { new RectangleF(-5, -5, 10, 10) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Intersect_TestData))]
        public void Intersect_RectangleF_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Intersect(rect);
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Intersect_InfiniteRegionWithSmallerRectangleF_Success()
        {
            var region = new Region();
            region.Intersect(new RectangleF(5, 5, -10, -10));

            Assert.False(region.IsEmpty(s_graphic));
            Assert.False(region.IsInfinite(s_graphic));
            Assert.Equal(new RectangleF[] { new RectangleF(-5, -5, 10, 10) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Intersect_TestData))]
        public void Intersect_GraphicsPath_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                var path = new GraphicsPath();
                path.AddRectangle(rect);
                region.Intersect(path);
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Intersect_EmptyPathWithInfiniteRegion_MakesEmpty()
        {
            var region = new Region();
            region.Intersect(new GraphicsPath());
            Assert.True(region.IsEmpty(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Intersect_NullGraphicsPath_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("path", () => region.Intersect((GraphicsPath)null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Intersect_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.Intersect(new GraphicsPath()));
            Assert.Throws<ArgumentException>(null, () => region.Intersect(new Rectangle()));
            Assert.Throws<ArgumentException>(null, () => region.Intersect(new RectangleF()));
            Assert.Throws<ArgumentException>(null, () => region.Intersect(new Region()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void IsEmpty_NullGraphics_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("g", () => region.IsEmpty(null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void IsEmpty_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.IsEmpty(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void IsInfinite_NullGraphics_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("g", () => region.IsInfinite(null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void IsInfinite_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.IsInfinite(s_graphic));
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

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(IsVisible_Rectangle_TestData))]
        public void IsVisible_Rectangle_ReturnsExpected(Region region, Rectangle rectangle, bool expected)
        {
            Assert.Equal(expected, region.IsVisible(rectangle));
            Assert.Equal(expected, region.IsVisible((RectangleF)rectangle));
            Assert.Equal(expected, region.IsVisible(rectangle, s_graphic));
            Assert.Equal(expected, region.IsVisible(rectangle, null));
            Assert.Equal(expected, region.IsVisible((RectangleF)rectangle, s_graphic));
            Assert.Equal(expected, region.IsVisible((RectangleF)rectangle, null));

            Assert.Equal(expected, region.IsVisible(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
            Assert.Equal(expected, region.IsVisible((float)rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height));
            Assert.Equal(expected, region.IsVisible(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, s_graphic));
            Assert.Equal(expected, region.IsVisible(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, null));
            Assert.Equal(expected, region.IsVisible((float)rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, s_graphic));
            Assert.Equal(expected, region.IsVisible((float)rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, null));
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

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(IsVisible_Point_TestData))]
        public void IsVisible_Point_ReturnsExpected(Region region, Point point, bool expected)
        {
            Assert.Equal(expected, region.IsVisible(point));
            Assert.Equal(expected, region.IsVisible((PointF)point));
            Assert.Equal(expected, region.IsVisible(point, s_graphic));
            Assert.Equal(expected, region.IsVisible(point, null));
            Assert.Equal(expected, region.IsVisible((PointF)point, s_graphic));
            Assert.Equal(expected, region.IsVisible((PointF)point, null));
            
            Assert.Equal(expected, region.IsVisible(point.X, point.Y));
            Assert.Equal(expected, region.IsVisible(point.X, point.Y, s_graphic));
            Assert.Equal(expected, region.IsVisible(point.X, point.Y, null));

            Assert.Equal(expected, region.IsVisible(point.X, point.Y, s_graphic));
            Assert.Equal(expected, region.IsVisible(point.X, point.Y, null));
            Assert.Equal(expected, region.IsVisible((float)point.X, point.Y, s_graphic));
            Assert.Equal(expected, region.IsVisible((float)point.X, point.Y, null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void IsVisible_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.IsVisible(1f, 2f));
            Assert.Throws<ArgumentException>(null, () => region.IsVisible(new PointF(1, 2)));
            Assert.Throws<ArgumentException>(null, () => region.IsVisible(new Point(1, 2)));

            Assert.Throws<ArgumentException>(null, () => region.IsVisible(1f, 2f, s_graphic));
            Assert.Throws<ArgumentException>(null, () => region.IsVisible(new PointF(1, 2), s_graphic));
            Assert.Throws<ArgumentException>(null, () => region.IsVisible(new Point(1, 2), s_graphic));

            Assert.Throws<ArgumentException>(null, () => region.IsVisible(1f, 2f, 3f, 4f));
            Assert.Throws<ArgumentException>(null, () => region.IsVisible(new Rectangle(1, 2, 3, 4)));
            Assert.Throws<ArgumentException>(null, () => region.IsVisible(new RectangleF(1, 2, 3, 4)));

            Assert.Throws<ArgumentException>(null, () => region.IsVisible(1f, 2f, 3f, 4f, s_graphic));
            Assert.Throws<ArgumentException>(null, () => region.IsVisible(new Rectangle(1, 2, 3, 4), s_graphic));
            Assert.Throws<ArgumentException>(null, () => region.IsVisible(new RectangleF(1, 2, 3, 4), s_graphic));

            Assert.Throws<ArgumentException>(null, () => region.IsVisible(1, 2, s_graphic));
            Assert.Throws<ArgumentException>(null, () => region.IsVisible(1, 2, 3, 4));
            Assert.Throws<ArgumentException>(null, () => region.IsVisible(1, 2, 3, 4, s_graphic));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Region_TestData))]
        public void MakeEmpty_NonEmpty_Success(Region region)
        {
            region.MakeEmpty();
            Assert.True(region.IsEmpty(s_graphic));
            Assert.False(region.IsInfinite(s_graphic));
            Assert.Equal(RectangleF.Empty, region.GetBounds(s_graphic));
            Assert.Empty(region.GetRegionScans(new Matrix()));

            region.MakeEmpty();
            Assert.True(region.IsEmpty(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void MakeEmpty_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.MakeEmpty());
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Region_TestData))]
        public void MakeInfinite_NonInfinity_Success(Region region)
        {
            region.MakeInfinite();
            Assert.False(region.IsEmpty(s_graphic));
            Assert.True(region.IsInfinite(s_graphic));
            Assert.Equal(new RectangleF(-4194304, -4194304, 8388608, 8388608), region.GetBounds(s_graphic));

            region.MakeInfinite();
            Assert.False(region.IsEmpty(s_graphic));
            Assert.True(region.IsInfinite(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void MakeInfinite_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.MakeInfinite());
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

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Union_TestData))]
        public void Union_Region_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Union(new Region(rect));
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Union_InfiniteRegion_Success()
        {
            var region = new Region(new Rectangle(1, 2, 3, 4));
            region.Union(new Region());

            Assert.Equal(new RectangleF[] { new Rectangle(-4194304, -4194304, 8388608, 8388608) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Union_NullRegion_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("region", () => region.Union((Region)null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Union_DisposedRegion_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => new Region().Union(region));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Union_SameRegion_ThrowsInvalidOperationException()
        {
            var region = new Region();
            Assert.Throws<InvalidOperationException>(() => region.Union(region));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Union_TestData))]
        public void Union_Rectangle_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Union(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Union_TestData))]
        public void Union_RectangleF_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Union(rect);
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Union_TestData))]
        public void Union_GraphicsPath_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                var path = new GraphicsPath();
                path.AddRectangle(rect);
                region.Union(path);
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Union_EmptyPathWithInfiniteRegion_MakesInfinite()
        {
            var region = new Region();
            region.Union(new GraphicsPath());
            Assert.True(region.IsInfinite(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Union_NullGraphicsPath_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("path", () => region.Union((GraphicsPath)null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Union_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.Union(new GraphicsPath()));
            Assert.Throws<ArgumentException>(null, () => region.Union(new Rectangle()));
            Assert.Throws<ArgumentException>(null, () => region.Union(new RectangleF()));
            Assert.Throws<ArgumentException>(null, () => region.Union(new Region()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Transform_EmptyMatrix_Nop()
        {
            var region = new Region(new RectangleF(1, 2, 3, 4));
            region.Transform(new Matrix());
            Assert.Equal(new RectangleF[] { new RectangleF(1, 2, 3, 4) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Transform_CustomMatrix_Success()
        {
            var matrix = new Matrix();
            matrix.Translate(10, 11);
            matrix.Scale(5, 6);

            var region = new Region(new RectangleF(1, 2, 3, 4));
            region.Transform(matrix);
            Assert.Equal(new RectangleF[] { new RectangleF(15, 23, 15, 24) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(1, 2, 0, 0, 0)]
        [InlineData(0, 0, 2, 2, 0)]
        [InlineData(0, 0, 0.5, 0.5, 0)]
        [InlineData(0, 0, 1, 1, 45)]
        public void Transform_Infinity_Nop(int x, int y, float scaleX, float scaleY, int angle)
        {
            var matrix = new Matrix();
            matrix.Translate(10, 11);
            matrix.Scale(scaleX, scaleY);
            matrix.Rotate(angle);

            var region = new Region();
            region.Transform(matrix);

            Assert.True(region.IsInfinite(s_graphic));
            Assert.Equal(new RectangleF[] { new RectangleF(-4194304, -4194304, 8388608, 8388608) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Tranform_InfinityIntersectScale_Success()
        {
            var matrix = new Matrix();
            matrix.Scale(2, 0.5f);

            var region = new Region();
            region.Intersect(new Rectangle(-10, -10, 20, 20));
            region.Transform(matrix);

            Assert.False(region.IsInfinite(s_graphic));
            Assert.Equal(new RectangleF[] { new RectangleF(-20, -5, 40, 10) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Tranform_InfinityIntersectTransform_Success()
        {
            var region = new Region();
            region.Intersect(new Rectangle(-10, -10, 20, 20));
            region.Transform(new Matrix(2, 0, 0, 0.5f, 10, 10));

            Assert.False(region.IsInfinite(s_graphic));
            Assert.Equal(new RectangleF[] { new RectangleF(-10, 5, 40, 10) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Transform_NullMatrix_ThrowsArgumentNullException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentNullException>("matrix", () => region.Transform(null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Transform_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.Transform(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(0, 0)]
        [InlineData(2, 3)]
        [InlineData(-2, -3)]
        public void Translate_Int_Success(float dx, float dy)
        {
            var region = new Region(new RectangleF(1, 2, 3, 4));
            region.Translate(dx, dy);
            Assert.Equal(new RectangleF[] { new RectangleF(1 + dx, 2 + dy, 3, 4) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Translate_IntInfinityIntersect_Success()
        {
            var region = new Region();
            region.Intersect(new Rectangle(-10, -10, 20, 20));
            region.Translate(10, 10);

            Assert.False(region.IsInfinite(s_graphic));
            Assert.Equal(new RectangleF[] { new RectangleF(0, 0, 20, 20) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(0, 0)]
        [InlineData(2, 3)]
        public void Translate_Float_Success(int dx, int dy)
        {
            var region = new Region(new RectangleF(1, 2, 3, 4));
            region.Translate(dx, dy);
            Assert.Equal(new RectangleF[] { new RectangleF(1 + dx, 2 + dy, 3, 4) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Translate_FloatInfinityIntersect_Success()
        {
            var region = new Region();
            region.Intersect(new Rectangle(-10, -10, 20, 20));
            region.Translate(10f, 10f);

            Assert.False(region.IsInfinite(s_graphic));
            Assert.Equal(new RectangleF[] { new RectangleF(0, 0, 20, 20) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Translate_Infinity_Nop()
        {
            var region = new Region();
            region.Translate(10, 10);
            region.Translate(10f, 10f);

            Assert.True(region.IsInfinite(s_graphic));
            Assert.Equal(new RectangleF[] { new RectangleF(-4194304, -4194304, 8388608, 8388608) }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(float.MaxValue)]
        [InlineData(float.MinValue)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void Translate_InvalidFloatValue_EmptiesRegion(float f)
        {
            var region = new Region(new RectangleF(1, 2, 3, 4));
            region.Translate(f, 0);

            Assert.True(region.IsEmpty(s_graphic));
            Assert.False(region.IsInfinite(s_graphic));
            Assert.Empty(region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Translate_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.Translate(1, 2));
            Assert.Throws<ArgumentException>(null, () => region.Translate(1f, 2f));
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

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Xor_TestData))]
        public void Xor_Region_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Xor(new Region(rect));
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Xor_InfiniteRegion_Success()
        {
            var region = new Region(new Rectangle(1, 2, 3, 4));
            region.Xor(new Region());

            Assert.Equal(new RectangleF[]
            {
                new RectangleF(-4194304, -4194304, 8388608, 4194306),
                new RectangleF(-4194304, 2, 4194305, 4),
                new RectangleF(4, 2, 4194300, 4),
                new RectangleF(-4194304, 6, 8388608, 4194298)
            }, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Xor_NullRegion_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("region", () => region.Xor((Region)null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Xor_DisposedRegion_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => new Region().Xor(region));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Xor_SameRegion_ThrowsInvalidOperationException()
        {
            var region = new Region();
            Assert.Throws<InvalidOperationException>(() => region.Xor(region));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Xor_TestData))]
        public void Xor_Rectangle_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Xor(new Rectangle((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height));
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Xor_TestData))]
        public void Xor_RectangleF_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                region.Xor(rect);
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalTheory(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [MemberData(nameof(Xor_TestData))]
        public void Xor_GraphicsPath_Success(Region region, RectangleF[] rectangles, RectangleF[] expectedScans)
        {
            foreach (RectangleF rect in rectangles)
            {
                var path = new GraphicsPath();
                path.AddRectangle(rect);
                region.Xor(path);
            }
            Assert.Equal(expectedScans, region.GetRegionScans(new Matrix()));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Xor_EmptyPathWithInfiniteRegion_MakesInfinite()
        {
            var region = new Region();
            region.Xor(new GraphicsPath());
            Assert.True(region.IsInfinite(s_graphic));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Xor_NullGraphicsPath_ThrowsArgumentNullException()
        {
            var region = new Region();
            Assert.Throws<ArgumentNullException>("path", () => region.Xor((GraphicsPath)null));
        }

        [ConditionalFact(nameof(PlatformDetection) + "." + nameof(PlatformDetection.IsNotWindowsNanoServer))]
        public void Xor_Disposed_ThrowsArgumentException()
        {
            var region = new Region();
            region.Dispose();
            Assert.Throws<ArgumentException>(null, () => region.Xor(new GraphicsPath()));
            Assert.Throws<ArgumentException>(null, () => region.Xor(new Rectangle()));
            Assert.Throws<ArgumentException>(null, () => region.Xor(new RectangleF()));
            Assert.Throws<ArgumentException>(null, () => region.Xor(new Region()));
        }
    }
}

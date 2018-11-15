// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Drawing.Drawing2D.Tests
{
    public class AdjustableArrowCapTests
    {
        public static IEnumerable<object[]> Ctor_Float_Float_TestData()
        {
            yield return new object[] { 1f, 1f };
            yield return new object[] { 50f, 50f };
            yield return new object[] { float.MaxValue, float.MaxValue };
            // Nonsensical values -- but still permitted.
            yield return new object[] { -1f, 1f };
            yield return new object[] { float.PositiveInfinity, 1f };
            yield return new object[] { float.NegativeInfinity, 1f };
            yield return new object[] { float.NaN, 1f };
            yield return new object[] { 0f, 1f };
            yield return new object[] { 0f, 0f };
            yield return new object[] { 1f, -1f };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Float_Float_TestData))]
        public void Ctor_Float_Float(float width, float height)
        {
            using (AdjustableArrowCap arrowCap = new AdjustableArrowCap(width, height))
            {
                Assert.Equal(width, arrowCap.Width);
                Assert.Equal(height, arrowCap.Height);
                Assert.Equal(true, arrowCap.Filled);
            }
        }

        public static IEnumerable<object[]> Ctor_Float_Float_Bool_TestData()
        {
            foreach (object[] data in Ctor_Float_Float_TestData())
            {
                yield return new object[] { data[0], data[1], true };
                yield return new object[] { data[0], data[1], false };
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Ctor_Float_Float_Bool_TestData))]
        public void Ctor_Float_Float_Bool(float width, float height, bool filled)
        {
            using (AdjustableArrowCap arrowCap = new AdjustableArrowCap(width, height, filled))
            {
                Assert.Equal(width, arrowCap.Width);
                Assert.Equal(height, arrowCap.Height);
                Assert.Equal(filled, arrowCap.Filled);
            }
        }

        public static IEnumerable<object[]> Properties_TestData()
        {
            yield return new object[] { -1 };
            yield return new object[] { 0 };
            yield return new object[] { 10 };
            yield return new object[] { 5000 };
            yield return new object[] { float.MaxValue };
            yield return new object[] { float.PositiveInfinity };
            yield return new object[] { float.NegativeInfinity };
            yield return new object[] { float.NaN };
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Properties_TestData))]
        public void Width_Set_GetReturnsExpected(float width)
        {
            using (AdjustableArrowCap arrowCap = new AdjustableArrowCap(1, 1))
            {
                arrowCap.Width = width;
                Assert.Equal(width, arrowCap.Width);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Properties_TestData))]
        public void Height_Set_GetReturnsExpected(float height)
        {
            using (AdjustableArrowCap arrowCap = new AdjustableArrowCap(1, 1))
            {
                arrowCap.Height = height;
                Assert.Equal(height, arrowCap.Height);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [MemberData(nameof(Properties_TestData))]
        public void MiddleInset_Set_GetReturnsExpected(float middleInset)
        {
            using (AdjustableArrowCap arrowCap = new AdjustableArrowCap(1, 1))
            {
                arrowCap.MiddleInset = middleInset;
                Assert.Equal(middleInset, arrowCap.MiddleInset);
            }
        }

        [ConditionalTheory(Helpers.IsDrawingSupported)]
        [InlineData(true)]
        [InlineData(false)]
        public void Filled_Set_GetReturnsExpected(bool filled)
        {
            using (AdjustableArrowCap arrowCap = new AdjustableArrowCap(1, 1))
            {
                arrowCap.Filled = filled;
                Assert.Equal(filled, arrowCap.Filled);
            }
        }

        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void Clone_Success()
        {
            using (AdjustableArrowCap arrowCap = new AdjustableArrowCap(1, 1))
            using (AdjustableArrowCap clone = Assert.IsType<AdjustableArrowCap>(arrowCap.Clone()))
            {
                Assert.NotSame(clone, arrowCap);
                Assert.Equal(clone.Width, arrowCap.Width);
                Assert.Equal(clone.Height, arrowCap.Height);
                Assert.Equal(clone.MiddleInset, arrowCap.MiddleInset);
                Assert.Equal(clone.Filled, arrowCap.Filled);
            }
        }

        [ActiveIssue(20884, TestPlatforms.AnyUnix)]
        [ConditionalFact(Helpers.IsDrawingSupported)]
        public void BaseCap_ReturnsTriangle()
        {
            using (AdjustableArrowCap arrowCap = new AdjustableArrowCap(1, 1))
            {
                Assert.Equal(LineCap.Triangle, arrowCap.BaseCap);
            }
        }
    }
}

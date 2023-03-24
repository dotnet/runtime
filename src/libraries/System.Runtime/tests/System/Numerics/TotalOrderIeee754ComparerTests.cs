// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Runtime.Tests
{
    public sealed class TotalOrderIeee754ComparerTests
    {
        public static IEnumerable<object[]> SingleTestData
        {
            get
            {
                yield return new object[] { 0.0f, 0.0f, 0 };
                yield return new object[] { -0.0f, -0.0f, 0 };
                yield return new object[] { 0.0f, -0.0f, 1 };
                yield return new object[] { -0.0f, 0.0f, -1 };
                yield return new object[] { 0.0f, 1.0f, -1 };
                yield return new object[] { float.PositiveInfinity, 1.0f, 1 };
                yield return new object[] { BitConverter.UInt32BitsToSingle(0xFFC00000), float.NegativeInfinity, -1 };
                yield return new object[] { BitConverter.UInt32BitsToSingle(0xFFC00000), -1.0f, -1 };
                yield return new object[] { BitConverter.UInt32BitsToSingle(0x7FC00000), 1.0f, 1 };
                yield return new object[] { BitConverter.UInt32BitsToSingle(0x7FC00000), float.PositiveInfinity, 1 };
                yield return new object[] { float.NaN, float.NaN, 0 };
                yield return new object[] { BitConverter.UInt32BitsToSingle(0xFFC00000), BitConverter.UInt32BitsToSingle(0x7FC00000), -1 };
                yield return new object[] { BitConverter.UInt32BitsToSingle(0x7FC00000), BitConverter.UInt32BitsToSingle(0x7FC00001), -1 }; // implementation defined, not part of IEEE 754 totalOrder
            }
        }

        [Theory]
        [MemberData(nameof(SingleTestData))]
        public void TotalOrderTestSingle(float x, float y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<float>();
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }

        public static IEnumerable<object[]> DoubleTestData
        {
            get
            {
                yield return new object[] { 0.0, 0.0, 0 };
                yield return new object[] { -0.0, -0.0, 0 };
                yield return new object[] { 0.0, -0.0, 1 };
                yield return new object[] { -0.0, 0.0, -1 };
                yield return new object[] { 0.0, 1.0, -1 };
                yield return new object[] { double.PositiveInfinity, 1.0, 1 };
                yield return new object[] { BitConverter.UInt64BitsToDouble(0xFFF80000_00000000), double.NegativeInfinity, -1 };
                yield return new object[] { BitConverter.UInt64BitsToDouble(0xFFF80000_00000000), -1.0, -1 };
                yield return new object[] { BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), 1.0, 1 };
                yield return new object[] { BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), double.PositiveInfinity, 1 };
                yield return new object[] { double.NaN, double.NaN, 0 };
                yield return new object[] { BitConverter.UInt64BitsToDouble(0xFFF80000_00000000), BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), -1 };
                yield return new object[] { BitConverter.UInt64BitsToDouble(0x7FF80000_00000000), BitConverter.UInt64BitsToDouble(0x7FF80000_00000001), -1 }; // implementation defined, not part of IEEE 754 totalOrder
            }
        }

        [Theory]
        [MemberData(nameof(DoubleTestData))]
        public void TotalOrderTestDouble(double x, double y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<double>();
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }
        public static IEnumerable<object[]> HalfTestData
        {
            get
            {
                yield return new object[] { (Half)0.0, (Half)0.0, 0 };
                yield return new object[] { (Half)(-0.0), (Half)(-0.0), 0 };
                yield return new object[] { (Half)0.0, (Half)(-0.0), 1 };
                yield return new object[] { (Half)(-0.0), (Half)0.0, -1 };
                yield return new object[] { (Half)0.0, (Half)1.0, -1 };
                yield return new object[] { Half.PositiveInfinity, (Half)1.0, 1 };
                yield return new object[] { BitConverter.UInt16BitsToHalf(0xFE00), Half.NegativeInfinity, -1 };
                yield return new object[] { BitConverter.UInt16BitsToHalf(0xFE00), (Half)(-1.0), -1 };
                yield return new object[] { BitConverter.UInt16BitsToHalf(0x7E00), (Half)1.0, 1 };
                yield return new object[] { BitConverter.UInt16BitsToHalf(0x7E00), Half.PositiveInfinity, 1 };
                yield return new object[] { Half.NaN, Half.NaN, 0 };
                yield return new object[] { BitConverter.UInt16BitsToHalf(0xFE00), BitConverter.UInt16BitsToHalf(0x7E00), -1 };
                yield return new object[] { BitConverter.UInt16BitsToHalf(0x7E00), BitConverter.UInt16BitsToHalf(0x7E01), -1 }; // implementation defined, not part of IEEE 754 totalOrder
            }
        }

        [Theory]
        [MemberData(nameof(HalfTestData))]
        public void TotalOrderTestHalf(Half x, Half y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<Half>();
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }

        [Theory]
        [MemberData(nameof(SingleTestData))]
        public void TotalOrderTestNFloat(float x, float y, int result)
        {
            var comparer = new TotalOrderIeee754Comparer<NFloat>();
            Assert.Equal(result, Math.Sign(comparer.Compare(x, y)));
        }
    }
}

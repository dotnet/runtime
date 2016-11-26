// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class CSharpBinaryOperationTests
    {
        private static readonly int[] SomeInt32 = {0, 1, 2, -1, int.MinValue, int.MaxValue, int.MaxValue - 1};

        private static IEnumerable<object[]> CrossJoinInt32()
            => from i in SomeInt32 from j in SomeInt32 select new object[] {i, j};

        private static readonly double[] SomeDouble = {0.0, 1.0, 2.0, -1.0, double.PositiveInfinity, double.NaN};

        private static IEnumerable<object[]> CrossJoinDouble()
            => from i in SomeDouble from j in SomeDouble select new object[] {i, j};

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void AddInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x + y, dX + dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void AddOvfInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            int result;
            try
            {
                result = checked(x + y);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => checked(dX + dY));
                return;
            }

            Assert.Equal(result, checked(dX + dY));
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void AndInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x & y, dX & dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void DivideInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            if (y == 0)
                Assert.Throws<DivideByZeroException>(() => dX / dY);
            else if (y == -1 && x == int.MinValue)
                Assert.Throws<OverflowException>(() => dX / dY);
            else
                Assert.Equal(x / y, dX / dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void EqualInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x == y, dX == dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void ExclusiveOrInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x ^ y, dX ^ dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void GreaterThanInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x > y, dX > dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void GreaterThanOrEqualInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x >= y, dX >= dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void LeftShiftInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x << y, dX << dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void LessThanInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x < y, dX < dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void LessThanOrEqualInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x <= y, dX <= dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void ModuloInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            if (y == 0)
                Assert.Throws<DivideByZeroException>(() => dX % dY);
            else if (y == -1 && x == int.MinValue)
                Assert.Throws<OverflowException>(() => dX % dY);
            else
                Assert.Equal(x % y, dX % dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void MultiplyInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x * y, dX * dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void MultiplyOvfInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            int result;
            try
            {
                result = checked(x * y);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => checked(dX * dY));
                return;
            }

            Assert.Equal(result, dX * dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void NotEqualInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x != y, dX != dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void OrInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x | y, dX | dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void RightShiftInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x >> y, dX >> dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void SubtractInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x - y, dX - dY);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void SubtractOvfInt32(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            int result;
            try
            {
                result = checked(x - y);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => checked(dX - dY));
                return;
            }

            Assert.Equal(result, checked(dX - dY));
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void AddInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX += dY;
            Assert.Equal(x + y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void AddOvfInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            int result;
            try
            {
                result = checked(x + y);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => checked(dX += dY));
                return;
            }

            checked
            {
                dX += dY;
            }
            Assert.Equal(result, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void AndInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX &= dY;
            Assert.Equal(x & y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void DivideInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            if (y == 0)
                Assert.Throws<DivideByZeroException>(() => dX /= dY);
            else if (y == -1 && x == int.MinValue)
                Assert.Throws<OverflowException>(() => dX /= dY);
            else
            {
                dX /= dY;
                Assert.Equal(x / y, dX);
            }
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void ExclusiveOrInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX ^= dY;
            Assert.Equal(x ^ y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void LeftShiftInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX <<= dY;
            Assert.Equal(x << y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void ModuloInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            if (y == 0)
                Assert.Throws<DivideByZeroException>(() => dX %= dY);
            else if (y == -1 && x == int.MinValue)
                Assert.Throws<OverflowException>(() => dX %= dY);
            else
            {
                dX %= dY;
                Assert.Equal(x % y, dX);
            }
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void MultiplyInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX *= dY;
            Assert.Equal(x * y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void MultiplyOvfInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            int result;
            try
            {
                result = checked(x * y);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => checked(dX *= dY));
                return;
            }

            dX *= dY;
            Assert.Equal(result, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void OrInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX |= dY;
            Assert.Equal(x | y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void RightShiftInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX >>= dY;
            Assert.Equal(x >> y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void SubtractInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX -= dY;
            Assert.Equal(x - y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinInt32))]
        public void SubtractOvfInt32Assign(int x, int y)
        {
            dynamic dX = x;
            dynamic dY = y;
            int result;
            try
            {
                result = checked(x - y);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => checked(dX -= dY));
                return;
            }

            checked
            {
                dX -= dY;
            }
            Assert.Equal(result, dX);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void AddDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x + y, dX + dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void DivideDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x / y, dX / dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void EqualDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x == y, dX == dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void GreaterThanDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x > y, dX > dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void GreaterThanOrEqualDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x >= y, dX >= dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void LessThanDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x < y, dX < dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void LessThanOrEqualDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x <= y, dX <= dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void ModuloDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x % y, dX % dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void MultiplyDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x * y, dX * dY);
        }


        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void NotEqualDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x != y, dX != dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void SubtractDouble(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            Assert.Equal(x - y, dX - dY);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void AddDoubleAssign(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX += dY;
            Assert.Equal(x + y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void DivideDoubleAssign(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX /= dY;
            Assert.Equal(x / y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void ModuloDoubleAssign(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX %= dY;
            Assert.Equal(x % y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void MultiplyDoubleAssign(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX *= dY;
            Assert.Equal(x * y, dX);
        }

        [Theory, MemberData(nameof(CrossJoinDouble))]
        public void SubtractDoubleAssign(double x, double y)
        {
            dynamic dX = x;
            dynamic dY = y;
            dX -= dY;
            Assert.Equal(x - y, dX);
        }

        [Fact]
        public void InvalidOperationForType()
        {
            dynamic dX = "23";
            dynamic dY = "49";
            Assert.Throws<RuntimeBinderException>(() => dX * dY);
            dX = 23;
            dY = 49;
            Assert.Throws<RuntimeBinderException>(() => dX && dY);
        }
    }
}

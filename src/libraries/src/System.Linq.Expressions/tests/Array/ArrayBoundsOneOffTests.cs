// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Array
{
    public static unsafe class ArrayBoundsOneOffTests
    {
        [Fact]
        public static void CompileWithCastTest()
        {
            Expression<Func<object[]>> expr = () => (object[])new BaseClass[1];
            expr.Compile();
        }

        [Fact]
        public static void ToStringTest()
        {
            Expression<Func<int, object>> x = c => new double[c, c];
            Assert.Equal("c => new System.Double[,](c, c)", x.ToString());

            object y = x.Compile()(2);
            Assert.Equal("System.Double[,]", y.ToString());
        }

        [Fact]
        public static void ArrayBoundsVectorNegativeThrowsOverflowException()
        {
            Expression<Func<int, int[]>> e = a => new int[a];
            Func<int, int[]> f = e.Compile();

            Assert.Throws<OverflowException>(() => f(-1));
        }

        [Fact]
        public static void ArrayBoundsMultiDimensionalNegativeThrowsOverflowException()
        {
            Expression<Func<int, int, int[,]>> e = (a, b) => new int[a, b];
            Func<int, int, int[,]> f = e.Compile();

            Assert.Throws<OverflowException>(() => f(-1, 1));
            Assert.Throws<OverflowException>(() => f(1, -1));
        }
    }
}

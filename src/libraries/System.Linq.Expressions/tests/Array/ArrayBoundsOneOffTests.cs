// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class ArrayBoundsOneOffTests
    {
        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CompileWithCastTest(bool useInterpreter)
        {
            Expression<Func<object[]>> expr = () => (object[])new BaseClass[1];
            expr.Compile(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ToStringTest(bool useInterpreter)
        {
            Expression<Func<int, object>> x = c => new double[c, c];
            Assert.Equal("c => new System.Double[,](c, c)", x.ToString());

            object y = x.Compile(useInterpreter)(2);
            Assert.Equal("System.Double[,]", y.ToString());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ArrayBoundsVectorNegativeThrowsOverflowException(bool useInterpreter)
        {
            Expression<Func<int, int[]>> e = a => new int[a];
            Func<int, int[]> f = e.Compile(useInterpreter);

            Assert.Throws<OverflowException>(() => f(-1));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void ArrayBoundsMultiDimensionalNegativeThrowsOverflowException(bool useInterpreter)
        {
            Expression<Func<int, int, int[,]>> e = (a, b) => new int[a, b];
            Func<int, int, int[,]> f = e.Compile(useInterpreter);

            Assert.Throws<OverflowException>(() => f(-1, 1));
            Assert.Throws<OverflowException>(() => f(1, -1));
        }
    }
}

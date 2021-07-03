// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Tests
{
    public sealed class Int32GenericMathTests : GenericMathTests<int>
    {
        public override void AverageTest()
        {
            var values = Enumerable.Range(0, 32768);
            Assert.Equal(expected: 16383.5, actual: GenericMath.Average<int, double>(values));
        }

        public override void StandardDeviationTest()
        {
            var values = Enumerable.Range(0, 32768);
            Assert.Equal(expected: 9459.451146868934, actual: GenericMath.StandardDeviation<int, double>(values));
        }

        public override void SumTest()
        {
            var values = Enumerable.Range(0, 32768);
            Assert.Equal(expected: 536854528, actual: GenericMath.Sum<int, double>(values));
        }

        public override void SumInt32Test()
        {
            var values = Enumerable.Range(0, 32768);
            Assert.Equal(expected: 536854528, actual: GenericMath.Sum<int, int>(values));
        }
    }
}

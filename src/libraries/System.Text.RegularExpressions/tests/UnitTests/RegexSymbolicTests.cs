// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;
using System.Text.RegularExpressions.Symbolic;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexSymbolicTests
    {
        [Theory]
        [InlineData(UnicodeCategory.DecimalDigitNumber, 370)] //37 different kinds of decimal digits
        [InlineData(UnicodeCategory.Surrogate, 2048)]         //1024 low surrogates and 1024 high surrogates
        public void BDDCardinalityTests(UnicodeCategory category, uint expectedCardinality)
        {
            BDD digits = UnicodeCategoryConditions.GetCategory(category);
            Assert.Equal(expectedCardinality, ComputeCardinality(digits));
        }

        /// <summary>Returns how many characters this BDD represents.</summary>
        private static uint ComputeCardinality(BDD bdd)
        {

            if (bdd.IsEmpty)
            {
                return 0;
            }
            if (bdd.IsFull)
            {
                return ushort.MaxValue;
            }

            (uint Lower, uint Upper)[] ranges = BDDRangeConverter.ToRanges(bdd);
            uint result = 0;
            for (int i = 0; i < ranges.Length; i++)
            {
                result += ranges[i].Upper - ranges[i].Lower + 1;
            }
            return result;
        }
    }
}

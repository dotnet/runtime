// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Extensions.Logging.Test
{
    public class LogLevelEnumTest
    {
        [Fact]
        public static void EnumStartsAtZero()
        {
            Assert.Equal(0, GetEnumValues().Min());
        }

        [Fact]
        public static void EnumValuesAreUniqueAndConsecutive()
        {
            var values = GetEnumValues();
            values.Sort();
            Assert.Equal(new[] { 0, 1, 2, 3, 4, 5, 6 }, values);
        }

        private static List<int> GetEnumValues()
        {
            return Enum.GetValues(typeof(LogLevel)).Cast<int>().ToList();
        }
    }
}

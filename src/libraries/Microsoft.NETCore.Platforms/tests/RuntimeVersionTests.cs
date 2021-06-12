// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.NETCore.Platforms.BuildTasks.Tests
{
    public class RuntimeVersionTests
    {
        public enum Comparison
        {
            LessThan,
            Equal,
            GreaterThan
        }

        public static IEnumerable<object[]> ComparisonData()
        {
            yield return new object[] { "0.0", "00.0", Comparison.LessThan };
            yield return new object[] { "2.0", "1.0", Comparison.GreaterThan };
            yield return new object[] { "2", "1.0", Comparison.GreaterThan };
            yield return new object[] { "2", "1", Comparison.GreaterThan };
            yield return new object[] { "10", "10.0", Comparison.LessThan };
            yield return new object[] { "10", "10.00", Comparison.LessThan };
            yield return new object[] { "10.0", "10.0", Comparison.Equal };
            yield return new object[] { "10.0", null, Comparison.GreaterThan };
            yield return new object[] { "8", "8", Comparison.Equal };
        }

        [MemberData(nameof(ComparisonData))]
        [Theory]
        public static void CompareTo(string vs1, string vs2, Comparison expected)
        {
            RuntimeVersion v1 = new RuntimeVersion(vs1);
            RuntimeVersion v2 = vs2 == null ? null : new RuntimeVersion(vs2);
            int actual = v1.CompareTo(v2);
            int invActual = v2?.CompareTo(v1) ?? -1;

            switch (expected)
            {
                case Comparison.LessThan:
                    Assert.True(actual < 0);
                    Assert.True(invActual > 0);
                    break;
                case Comparison.Equal:
                    Assert.Equal(0, actual);
                    Assert.Equal(0, invActual);
                    break;
                case Comparison.GreaterThan:
                    Assert.True(actual > 0);
                    Assert.True(invActual < 0);
                    break;
            }
        }

        [MemberData(nameof(ComparisonData))]
        [Theory]
        public static void GreaterThan(string vs1, string vs2, Comparison expected)
        {
            RuntimeVersion v1 = new RuntimeVersion(vs1);
            RuntimeVersion v2 = vs2 == null ? null : new RuntimeVersion(vs2);
            bool actual = v1 > v2;
            bool invActual = v2 > v1;

            switch (expected)
            {
                case Comparison.LessThan:
                    Assert.False(actual);
                    Assert.True(invActual);
                    break;
                case Comparison.Equal:
                    Assert.False(actual);
                    Assert.False(invActual);
                    break;
                case Comparison.GreaterThan:
                    Assert.True(actual);
                    Assert.False(invActual);
                    break;
            }
        }

        [MemberData(nameof(ComparisonData))]
        [Theory]
        public static void GreaterThanOrEqual(string vs1, string vs2, Comparison expected)
        {
            RuntimeVersion v1 = new RuntimeVersion(vs1);
            RuntimeVersion v2 = vs2 == null ? null : new RuntimeVersion(vs2);
            bool actual = v1 >= v2;
            bool invActual = v2 >= v1;

            switch (expected)
            {
                case Comparison.LessThan:
                    Assert.False(actual);
                    Assert.True(invActual);
                    break;
                case Comparison.Equal:
                    Assert.True(actual);
                    Assert.True(invActual);
                    break;
                case Comparison.GreaterThan:
                    Assert.True(actual);
                    Assert.False(invActual);
                    break;
            }
        }

        [MemberData(nameof(ComparisonData))]
        [Theory]
        public static void LessThan(string vs1, string vs2, Comparison expected)
        {
            RuntimeVersion v1 = new RuntimeVersion(vs1);
            RuntimeVersion v2 = vs2 == null ? null : new RuntimeVersion(vs2);
            bool actual = v1 < v2;
            bool invActual = v2 < v1;

            switch (expected)
            {
                case Comparison.LessThan:
                    Assert.True(actual);
                    Assert.False(invActual);
                    break;
                case Comparison.Equal:
                    Assert.False(actual);
                    Assert.False(invActual);
                    break;
                case Comparison.GreaterThan:
                    Assert.False(actual);
                    Assert.True(invActual);
                    break;
            }
        }

        [MemberData(nameof(ComparisonData))]
        [Theory]
        public static void LessThanOrEqual(string vs1, string vs2, Comparison expected)
        {
            RuntimeVersion v1 = new RuntimeVersion(vs1);
            RuntimeVersion v2 = vs2 == null ? null : new RuntimeVersion(vs2);
            bool actual = v1 <= v2;
            bool invActual = v2 <= v1;

            switch (expected)
            {
                case Comparison.LessThan:
                    Assert.True(actual);
                    Assert.False(invActual);
                    break;
                case Comparison.Equal:
                    Assert.True(actual);
                    Assert.True(invActual);
                    break;
                case Comparison.GreaterThan:
                    Assert.False(actual);
                    Assert.True(invActual);
                    break;
            }
        }

        [MemberData(nameof(ComparisonData))]
        [Theory]
        public static void Equal(string vs1, string vs2, Comparison expected)
        {
            RuntimeVersion v1 = new RuntimeVersion(vs1);
            RuntimeVersion v2 = vs2 == null ? null : new RuntimeVersion(vs2);
            bool actual = v1 == v2;
            bool invActual = v2 == v1;

            switch (expected)
            {
                case Comparison.LessThan:
                    Assert.False(actual);
                    Assert.False(invActual);
                    break;
                case Comparison.Equal:
                    Assert.True(actual);
                    Assert.True(invActual);
                    break;
                case Comparison.GreaterThan:
                    Assert.False(actual);
                    Assert.False(invActual);
                    break;
            }
        }

        [MemberData(nameof(ComparisonData))]
        [Theory]
        public static void GetHashCodeUnique(string vs1, string vs2, Comparison expected)
        {
            RuntimeVersion v1 = new RuntimeVersion(vs1);
            RuntimeVersion v2 = vs2 == null ? null : new RuntimeVersion(vs2);
            int h1 = v1.GetHashCode();
            int h2 = v2?.GetHashCode() ?? 0;

            switch (expected)
            {
                case Comparison.LessThan:
                    Assert.NotEqual(h1, h2);
                    break;
                case Comparison.Equal:
                    Assert.Equal(h1, h2);
                    break;
                case Comparison.GreaterThan:
                    Assert.NotEqual(h1, h2);
                    break;
            }
        }
        public static IEnumerable<object[]> ValidVersions()
        {
            yield return new object[] { "0" };
            yield return new object[] { "00" };
            yield return new object[] { "000" };
            yield return new object[] { "1" };
            yield return new object[] { "1.0" };
            yield return new object[] { "1.1" };
            yield return new object[] { "1.01" };
            yield return new object[] { "1.2.3.4" };
            yield return new object[] { "1.02.03.04" };
        }


        [MemberData(nameof(ValidVersions))]
        [Theory]
        public static void RoundTripToString(string expected)
        {
            RuntimeVersion version = new RuntimeVersion(expected);
            string actual = version.ToString();
            Assert.Equal(expected, actual);
        }

    }
}

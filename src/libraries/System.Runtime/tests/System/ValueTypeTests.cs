// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Tests
{
    public static class ValueTypeTests
    {
        [Fact]
        public static void ToStringTest()
        {
            object obj = new S();
            Assert.Equal(obj.ToString(), obj.GetType().ToString());
            Assert.Equal("System.Tests.ValueTypeTests+S", obj.ToString());
        }

        [Fact]
        public static void StructWithDoubleFieldNotTightlyPackedZeroCompareTest()
        {
            StructWithDoubleFieldNotTightlyPacked obj1 = new StructWithDoubleFieldNotTightlyPacked();
            obj1.value1 = 1;
            obj1.value2 = 0.0;

            StructWithDoubleFieldNotTightlyPacked obj2 = new StructWithDoubleFieldNotTightlyPacked();
            obj2.value1 = 1;
            obj2.value2 = -0.0;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithDoubleFieldTightlyPackedZeroCompareTest()
        {
            StructWithDoubleFieldTightlyPacked obj1 = new StructWithDoubleFieldTightlyPacked();
            obj1.value1 = 1;
            obj1.value2 = 0.0;

            StructWithDoubleFieldTightlyPacked obj2 = new StructWithDoubleFieldTightlyPacked();
            obj2.value1 = 1;
            obj2.value2 = -0.0;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithDoubleFieldNotTightlyPackedNaNCompareTest()
        {
            StructWithDoubleFieldNotTightlyPacked obj1 = new StructWithDoubleFieldNotTightlyPacked();
            obj1.value1 = 1;
            obj1.value2 = double.NaN;

            StructWithDoubleFieldNotTightlyPacked obj2 = new StructWithDoubleFieldNotTightlyPacked();
            obj2.value1 = 1;
            obj2.value2 = -double.NaN;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithDoubleFieldTightlyPackedNaNCompareTest()
        {
            StructWithDoubleFieldTightlyPacked obj1 = new StructWithDoubleFieldTightlyPacked();
            obj1.value1 = 1;
            obj1.value2 = double.NaN;

            StructWithDoubleFieldTightlyPacked obj2 = new StructWithDoubleFieldTightlyPacked();
            obj2.value1 = 1;
            obj2.value2 = -double.NaN;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithNestedDoubleFieldNotTightlyPackedZeroCompareTest()
        {
            StructWithDoubleFieldNestedNotTightlyPacked obj1 = new StructWithDoubleFieldNestedNotTightlyPacked();
            obj1.value1.value1 = 1;
            obj1.value2.value2 = 0.0;

            StructWithDoubleFieldNestedNotTightlyPacked obj2 = new StructWithDoubleFieldNestedNotTightlyPacked();
            obj2.value1.value1 = 1;
            obj2.value2.value2 = -0.0;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithNestedDoubleFieldTightlyPackedZeroCompareTest()
        {
            StructWithDoubleFieldNestedTightlyPacked obj1 = new StructWithDoubleFieldNestedTightlyPacked();
            obj1.value1.value1 = 1;
            obj1.value2.value2 = 0.0;

            StructWithDoubleFieldNestedTightlyPacked obj2 = new StructWithDoubleFieldNestedTightlyPacked();
            obj2.value1.value1 = 1;
            obj2.value2.value2 = -0.0;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithNestedDoubleFieldNotTightlyPackedNaNCompareTest()
        {
            StructWithDoubleFieldNestedNotTightlyPacked obj1 = new StructWithDoubleFieldNestedNotTightlyPacked();
            obj1.value1.value1 = 1;
            obj1.value2.value2 = double.NaN;

            StructWithDoubleFieldNestedNotTightlyPacked obj2 = new StructWithDoubleFieldNestedNotTightlyPacked();
            obj2.value1.value1 = 1;
            obj2.value2.value2 = -double.NaN;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithNestedDoubleFieldTightlyPackedNaNCompareTest()
        {
            StructWithDoubleFieldNestedTightlyPacked obj1 = new StructWithDoubleFieldNestedTightlyPacked();
            obj1.value1.value1 = 1;
            obj1.value2.value2 = double.NaN;

            StructWithDoubleFieldNestedTightlyPacked obj2 = new StructWithDoubleFieldNestedTightlyPacked();
            obj2.value1.value1 = 1;
            obj2.value2.value2 = -double.NaN;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithFloatFieldNotTightlyPackedZeroCompareTest()
        {
            StructWithFloatFieldNotTightlyPacked obj1 = new StructWithFloatFieldNotTightlyPacked();
            obj1.value1 = 0.0f;
            obj1.value2 = 1;

            StructWithFloatFieldNotTightlyPacked obj2 = new StructWithFloatFieldNotTightlyPacked();
            obj2.value1 = -0.0f;
            obj2.value2 = 1;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithFloatFieldTightlyPackedZeroCompareTest()
        {
            StructWithFloatFieldTightlyPacked obj1 = new StructWithFloatFieldTightlyPacked();
            obj1.value1 = 0.0f;
            obj1.value2 = 1;

            StructWithFloatFieldTightlyPacked obj2 = new StructWithFloatFieldTightlyPacked();
            obj2.value1 = -0.0f;
            obj2.value2 = 1;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithFloatFieldNotTightlyPackedNaNCompareTest()
        {
            StructWithFloatFieldNotTightlyPacked obj1 = new StructWithFloatFieldNotTightlyPacked();
            obj1.value1 = float.NaN;
            obj1.value2 = 1;

            StructWithFloatFieldNotTightlyPacked obj2 = new StructWithFloatFieldNotTightlyPacked();
            obj2.value1 = -float.NaN;
            obj2.value2 = 1;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithFloatFieldTightlyPackedNaNCompareTest()
        {
            StructWithFloatFieldTightlyPacked obj1 = new StructWithFloatFieldTightlyPacked();
            obj1.value1 = float.NaN;
            obj1.value2 = 1;

            StructWithFloatFieldTightlyPacked obj2 = new StructWithFloatFieldTightlyPacked();
            obj2.value1 = -float.NaN;
            obj2.value2 = 1;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithNestedFloatFieldNotTightlyPackedZeroCompareTest()
        {
            StructWithFloatFieldNestedNotTightlyPacked obj1 = new StructWithFloatFieldNestedNotTightlyPacked();
            obj1.value1.value1 = 0.0f;
            obj1.value2.value2 = 1;

            StructWithFloatFieldNestedNotTightlyPacked obj2 = new StructWithFloatFieldNestedNotTightlyPacked();
            obj2.value1.value1 = -0.0f;
            obj2.value2.value2 = 1;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithNestedFloatFieldTightlyPackedZeroCompareTest()
        {
            StructWithFloatFieldNestedTightlyPacked obj1 = new StructWithFloatFieldNestedTightlyPacked();
            obj1.value1.value1 = 0.0f;
            obj1.value2.value2 = 1;

            StructWithFloatFieldNestedTightlyPacked obj2 = new StructWithFloatFieldNestedTightlyPacked();
            obj2.value1.value1 = -0.0f;
            obj2.value2.value2 = 1;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithNestedFloatFieldNotTightlyPackedNaNCompareTest()
        {
            StructWithFloatFieldNestedNotTightlyPacked obj1 = new StructWithFloatFieldNestedNotTightlyPacked();
            obj1.value1.value1 = float.NaN;
            obj1.value2.value2 = 1;

            StructWithFloatFieldNestedNotTightlyPacked obj2 = new StructWithFloatFieldNestedNotTightlyPacked();
            obj2.value1.value1 = -float.NaN;
            obj2.value2.value2 = 1;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithNestedFloatFieldTightlyPackedNaNCompareTest()
        {
            StructWithFloatFieldNestedTightlyPacked obj1 = new StructWithFloatFieldNestedTightlyPacked();
            obj1.value1.value1 = float.NaN;
            obj1.value2.value2 = 1;

            StructWithFloatFieldNestedTightlyPacked obj2 = new StructWithFloatFieldNestedTightlyPacked();
            obj2.value1.value1 = -float.NaN;
            obj2.value2.value2 = 1;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithoutNestedOverriddenEqualsCompareTest()
        {
            StructWithoutNestedOverriddenEqualsAndGetHashCode obj1 = new StructWithoutNestedOverriddenEqualsAndGetHashCode();
            obj1.value1.value = 1;
            obj1.value2.value = 2;

            StructWithoutNestedOverriddenEqualsAndGetHashCode obj2 = new StructWithoutNestedOverriddenEqualsAndGetHashCode();
            obj2.value1.value = 1;
            obj2.value2.value = 2;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructWithNestedOverriddenEqualsCompareTest()
        {
            StructWithNestedOverriddenEqualsAndGetHashCode obj1 = new StructWithNestedOverriddenEqualsAndGetHashCode();
            obj1.value1.value = 1;
            obj1.value2.value = 2;

            StructWithNestedOverriddenEqualsAndGetHashCode obj2 = new StructWithNestedOverriddenEqualsAndGetHashCode();
            obj2.value1.value = 1;
            obj2.value2.value = 2;

            Assert.False(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        [Fact]
        public static void StructContainsPointerCompareTest()
        {
            StructContainsPointer obj1 = new StructContainsPointer();
            obj1.value1 = 1;
            obj1.value2 = 0.0;

            StructContainsPointer obj2 = new StructContainsPointer();
            obj2.value1 = 1;
            obj2.value2 = -0.0;

            Assert.True(obj1.Equals(obj2));
            Assert.Equal(obj1.GetHashCode(), obj2.GetHashCode());
        }

        public struct S
        {
            public int x;
            public int y;
        }

        public struct StructWithDoubleFieldNotTightlyPacked
        {
            public int value1;
            public double value2;
        }

        public struct StructWithDoubleFieldTightlyPacked
        {
            public double value1;
            public double value2;
        }

        public struct StructWithDoubleFieldNestedNotTightlyPacked
        {
            public StructWithDoubleFieldNotTightlyPacked value1;
            public StructWithDoubleFieldNotTightlyPacked value2;
        }

        public struct StructWithDoubleFieldNestedTightlyPacked
        {
            public StructWithDoubleFieldTightlyPacked value1;
            public StructWithDoubleFieldTightlyPacked value2;
        }

        public struct StructWithFloatFieldNotTightlyPacked
        {
            public float value1;
            public long value2;
        }

        public struct StructWithFloatFieldTightlyPacked
        {
            public float value1;
            public float value2;
        }

        public struct StructWithFloatFieldNestedNotTightlyPacked
        {
            public StructWithFloatFieldNotTightlyPacked value1;
            public StructWithFloatFieldNotTightlyPacked value2;
        }

        public struct StructWithFloatFieldNestedTightlyPacked
        {
            public StructWithFloatFieldTightlyPacked value1;
            public StructWithFloatFieldTightlyPacked value2;
        }

        public struct StructNonOverriddenEqualsOrGetHasCode
        {
            public byte value;
        }

        public struct StructNeverEquals
        {
            public byte value;

            public override bool Equals(object obj)
            {
                return false;
            }

            public override int GetHashCode()
            {
                return 0;
            }
        }

        public struct StructWithoutNestedOverriddenEqualsAndGetHashCode
        {
            public StructNonOverriddenEqualsOrGetHasCode value1;
            public StructNonOverriddenEqualsOrGetHasCode value2;
        }

        public struct StructWithNestedOverriddenEqualsAndGetHashCode
        {
            public StructNeverEquals value1;
            public StructNeverEquals value2;
        }

        public struct StructContainsPointer
        {
            public string s;
            public double value1;
            public double value2;
        }
    }
}

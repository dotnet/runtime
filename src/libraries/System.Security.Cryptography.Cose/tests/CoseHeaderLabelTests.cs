// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Security.Cryptography.Cose.Tests
{
    public class CoseHeaderLabelTests
    {
        [Fact]
        public void CoseHeaderLabel_GetHashCode()
        {
            // First, construct a COSE header of (0) using several different methods.
            // They should all have the same hash code, though we don't know what
            // the hash code is.

            CoseHeaderLabel label1 = default;
            CoseHeaderLabel label2 = new CoseHeaderLabel();
            CoseHeaderLabel label3 = new CoseHeaderLabel(0);

            int label1HashCode = label1.GetHashCode();
            Assert.Equal(label1HashCode, label2.GetHashCode());
            Assert.Equal(label1HashCode, label3.GetHashCode());

            // Make sure the integer hash code calculation really is randomized.
            // Checking 1 & 2 together rather than independently cuts the false
            // positive rate down to nearly 1 in 2^64.

            bool isReturningNormalInt32HashCode =
                (new CoseHeaderLabel(1).GetHashCode() == 1)
                && (new CoseHeaderLabel(2).GetHashCode() == 2);
            Assert.False(isReturningNormalInt32HashCode);

            // Make sure the string hash code calculation really is randomized.
            // Checking 1 & 2 together rather than independently cuts the false
            // positive rate down to nearly 1 in 2^64.

            bool isReturningNormalStringHashCode =
                (new CoseHeaderLabel("Hello").GetHashCode() == "Hello".GetHashCode())
                && (new CoseHeaderLabel("World").GetHashCode() == "World".GetHashCode());
            Assert.Equal(PlatformDetection.IsNetCore, isReturningNormalStringHashCode);
        }

        [Fact]
        public void CoseHeaderLabel_Equals()
        {
            Assert.True(default(CoseHeaderLabel).Equals(default), "default(CoseHeaderLabel).Equals(default)");
            Assert.True(default(CoseHeaderLabel).Equals(new CoseHeaderLabel(0)), "default(CoseHeaderLabel).Equals(new CoseHeaderLabel(0))");
            Assert.True(new CoseHeaderLabel(1).Equals(new CoseHeaderLabel(1)), "new CoseHeaderLabel(1).Equals(new CoseHeaderLabel(1))");
            Assert.True(new CoseHeaderLabel("").Equals(new CoseHeaderLabel("")), "new CoseHeaderLabel(\"\").Equals(new CoseHeaderLabel(\"\"))");

            Assert.False(default(CoseHeaderLabel).Equals(new CoseHeaderLabel(1)), "default(CoseHeaderLabel).Equals(new CoseHeaderLabel(1))");
            Assert.False(default(CoseHeaderLabel).Equals(new CoseHeaderLabel("")), "default(CoseHeaderLabel).Equals(new CoseHeaderLabel(\"\"))");
            Assert.False(new CoseHeaderLabel(0).Equals(new CoseHeaderLabel(1)), "new CoseHeaderLabel(0).Equals(new CoseHeaderLabel(1))");
            Assert.False(new CoseHeaderLabel("foo").Equals(new CoseHeaderLabel("bar")), "new CoseHeaderLabel(\"foo\").Equals(new CoseHeaderLabel(\"bar\"))");
        }

        [Fact]
        public void CoseHeaderLabel_op_Equality()
        {
            Assert.True(default(CoseHeaderLabel) == default, "default(CoseHeaderLabel) == default");
            Assert.True(default(CoseHeaderLabel) == new CoseHeaderLabel(0), "default(CoseHeaderLabel) == new CoseHeaderLabel(0)");
            Assert.True(new CoseHeaderLabel(1) == new CoseHeaderLabel(1), "new CoseHeaderLabel(1) == new CoseHeaderLabel(1)");
            Assert.True(new CoseHeaderLabel("") == new CoseHeaderLabel(""), "new CoseHeaderLabel(\"\") == new CoseHeaderLabel(\"\")");

            Assert.False(default(CoseHeaderLabel) == new CoseHeaderLabel(1), "default(CoseHeaderLabel) == new CoseHeaderLabel(1)");
            Assert.False(default(CoseHeaderLabel) == new CoseHeaderLabel(""), "default(CoseHeaderLabel) == new CoseHeaderLabel(\"\")");
            Assert.False(new CoseHeaderLabel(0) == new CoseHeaderLabel(1), "new CoseHeaderLabel(0) == new CoseHeaderLabel(1)");
            Assert.False(new CoseHeaderLabel("foo") == new CoseHeaderLabel("bar"), "new CoseHeaderLabel(\"foo\") == new CoseHeaderLabel(\"bar\")");
        }

        [Fact]
        public void CoseHeaderLabel_op_Inequality()
        {
            Assert.False(default(CoseHeaderLabel) != default, "default(CoseHeaderLabel) != default");
            Assert.False(default(CoseHeaderLabel) != new CoseHeaderLabel(0), "default(CoseHeaderLabel) != new CoseHeaderLabel(0)");
            Assert.False(new CoseHeaderLabel(1) != new CoseHeaderLabel(1), "new CoseHeaderLabel(1) != new CoseHeaderLabel(1)");
            Assert.False(new CoseHeaderLabel("") != new CoseHeaderLabel(""), "new CoseHeaderLabel(\"\") != new CoseHeaderLabel(\"\")");

            Assert.True(default(CoseHeaderLabel) != new CoseHeaderLabel(1), "default(CoseHeaderLabel) != new CoseHeaderLabel(1)");
            Assert.True(default(CoseHeaderLabel) != new CoseHeaderLabel(""), "default(CoseHeaderLabel) != new CoseHeaderLabel(\"\")");
            Assert.True(new CoseHeaderLabel(0) != new CoseHeaderLabel(1), "new CoseHeaderLabel(0) != new CoseHeaderLabel(1)");
            Assert.True(new CoseHeaderLabel("foo") != new CoseHeaderLabel("bar"), "new CoseHeaderLabel(\"foo\") != new CoseHeaderLabel(\"bar\")");
        }
    }
}

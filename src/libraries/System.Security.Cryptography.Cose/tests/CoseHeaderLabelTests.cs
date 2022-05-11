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
            CoseHeaderLabel label = default;
            Assert.Equal(0, label.GetHashCode()); // There's no need to call GetHashCode on integers as they are their own hashcode.

            label = new CoseHeaderLabel();
            Assert.Equal(0, label.GetHashCode());

            label = new CoseHeaderLabel(0);
            Assert.Equal(0, label.GetHashCode());

            label = new CoseHeaderLabel("");
            Assert.Equal("".GetHashCode(), label.GetHashCode());

            label = new CoseHeaderLabel(1);
            Assert.Equal(1, label.GetHashCode());

            label = new CoseHeaderLabel("content-type");
            Assert.Equal("content-type".GetHashCode(), label.GetHashCode());
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

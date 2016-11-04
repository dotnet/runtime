// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.ComponentModel.Tests
{
    public sealed class CustomDefaultValueAttribute : DefaultValueAttribute
    {
        public CustomDefaultValueAttribute(object value) : base(value) { }

        public new void SetValue(object value) => base.SetValue(value);
    }

    public static class DefaultValueAttributeTestsNetStandard17
    {
        [Fact]
        public static void SetValue()
        {
            var attr = new CustomDefaultValueAttribute(null);

            attr.SetValue(true);
            Assert.Equal(true, attr.Value);

            attr.SetValue(false);
            Assert.Equal(false, attr.Value);

            attr.SetValue(12.8f);
            Assert.Equal(12.8f, attr.Value);

            attr.SetValue(12.8);
            Assert.Equal(12.8, attr.Value);

            attr.SetValue((byte)1);
            Assert.Equal((byte)1, attr.Value);

            attr.SetValue(28);
            Assert.Equal(28, attr.Value);

            attr.SetValue(TimeSpan.FromHours(1));
            Assert.Equal(TimeSpan.FromHours(1), attr.Value);

            attr.SetValue(null);
            Assert.Null(attr.Value);
        }
    }
}
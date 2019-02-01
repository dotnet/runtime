// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualBasic.CompilerServices;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public class VersionedTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData('a', false)]
        [InlineData(1, true)]
        [InlineData("12x", false)]
        [InlineData("123", true)]
        [InlineData('1', true)]
        [InlineData('a', false)]
        [InlineData("&O123", true)]
        [InlineData("&H123", true)]
        public void IsNumeric(object value, bool expected)
        {
            Assert.Equal(expected, Versioned.IsNumeric(value));
        }
        
        [Theory]
        [InlineData(null, null)]
        [InlineData("OBJECT", "System.Object")]
        [InlineData(" OBJECT ", "System.Object")]
        [InlineData("object", "System.Object")]
        [InlineData("custom", null)]
        public void SystemTypeName(string value, string expected)
        {
            Assert.Equal(expected, Versioned.SystemTypeName(value));
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("System.Object", "Object")]
        [InlineData("Object", "Object")]
        [InlineData(" object ", "Object")]
        [InlineData("custom", null)]
        public void VbTypeName(string value, string expected)
        {
            Assert.Equal(expected, Versioned.VbTypeName(value));
        }
    }
}

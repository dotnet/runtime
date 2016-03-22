// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Globalization.Tests
{
    public class NumberFormatInfoGetFormat
    {
        [Fact]
        public void GetFormat_NumberFormatInfo()
        {
            NumberFormatInfo format = new NumberFormatInfo();
            Assert.Same(format, format.GetFormat(typeof(NumberFormatInfo)));
        }

        [Fact]
        public void GetFormat_InvalidType()
        {
            NumberFormatInfo format = new NumberFormatInfo();
            Assert.Null(format.GetFormat(typeof(object)));
        }
    }
}

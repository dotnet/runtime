// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Principal;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class PageResultRequestControlTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var control = new PageResultRequestControl();
            Assert.True(control.IsCritical);
            Assert.Empty(control.Cookie);
            Assert.True(control.ServerSide);
            Assert.Equal(512, control.PageSize);
            Assert.Equal("1.2.840.113556.1.4.319", control.Type);
            
            Assert.Equal(new byte[] { 48, 132, 0, 0, 0, 6, 2, 2, 2, 0, 4, 0 }, control.GetValue());
        }

        [Theory]
        [InlineData(0, new byte[] { 48, 132, 0, 0, 0, 5, 2, 1, 0, 4, 0 })]
        [InlineData(10, new byte[] { 48, 132, 0, 0, 0, 5, 2, 1, 10, 4, 0 })]
        public void Ctor_PageSize(int pageSize, byte[] expectedValue)
        {
            var control = new PageResultRequestControl(pageSize);
            Assert.True(control.IsCritical);
            Assert.Empty(control.Cookie);
            Assert.True(control.ServerSide);
            Assert.Equal(pageSize, control.PageSize);
            Assert.Equal("1.2.840.113556.1.4.319", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }

        [Fact]
        public void Ctor_NegativePageSize_ThrowsArgumentException()
        {
            AssertExtensions.Throws<ArgumentException>("value", () => new PageResultRequestControl(-1));
        }

        [Theory]
        [InlineData(null, new byte[] { 48, 132, 0, 0, 0, 6, 2, 2, 2, 0, 4, 0 })]
        [InlineData(new byte[0], new byte[] { 48, 132, 0, 0, 0, 6, 2, 2, 2, 0, 4, 0 })]
        [InlineData(new byte[] { 1, 2, 3, }, new byte[] { 48, 132, 0, 0, 0, 9, 2, 2, 2, 0, 4, 3, 1, 2, 3 })]
        public void Ctor_Cookie(byte[] cookie, byte[] expectedValue)
        {
            var control = new PageResultRequestControl(cookie);
            Assert.True(control.IsCritical);
            Assert.NotSame(cookie, control.Cookie);
            Assert.Equal(cookie ?? Array.Empty<byte>(), control.Cookie);
            Assert.True(control.ServerSide);
            Assert.Equal(512, control.PageSize);
            Assert.Equal("1.2.840.113556.1.4.319", control.Type);

            Assert.Equal(expectedValue, control.GetValue());
        }

        [Fact]
        public void Cookie_Set_GetReturnsExpected()
        {
            var control = new PageResultRequestControl { Cookie = new byte[] { 1, 2, 3 } };
            Assert.Equal(new byte[] { 1, 2, 3 }, control.Cookie);
        }
    }
}

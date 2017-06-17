// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Xunit;

namespace System.DirectoryServices.Protocols.Tests
{
    public class SearchOptionsControlTests
    {
        [Fact]
        public void Ctor_Default()
        {
            var control = new SearchOptionsControl();
            Assert.True(control.IsCritical);
            Assert.Equal(SearchOption.DomainScope, control.SearchOption);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.1340", control.Type);
            
            Assert.Equal(new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 1 }, control.GetValue());
        }

        [Fact]
        public void Ctor_Flags()
        {
            var control = new SearchOptionsControl(SearchOption.PhantomRoot);
            Assert.True(control.IsCritical);
            Assert.Equal(SearchOption.PhantomRoot, control.SearchOption);
            Assert.True(control.ServerSide);
            Assert.Equal("1.2.840.113556.1.4.1340", control.Type);

            Assert.Equal(new byte[] { 48, 132, 0, 0, 0, 3, 2, 1, 2 }, control.GetValue());
        }

        [Theory]
        [InlineData(SearchOption.DomainScope - 1)]
        [InlineData(SearchOption.PhantomRoot + 1)]
        public void Ctor_InvalidFlags_ThrowsInvalidEnumArgumentException(SearchOption flag)
        {
            AssertExtensions.Throws<InvalidEnumArgumentException>("value", () => new SearchOptionsControl(flag));
        }
    }
}

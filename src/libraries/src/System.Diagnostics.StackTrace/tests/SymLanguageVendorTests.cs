// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.SymbolStore.Tests
{
    public class SymLanguageVendorTests
    {
        [Fact]
        public void Ctor_Default()
        {
            Assert.NotNull(new SymLanguageVendor());
        }

        [Fact]
        public void Microsoft_Get_ReturnsExpected()
        {
            Assert.Equal(new Guid("994b45c4-e6e9-11d2-903f-00c04fa302a1"), SymLanguageVendor.Microsoft);
        }
    }
}

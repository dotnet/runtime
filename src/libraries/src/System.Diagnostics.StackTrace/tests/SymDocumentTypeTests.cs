// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Diagnostics.SymbolStore.Tests
{
    public class SymDocumentTypeTests
    {
        [Fact]
        public void Ctor_Default()
        {
            Assert.NotNull(new SymDocumentType());
        }

        [Fact]
        public void Text_Get_ReturnsExpected()
        {
            Assert.Equal(new Guid("5a869d0b-6611-11d3-bd2a-0000f80849bd"), SymDocumentType.Text);
        }
    }
}
